// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using System.Linq;
using Framework.Constants;
using Forged.RealmServer.DungeonFinding;
using Forged.RealmServer.Entities;
using Forged.RealmServer.Networking;
using Game.Common.Handlers;
using Forged.RealmServer.Networking.Packets;
using Serilog;

namespace Forged.RealmServer;

public class LFGHandler : IWorldSessionHandler
{
    private readonly WorldSession _session;
    private readonly GameTime _gameTime;
    private readonly LFGManager _lFGManager;
    private readonly CliDB _cliDB;

    public LFGHandler(WorldSession session, GameTime gameTime, LFGManager lFGManager, CliDB cliDB)
    {
        _session = session;
        _gameTime = gameTime;
        _lFGManager = lFGManager;
        _cliDB = cliDB;
    }

    public void SendLfgPlayerLockInfo()
	{
		// Get Random dungeons that can be done at a certain level and expansion
		var level = _session.Player.Level;
		var contentTuningReplacementConditionMask = _session.Player.PlayerData.CtrOptions.GetValue().ContentTuningConditionMask;
		var randomDungeons = _lFGManager.GetRandomAndSeasonalDungeons(level, (uint)_session.Expansion, contentTuningReplacementConditionMask);

		LfgPlayerInfo lfgPlayerInfo = new();

		// Get player locked Dungeons
		foreach (var locked in _lFGManager.GetLockedDungeons(_session.Player.GUID))
			lfgPlayerInfo.BlackList.Slot.Add(new LFGBlackListSlot(locked.Key, (uint)locked.Value.lockStatus, locked.Value.requiredItemLevel, (int)locked.Value.currentItemLevel, 0));

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

			var reward = _lFGManager.GetRandomDungeonReward(slot, level);

			if (reward != null)
			{
				var quest = Global.ObjectMgr.GetQuestTemplate(reward.firstQuest);

				if (quest != null)
				{
					playerDungeonInfo.FirstReward = !_session.Player.CanRewardQuest(quest, false);

					if (!playerDungeonInfo.FirstReward)
						quest = Global.ObjectMgr.GetQuestTemplate(reward.otherQuest);

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

		if (!group)
			return;

		LfgPartyInfo lfgPartyInfo = new();

		// Get the Locked dungeons of the other party members
		for (var refe = group.FirstMember; refe != null; refe = refe.Next())
		{
			var plrg = refe.Source;

			if (!plrg)
				continue;

			var pguid = plrg.GUID;

			if (pguid == guid)
				continue;

			LFGBlackList lfgBlackList = new();
			lfgBlackList.PlayerGuid = pguid;

			foreach (var locked in _lFGManager.GetLockedDungeons(pguid))
				lfgBlackList.Slot.Add(new LFGBlackListSlot(locked.Key, (uint)locked.Value.lockStatus, locked.Value.requiredItemLevel, (int)locked.Value.currentItemLevel, 0));

			lfgPartyInfo.Player.Add(lfgBlackList);
		}

		Log.Logger.Debug("SMSG_LFG_PARTY_INFO {0}", _session.GetPlayerInfo());
		_session.SendPacket(lfgPartyInfo);
	}

	public void SendLfgUpdateStatus(LfgUpdateData updateData, bool party)
	{
		var join = false;
		var queued = false;

		switch (updateData.updateType)
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
				join = updateData.state != LfgState.Rolecheck && updateData.state != LfgState.None;
				queued = updateData.state == LfgState.Queued;

				break;
			default:
				break;
		}

		LFGUpdateStatus lfgUpdateStatus = new();

		var ticket = _lFGManager.GetTicket(_session.Player.GUID);

		if (ticket != null)
			lfgUpdateStatus.Ticket = ticket;

		lfgUpdateStatus.SubType = (byte)LfgQueueType.Dungeon; // other types not implemented
		lfgUpdateStatus.Reason = (byte)updateData.updateType;

		foreach (var dungeonId in updateData.dungeons)
			lfgUpdateStatus.Slots.Add(_lFGManager.GetLFGDungeonEntry(dungeonId));

		lfgUpdateStatus.RequestedRoles = (uint)_lFGManager.GetRoles(_session.Player.GUID);
		//lfgUpdateStatus.SuspendedPlayers;
		lfgUpdateStatus.IsParty = party;
		lfgUpdateStatus.NotifyUI = true;
		lfgUpdateStatus.Joined = join;
		lfgUpdateStatus.LfgJoined = updateData.updateType != LfgUpdateType.RemovedFromQueue;
		lfgUpdateStatus.Queued = queued;
		lfgUpdateStatus.QueueMapID = _lFGManager.GetDungeonMapId(_session.Player.GUID);

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

		if (roleCheck.rDungeonId != 0)
			dungeons.Add(roleCheck.rDungeonId);
		else
			dungeons = roleCheck.dungeons;

		Log.Logger.Debug("SMSG_LFG_ROLE_CHECK_UPDATE {0}", _session.GetPlayerInfo());

		LFGRoleCheckUpdate lfgRoleCheckUpdate = new();
		lfgRoleCheckUpdate.PartyIndex = 127;
		lfgRoleCheckUpdate.RoleCheckStatus = (byte)roleCheck.state;
		lfgRoleCheckUpdate.IsBeginning = roleCheck.state == LfgRoleCheckState.Initialiting;

		foreach (var dungeonId in dungeons)
			lfgRoleCheckUpdate.JoinSlots.Add(_lFGManager.GetLFGDungeonEntry(dungeonId));

		lfgRoleCheckUpdate.GroupFinderActivityID = 0;

		if (!roleCheck.roles.Empty())
		{
			// Leader info MUST be sent 1st :S
			var roles = (byte)roleCheck.roles.Find(roleCheck.leader).Value;
			lfgRoleCheckUpdate.Members.Add(new LFGRoleCheckUpdateMember(roleCheck.leader, roles, Global.CharacterCacheStorage.GetCharacterCacheByGuid(roleCheck.leader).Level, roles > 0));

			foreach (var it in roleCheck.roles)
			{
				if (it.Key == roleCheck.leader)
					continue;

				roles = (byte)it.Value;
				lfgRoleCheckUpdate.Members.Add(new LFGRoleCheckUpdateMember(it.Key, roles, Global.CharacterCacheStorage.GetCharacterCacheByGuid(it.Key).Level, roles > 0));
			}
		}

		_session.SendPacket(lfgRoleCheckUpdate);
	}

	public void SendLfgJoinResult(LfgJoinResultData joinData)
	{
		LFGJoinResult lfgJoinResult = new();

		var ticket = _lFGManager.GetTicket(_session.Player.GUID);

		if (ticket != null)
			lfgJoinResult.Ticket = ticket;

		lfgJoinResult.Result = (byte)joinData.result;

		if (joinData.result == LfgJoinResult.RoleCheckFailed)
			lfgJoinResult.ResultDetail = (byte)joinData.state;
		else if (joinData.result == LfgJoinResult.NoSlots)
			lfgJoinResult.BlackListNames = joinData.playersMissingRequirement;

		foreach (var it in joinData.lockmap)
		{
			var blackList = new LFGBlackListPkt();
			blackList.PlayerGuid = it.Key;

			foreach (var lockInfo in it.Value)
			{
				Log.Logger.Verbose(
							"SendLfgJoinResult:: {0} DungeonID: {1} Lock status: {2} Required itemLevel: {3} Current itemLevel: {4}",
							it.Key.ToString(),
							(lockInfo.Key & 0x00FFFFFF),
							lockInfo.Value.lockStatus,
							lockInfo.Value.requiredItemLevel,
							lockInfo.Value.currentItemLevel);

				blackList.Slot.Add(new LFGBlackListSlot(lockInfo.Key, (uint)lockInfo.Value.lockStatus, lockInfo.Value.requiredItemLevel, (int)lockInfo.Value.currentItemLevel, 0));
			}

			lfgJoinResult.BlackList.Add(blackList);
		}

		_session.SendPacket(lfgJoinResult);
	}

	public void SendLfgQueueStatus(LfgQueueStatusData queueData)
	{
		Log.Logger.Debug(
					"SMSG_LFG_QUEUE_STATUS {0} state: {1} dungeon: {2}, waitTime: {3}, " +
					"avgWaitTime: {4}, waitTimeTanks: {5}, waitTimeHealer: {6}, waitTimeDps: {7}, queuedTime: {8}, tanks: {9}, healers: {10}, dps: {11}",
                    _session.GetPlayerInfo(),
					_lFGManager.GetState(_session.Player.GUID),
					queueData.dungeonId,
					queueData.waitTime,
					queueData.waitTimeAvg,
					queueData.waitTimeTank,
					queueData.waitTimeHealer,
					queueData.waitTimeDps,
					queueData.queuedTime,
					queueData.tanks,
					queueData.healers,
					queueData.dps);

		LFGQueueStatus lfgQueueStatus = new();

		var ticket = _lFGManager.GetTicket(_session.Player.GUID);

		if (ticket != null)
			lfgQueueStatus.Ticket = ticket;

		lfgQueueStatus.Slot = queueData.queueId;
		lfgQueueStatus.AvgWaitTimeMe = (uint)queueData.waitTime;
		lfgQueueStatus.AvgWaitTime = (uint)queueData.waitTimeAvg;
		lfgQueueStatus.AvgWaitTimeByRole[0] = (uint)queueData.waitTimeTank;
		lfgQueueStatus.AvgWaitTimeByRole[1] = (uint)queueData.waitTimeHealer;
		lfgQueueStatus.AvgWaitTimeByRole[2] = (uint)queueData.waitTimeDps;
		lfgQueueStatus.LastNeeded[0] = queueData.tanks;
		lfgQueueStatus.LastNeeded[1] = queueData.healers;
		lfgQueueStatus.LastNeeded[2] = queueData.dps;
		lfgQueueStatus.QueuedTime = queueData.queuedTime;

		_session.SendPacket(lfgQueueStatus);
	}

	public void SendLfgPlayerReward(LfgPlayerRewardData rewardData)
	{
		if (rewardData.rdungeonEntry == 0 || rewardData.sdungeonEntry == 0 || rewardData.quest == null)
			return;

		Log.Logger.Debug(
					"SMSG_LFG_PLAYER_REWARD {0} rdungeonEntry: {1}, sdungeonEntry: {2}, done: {3}",
                    _session.GetPlayerInfo(),
					rewardData.rdungeonEntry,
					rewardData.sdungeonEntry,
					rewardData.done);

		LFGPlayerReward lfgPlayerReward = new();
		lfgPlayerReward.QueuedSlot = rewardData.rdungeonEntry;
		lfgPlayerReward.ActualSlot = rewardData.sdungeonEntry;
		lfgPlayerReward.RewardMoney = _session.Player.GetQuestMoneyReward(rewardData.quest);
		lfgPlayerReward.AddedXP = _session.Player.GetQuestXPReward(rewardData.quest);

		for (byte i = 0; i < SharedConst.QuestRewardItemCount; ++i)
		{
			var itemId = rewardData.quest.RewardItemId[i];

			if (itemId != 0)
				lfgPlayerReward.Rewards.Add(new LFGPlayerRewards(itemId, rewardData.quest.RewardItemCount[i], 0, false));
		}

		for (byte i = 0; i < SharedConst.QuestRewardCurrencyCount; ++i)
		{
			var currencyId = rewardData.quest.RewardCurrencyId[i];

			if (currencyId != 0)
				lfgPlayerReward.Rewards.Add(new LFGPlayerRewards(currencyId, rewardData.quest.RewardCurrencyCount[i], 0, true));
		}

		_session.SendPacket(lfgPlayerReward);
	}

	public void SendLfgBootProposalUpdate(LfgPlayerBoot boot)
	{
		var playerVote = boot.votes.LookupByKey(_session.Player.GUID);
		byte votesNum = 0;
		byte agreeNum = 0;
		var secsleft = (uint)((boot.cancelTime - _gameTime.GetGameTime) / 1000);

		foreach (var it in boot.votes)
			if (it.Value != LfgAnswer.Pending)
			{
				++votesNum;

				if (it.Value == LfgAnswer.Agree)
					++agreeNum;
			}

		Log.Logger.Debug(
					"SMSG_LFG_BOOT_PROPOSAL_UPDATE {0} inProgress: {1} - didVote: {2} - agree: {3} - victim: {4} votes: {5} - agrees: {6} - left: {7} - needed: {8} - reason {9}",
                    _session.GetPlayerInfo(),
					boot.inProgress,
					playerVote != LfgAnswer.Pending,
					playerVote == LfgAnswer.Agree,
					boot.victim.ToString(),
					votesNum,
					agreeNum,
					secsleft,
					SharedConst.LFGKickVotesNeeded,
					boot.reason);

		LfgBootPlayer lfgBootPlayer = new();
		lfgBootPlayer.Info.VoteInProgress = boot.inProgress;                        // Vote in progress
        lfgBootPlayer.Info.VotePassed = agreeNum >= SharedConst.LFGKickVotesNeeded; // Did succeed
        lfgBootPlayer.Info.MyVoteCompleted = playerVote != LfgAnswer.Pending;       // Did Vote
        lfgBootPlayer.Info.MyVote = playerVote == LfgAnswer.Agree;                  // Agree
        lfgBootPlayer.Info.Target = boot.victim;                                    // Victim GUID
        lfgBootPlayer.Info.TotalVotes = votesNum;                                   // Total Votes
        lfgBootPlayer.Info.BootVotes = agreeNum;                                    // Agree Count
        lfgBootPlayer.Info.TimeLeft = secsleft;                                     // Time Left
        lfgBootPlayer.Info.VotesNeeded = SharedConst.LFGKickVotesNeeded;            // Needed Votes
        lfgBootPlayer.Info.Reason = boot.reason;                                    // Kick reason
		_session.SendPacket(lfgBootPlayer);
	}

	public void SendLfgProposalUpdate(LfgProposal proposal)
	{
		var playerGuid = _session.Player.GUID;
		var guildGuid = proposal.players.LookupByKey(playerGuid).group;
		var silent = !proposal.isNew && guildGuid == proposal.group;
		var dungeonEntry = proposal.dungeonId;

		Log.Logger.Debug("SMSG_LFG_PROPOSAL_UPDATE {0} state: {1}", _session.GetPlayerInfo(), proposal.state);

		// show random dungeon if player selected random dungeon and it's not lfg group
		if (!silent)
		{
			var playerDungeons = _lFGManager.GetSelectedDungeons(playerGuid);

			if (!playerDungeons.Contains(proposal.dungeonId))
				dungeonEntry = playerDungeons.First();
		}

		LFGProposalUpdate lfgProposalUpdate = new();

		var ticket = _lFGManager.GetTicket(_session.Player.GUID);

		if (ticket != null)
			lfgProposalUpdate.Ticket = ticket;

		lfgProposalUpdate.InstanceID = 0;
		lfgProposalUpdate.ProposalID = proposal.id;
		lfgProposalUpdate.Slot = _lFGManager.GetLFGDungeonEntry(dungeonEntry);
		lfgProposalUpdate.State = (byte)proposal.state;
		lfgProposalUpdate.CompletedMask = proposal.encounters;
		lfgProposalUpdate.ValidCompletedMask = true;
		lfgProposalUpdate.ProposalSilent = silent;
		lfgProposalUpdate.IsRequeue = !proposal.isNew;

		foreach (var pair in proposal.players)
		{
			var proposalPlayer = new LFGProposalUpdatePlayer();
            proposalPlayer.Roles = (uint)pair.Value.role;
            proposalPlayer.Me = (pair.Key == playerGuid);
            proposalPlayer.MyParty = !pair.Value.group.IsEmpty && pair.Value.group == proposal.group;
            proposalPlayer.SameParty = !pair.Value.group.IsEmpty && pair.Value.group == guildGuid;
            proposalPlayer.Responded = (pair.Value.accept != LfgAnswer.Pending);
            proposalPlayer.Accepted = (pair.Value.accept == LfgAnswer.Agree);

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
		_session.SendPacket(new LfgOfferContinue(_lFGManager.GetLFGDungeonEntry(dungeonEntry)));
	}

	public void SendLfgTeleportError(LfgTeleportResult err)
	{
		Log.Logger.Debug("SMSG_LFG_TELEPORT_DENIED {0} reason: {1}", _session.GetPlayerInfo(), err);
		_session.SendPacket(new LfgTeleportDenied(err));
	}

	[WorldPacketHandler(ClientOpcodes.DfJoin)]
	void HandleLfgJoin(DFJoin dfJoin)
	{
		if (!_lFGManager.IsOptionEnabled(LfgOptions.EnableDungeonFinder | LfgOptions.EnableRaidBrowser) ||
			(_session.Player.Group &&
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

			if (_cliDB.LFGDungeonsStorage.ContainsKey(dungeon))
				newDungeons.Add(dungeon);
		}

		Log.Logger.Debug("CMSG_DF_JOIN {0} roles: {1}, Dungeons: {2}", _session.GetPlayerInfo(), dfJoin.Roles, newDungeons.Count);

		_lFGManager.JoinLfg(_session.Player, dfJoin.Roles, newDungeons);
	}

	[WorldPacketHandler(ClientOpcodes.DfLeave)]
	void HandleLfgLeave(DFLeave dfLeave)
	{
		var group = _session.Player.Group;

		Log.Logger.Debug("CMSG_DF_LEAVE {0} in group: {1} sent guid {2}.", _session.GetPlayerInfo(), group ? 1 : 0, dfLeave.Ticket.RequesterGuid.ToString());

		// Check cheating - only leader can leave the queue
		if (!group || group.LeaderGUID == dfLeave.Ticket.RequesterGuid)
			_lFGManager.LeaveLfg(dfLeave.Ticket.RequesterGuid);
	}

	[WorldPacketHandler(ClientOpcodes.DfProposalResponse)]
	void HandleLfgProposalResult(DFProposalResponse dfProposalResponse)
	{
		Log.Logger.Debug("CMSG_LFG_PROPOSAL_RESULT {0} proposal: {1} accept: {2}", _session.GetPlayerInfo(), dfProposalResponse.ProposalID, dfProposalResponse.Accepted ? 1 : 0);
		_lFGManager.UpdateProposal(dfProposalResponse.ProposalID, _session.Player.GUID, dfProposalResponse.Accepted);
	}

	[WorldPacketHandler(ClientOpcodes.DfSetRoles)]
	void HandleLfgSetRoles(DFSetRoles dfSetRoles)
	{
		var guid = _session.Player.GUID;
		var group = _session.Player.Group;

		if (!group)
		{
			Log.Logger.Debug(
						"CMSG_DF_SET_ROLES {0} Not in group",
                        _session.GetPlayerInfo());

			return;
		}

		var gguid = group.GUID;
		Log.Logger.Debug("CMSG_DF_SET_ROLES: Group {0}, Player {1}, Roles: {2}", gguid.ToString(), _session.GetPlayerInfo(), dfSetRoles.RolesDesired);
		_lFGManager.UpdateRoleCheck(gguid, guid, dfSetRoles.RolesDesired);
	}

	[WorldPacketHandler(ClientOpcodes.DfBootPlayerVote)]
	void HandleLfgSetBootVote(DFBootPlayerVote dfBootPlayerVote)
	{
		var guid = _session.Player.GUID;
		Log.Logger.Debug("CMSG_LFG_SET_BOOT_VOTE {0} agree: {1}", _session.GetPlayerInfo(), dfBootPlayerVote.Vote ? 1 : 0);
		_lFGManager.UpdateBoot(guid, dfBootPlayerVote.Vote);
	}

	[WorldPacketHandler(ClientOpcodes.DfTeleport)]
	void HandleLfgTeleport(DFTeleport dfTeleport)
	{
		Log.Logger.Debug("CMSG_DF_TELEPORT {0} out: {1}", _session.GetPlayerInfo(), dfTeleport.TeleportOut ? 1 : 0);
		_lFGManager.TeleportPlayer(_session.Player, dfTeleport.TeleportOut, true);
	}
}