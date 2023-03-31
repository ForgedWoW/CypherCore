// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.RealmServer.Entities;
using Forged.RealmServer.Groups;
using Forged.RealmServer.Networking;
using Forged.RealmServer.Networking.Packets;
using Framework.Constants;
using Game.Common.Handlers;
using Serilog;
using System;

namespace Forged.RealmServer;

public class GroupHandler : IWorldSessionHandler
{
    private readonly WorldSession _session;
    private readonly WorldConfig _worldConfig;
    private readonly ObjectAccessor _objectAccessor;
    private readonly GroupManager _groupManager;

    public GroupHandler(WorldSession session, WorldConfig worldConfig, ObjectAccessor objectAccessor, GroupManager groupManager)
    {
        _session = session;
        _worldConfig = worldConfig;
        _objectAccessor = objectAccessor;
        _groupManager = groupManager;
    }

    public void SendPartyResult(PartyOperation operation, string member, PartyResult res, uint val = 0)
	{
		PartyCommandResult packet = new();

		packet.Name = member;
		packet.Command = (byte)operation;
		packet.Result = (byte)res;
		packet.ResultData = val;
		packet.ResultGUID = ObjectGuid.Empty;

		_session.SendPacket(packet);
	}

	[WorldPacketHandler(ClientOpcodes.PartyInvite)]
	void HandlePartyInvite(PartyInviteClient packet)
	{
		var invitingPlayer = _session.Player;
		var invitedPlayer = _objectAccessor.FindPlayerByName(packet.TargetName);

		// no player
		if (invitedPlayer == null)
		{
			SendPartyResult(PartyOperation.Invite, packet.TargetName, PartyResult.BadPlayerNameS);

			return;
		}

		// player trying to invite himself (most likely cheating)
		if (invitedPlayer == invitingPlayer)
		{
			SendPartyResult(PartyOperation.Invite, invitedPlayer.GetName(), PartyResult.BadPlayerNameS);

			return;
		}

		// restrict invite to GMs
		if (!_worldConfig.GetBoolValue(WorldCfg.AllowGmGroup) && !invitingPlayer.IsGameMaster && invitedPlayer.IsGameMaster)
		{
			SendPartyResult(PartyOperation.Invite, invitedPlayer.GetName(), PartyResult.BadPlayerNameS);

			return;
		}

		// can't group with
		if (!invitingPlayer.IsGameMaster && !_worldConfig.GetBoolValue(WorldCfg.AllowTwoSideInteractionGroup) && invitingPlayer.Team != invitedPlayer.Team)
		{
			SendPartyResult(PartyOperation.Invite, invitedPlayer.GetName(), PartyResult.PlayerWrongFaction);

			return;
		}

		if (invitingPlayer.InstanceId != 0 && invitedPlayer.InstanceId != 0 && invitingPlayer.InstanceId != invitedPlayer.InstanceId && invitingPlayer.Location.MapId == invitedPlayer.Location.MapId)
		{
			SendPartyResult(PartyOperation.Invite, invitedPlayer.GetName(), PartyResult.TargetNotInInstanceS);

			return;
		}

		// just ignore us
		if (invitedPlayer.InstanceId != 0 && invitedPlayer.DungeonDifficultyId != invitingPlayer.DungeonDifficultyId)
		{
			SendPartyResult(PartyOperation.Invite, invitedPlayer.GetName(), PartyResult.IgnoringYouS);

			return;
		}

		if (invitedPlayer.Social.HasIgnore(invitingPlayer.GUID, invitingPlayer.Session.AccountGUID))
		{
			SendPartyResult(PartyOperation.Invite, invitedPlayer.GetName(), PartyResult.IgnoringYouS);

			return;
		}

		if (!invitedPlayer.Social.HasFriend(invitingPlayer.GUID) && invitingPlayer.Level < _worldConfig.GetIntValue(WorldCfg.PartyLevelReq))
		{
			SendPartyResult(PartyOperation.Invite, invitedPlayer.GetName(), PartyResult.InviteRestricted);

			return;
		}

		var group = invitingPlayer.Group;

		if (group != null && group.IsBGGroup)
			group = invitingPlayer.OriginalGroup;

		if (group == null)
			group = invitingPlayer.GroupInvite;

		var group2 = invitedPlayer.Group;

		if (group2 != null && group2.IsBGGroup)
			group2 = invitedPlayer.OriginalGroup;

		PartyInvite partyInvite;

		// player already in another group or invited
		if (group2 || invitedPlayer.GroupInvite)
		{
			SendPartyResult(PartyOperation.Invite, invitedPlayer.GetName(), PartyResult.AlreadyInGroupS);

			if (group2)
			{
				// tell the player that they were invited but it failed as they were already in a group
				partyInvite = new PartyInvite();
				partyInvite.Initialize(invitingPlayer, packet.ProposedRoles, false);
				invitedPlayer.SendPacket(partyInvite);
			}

			return;
		}

		if (group)
		{
			// not have permissions for invite
			if (!group.IsLeader(invitingPlayer.GUID) && !group.IsAssistant(invitingPlayer.GUID))
			{
				if (group.IsCreated)
					SendPartyResult(PartyOperation.Invite, "", PartyResult.NotLeader);

				return;
			}

			// not have place
			if (group.IsFull)
			{
				SendPartyResult(PartyOperation.Invite, "", PartyResult.GroupFull);

				return;
			}
		}

		// ok, but group not exist, start a new group
		// but don't create and save the group to the DB until
		// at least one person joins
		if (group == null)
		{
			group = new PlayerGroup();

			// new group: if can't add then delete
			if (!group.AddLeaderInvite(invitingPlayer))
				return;

			if (!group.AddInvite(invitedPlayer))
			{
				group.RemoveAllInvites();

				return;
			}
		}
		else
		{
			// already existed group: if can't add then just leave
			if (!group.AddInvite(invitedPlayer))
				return;
		}

		partyInvite = new PartyInvite();
		partyInvite.Initialize(invitingPlayer, packet.ProposedRoles, true);
		invitedPlayer.SendPacket(partyInvite);

		SendPartyResult(PartyOperation.Invite, invitedPlayer.GetName(), PartyResult.Ok);
	}

