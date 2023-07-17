// Copyright (c) Forged WoW LLC <https://github.Com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.Com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Cache;
using Forged.MapServer.Chrono;
using Forged.MapServer.DataStorage;
using Forged.MapServer.DungeonFinding;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Globals;
using Forged.MapServer.Networking;
using Forged.MapServer.Networking.Packets.LFG;
using Forged.MapServer.Server;
using Framework.Constants;
using Game.Common.Handlers;
using Serilog;
using System.Collections.Generic;
using System.Linq;

namespace Forged.MapServer.OpCodeHandlers;

public class LFGHandler : IWorldSessionHandler
{
    private readonly WorldSession _session;
    private readonly LFGManager _lfgManager;
    private readonly GameObjectManager _gameObjectManager;
	private readonly CharacterCache _characterCache;
    private readonly CliDB _cliDb;

	public LFGHandler(WorldSession session, LFGManager lfgManager, GameObjectManager gameObjectManager,
        CharacterCache characterCache, CliDB cliDb)
    {
        _session = session;
		_lfgManager = lfgManager;
		_gameObjectManager = gameObjectManager;
		_characterCache = characterCache;
		_cliDb = cliDb;
    }

    public void SendLfgPlayerLockInfo()
	{
		// Get Random dungeons that can be done at a certain level and expansion
		var level = _session.Player.Level;
		var contentTuningReplacementConditionMask = _session.Player.PlayerData.CtrOptions.Value.ContentTuningConditionMask;
		var randomDungeons = _lfgManager.GetRandomAndSeasonalDungeons(level, (uint)_session.Expansion, contentTuningReplacementConditionMask);

		LfgPlayerInfo lfgPlayerInfo = new();

		// Get player locked Dungeons
		foreach (var locked in _lfgManager.GetLockedDungeons(_session.Player.GUID))
			lfgPlayerInfo.BlackList.Slot.Add(new LFGBlackListSlot(locked.Key, (uint)locked.Value.LockStatus, locked.Value.RequiredItemLevel, (int)locked.Value.CurrentItemLevel, 0));

		foreach (var slot in randomDungeons)
		{
			var playerDungeonInfo = new LfgPlayerDungeonInfo();
			playerDungeonInfo.Slot = slot;
			playerDungeonInfo.CompletionQuantity = 1;
			playerDungeonInfo.CompletionLimit = 1;
			playerDungeonInfo.CompletionCurrencyID = 0;
			playerDungeonInfo.SpecificQuantity = 0;
			playerDungeonInfo.SpecificLimit = 1;
			playerDungeonInfo.OverallQuantity = 0;
			playerDungeonInfo.OverallLimit = 1;
			playerDungeonInfo.PurseWeeklyQuantity = 0;
			playerDungeonInfo.PurseWeeklyLimit = 0;
			playerDungeonInfo.PurseQuantity = 0;
			playerDungeonInfo.PurseLimit = 0;
			playerDungeonInfo.Quantity = 1;
			playerDungeonInfo.CompletedMask = 0;
			playerDungeonInfo.EncounterMask = 0;

			var reward = _lfgManager.GetRandomDungeonReward(slot, level);

			if (reward != null)
			{
				var quest = _gameObjectManager.GetQuestTemplate(reward.FirstQuest);

				if (quest != null)
				{
					playerDungeonInfo.FirstReward = !_session.Player.CanRewardQuest(quest, false);

					if (!playerDungeonInfo.FirstReward)
						quest = _gameObjectManager.GetQuestTemplate(reward.OtherQuest);

					if (quest != null)
					{
						playerDungeonInfo.Rewards.RewardMoney = _session.Player.GetQuestMoneyReward(quest);
						playerDungeonInfo.Rewards.RewardXP = _session.Player.GetQuestXPReward(quest);

						for (byte i = 0; i < SharedConst.QuestRewardItemCount; ++i)
						{
							var itemId = quest.RewardItemId[i];

							if (itemId != 0)
								playerDungeonInfo.Rewards.Item.Add(new LfgPlayerQuestRewardItem(itemId, quest.RewardItemCount[i]));
						}

						for (byte i = 0; i < SharedConst.QuestRewardCurrencyCount; ++i)
						{
							var curencyId = quest.RewardCurrencyId[i];

							if (curencyId != 0)
								playerDungeonInfo.Rewards.Currency.Add(new LfgPlayerQuestRewardCurrency(curencyId, quest.RewardCurrencyCount[i]));
						}
					}
				}
			}

			lfgPlayerInfo.Dungeons.Add(playerDungeonInfo);
		}

		_session.SendPacket(lfgPlayerInfo);
	}

