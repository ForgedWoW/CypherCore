// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.BattleGrounds;
using Forged.MapServer.Cache;
using Forged.MapServer.DataStorage;
using Forged.MapServer.DungeonFinding;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Globals;
using Forged.MapServer.Groups;
using Forged.MapServer.Networking;
using Forged.MapServer.Networking.Packets.Misc;
using Forged.MapServer.Networking.Packets.Party;
using Forged.MapServer.Scripting;
using Forged.MapServer.Server;
using Framework.Constants;
using Framework.Database;
using Game.Common;
using Game.Common.Handlers;
using Microsoft.Extensions.Configuration;
using Serilog;

// ReSharper disable UnusedMember.Local

namespace Forged.MapServer.OpCodeHandlers;

public class GroupHandler : IWorldSessionHandler
{
    private readonly ClassFactory _classFactory;
    private readonly IConfiguration _config;
    private readonly GroupManager _groupManager;
    private readonly ObjectAccessor _objectAccessor;
    private readonly WorldSession _session;

    public GroupHandler(ClassFactory classFactory, WorldSession session, IConfiguration config, ObjectAccessor objectAccessor,
        GroupManager groupManager)
    {
        _classFactory = classFactory;
        _session = session;
        _config = config;
        _objectAccessor = objectAccessor;
        _groupManager = groupManager;
    }

    public void SendPartyResult(PartyOperation operation, string member, PartyResult res, uint val = 0)
    {
        PartyCommandResult packet = new()
        {
            Name = member,
            Command = (byte)operation,
            Result = (byte)res,
            ResultData = val,
            ResultGUID = ObjectGuid.Empty
        };

        _session.SendPacket(packet);
    }