	[WorldPacketHandler(ClientOpcodes.PartyInviteResponse)]
	void HandlePartyInviteResponse(PartyInviteResponse packet)
	{
		var group = _session.Player.GroupInvite;

		if (!group)
			return;

		if (packet.Accept)
		{
			// Remove player from invitees in any case
			group.RemoveInvite(_session.Player);

			if (group.LeaderGUID == _session.Player.GUID)
			{
				Log.Logger.Error("HandleGroupAcceptOpcode: player {0} ({1}) tried to accept an invite to his own group", _session.Player.GetName(), _session.Player.GUID.ToString());

				return;
			}

			// Group is full
			if (group.IsFull)
			{
				SendPartyResult(PartyOperation.Invite, "", PartyResult.GroupFull);

				return;
			}

			var leader = _objectAccessor.FindPlayer(group.LeaderGUID);

			// Forming a new group, create it
			if (!group.IsCreated)
			{
				// This can happen if the leader is zoning. To be removed once delayed actions for zoning are implemented
				if (!leader)
				{
					group.RemoveAllInvites();

					return;
				}

				// If we're about to create a group there really should be a leader present
				group.RemoveInvite(leader);
				group.Create(leader);
                _groupManager.AddGroup(group);
			}

			// Everything is fine, do it, PLAYER'S GROUP IS SET IN ADDMEMBER!!!
			if (!group.AddMember(_session.Player))
				return;

			group.BroadcastGroupUpdate();
		}
		else
		{
			// Remember leader if online (group will be invalid if group gets disbanded)
			var leader = _objectAccessor.FindPlayer(group.LeaderGUID);

            // uninvite, group can be deleted
            _session.Player.UninviteFromGroup();

			if (!leader || leader.Session == null)
				return;

			// report
			GroupDecline decline = new(_session.Player.GetName());
			leader.SendPacket(decline);
		}
	}

	[WorldPacketHandler(ClientOpcodes.PartyUninvite)]
	void HandlePartyUninvite(PartyUninvite packet)
	{
		//can't uninvite yourself
		if (packet.TargetGUID == _session.Player.GUID)
		{
			Log.Logger.Error(
						"HandleGroupUninviteGuidOpcode: leader {0}({1}) tried to uninvite himself from the group.",
                        _session.Player.GetName(),
                        _session.Player.GUID.ToString());

			return;
		}

		var res = _session.Player.CanUninviteFromGroup(packet.TargetGUID);

		if (res != PartyResult.Ok)
		{
			SendPartyResult(PartyOperation.UnInvite, "", res);

			return;
		}

		var grp = _session.Player.Group;
		// grp is checked already above in CanUninviteFromGroup()

		if (grp.IsMember(packet.TargetGUID))
		{
			Player.RemoveFromGroup(grp, packet.TargetGUID, RemoveMethod.Kick, _session.Player.GUID, packet.Reason);

			return;
		}

		var player = grp.GetInvited(packet.TargetGUID);

		if (player)
		{
			player.UninviteFromGroup();

			return;
		}

		SendPartyResult(PartyOperation.UnInvite, "", PartyResult.TargetNotInGroupS);
	}