	public void SendLfgPartyLockInfo()
	{
		var guid = _session.Player.GUID;
		var group = _session.Player.Group;

		if (group == null)
			return;

		LfgPartyInfo lfgPartyInfo = new();

		// Get the Locked dungeons of the other party members
		for (var refe = group.FirstMember; refe != null; refe = refe.Next())
		{
			var plrg = refe.Source;

			if (plrg == null)
				continue;

			var pguid = plrg.GUID;

			if (pguid == guid)
				continue;

			LFGBlackList lfgBlackList = new();
			lfgBlackList.PlayerGuid = pguid;

			foreach (var locked in _lfgManager.GetLockedDungeons(pguid))
				lfgBlackList.Slot.Add(new LFGBlackListSlot(locked.Key, (uint)locked.Value.LockStatus, locked.Value.RequiredItemLevel, (int)locked.Value.CurrentItemLevel, 0));

			lfgPartyInfo.Player.Add(lfgBlackList);
		}

		Log.Logger.Debug("SMSG_LFG_PARTY_INFO {0}", _session.GetPlayerInfo());
		_session.SendPacket(lfgPartyInfo);
	}

	public void SendLfgUpdateStatus(LfgUpdateData updateData, bool party)
	{
		var join = false;
		var queued = false;

		switch (updateData.UpdateType)
		{
			case LfgUpdateType.JoinQueueInitial: // Joined queue outside the dungeon
				join = true;

				break;
			case LfgUpdateType.JoinQueue:
			case LfgUpdateType.AddedToQueue: // Rolecheck Success
				join = true;
				queued = true;

				break;
			case LfgUpdateType.ProposalBegin:
				join = true;

				break;
			case LfgUpdateType.UpdateStatus:
				join = updateData.State != LfgState.Rolecheck && updateData.State != LfgState.None;
				queued = updateData.State == LfgState.Queued;

				break;
			
		}

		LFGUpdateStatus lfgUpdateStatus = new();

		var ticket = _lfgManager.GetTicket(_session.Player.GUID);

		if (ticket != null)
			lfgUpdateStatus.Ticket = ticket;

		lfgUpdateStatus.SubType = (byte)LfgQueueType.Dungeon; // other types not implemented
		lfgUpdateStatus.Reason = (byte)updateData.UpdateType;

		foreach (var dungeonId in updateData.Dungeons)
			lfgUpdateStatus.Slots.Add(_lfgManager.GetLFGDungeonEntry(dungeonId));

		lfgUpdateStatus.RequestedRoles = (uint)_lfgManager.GetRoles(_session.Player.GUID);
		//lfgUpdateStatus.SuspendedPlayers;
		lfgUpdateStatus.IsParty = party;
		lfgUpdateStatus.NotifyUI = true;
		lfgUpdateStatus.Joined = join;
		lfgUpdateStatus.LfgJoined = updateData.UpdateType != LfgUpdateType.RemovedFromQueue;
		lfgUpdateStatus.Queued = queued;
		lfgUpdateStatus.QueueMapID = _lfgManager.GetDungeonMapId(_session.Player.GUID);

		_session.SendPacket(lfgUpdateStatus);
	}

	public void SendLfgRoleChosen(ObjectGuid guid, LfgRoles roles)
	{
		RoleChosen roleChosen = new();
		roleChosen.Player = guid;
		roleChosen.RoleMask = roles;
		roleChosen.Accepted = roles > 0;
		_session.SendPacket(roleChosen);
	}