    [WorldPacketHandler(ClientOpcodes.ChangeSubGroup, Processing = PacketProcessing.ThreadUnsafe)]
    private void HandleChangeSubGroup(ChangeSubGroup packet)
    {
        // we will get correct for group here, so we don't have to check if group is BG raid
        var group = _session.Player.Group;

        if (group == null)
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

    [WorldPacketHandler(ClientOpcodes.ClearRaidMarker)]
    private void HandleClearRaidMarker(ClearRaidMarker packet)
    {
        var group = _session.Player.Group;

        if (group == null)
            return;

        if (group.IsRaidGroup && !group.IsLeader(_session.Player.GUID) && !group.IsAssistant(_session.Player.GUID))
            return;

        group.DeleteRaidMarker(packet.MarkerId);
    }

    [WorldPacketHandler(ClientOpcodes.ConvertRaid)]
    private void HandleConvertRaid(ConvertRaid packet)
    {
        var group = _session.Player.Group;

        if (group == null)
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

    [WorldPacketHandler(ClientOpcodes.DoReadyCheck)]
    private void HandleDoReadyCheckOpcode(DoReadyCheck packet)
    {
        var group = _session.Player.Group;

        if (group == null)
            return;

        /* error handling */
        if (!group.IsLeader(_session.Player.GUID) && !group.IsAssistant(_session.Player.GUID))
            return;

        // everything's fine, do it
        group.StartReadyCheck(_session.Player.GUID, packet.PartyIndex, TimeSpan.FromMilliseconds(MapConst.ReadycheckDuration));
    }

    [WorldPacketHandler(ClientOpcodes.InitiateRolePoll)]
    private void HandleInitiateRolePoll(InitiateRolePoll packet)
    {
        var group = _session.Player.Group;

        if (group == null)
            return;

        var guid = _session.Player.GUID;

        if (!group.IsLeader(guid) && !group.IsAssistant(guid))
            return;

        RolePollInform rolePollInform = new()
        {
            From = guid,
            PartyIndex = packet.PartyIndex
        };

        group.BroadcastPacket(rolePollInform, true);
    }

    [WorldPacketHandler(ClientOpcodes.LeaveGroup)]
    private void HandleLeaveGroup(LeaveGroup packet)
    {
        if (packet == null)
            return;

        var grp = _session.Player.Group;
        var grpInvite = _session.Player.GroupInvite;

        if (grp == null && grpInvite == null)
            return;

        if (_session.Player.InBattleground)
        {
            SendPartyResult(PartyOperation.Invite, "", PartyResult.InviteRestricted);

            return;
        }

        /* error handling */
        /********************/

        // everything's fine, do it
        if (grp != null)
        {
            SendPartyResult(PartyOperation.Leave, _session.Player.GetName(), PartyResult.Ok);
            _session.Player.RemoveFromGroup(RemoveMethod.Leave);
        }
        else if (grpInvite.LeaderGUID == _session.Player.GUID)
        {
            // pending group creation being cancelled
            SendPartyResult(PartyOperation.Leave, _session.Player.GetName(), PartyResult.Ok);
            grpInvite.Disband();
        }
    }

    [WorldPacketHandler(ClientOpcodes.MinimapPing)]
    private void HandleMinimapPing(MinimapPingClient packet)
    {
        if (_session.Player.Group == null)
            return;

        MinimapPing minimapPing = new()
        {
            Sender = _session.Player.GUID,
            PositionX = packet.PositionX,
            PositionY = packet.PositionY
        };

        _session.Player.Group.BroadcastPacket(minimapPing, true, -1, _session.Player.GUID);
    }

    [WorldPacketHandler(ClientOpcodes.OptOutOfLoot, Processing = PacketProcessing.Inplace)]
    private void HandleOptOutOfLoot(OptOutOfLoot packet)
    {
        // ignore if player not loaded
        if (_session.Player == null) // needed because STATUS_AUTHED
        {
            if (packet.PassOnLoot)
                Log.Logger.Error("CMSG_OPT_OUT_OF_LOOT value<>0 for not-loaded character!");

            return;
        }

        _session.Player.PassOnGroupLoot = packet.PassOnLoot;
    }

    [WorldPacketHandler(ClientOpcodes.PartyInvite)]
    private void HandlePartyInvite(PartyInviteClient packet)
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
        if (!_config.GetValue("GM.AllowInvite", false) && !invitingPlayer.IsGameMaster && invitedPlayer.IsGameMaster)
        {
            SendPartyResult(PartyOperation.Invite, invitedPlayer.GetName(), PartyResult.BadPlayerNameS);

            return;
        }

        // can't group with
        if (!invitingPlayer.IsGameMaster && !_config.GetValue("AllowTwoSide.Interaction.Group", false) && invitingPlayer.Team != invitedPlayer.Team)
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

        if (!invitedPlayer.Social.HasFriend(invitingPlayer.GUID) && invitingPlayer.Level < _config.GetValue("PartyLevelReq", 1))
        {
            SendPartyResult(PartyOperation.Invite, invitedPlayer.GetName(), PartyResult.InviteRestricted);

            return;
        }

        var group = invitingPlayer.Group;

        if (group is { IsBGGroup: true })
            group = invitingPlayer.OriginalGroup;

        group ??= invitingPlayer.GroupInvite;

        var group2 = invitedPlayer.Group;

        if (group2 is { IsBGGroup: true })
            group2 = invitedPlayer.OriginalGroup;

        PartyInvite partyInvite;

        // player already in another group or invited
        if (group2 != null || invitedPlayer.GroupInvite != null)
        {
            SendPartyResult(PartyOperation.Invite, invitedPlayer.GetName(), PartyResult.AlreadyInGroupS);

            if (group2 == null)
                return;

            // tell the player that they were invited but it failed as they were already in a group
            partyInvite = new PartyInvite();
            partyInvite.Initialize(invitingPlayer, packet.ProposedRoles, false);
            invitedPlayer.SendPacket(partyInvite);

            return;
        }

        if (group != null)
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
            group = new PlayerGroup(_classFactory.Resolve<ScriptManager>(), _classFactory.Resolve<PlayerComputators>(),
                _classFactory.Resolve<CharacterDatabase>(), _objectAccessor, _classFactory.Resolve<CliDB>(), _classFactory.Resolve<DB2Manager>(),
                _classFactory.Resolve<BattlegroundManager>(), _classFactory.Resolve<GroupManager>(), _classFactory.Resolve<CharacterCache>(),
                _classFactory.Resolve<LFGManager>(), _classFactory.Resolve<GameObjectManager>());

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
    private void HandlePartyInviteResponse(PartyInviteResponse packet)
    {
        var group = _session.Player.GroupInvite;

        if (group == null)
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
                if (leader == null)
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

            if (leader == null || leader.Session == null)
                return;

            // report
            GroupDecline decline = new(_session.Player.GetName());
            leader.SendPacket(decline);
        }
    }

    [WorldPacketHandler(ClientOpcodes.PartyUninvite)]
    private void HandlePartyUninvite(PartyUninvite packet)
    {
        //can't uninvite yourself
        if (packet.TargetGUID == _session.Player.GUID)
        {
            Log.Logger.Error("HandleGroupUninviteGuidOpcode: leader {0}({1}) tried to uninvite himself from the group.",
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
            _session.Player.RemoveFromGroup(RemoveMethod.Kick);

            return;
        }

        var player = grp.GetInvited(packet.TargetGUID);

        if (player != null)
        {
            player.UninviteFromGroup();

            return;
        }

        SendPartyResult(PartyOperation.UnInvite, "", PartyResult.TargetNotInGroupS);
    }

    [WorldPacketHandler(ClientOpcodes.RandomRoll)]
    private void HandleRandomRoll(RandomRollClient packet)
    {
        if (packet.Min > packet.Max || packet.Max > 1000000) // < 32768 for urand call
            return;

        _session.Player.DoRandomRoll(packet.Min, packet.Max);
    }

    [WorldPacketHandler(ClientOpcodes.ReadyCheckResponse, Processing = PacketProcessing.Inplace)]
    private void HandleReadyCheckResponseOpcode(ReadyCheckResponseClient packet)
    {
        var group = _session.Player.Group;

        // everything's fine, do it
        group?.SetMemberReadyCheck(_session.Player.GUID, packet.IsReady);
    }

    [WorldPacketHandler(ClientOpcodes.RequestPartyJoinUpdates)]
    private void HandleRequestPartyJoinUpdates(RequestPartyJoinUpdates packet)
    {
        var group = _session.Player.Group;

        if (group == null)
            return;

        group.SendTargetIconList(_session, packet.PartyIndex);
        group.SendRaidMarkersChanged(_session, packet.PartyIndex);
    }

    [WorldPacketHandler(ClientOpcodes.RequestPartyMemberStats)]
    private void HandleRequestPartyMemberStats(RequestPartyMemberStats packet)
    {
        PartyMemberFullState partyMemberStats = new();

        var player = _objectAccessor.FindConnectedPlayer(packet.TargetGUID);

        if (player == null)
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
    private void HandleRequestRaidInfo(RequestRaidInfo packet)
    {
        if (packet == null)
            return;

        // every time the player checks the character screen
        _session.Player.SendRaidInfo();
    }

    [WorldPacketHandler(ClientOpcodes.SetAssistantLeader)]
    private void HandleSetAssistantLeader(SetAssistantLeader packet)
    {
        var group = _session.Player.Group;

        if (group == null)
            return;

        if (!group.IsLeader(_session.Player.GUID))
            return;

        group.SetGroupMemberFlag(packet.Target, packet.Apply, GroupMemberFlags.Assistant);
    }

    [WorldPacketHandler(ClientOpcodes.SetEveryoneIsAssistant)]
    private void HandleSetEveryoneIsAssistant(SetEveryoneIsAssistant packet)
    {
        var group = _session.Player.Group;

        if (group == null)
            return;

        if (!group.IsLeader(_session.Player.GUID))
            return;

        group.SetEveryoneIsAssistant(packet.EveryoneIsAssistant);
    }

    [WorldPacketHandler(ClientOpcodes.SetLootMethod)]
    private void HandleSetLootMethod(SetLootMethod packet)
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

        if (packet.LootThreshold is < ItemQuality.Uncommon or > ItemQuality.Artifact)
            return;

        if (packet.LootMethod == LootMethod.MasterLoot && !group.IsMember(packet.LootMasterGUID))
            return;

        // everything's fine, do it
        group.SetLootMethod(packet.LootMethod);
        group.SetMasterLooterGuid(packet.LootMasterGUID);
        group.SetLootThreshold(packet.LootThreshold);
        group.SendUpdate();
    }

    [WorldPacketHandler(ClientOpcodes.SetPartyAssignment)]
    private void HandleSetPartyAssignment(SetPartyAssignment packet)
    {
        var group = _session.Player.Group;

        if (group == null)
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

    [WorldPacketHandler(ClientOpcodes.SetPartyLeader, Processing = PacketProcessing.Inplace)]
    private void HandleSetPartyLeader(SetPartyLeader packet)
    {
        var player = _objectAccessor.FindConnectedPlayer(packet.TargetGUID);
        var group = _session.Player.Group;

        if (group == null || player == null)
            return;

        if (!group.IsLeader(_session.Player.GUID) || player.Group != group)
            return;

        // Everything's fine, accepted.
        group.ChangeLeader(packet.TargetGUID, packet.PartyIndex);
        group.SendUpdate();
    }

    [WorldPacketHandler(ClientOpcodes.SetRole)]
    private void HandleSetRole(SetRole packet)
    {
        RoleChangedInform roleChangedInform = new();

        var group = _session.Player.Group;
        var oldRole = (byte)(group?.GetLfgRoles(packet.TargetGUID) ?? 0);

        if (oldRole == packet.Role)
            return;

        roleChangedInform.PartyIndex = packet.PartyIndex;
        roleChangedInform.From = _session.Player.GUID;
        roleChangedInform.ChangedUnit = packet.TargetGUID;
        roleChangedInform.OldRole = oldRole;
        roleChangedInform.NewRole = packet.Role;

        if (group != null)
        {
            group.BroadcastPacket(roleChangedInform, false);
            group.SetLfgRoles(packet.TargetGUID, (LfgRoles)packet.Role);
        }
        else
        {
            _session.SendPacket(roleChangedInform);
        }
    }

    [WorldPacketHandler(ClientOpcodes.SwapSubGroups, Processing = PacketProcessing.ThreadUnsafe)]
    private void HandleSwapSubGroups(SwapSubGroups packet)
    {
        var group = _session.Player.Group;

        if (group == null)
            return;

        var senderGuid = _session.Player.GUID;

        if (!group.IsLeader(senderGuid) && !group.IsAssistant(senderGuid))
            return;

        group.SwapMembersGroups(packet.FirstTarget, packet.SecondTarget);
    }

    [WorldPacketHandler(ClientOpcodes.UpdateRaidTarget)]
    private void HandleUpdateRaidTarget(UpdateRaidTarget packet)
    {
        var group = _session.Player.Group;

        if (group == null)
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

                if (target == null || target.WorldObjectCombat.IsHostileTo(_session.Player))
                    return;
            }

            group.SetTargetIcon((byte)packet.Symbol, packet.Target, _session.Player.GUID, packet.PartyIndex);
        }
    }
}