	[WorldPacketHandler(ClientOpcodes.SetPartyLeader, Processing = PacketProcessing.Inplace)]
	void HandleSetPartyLeader(SetPartyLeader packet)
	{
		var player = _objectAccessor.FindConnectedPlayer(packet.TargetGUID);
		var group = _session.Player.Group;

		if (!group || !player)
			return;

		if (!group.IsLeader(_session.Player.GUID) || player.Group != group)
			return;

		// Everything's fine, accepted.
		group.ChangeLeader(packet.TargetGUID, packet.PartyIndex);
		group.SendUpdate();
	}

	[WorldPacketHandler(ClientOpcodes.SetRole)]
	void HandleSetRole(SetRole packet)
	{
		RoleChangedInform roleChangedInform = new();

		var group = _session.Player.Group;
		var oldRole = (byte)(group ? group.GetLfgRoles(packet.TargetGUID) : 0);

		if (oldRole == packet.Role)
			return;

		roleChangedInform.PartyIndex = packet.PartyIndex;
		roleChangedInform.From = _session.Player.GUID;
		roleChangedInform.ChangedUnit = packet.TargetGUID;
		roleChangedInform.OldRole = oldRole;
		roleChangedInform.NewRole = packet.Role;

		if (group)
		{
			group.BroadcastPacket(roleChangedInform, false);
			group.SetLfgRoles(packet.TargetGUID, (LfgRoles)packet.Role);
		}
		else
		{
            _session.SendPacket(roleChangedInform);
		}
	}

	[WorldPacketHandler(ClientOpcodes.LeaveGroup)]
	void HandleLeaveGroup(LeaveGroup packet)
	{
		var grp = _session.Player.Group;
		var grpInvite = _session.Player.GroupInvite;

		if (grp == null && grpInvite == null)
			return;

		if (_session.Player.InBattleground)
		{
			SendPartyResult(PartyOperation.Invite, "", PartyResult.InviteRestricted);

			return;
		}

		/** error handling **/
		/********************/

		// everything's fine, do it
		if (grp != null)
		{
			SendPartyResult(PartyOperation.Leave, _session.Player.GetName(), PartyResult.Ok);
            _session.Player.RemoveFromGroup(RemoveMethod.Leave);
		}
		else if (grpInvite != null && grpInvite.LeaderGUID == _session.Player.GUID)
		{
			// pending group creation being cancelled
			SendPartyResult(PartyOperation.Leave, _session.Player.GetName(), PartyResult.Ok);
			grpInvite.Disband();
		}
	}

	[WorldPacketHandler(ClientOpcodes.SetLootMethod)]
	void HandleSetLootMethod(SetLootMethod packet)
	{
		// not allowed to change
		var group = _session.Player.Group;

		if (group == null)
			return;

		if (!group.IsLeader(_session.Player.GUID))
			return;

		if (group.IsLFGGroup)
			return;

		switch (packet.LootMethod)
		{
			case LootMethod.FreeForAll:
			case LootMethod.MasterLoot:
			case LootMethod.GroupLoot:
			case LootMethod.PersonalLoot:
				break;
			default:
				return;
		}

		if (packet.LootThreshold < ItemQuality.Uncommon || packet.LootThreshold > ItemQuality.Artifact)
			return;

		if (packet.LootMethod == LootMethod.MasterLoot && !group.IsMember(packet.LootMasterGUID))
			return;

		// everything's fine, do it
		group.SetLootMethod(packet.LootMethod);
		group.SetMasterLooterGuid(packet.LootMasterGUID);
		group.SetLootThreshold(packet.LootThreshold);
		group.SendUpdate();
	}

	[WorldPacketHandler(ClientOpcodes.MinimapPing)]
	void HandleMinimapPing(MinimapPingClient packet)
	{
		if (!_session.Player.Group)
			return;

		MinimapPing minimapPing = new();
		minimapPing.Sender = _session.Player.GUID;
		minimapPing.PositionX = packet.PositionX;
		minimapPing.PositionY = packet.PositionY;
        _session.Player.Group.BroadcastPacket(minimapPing, true, -1, _session.Player.GUID);
	}