	public void SendLfgRoleCheckUpdate(LfgRoleCheck roleCheck)
	{
		List<uint> dungeons = new();

		if (roleCheck.RDungeonId != 0)
			dungeons.Add(roleCheck.RDungeonId);
		else
			dungeons = roleCheck.Dungeons;

		Log.Logger.Debug("SMSG_LFG_ROLE_CHECK_UPDATE {0}", _session.GetPlayerInfo());

		LFGRoleCheckUpdate lfgRoleCheckUpdate = new();
		lfgRoleCheckUpdate.PartyIndex = 127;
		lfgRoleCheckUpdate.RoleCheckStatus = (byte)roleCheck.State;
		lfgRoleCheckUpdate.IsBeginning = roleCheck.State == LfgRoleCheckState.Initialiting;

		foreach (var dungeonId in dungeons)
			lfgRoleCheckUpdate.JoinSlots.Add(_lfgManager.GetLFGDungeonEntry(dungeonId));

		lfgRoleCheckUpdate.GroupFinderActivityID = 0;

		if (!roleCheck.Roles.Empty())
		{
			// Leader info MUST be sent 1st :S
			var roles = (byte)roleCheck.Roles.Find(roleCheck.Leader).Value;
			lfgRoleCheckUpdate.Members.Add(new LFGRoleCheckUpdateMember(roleCheck.Leader, roles, _characterCache.GetCharacterCacheByGuid(roleCheck.Leader).Level, roles > 0));

			foreach (var it in roleCheck.Roles)
			{
				if (it.Key == roleCheck.Leader)
					continue;

				roles = (byte)it.Value;
				lfgRoleCheckUpdate.Members.Add(new LFGRoleCheckUpdateMember(it.Key, roles, _characterCache.GetCharacterCacheByGuid(it.Key).Level, roles > 0));
			}
		}

		_session.SendPacket(lfgRoleCheckUpdate);
	}

	public void SendLfgJoinResult(LfgJoinResultData joinData)
	{
		LFGJoinResult lfgJoinResult = new();

		var ticket = _lfgManager.GetTicket(_session.Player.GUID);

		if (ticket != null)
			lfgJoinResult.Ticket = ticket;

		lfgJoinResult.Result = (byte)joinData.Result;

		if (joinData.Result == LfgJoinResult.RoleCheckFailed)
			lfgJoinResult.ResultDetail = (byte)joinData.State;
		else if (joinData.Result == LfgJoinResult.NoSlots)
			lfgJoinResult.BlackListNames = joinData.PlayersMissingRequirement;

		foreach (var it in joinData.Lockmap)
		{
			var blackList = new LFGBlackListPkt();
			blackList.PlayerGuid = it.Key;

			foreach (var lockInfo in it.Value)
			{
				Log.Logger.Debug("SendLfgJoinResult:: {0} DungeonID: {1} Lock status: {2} Required itemLevel: {3} Current itemLevel: {4}",
							it.Key.ToString(),
							(lockInfo.Key & 0x00FFFFFF),
							lockInfo.Value.LockStatus,
							lockInfo.Value.RequiredItemLevel,
							lockInfo.Value.CurrentItemLevel);

				blackList.Slot.Add(new LFGBlackListSlot(lockInfo.Key, (uint)lockInfo.Value.LockStatus, lockInfo.Value.RequiredItemLevel, (int)lockInfo.Value.CurrentItemLevel, 0));
			}

			lfgJoinResult.BlackList.Add(blackList);
		}

		_session.SendPacket(lfgJoinResult);
	}

	public void SendLfgQueueStatus(LfgQueueStatusData queueData)
	{
        Log.Logger.Debug("SMSG_LFG_QUEUE_STATUS {0} state: {1} dungeon: {2}, waitTime: {3}, " +
                         "avgWaitTime: {4}, waitTimeTanks: {5}, waitTimeHealer: {6}, waitTimeDps: {7}, queuedTime: {8}, tanks: {9}, healers: {10}, dps: {11}",
					_session.GetPlayerInfo(),
					_lfgManager.GetState(_session.Player.GUID),
					queueData.DungeonId,
					queueData.WaitTime,
					queueData.WaitTimeAvg,
					queueData.WaitTimeTank,
					queueData.WaitTimeHealer,
					queueData.WaitTimeDps,
					queueData.QueuedTime,
					queueData.Tanks,
					queueData.Healers,
					queueData.Dps);

		LFGQueueStatus lfgQueueStatus = new();

		var ticket = _lfgManager.GetTicket(_session.Player.GUID);

		if (ticket != null)
			lfgQueueStatus.Ticket = ticket;

		lfgQueueStatus.Slot = queueData.QueueId;
		lfgQueueStatus.AvgWaitTimeMe = (uint)queueData.WaitTime;
		lfgQueueStatus.AvgWaitTime = (uint)queueData.WaitTimeAvg;
		lfgQueueStatus.AvgWaitTimeByRole[0] = (uint)queueData.WaitTimeTank;
		lfgQueueStatus.AvgWaitTimeByRole[1] = (uint)queueData.WaitTimeHealer;
		lfgQueueStatus.AvgWaitTimeByRole[2] = (uint)queueData.WaitTimeDps;
		lfgQueueStatus.LastNeeded[0] = queueData.Tanks;
		lfgQueueStatus.LastNeeded[1] = queueData.Healers;
		lfgQueueStatus.LastNeeded[2] = queueData.Dps;
		lfgQueueStatus.QueuedTime = queueData.QueuedTime;

		_session.SendPacket(lfgQueueStatus);
	}

