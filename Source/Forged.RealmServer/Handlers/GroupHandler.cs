// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Game.Entities;
using Forged.RealmServer.Groups;
using Game.Common.Entities.Objects;
using Game.Common.Entities.Players;
using Game.Common.Networking;
using Game.Common.Networking.Packets.Misc;
using Game.Common.Networking.Packets.Party;

namespace Forged.RealmServer;

public partial class WorldSession
{
	public void SendPartyResult(PartyOperation operation, string member, PartyResult res, uint val = 0)
	{
		PartyCommandResult packet = new();

		packet.Name = member;
		packet.Command = (byte)operation;
		packet.Result = (byte)res;
		packet.ResultData = val;
		packet.ResultGUID = ObjectGuid.Empty;

		SendPacket(packet);
	}

	[WorldPacketHandler(ClientOpcodes.PartyInvite)]
	void HandlePartyInvite(PartyInviteClient packet)
	{
		var invitingPlayer = Player;
		var invitedPlayer = Global.ObjAccessor.FindPlayerByName(packet.TargetName);

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
		if (!WorldConfig.GetBoolValue(WorldCfg.AllowGmGroup) && !invitingPlayer.IsGameMaster && invitedPlayer.IsGameMaster)
		{
			SendPartyResult(PartyOperation.Invite, invitedPlayer.GetName(), PartyResult.BadPlayerNameS);

			return;
		}

		// can't group with
		if (!invitingPlayer.IsGameMaster && !WorldConfig.GetBoolValue(WorldCfg.AllowTwoSideInteractionGroup) && invitingPlayer.Team != invitedPlayer.Team)
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

		if (!invitedPlayer.Social.HasFriend(invitingPlayer.GUID) && invitingPlayer.Level < WorldConfig.GetIntValue(WorldCfg.PartyLevelReq))
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
		var group = Player.GroupInvite;

		if (!group)
			return;

		if (packet.Accept)
		{
			// Remove player from invitees in any case
			group.RemoveInvite(Player);

			if (group.LeaderGUID == Player.GUID)
			{
				Log.outError(LogFilter.Network, "HandleGroupAcceptOpcode: player {0} ({1}) tried to accept an invite to his own group", Player.GetName(), Player.GUID.ToString());

				return;
			}

			// Group is full
			if (group.IsFull)
			{
				SendPartyResult(PartyOperation.Invite, "", PartyResult.GroupFull);

				return;
			}

			var leader = Global.ObjAccessor.FindPlayer(group.LeaderGUID);

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
				Global.GroupMgr.AddGroup(group);
			}

			// Everything is fine, do it, PLAYER'S GROUP IS SET IN ADDMEMBER!!!
			if (!group.AddMember(Player))
				return;

			group.BroadcastGroupUpdate();
		}
		else
		{
			// Remember leader if online (group will be invalid if group gets disbanded)
			var leader = Global.ObjAccessor.FindPlayer(group.LeaderGUID);

			// uninvite, group can be deleted
			Player.UninviteFromGroup();

			if (!leader || leader.Session == null)
				return;

			// report
			GroupDecline decline = new(Player.GetName());
			leader.SendPacket(decline);
		}
	}

	[WorldPacketHandler(ClientOpcodes.PartyUninvite)]
	void HandlePartyUninvite(PartyUninvite packet)
	{
		//can't uninvite yourself
		if (packet.TargetGUID == Player.GUID)
		{
			Log.outError(LogFilter.Network,
						"HandleGroupUninviteGuidOpcode: leader {0}({1}) tried to uninvite himself from the group.",
						Player.GetName(),
						Player.GUID.ToString());

			return;
		}

		var res = Player.CanUninviteFromGroup(packet.TargetGUID);

		if (res != PartyResult.Ok)
		{
			SendPartyResult(PartyOperation.UnInvite, "", res);

			return;
		}

		var grp = Player.Group;
		// grp is checked already above in CanUninviteFromGroup()

		if (grp.IsMember(packet.TargetGUID))
		{
			Player.RemoveFromGroup(grp, packet.TargetGUID, RemoveMethod.Kick, Player.GUID, packet.Reason);

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
		var player = Global.ObjAccessor.FindConnectedPlayer(packet.TargetGUID);
		var group = Player.Group;

		if (!group || !player)
			return;

		if (!group.IsLeader(Player.GUID) || player.Group != group)
			return;

		// Everything's fine, accepted.
		group.ChangeLeader(packet.TargetGUID, packet.PartyIndex);
		group.SendUpdate();
	}

	[WorldPacketHandler(ClientOpcodes.SetRole)]
	void HandleSetRole(SetRole packet)
	{
		RoleChangedInform roleChangedInform = new();

		var group = Player.Group;
		var oldRole = (byte)(group ? group.GetLfgRoles(packet.TargetGUID) : 0);

		if (oldRole == packet.Role)
			return;

		roleChangedInform.PartyIndex = packet.PartyIndex;
		roleChangedInform.From = Player.GUID;
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
			SendPacket(roleChangedInform);
		}
	}

	[WorldPacketHandler(ClientOpcodes.LeaveGroup)]
	void HandleLeaveGroup(LeaveGroup packet)
	{
		var grp = Player.Group;
		var grpInvite = Player.GroupInvite;

		if (grp == null && grpInvite == null)
			return;

		if (Player.InBattleground)
		{
			SendPartyResult(PartyOperation.Invite, "", PartyResult.InviteRestricted);

			return;
		}

		/** error handling **/
		/********************/

		// everything's fine, do it
		if (grp != null)
		{
			SendPartyResult(PartyOperation.Leave, Player.GetName(), PartyResult.Ok);
			Player.RemoveFromGroup(RemoveMethod.Leave);
		}
		else if (grpInvite != null && grpInvite.LeaderGUID == Player.GUID)
		{
			// pending group creation being cancelled
			SendPartyResult(PartyOperation.Leave, Player.GetName(), PartyResult.Ok);
			grpInvite.Disband();
		}
	}

	[WorldPacketHandler(ClientOpcodes.SetLootMethod)]
	void HandleSetLootMethod(SetLootMethod packet)
	{
		// not allowed to change
		var group = Player.Group;

		if (group == null)
			return;

		if (!group.IsLeader(Player.GUID))
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
		if (!Player.Group)
			return;

		MinimapPing minimapPing = new();
		minimapPing.Sender = Player.GUID;
		minimapPing.PositionX = packet.PositionX;
		minimapPing.PositionY = packet.PositionY;
		Player.Group.BroadcastPacket(minimapPing, true, -1, Player.GUID);
	}

	[WorldPacketHandler(ClientOpcodes.RandomRoll)]
	void HandleRandomRoll(RandomRollClient packet)
	{
		if (packet.Min > packet.Max || packet.Max > 1000000) // < 32768 for urand call
			return;

		Player.DoRandomRoll(packet.Min, packet.Max);
	}

	[WorldPacketHandler(ClientOpcodes.UpdateRaidTarget)]
	void HandleUpdateRaidTarget(UpdateRaidTarget packet)
	{
		var group = Player.Group;

		if (!group)
			return;

		if (packet.Symbol == -1) // target icon request
		{
			group.SendTargetIconList(this, packet.PartyIndex);
		}
		else // target icon update
		{
			if (group.IsRaidGroup && !group.IsLeader(Player.GUID) && !group.IsAssistant(Player.GUID))
				return;

			if (packet.Target.IsPlayer)
			{
				var target = Global.ObjAccessor.FindConnectedPlayer(packet.Target);

				if (!target || target.IsHostileTo(Player))
					return;
			}

			group.SetTargetIcon((byte)packet.Symbol, packet.Target, Player.GUID, packet.PartyIndex);
		}
	}

	[WorldPacketHandler(ClientOpcodes.ConvertRaid)]
	void HandleConvertRaid(ConvertRaid packet)
	{
		var group = Player.Group;

		if (!group)
			return;

		if (Player.InBattleground)
			return;

		// error handling
		if (!group.IsLeader(Player.GUID) || group.MembersCount < 2)
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
		var group = Player.Group;

		if (!group)
			return;

		group.SendTargetIconList(this, packet.PartyIndex);
		group.SendRaidMarkersChanged(this, packet.PartyIndex);
	}

	[WorldPacketHandler(ClientOpcodes.ChangeSubGroup, Processing = PacketProcessing.ThreadUnsafe)]
	void HandleChangeSubGroup(ChangeSubGroup packet)
	{
		// we will get correct for group here, so we don't have to check if group is BG raid
		var group = Player.Group;

		if (!group)
			return;

		if (packet.NewSubGroup >= MapConst.MaxRaidSubGroups)
			return;

		var senderGuid = Player.GUID;

		if (!group.IsLeader(senderGuid) && !group.IsAssistant(senderGuid))
			return;

		if (!group.HasFreeSlotSubGroup(packet.NewSubGroup))
			return;

		group.ChangeMembersGroup(packet.TargetGUID, packet.NewSubGroup);
	}

	[WorldPacketHandler(ClientOpcodes.SwapSubGroups, Processing = PacketProcessing.ThreadUnsafe)]
	void HandleSwapSubGroups(SwapSubGroups packet)
	{
		var group = Player.Group;

		if (!group)
			return;

		var senderGuid = Player.GUID;

		if (!group.IsLeader(senderGuid) && !group.IsAssistant(senderGuid))
			return;

		group.SwapMembersGroups(packet.FirstTarget, packet.SecondTarget);
	}

	[WorldPacketHandler(ClientOpcodes.SetAssistantLeader)]
	void HandleSetAssistantLeader(SetAssistantLeader packet)
	{
		var group = Player.Group;

		if (!group)
			return;

		if (!group.IsLeader(Player.GUID))
			return;

		group.SetGroupMemberFlag(packet.Target, packet.Apply, GroupMemberFlags.Assistant);
	}

	[WorldPacketHandler(ClientOpcodes.SetPartyAssignment)]
	void HandleSetPartyAssignment(SetPartyAssignment packet)
	{
		var group = Player.Group;

		if (!group)
			return;

		var senderGuid = Player.GUID;

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
		var group = Player.Group;

		if (!group)
			return;

		/** error handling **/
		if (!group.IsLeader(Player.GUID) && !group.IsAssistant(Player.GUID))
			return;

		// everything's fine, do it
		group.StartReadyCheck(Player.GUID, packet.PartyIndex, TimeSpan.FromMilliseconds(MapConst.ReadycheckDuration));
	}

	[WorldPacketHandler(ClientOpcodes.ReadyCheckResponse, Processing = PacketProcessing.Inplace)]
	void HandleReadyCheckResponseOpcode(ReadyCheckResponseClient packet)
	{
		var group = Player.Group;

		if (!group)
			return;

		// everything's fine, do it
		group.SetMemberReadyCheck(Player.GUID, packet.IsReady);
	}

	[WorldPacketHandler(ClientOpcodes.RequestPartyMemberStats)]
	void HandleRequestPartyMemberStats(RequestPartyMemberStats packet)
	{
		PartyMemberFullState partyMemberStats = new();

		var player = Global.ObjAccessor.FindConnectedPlayer(packet.TargetGUID);

		if (!player)
		{
			partyMemberStats.MemberGuid = packet.TargetGUID;
			partyMemberStats.MemberStats.Status = GroupMemberOnlineStatus.Offline;
		}
		else
		{
			partyMemberStats.Initialize(player);
		}

		SendPacket(partyMemberStats);
	}

	[WorldPacketHandler(ClientOpcodes.RequestRaidInfo)]
	void HandleRequestRaidInfo(RequestRaidInfo packet)
	{
		// every time the player checks the character screen
		Player.SendRaidInfo();
	}

	[WorldPacketHandler(ClientOpcodes.OptOutOfLoot, Processing = PacketProcessing.Inplace)]
	void HandleOptOutOfLoot(OptOutOfLoot packet)
	{
		// ignore if player not loaded
		if (!Player) // needed because STATUS_AUTHED
		{
			if (packet.PassOnLoot)
				Log.outError(LogFilter.Network, "CMSG_OPT_OUT_OF_LOOT value<>0 for not-loaded character!");

			return;
		}

		Player.PassOnGroupLoot = packet.PassOnLoot;
	}

	[WorldPacketHandler(ClientOpcodes.InitiateRolePoll)]
	void HandleInitiateRolePoll(InitiateRolePoll packet)
	{
		var group = Player.Group;

		if (!group)
			return;

		var guid = Player.GUID;

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
		var group = Player.Group;

		if (!group)
			return;

		if (!group.IsLeader(Player.GUID))
			return;

		group.SetEveryoneIsAssistant(packet.EveryoneIsAssistant);
	}

	[WorldPacketHandler(ClientOpcodes.ClearRaidMarker)]
	void HandleClearRaidMarker(ClearRaidMarker packet)
	{
		var group = Player.Group;

		if (!group)
			return;

		if (group.IsRaidGroup && !group.IsLeader(Player.GUID) && !group.IsAssistant(Player.GUID))
			return;

		group.DeleteRaidMarker(packet.MarkerId);
	}
}