	[WorldPacketHandler(ClientOpcodes.RandomRoll)]
	void HandleRandomRoll(RandomRollClient packet)
	{
		if (packet.Min > packet.Max || packet.Max > 1000000) // < 32768 for urand call
			return;

        _session.Player.DoRandomRoll(packet.Min, packet.Max);
	}

	[WorldPacketHandler(ClientOpcodes.UpdateRaidTarget)]
	void HandleUpdateRaidTarget(UpdateRaidTarget packet)
	{
		var group = _session.Player.Group;

		if (!group)
			return;

		if (packet.Symbol == -1) // target icon request
		{
			group.SendTargetIconList(_session, packet.PartyIndex);
		}
		else // target icon update
		{
			if (group.IsRaidGroup && !group.IsLeader(_session.Player.GUID) && !group.IsAssistant(_session.Player.GUID))
				return;

			if (packet.Target.IsPlayer)
			{
				var target = _objectAccessor.FindConnectedPlayer(packet.Target);

				if (!target || target.IsHostileTo(_session.Player))
					return;
			}

			group.SetTargetIcon((byte)packet.Symbol, packet.Target, _session.Player.GUID, packet.PartyIndex);
		}
	}

	[WorldPacketHandler(ClientOpcodes.ConvertRaid)]
	void HandleConvertRaid(ConvertRaid packet)
	{
		var group = _session.Player.Group;

		if (!group)
			return;

		if (_session.Player.InBattleground)
			return;

		// error handling
		if (!group.IsLeader(_session.Player.GUID) || group.MembersCount < 2)
			return;

		// everything's fine, do it (is it 0 (PartyOperation.Invite) correct code)
		SendPartyResult(PartyOperation.Invite, "", PartyResult.Ok);

		// New 4.x: it is now possible to convert a raid to a group if member count is 5 or less
		if (packet.Raid)
			group.ConvertToRaid();
		else
			group.ConvertToGroup();
	}

	[WorldPacketHandler(ClientOpcodes.RequestPartyJoinUpdates)]
	void HandleRequestPartyJoinUpdates(RequestPartyJoinUpdates packet)
	{
		var group = _session.Player.Group;

		if (!group)
			return;

		group.SendTargetIconList(_session, packet.PartyIndex);
		group.SendRaidMarkersChanged(_session, packet.PartyIndex);
	}

	[WorldPacketHandler(ClientOpcodes.ChangeSubGroup, Processing = PacketProcessing.ThreadUnsafe)]
	void HandleChangeSubGroup(ChangeSubGroup packet)
	{
		// we will get correct for group here, so we don't have to check if group is BG raid
		var group = _session.Player.Group;

		if (!group)
			return;

		if (packet.NewSubGroup >= MapConst.MaxRaidSubGroups)
			return;

		var senderGuid = _session.Player.GUID;

		if (!group.IsLeader(senderGuid) && !group.IsAssistant(senderGuid))
			return;

		if (!group.HasFreeSlotSubGroup(packet.NewSubGroup))
			return;

		group.ChangeMembersGroup(packet.TargetGUID, packet.NewSubGroup);
	}

	[WorldPacketHandler(ClientOpcodes.SwapSubGroups, Processing = PacketProcessing.ThreadUnsafe)]
	void HandleSwapSubGroups(SwapSubGroups packet)
	{
		var group = _session.Player.Group;

		if (!group)
			return;

		var senderGuid = _session.Player.GUID;

		if (!group.IsLeader(senderGuid) && !group.IsAssistant(senderGuid))
			return;

		group.SwapMembersGroups(packet.FirstTarget, packet.SecondTarget);
	}

	[WorldPacketHandler(ClientOpcodes.SetAssistantLeader)]
	void HandleSetAssistantLeader(SetAssistantLeader packet)
	{
		var group = _session.Player.Group;

		if (!group)
			return;

		if (!group.IsLeader(_session.Player.GUID))
			return;

		group.SetGroupMemberFlag(packet.Target, packet.Apply, GroupMemberFlags.Assistant);
	}