	public void SendLfgPlayerReward(LfgPlayerRewardData rewardData)
	{
		if (rewardData.RdungeonEntry == 0 || rewardData.SdungeonEntry == 0 || rewardData.Quest == null)
			return;

        Log.Logger.Debug("SMSG_LFG_PLAYER_REWARD {0} rdungeonEntry: {1}, sdungeonEntry: {2}, done: {3}",
					_session.GetPlayerInfo(),
					rewardData.RdungeonEntry,
					rewardData.SdungeonEntry,
					rewardData.Done);

		LFGPlayerReward lfgPlayerReward = new();
		lfgPlayerReward.QueuedSlot = rewardData.RdungeonEntry;
		lfgPlayerReward.ActualSlot = rewardData.SdungeonEntry;
		lfgPlayerReward.RewardMoney = _session.Player.GetQuestMoneyReward(rewardData.Quest);
		lfgPlayerReward.AddedXP = _session.Player.GetQuestXPReward(rewardData.Quest);

		for (byte i = 0; i < SharedConst.QuestRewardItemCount; ++i)
		{
			var itemId = rewardData.Quest.RewardItemId[i];

			if (itemId != 0)
				lfgPlayerReward.Rewards.Add(new LFGPlayerRewards(itemId, rewardData.Quest.RewardItemCount[i], 0, false));
		}

		for (byte i = 0; i < SharedConst.QuestRewardCurrencyCount; ++i)
		{
			var currencyId = rewardData.Quest.RewardCurrencyId[i];

			if (currencyId != 0)
				lfgPlayerReward.Rewards.Add(new LFGPlayerRewards(currencyId, rewardData.Quest.RewardCurrencyCount[i], 0, true));
		}

		_session.SendPacket(lfgPlayerReward);
	}

	public void SendLfgBootProposalUpdate(LfgPlayerBoot boot)
	{
		var playerVote = boot.Votes.LookupByKey(_session.Player.GUID);
		byte votesNum = 0;
		byte agreeNum = 0;
		var secsleft = (uint)((boot.CancelTime - GameTime.CurrentTime) / 1000);

		foreach (var it in boot.Votes)
			if (it.Value != LfgAnswer.Pending)
			{
				++votesNum;

				if (it.Value == LfgAnswer.Agree)
					++agreeNum;
			}

        Log.Logger.Debug("SMSG_LFG_BOOT_PROPOSAL_UPDATE {0} inProgress: {1} - didVote: {2} - agree: {3} - victim: {4} votes: {5} - agrees: {6} - left: {7} - needed: {8} - reason {9}",
					_session.GetPlayerInfo(),
					boot.InProgress,
					playerVote != LfgAnswer.Pending,
					playerVote == LfgAnswer.Agree,
					boot.Victim.ToString(),
					votesNum,
					agreeNum,
					secsleft,
					SharedConst.LFGKickVotesNeeded,
					boot.Reason);

		LfgBootPlayer lfgBootPlayer = new();
		lfgBootPlayer.Info.VoteInProgress = boot.InProgress;                        // Vote in progress
		lfgBootPlayer.Info.VotePassed = agreeNum >= SharedConst.LFGKickVotesNeeded; // Did succeed
		lfgBootPlayer.Info.MyVoteCompleted = playerVote != LfgAnswer.Pending;       // Did Vote
		lfgBootPlayer.Info.MyVote = playerVote == LfgAnswer.Agree;                  // Agree
		lfgBootPlayer.Info.Target = boot.Victim;                                    // Victim GUID
		lfgBootPlayer.Info.TotalVotes = votesNum;                                   // Total Votes
		lfgBootPlayer.Info.BootVotes = agreeNum;                                    // Agree Count
		lfgBootPlayer.Info.TimeLeft = secsleft;                                     // Time Left
		lfgBootPlayer.Info.VotesNeeded = SharedConst.LFGKickVotesNeeded;            // Needed Votes
		lfgBootPlayer.Info.Reason = boot.Reason;                                    // Kick reason
		_session.SendPacket(lfgBootPlayer);
	}

	public void SendLfgProposalUpdate(LfgProposal proposal)
	{
		var playerGuid = _session.Player.GUID;
		var guildGuid = proposal.Players.LookupByKey(playerGuid).Group;
		var silent = !proposal.IsNew && guildGuid == proposal.Group;
		var dungeonEntry = proposal.DungeonId;

		Log.Logger.Debug("SMSG_LFG_PROPOSAL_UPDATE {0} state: {1}", _session.GetPlayerInfo(), proposal.State);

		// show random dungeon if player selected random dungeon and it's not lfg group
		if (!silent)
		{
			var playerDungeons = _lfgManager.GetSelectedDungeons(playerGuid);

			if (!playerDungeons.Contains(proposal.DungeonId))
				dungeonEntry = playerDungeons.First();
		}

		LFGProposalUpdate lfgProposalUpdate = new();

		var ticket = _lfgManager.GetTicket(_session.Player.GUID);

		if (ticket != null)
			lfgProposalUpdate.Ticket = ticket;

		lfgProposalUpdate.InstanceID = 0;
		lfgProposalUpdate.ProposalID = proposal.ID;
		lfgProposalUpdate.Slot = _lfgManager.GetLFGDungeonEntry(dungeonEntry);
		lfgProposalUpdate.State = (byte)proposal.State;
		lfgProposalUpdate.CompletedMask = proposal.Encounters;
		lfgProposalUpdate.ValidCompletedMask = true;
		lfgProposalUpdate.ProposalSilent = silent;
		lfgProposalUpdate.IsRequeue = !proposal.IsNew;

		foreach (var pair in proposal.Players)
		{
			var proposalPlayer = new LFGProposalUpdatePlayer();
			proposalPlayer.Roles = (uint)pair.Value.Role;
			proposalPlayer.Me = (pair.Key == playerGuid);
			proposalPlayer.MyParty = !pair.Value.Group.IsEmpty && pair.Value.Group == proposal.Group;
			proposalPlayer.SameParty = !pair.Value.Group.IsEmpty && pair.Value.Group == guildGuid;
			proposalPlayer.Responded = (pair.Value.Accept != LfgAnswer.Pending);
			proposalPlayer.Accepted = (pair.Value.Accept == LfgAnswer.Agree);

			lfgProposalUpdate.Players.Add(proposalPlayer);
		}

		_session.SendPacket(lfgProposalUpdate);
	}

	public void SendLfgDisabled()
	{
		_session.SendPacket(new LfgDisabled());
	}

	public void SendLfgOfferContinue(uint dungeonEntry)
	{
        Log.Logger.Debug("SMSG_LFG_OFFER_CONTINUE {0} dungeon entry: {1}", _session.GetPlayerInfo(), dungeonEntry);
		_session.SendPacket(new LfgOfferContinue(_lfgManager.GetLFGDungeonEntry(dungeonEntry)));
	}

	public void SendLfgTeleportError(LfgTeleportResult err)
	{
		Log.Logger.Debug("SMSG_LFG_TELEPORT_DENIED {0} reason: {1}", _session.GetPlayerInfo(), err);
		_session.SendPacket(new LfgTeleportDenied(err));
	}

	[WorldPacketHandler(ClientOpcodes.DfJoin)]
	void HandleLfgJoin(DFJoin dfJoin)
	{
		if (!_lfgManager.IsOptionEnabled(LfgOptions.EnableDungeonFinder | LfgOptions.EnableRaidBrowser) ||
			(_session.Player.Group != null &&
			_session.Player.Group.LeaderGUID != _session.Player.GUID &&
			(_session.Player.Group.MembersCount == MapConst.MaxGroupSize || !_session.Player.Group.IsLFGGroup)))
			return;

		if (dfJoin.Slots.Empty())
		{
			Log.Logger.Debug("CMSG_DF_JOIN {0} no dungeons selected", _session.GetPlayerInfo());

			return;
		}

		List<uint> newDungeons = new();

		foreach (var slot in dfJoin.Slots)
		{
			var dungeon = slot & 0x00FFFFFF;

			if (_cliDb.LFGDungeonsStorage.ContainsKey(dungeon))
				newDungeons.Add(dungeon);
		}

		Log.Logger.Debug("CMSG_DF_JOIN {0} roles: {1}, Dungeons: {2}", _session.GetPlayerInfo(), dfJoin.Roles, newDungeons.Count);

		_lfgManager.JoinLfg(_session.Player, dfJoin.Roles, newDungeons);
	}