	[WorldPacketHandler(ClientOpcodes.SetPartyAssignment)]
	void HandleSetPartyAssignment(SetPartyAssignment packet)
	{
		var group = _session.Player.Group;

		if (!group)
			return;

		var senderGuid = _session.Player.GUID;

		if (!group.IsLeader(senderGuid) && !group.IsAssistant(senderGuid))
			return;

		switch ((GroupMemberAssignment)packet.Assignment)
		{
			case GroupMemberAssignment.MainAssist:
				group.RemoveUniqueGroupMemberFlag(GroupMemberFlags.MainAssist);
				group.SetGroupMemberFlag(packet.Target, packet.Set, GroupMemberFlags.MainAssist);

				break;
			case GroupMemberAssignment.MainTank:
				group.RemoveUniqueGroupMemberFlag(GroupMemberFlags.MainTank); // Remove main assist flag from current if any.
				group.SetGroupMemberFlag(packet.Target, packet.Set, GroupMemberFlags.MainTank);

				break;
		}

		group.SendUpdate();
	}

	[WorldPacketHandler(ClientOpcodes.DoReadyCheck)]
	void HandleDoReadyCheckOpcode(DoReadyCheck packet)
	{
		var group = _session.Player.Group;

		if (!group)
			return;

		/** error handling **/
		if (!group.IsLeader(_session.Player.GUID) && !group.IsAssistant(_session.Player.GUID))
			return;

		// everything's fine, do it
		group.StartReadyCheck(_session.Player.GUID, packet.PartyIndex, TimeSpan.FromMilliseconds(MapConst.ReadycheckDuration));
	}

	[WorldPacketHandler(ClientOpcodes.ReadyCheckResponse, Processing = PacketProcessing.Inplace)]
	void HandleReadyCheckResponseOpcode(ReadyCheckResponseClient packet)
	{
		var group = _session.Player.Group;

		if (!group)
			return;

		// everything's fine, do it
		group.SetMemberReadyCheck(_session.Player.GUID, packet.IsReady);
	}

	[WorldPacketHandler(ClientOpcodes.RequestPartyMemberStats)]
	void HandleRequestPartyMemberStats(RequestPartyMemberStats packet)
	{
		PartyMemberFullState partyMemberStats = new();

		var player = _objectAccessor.FindConnectedPlayer(packet.TargetGUID);

		if (!player)
		{
			partyMemberStats.MemberGuid = packet.TargetGUID;
			partyMemberStats.MemberStats.Status = GroupMemberOnlineStatus.Offline;
		}
		else
		{
			partyMemberStats.Initialize(player);
		}

        _session.SendPacket(partyMemberStats);
	}

	[WorldPacketHandler(ClientOpcodes.RequestRaidInfo)]
	void HandleRequestRaidInfo(RequestRaidInfo packet)
	{
        // every time the player checks the character screen
        _session.Player.SendRaidInfo();
	}

	[WorldPacketHandler(ClientOpcodes.OptOutOfLoot, Processing = PacketProcessing.Inplace)]
	void HandleOptOutOfLoot(OptOutOfLoot packet)
	{
		// ignore if player not loaded
		if (!_session.Player) // needed because STATUS_AUTHED
		{
			if (packet.PassOnLoot)
				Log.Logger.Error("CMSG_OPT_OUT_OF_LOOT value<>0 for not-loaded character!");

			return;
		}

        _session.Player.PassOnGroupLoot = packet.PassOnLoot;
	}

	[WorldPacketHandler(ClientOpcodes.InitiateRolePoll)]
	void HandleInitiateRolePoll(InitiateRolePoll packet)
	{
		var group = _session.Player.Group;

		if (!group)
			return;

		var guid = _session.Player.GUID;

		if (!group.IsLeader(guid) && !group.IsAssistant(guid))
			return;

		RolePollInform rolePollInform = new();
		rolePollInform.From = guid;
		rolePollInform.PartyIndex = packet.PartyIndex;
		group.BroadcastPacket(rolePollInform, true);
	}

	[WorldPacketHandler(ClientOpcodes.SetEveryoneIsAssistant)]
	void HandleSetEveryoneIsAssistant(SetEveryoneIsAssistant packet)
	{
		var group = _session.Player.Group;

		if (!group)
			return;

		if (!group.IsLeader(_session.Player.GUID))
			return;

		group.SetEveryoneIsAssistant(packet.EveryoneIsAssistant);
	}

	[WorldPacketHandler(ClientOpcodes.ClearRaidMarker)]
	void HandleClearRaidMarker(ClearRaidMarker packet)
	{
		var group = _session.Player.Group;

		if (!group)
			return;

		if (group.IsRaidGroup && !group.IsLeader(_session.Player.GUID) && !group.IsAssistant(_session.Player.GUID))
			return;

		group.DeleteRaidMarker(packet.MarkerId);
	}
}