	[WorldPacketHandler(ClientOpcodes.DfLeave)]
	void HandleLfgLeave(DFLeave dfLeave)
	{
		var group = _session.Player.Group;

		Log.Logger.Debug("CMSG_DF_LEAVE {0} in group: {1} sent guid {2}.", _session.GetPlayerInfo(), group != null ? 1 : 0, dfLeave.Ticket.RequesterGuid.ToString());

		// Check cheating - only leader can leave the queue
		if (group == null || group.LeaderGUID == dfLeave.Ticket.RequesterGuid)
			_lfgManager.LeaveLfg(dfLeave.Ticket.RequesterGuid);
	}

	[WorldPacketHandler(ClientOpcodes.DfProposalResponse)]
	void HandleLfgProposalResult(DFProposalResponse dfProposalResponse)
	{
		Log.Logger.Debug("CMSG_LFG_PROPOSAL_RESULT {0} proposal: {1} accept: {2}", _session.GetPlayerInfo(), dfProposalResponse.ProposalID, dfProposalResponse.Accepted ? 1 : 0);
		_lfgManager.UpdateProposal(dfProposalResponse.ProposalID, _session.Player.GUID, dfProposalResponse.Accepted);
	}

	[WorldPacketHandler(ClientOpcodes.DfSetRoles)]
	void HandleLfgSetRoles(DFSetRoles dfSetRoles)
	{
		var guid = _session.Player.GUID;
		var group = _session.Player.Group;

		if (group == null)
		{
			Log.Logger.Debug("CMSG_DF_SET_ROLES {0} Not in group",
						_session.GetPlayerInfo());

			return;
		}

		var gguid = group.GUID;
		Log.Logger.Debug("CMSG_DF_SET_ROLES: Group {0}, _session.Player {1}, Roles: {2}", gguid.ToString(), _session.GetPlayerInfo(), dfSetRoles.RolesDesired);
		_lfgManager.UpdateRoleCheck(gguid, guid, dfSetRoles.RolesDesired);
	}

	[WorldPacketHandler(ClientOpcodes.DfBootPlayerVote)]
	void HandleLfgSetBootVote(DFBootPlayerVote dfBootPlayerVote)
	{
		var guid = _session.Player.GUID;
		Log.Logger.Debug("CMSG_LFG_SET_BOOT_VOTE {0} agree: {1}", _session.GetPlayerInfo(), dfBootPlayerVote.Vote ? 1 : 0);
		_lfgManager.UpdateBoot(guid, dfBootPlayerVote.Vote);
	}

	[WorldPacketHandler(ClientOpcodes.DfTeleport)]
	void HandleLfgTeleport(DFTeleport dfTeleport)
	{
		Log.Logger.Debug("CMSG_DF_TELEPORT {0} out: {1}", _session.GetPlayerInfo(), dfTeleport.TeleportOut ? 1 : 0);
		_lfgManager.TeleportPlayer(_session.Player, dfTeleport.TeleportOut, true);
	}

	[WorldPacketHandler(ClientOpcodes.DfGetSystemInfo, Processing = PacketProcessing.ThreadSafe)]
	void HandleDfGetSystemInfo(DFGetSystemInfo dfGetSystemInfo)
	{
		Log.Logger.Debug("CMSG_LFG_Lock_INFO_REQUEST {0} for {1}", _session.GetPlayerInfo(), (dfGetSystemInfo.Player ? "player" : "party"));

		if (dfGetSystemInfo.Player)
			SendLfgPlayerLockInfo();
		else
			SendLfgPartyLockInfo();
	}

	[WorldPacketHandler(ClientOpcodes.DfGetJoinStatus, Processing = PacketProcessing.ThreadSafe)]
	void HandleDfGetJoinStatus(DFGetJoinStatus packet)
	{
		if (!_session.Player.IsUsingLfg)
			return;

		var guid = _session.Player.GUID;
		var updateData = _lfgManager.GetLfgStatus(guid);

		if (_session.Player.Group != null)
		{
			SendLfgUpdateStatus(updateData, true);
			updateData.Dungeons.Clear();
			SendLfgUpdateStatus(updateData, false);
		}
		else
		{
			SendLfgUpdateStatus(updateData, false);
			updateData.Dungeons.Clear();
			SendLfgUpdateStatus(updateData, true);
		}
	}
}