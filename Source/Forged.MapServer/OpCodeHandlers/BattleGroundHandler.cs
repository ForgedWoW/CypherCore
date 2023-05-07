// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.Arenas;
using Forged.MapServer.BattleFields;
using Forged.MapServer.BattleGrounds;
using Forged.MapServer.Conditions;
using Forged.MapServer.DataStorage;
using Forged.MapServer.DataStorage.ClientReader;
using Forged.MapServer.DataStorage.Structs.A;
using Forged.MapServer.DataStorage.Structs.B;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Globals;
using Forged.MapServer.Networking;
using Forged.MapServer.Networking.Packets.BattleGround;
using Forged.MapServer.Networking.Packets.NPC;
using Forged.MapServer.Server;
using Framework.Constants;
using Game.Common.Handlers;
using Serilog;
// ReSharper disable UnusedMember.Local

namespace Forged.MapServer.OpCodeHandlers;

public class BattleGroundHandler : IWorldSessionHandler
{
    private readonly WorldSession _worldSession;
    private readonly BattlegroundManager _battlegroundManager;
    private readonly BattleFieldManager _battleFieldManager;
    private readonly DB6Storage<BattlemasterListRecord> _battlemasterListRecords;
    private readonly DB2Manager _db2Manager;
    private readonly ArenaTeamManager _arenaTeamManager;
    private readonly DisableManager _disableManager;
    private readonly DB6Storage<AreaTableRecord> _areaTableRecords;
    private readonly ObjectAccessor _objectAccessor;

    public BattleGroundHandler(WorldSession worldSession, BattlegroundManager battlegroundManager, BattleFieldManager battleFieldManager, DB6Storage<BattlemasterListRecord> battlemasterListRecords, 
                               DB2Manager db2Manager, ArenaTeamManager arenaTeamManager, DisableManager disableManager, DB6Storage<AreaTableRecord> areaTableRecords, ObjectAccessor objectAccessor)
    {
        _worldSession = worldSession;
        _battlegroundManager = battlegroundManager;
        _battleFieldManager = battleFieldManager;
        _battlemasterListRecords = battlemasterListRecords;
        _db2Manager = db2Manager;
        _arenaTeamManager = arenaTeamManager;
        _disableManager = disableManager;
        _areaTableRecords = areaTableRecords;
        _objectAccessor = objectAccessor;
    }

    [WorldPacketHandler(ClientOpcodes.AreaSpiritHealerQuery)]
    private void HandleAreaSpiritHealerQuery(AreaSpiritHealerQuery areaSpiritHealerQuery)
    {
        var unit = ObjectAccessor.GetCreature(_worldSession.Player, areaSpiritHealerQuery.HealerGuid);

        if (unit is not { IsSpiritService: true })
            return;

        var bg = _worldSession.Player.Battleground;

        if (bg != null)
            _battlegroundManager.SendAreaSpiritHealerQuery(_worldSession.Player, bg, areaSpiritHealerQuery.HealerGuid);

        _battleFieldManager.GetBattlefieldToZoneId(_worldSession.Player.Location.Map, _worldSession.Player.Location.Zone)?.SendAreaSpiritHealerQuery(_worldSession.Player, areaSpiritHealerQuery.HealerGuid);
    }

    [WorldPacketHandler(ClientOpcodes.AreaSpiritHealerQueue)]
    private void HandleAreaSpiritHealerQueue(AreaSpiritHealerQueue areaSpiritHealerQueue)
    {
        var unit = ObjectAccessor.GetCreature(_worldSession.Player, areaSpiritHealerQueue.HealerGuid);

        if (unit is not { IsSpiritService: true })
            return;

        _worldSession.Player.Battleground?.AddPlayerToResurrectQueue(areaSpiritHealerQueue.HealerGuid, _worldSession.Player.GUID);
        _battleFieldManager.GetBattlefieldToZoneId(_worldSession.Player.Location.Map, _worldSession.Player.Location.Zone)?.AddPlayerToResurrectQueue(areaSpiritHealerQueue.HealerGuid, _worldSession.Player.GUID);
    }

    [WorldPacketHandler(ClientOpcodes.BattlefieldLeave)]
    private void HandleBattlefieldLeave(BattlefieldLeave packet)
    {
        // not allow leave Battlegroundin combat
        if (_worldSession.Player.IsInCombat && 
            _worldSession.Player.Battleground != null &&
            _worldSession.Player.Battleground.Status != BattlegroundStatus.WaitLeave)
            return;

        if (packet != null)
            _worldSession.Player.LeaveBattleground();
    }

    [WorldPacketHandler(ClientOpcodes.BattlefieldList)]
    private void HandleBattlefieldList(BattlefieldListRequest battlefieldList)
    {
        if (!_battlemasterListRecords.ContainsKey(battlefieldList.ListID))
        {
            Log.Logger.Debug("BattlegroundHandler: invalid bgtype ({0}) with _worldSession.Player (Name: {1}, GUID: {2}) received.", battlefieldList.ListID, _worldSession.Player.GetName(), _worldSession.Player.GUID.ToString());

            return;
        }

        _battlegroundManager.SendBattlegroundList(_worldSession.Player, ObjectGuid.Empty, (BattlegroundTypeId)battlefieldList.ListID);
    }

    [WorldPacketHandler(ClientOpcodes.BattlefieldPort)]
    private void HandleBattleFieldPort(BattlefieldPort battlefieldPort)
    {
        if (!_worldSession.Player.InBattlegroundQueue())
        {
            Log.Logger.Debug("CMSG_BATTLEFIELD_PORT {0} Slot: {1}, Unk: {2}, Time: {3}, AcceptedInvite: {4}. _worldSession.Player not in queue!",
                             _worldSession.GetPlayerInfo(),
                             battlefieldPort.Ticket.Id,
                             battlefieldPort.Ticket.Type,
                             battlefieldPort.Ticket.Time,
                             battlefieldPort.AcceptedInvite);

            return;
        }

        var bgQueueTypeId = _worldSession.Player.GetBattlegroundQueueTypeId(battlefieldPort.Ticket.Id);

        if (bgQueueTypeId == default)
        {
            Log.Logger.Debug("CMSG_BATTLEFIELD_PORT {0} Slot: {1}, Unk: {2}, Time: {3}, AcceptedInvite: {4}. Invalid queueSlot!",
                             _worldSession.GetPlayerInfo(),
                             battlefieldPort.Ticket.Id,
                             battlefieldPort.Ticket.Type,
                             battlefieldPort.Ticket.Time,
                             battlefieldPort.AcceptedInvite);

            return;
        }

        var bgQueue = _battlegroundManager.GetBattlegroundQueue(bgQueueTypeId);

        //we must use temporary variable, because GroupQueueInfo pointer can be deleted in BattlegroundQueue.RemovePlayer() function
        if (!bgQueue.GetPlayerGroupInfoData(_worldSession.Player.GUID, out var ginfo))
        {
            Log.Logger.Debug("CMSG_BATTLEFIELD_PORT {0} Slot: {1}, Unk: {2}, Time: {3}, AcceptedInvite: {4}. _worldSession.Player not in queue (No _worldSession.Player Group Info)!",
                             _worldSession.GetPlayerInfo(),
                             battlefieldPort.Ticket.Id,
                             battlefieldPort.Ticket.Type,
                             battlefieldPort.Ticket.Time,
                             battlefieldPort.AcceptedInvite);

            return;
        }

        // if action == 1, then instanceId is required
        if (ginfo.IsInvitedToBGInstanceGUID == 0 && battlefieldPort.AcceptedInvite)
        {
            Log.Logger.Debug("CMSG_BATTLEFIELD_PORT {0} Slot: {1}, Unk: {2}, Time: {3}, AcceptedInvite: {4}. _worldSession.Player is not invited to any bg!",
                             _worldSession.GetPlayerInfo(),
                             battlefieldPort.Ticket.Id,
                             battlefieldPort.Ticket.Type,
                             battlefieldPort.Ticket.Time,
                             battlefieldPort.AcceptedInvite);

            return;
        }

        var bgTypeId = (BattlegroundTypeId)bgQueueTypeId.BattlemasterListId;
        // BGTemplateId returns Battleground_AA when it is arena queue.
        // Do instance id search as there is no AA bg instances.
        var bg = _battlegroundManager.GetBattleground(ginfo.IsInvitedToBGInstanceGUID, bgTypeId == BattlegroundTypeId.Aa ? BattlegroundTypeId.None : bgTypeId);

        if (bg == null)
        {
            if (battlefieldPort.AcceptedInvite)
            {
                Log.Logger.Debug("CMSG_BATTLEFIELD_PORT {0} Slot: {1}, Unk: {2}, Time: {3}, AcceptedInvite: {4}. Cant find BG with id {5}!",
                                 _worldSession.GetPlayerInfo(),
                                 battlefieldPort.Ticket.Id,
                                 battlefieldPort.Ticket.Type,
                                 battlefieldPort.Ticket.Time,
                                 battlefieldPort.AcceptedInvite,
                                 ginfo.IsInvitedToBGInstanceGUID);

                return;
            }

            bg = _battlegroundManager.GetBattlegroundTemplate(bgTypeId);

            if (bg == null)
            {
                Log.Logger.Error("BattlegroundHandler: bg_template not found for type id {0}.", bgTypeId);

                return;
            }
        }

        // get real bg type
        bgTypeId = bg.GetTypeID();

        // expected bracket entry
        var bracketEntry = _db2Manager.GetBattlegroundBracketByLevel(bg.MapId, _worldSession.Player.Level);

        if (bracketEntry == null)
            return;

        //some checks if _worldSession.Player isn't cheating - it is not exactly cheating, but we cannot allow it
        if (battlefieldPort.AcceptedInvite && bgQueue.GetQueueId().TeamSize == 0)
        {
            //if _worldSession.Player is trying to enter Battleground(not arena!) and he has deserter debuff, we must just remove him from queue
            if (!_worldSession.Player.IsDeserter())
            {
                // send bg command result to show nice message
                _battlegroundManager.BuildBattlegroundStatusFailed(out var battlefieldStatus, bgQueueTypeId, _worldSession.Player, battlefieldPort.Ticket.Id, GroupJoinBattlegroundResult.Deserters);
                _worldSession.SendPacket(battlefieldStatus);
                battlefieldPort.AcceptedInvite = false;
                Log.Logger.Debug("_worldSession.Player {0} ({1}) has a deserter debuff, do not port him to Battleground!", _worldSession.Player.GetName(), _worldSession.Player.GUID.ToString());
            }

            //if _worldSession.Player don't match Battlegroundmax level, then do not allow him to enter! (this might happen when _worldSession.Player leveled up during his waiting in queue
            if (_worldSession.Player.Level > bg.MaxLevel)
            {
                Log.Logger.Debug("_worldSession.Player {0} ({1}) has level ({2}) higher than maxlevel ({3}) of Battleground({4})! Do not port him to Battleground!",
                                 _worldSession.Player.GetName(),
                                 _worldSession.Player.GUID.ToString(),
                                 _worldSession.Player.Level,
                                 bg.MaxLevel,
                                 bg.GetTypeID());

                battlefieldPort.AcceptedInvite = false;
            }
        }

        if (battlefieldPort.AcceptedInvite)
        {
            // check Freeze debuff
            if (_worldSession.Player.HasAura(9454))
                return;

            if (!_worldSession.Player.IsInvitedForBattlegroundQueueType(bgQueueTypeId))
                return; // cheating?

            if (!_worldSession.Player.InBattleground)
                _worldSession.Player.SetBattlegroundEntryPoint();

            // resurrect the _worldSession.Player
            if (!_worldSession.Player.IsAlive)
            {
                _worldSession.Player.ResurrectPlayer(1.0f);
                _worldSession.Player.SpawnCorpseBones();
            }

            // stop taxi flight at port
            _worldSession.Player.FinishTaxiFlight();

            _battlegroundManager.BuildBattlegroundStatusActive(out var battlefieldStatus, bg, _worldSession.Player, battlefieldPort.Ticket.Id, _worldSession.Player.GetBattlegroundQueueJoinTime(bgQueueTypeId), bg.ArenaType);
            _worldSession.SendPacket(battlefieldStatus);

            // remove BattlegroundQueue status from BGmgr
            bgQueue.RemovePlayer(_worldSession.Player.GUID, false);
            // this is still needed here if Battleground"jumping" shouldn't add deserter debuff
            // also this is required to prevent stuck at old Battlegroundafter SetBattlegroundId set to new
            _worldSession.Player.Battleground?.RemovePlayerAtLeave(_worldSession.Player.GUID, false, true);

            // set the destination instance id
            _worldSession.Player.SetBattlegroundId(bg.InstanceID, bgTypeId);
            // set the destination team
            _worldSession.Player.SetBgTeam(ginfo.Team);

            _battlegroundManager.SendToBattleground(_worldSession.Player, ginfo.IsInvitedToBGInstanceGUID, bgTypeId);
            Log.Logger.Debug($"Battleground: _worldSession.Player {_worldSession.Player.GetName()} ({_worldSession.Player.GUID}) joined battle for bg {bg.InstanceID}, bgtype {bg.GetTypeID()}, queue {bgQueueTypeId}.");
        }
        else // leave queue
        {
            // if _worldSession.Player leaves rated arena match before match start, it is counted as he played but he lost
            if (bgQueue.GetQueueId().Rated && ginfo.IsInvitedToBGInstanceGUID != 0)
            {
                var at = _arenaTeamManager.GetArenaTeamById((uint)ginfo.Team);

                if (at != null)
                {
                    Log.Logger.Debug("UPDATING memberLost's personal arena rating for {0} by opponents rating: {1}, because he has left queue!", _worldSession.Player.GUID.ToString(), ginfo.OpponentsTeamRating);
                    at.MemberLost(_worldSession.Player, ginfo.OpponentsMatchmakerRating);
                    at.SaveToDB();
                }
            }

            BattlefieldStatusNone battlefieldStatus = new();
            battlefieldStatus.Ticket = battlefieldPort.Ticket;
            _worldSession.SendPacket(battlefieldStatus);

            _worldSession.Player.RemoveBattlegroundQueueId(bgQueueTypeId); // must be called this way, because if you move this call to queue.removeplayer, it causes bugs
            bgQueue.RemovePlayer(_worldSession.Player.GUID, true);

            // _worldSession.Player left queue, we should update it - do not update Arena Queue
            if (bgQueue.GetQueueId().TeamSize == 0)
                _battlegroundManager.ScheduleQueueUpdate(ginfo.ArenaMatchmakerRating, bgQueueTypeId, bracketEntry.BracketId);

            Log.Logger.Debug($"Battleground: _worldSession.Player {_worldSession.Player.GetName()} ({_worldSession.Player.GUID}) left queue for bgtype {bg.GetTypeID()}, queue {bgQueueTypeId}.");
        }
    }

    [WorldPacketHandler(ClientOpcodes.BattlemasterHello)]
    private void HandleBattlemasterHello(Hello hello)
    {
        var unit = _worldSession.Player.GetNPCIfCanInteractWith(hello.Unit, NPCFlags.BattleMaster, NPCFlags2.None);

        if (unit == null)
            return;

        // Stop the npc if moving
        var pause = unit.MovementTemplate.InteractionPauseTimer;

        if (pause != 0)
            unit.PauseMovement(pause);

        unit.HomePosition = unit.Location;

        var bgTypeId = _battlegroundManager.GetBattleMasterBG(unit.Entry);

        if (!_worldSession.Player.GetBgAccessByLevel(bgTypeId))
        {
            // temp, must be gossip message...
            _worldSession.SendNotification(CypherStrings.YourBgLevelReqError);

            return;
        }

        _battlegroundManager.SendBattlegroundList(_worldSession.Player, hello.Unit, bgTypeId);
    }

    [WorldPacketHandler(ClientOpcodes.BattlemasterJoin)]
    private void HandleBattlemasterJoin(BattlemasterJoin battlemasterJoin)
    {
        var isPremade = false;

        if (battlemasterJoin.QueueIDs.Empty())
        {
            Log.Logger.Error($"Battleground: no bgtype received. possible cheater? {_worldSession.Player.GUID}");

            return;
        }

        var bgQueueTypeId = BattlegroundQueueTypeId.FromPacked(battlemasterJoin.QueueIDs[0]);

        if (!_battlegroundManager.IsValidQueueId(bgQueueTypeId))
        {
            Log.Logger.Error($"Battleground: invalid bg queue {bgQueueTypeId} received. possible cheater? {_worldSession.Player.GUID}");

            return;
        }

        var battlemasterListEntry = _battlemasterListRecords.LookupByKey(bgQueueTypeId.BattlemasterListId);

        if (_disableManager.IsDisabledFor(DisableType.Battleground, bgQueueTypeId.BattlemasterListId, null) || battlemasterListEntry.Flags.HasAnyFlag(BattlemasterListFlags.Disabled))
        {
            _worldSession.Player.SendSysMessage(CypherStrings.BgDisabled);

            return;
        }

        var bgTypeId = (BattlegroundTypeId)bgQueueTypeId.BattlemasterListId;

        // ignore if _worldSession.Player is already in BG
        if (_worldSession.Player.InBattleground)
            return;

        // get bg instance or bg template if instance not found
        var bg = _battlegroundManager.GetBattlegroundTemplate(bgTypeId);

        if (bg == null)
            return;

        // expected bracket entry
        var bracketEntry = _db2Manager.GetBattlegroundBracketByLevel(bg.MapId, _worldSession.Player.Level);

        if (bracketEntry == null)
            return;

        var err = GroupJoinBattlegroundResult.None;

        var grp = _worldSession.Player.Group;

        TeamFaction GetQueueTeam()
        {
            // mercenary applies only to unrated battlegrounds
            if (bg.IsRated || bg.IsArena)
                return _worldSession.Player.Team;

            if (_worldSession.Player.HasAura(BattlegroundConst.SPELL_MERCENARY_CONTRACT_HORDE))
                return TeamFaction.Horde;

            return _worldSession.Player.HasAura(BattlegroundConst.SPELL_MERCENARY_CONTRACT_ALLIANCE) ? TeamFaction.Alliance : _worldSession.Player.Team;
        }

        // check queue conditions
        if (grp == null)
        {
            BattlefieldStatusFailed battlefieldStatusFailed;

            if (_worldSession.Player.IsUsingLfg)
            {
                _battlegroundManager.BuildBattlegroundStatusFailed(out battlefieldStatusFailed, bgQueueTypeId, _worldSession.Player, 0, GroupJoinBattlegroundResult.LfgCantUseBattleground);
                _worldSession.SendPacket(battlefieldStatusFailed);

                return;
            }

            // check RBAC permissions
            if (!_worldSession.Player.CanJoinToBattleground(bg))
            {
                _battlegroundManager.BuildBattlegroundStatusFailed(out battlefieldStatusFailed, bgQueueTypeId, _worldSession.Player, 0, GroupJoinBattlegroundResult.JoinTimedOut);
                _worldSession.SendPacket(battlefieldStatusFailed);

                return;
            }

            // check Deserter debuff
            if (_worldSession.Player.IsDeserter())
            {
                _battlegroundManager.BuildBattlegroundStatusFailed(out battlefieldStatusFailed, bgQueueTypeId, _worldSession.Player, 0, GroupJoinBattlegroundResult.Deserters);
                _worldSession.SendPacket(battlefieldStatusFailed);

                return;
            }

            var isInRandomBgQueue = _worldSession.Player.InBattlegroundQueueForBattlegroundQueueType(_battlegroundManager.BGQueueTypeId((ushort)BattlegroundTypeId.Rb, BattlegroundQueueIdType.Battleground, false, 0)) || _worldSession.Player.InBattlegroundQueueForBattlegroundQueueType(_battlegroundManager.BGQueueTypeId((ushort)BattlegroundTypeId.RandomEpic, BattlegroundQueueIdType.Battleground, false, 0));

            if (bgTypeId != BattlegroundTypeId.Rb && bgTypeId != BattlegroundTypeId.RandomEpic && isInRandomBgQueue)
            {
                // _worldSession.Player is already in random queue
                _battlegroundManager.BuildBattlegroundStatusFailed(out battlefieldStatusFailed, bgQueueTypeId, _worldSession.Player, 0, GroupJoinBattlegroundResult.InRandomBg);
                _worldSession.SendPacket(battlefieldStatusFailed);

                return;
            }

            if (_worldSession.Player.InBattlegroundQueue(true) && !isInRandomBgQueue && bgTypeId is BattlegroundTypeId.Rb or BattlegroundTypeId.RandomEpic)
            {
                // _worldSession.Player is already in queue, can't start random queue
                _battlegroundManager.BuildBattlegroundStatusFailed(out battlefieldStatusFailed, bgQueueTypeId, _worldSession.Player, 0, GroupJoinBattlegroundResult.InNonRandomBg);
                _worldSession.SendPacket(battlefieldStatusFailed);

                return;
            }

            // check if already in queue
            if (_worldSession.Player.GetBattlegroundQueueIndex(bgQueueTypeId) < SharedConst.MaxPlayerBGQueues)
                return; // _worldSession.Player is already in this queue

            // check if has free queue slots
            if (!_worldSession.Player.HasFreeBattlegroundQueueId)
            {
                _battlegroundManager.BuildBattlegroundStatusFailed(out battlefieldStatusFailed, bgQueueTypeId, _worldSession.Player, 0, GroupJoinBattlegroundResult.TooManyQueues);
                _worldSession.SendPacket(battlefieldStatusFailed);

                return;
            }

            // check Freeze debuff
            if (_worldSession.Player.HasAura(9454))
                return;

            var bgQueue = _battlegroundManager.GetBattlegroundQueue(bgQueueTypeId);
            var ginfo = bgQueue.AddGroup(_worldSession.Player, null, GetQueueTeam(), bracketEntry, false, 0, 0);

            var avgTime = bgQueue.GetAverageQueueWaitTime(ginfo, bracketEntry.BracketId);
            var queueSlot = _worldSession.Player.AddBattlegroundQueueId(bgQueueTypeId);

            _battlegroundManager.BuildBattlegroundStatusQueued(out var battlefieldStatusQueued, bg, _worldSession.Player, queueSlot, ginfo.JoinTime, bgQueueTypeId, avgTime, 0, false);
            _worldSession.SendPacket(battlefieldStatusQueued);

            Log.Logger.Debug($"Battleground: _worldSession.Player joined queue for bg queue {bgQueueTypeId}, {_worldSession.Player.GUID}, NAME {_worldSession.Player.GetName()}");
        }
        else
        {
            if (grp.LeaderGUID != _worldSession.Player.GUID)
                return;

            err = grp.CanJoinBattlegroundQueue(bg, bgQueueTypeId, 0, bg.GetMaxPlayersPerTeam(), false, 0, out var errorGuid);
            isPremade = grp.MembersCount >= bg.MinPlayersPerTeam;

            var bgQueue = _battlegroundManager.GetBattlegroundQueue(bgQueueTypeId);
            GroupQueueInfo ginfo = null;
            uint avgTime = 0;

            if (err == 0)
            {
                Log.Logger.Debug("Battleground: the following players are joining as group:");
                ginfo = bgQueue.AddGroup(_worldSession.Player, grp, GetQueueTeam(), bracketEntry, isPremade, 0, 0);
                avgTime = bgQueue.GetAverageQueueWaitTime(ginfo, bracketEntry.BracketId);
            }

            if (ginfo == null)
            {
                Log.Logger.Error("Battleground: ginfo not found");

                return;
            }

            for (var refe = grp.FirstMember; refe != null; refe = refe.Next())
            {
                var member = refe.Source;

                if (member == null)
                    continue; // this should never happen

                if (err != 0)
                {
                    _battlegroundManager.BuildBattlegroundStatusFailed(out var battlefieldStatus, bgQueueTypeId, _worldSession.Player, 0, err, errorGuid);
                    member.SendPacket(battlefieldStatus);

                    continue;
                }

                // add to queue
                var queueSlot = member.AddBattlegroundQueueId(bgQueueTypeId);

                _battlegroundManager.BuildBattlegroundStatusQueued(out var battlefieldStatusQueued, bg, member, queueSlot, ginfo.JoinTime, bgQueueTypeId, avgTime, 0, true);
                member.SendPacket(battlefieldStatusQueued);
                Log.Logger.Debug($"Battleground: _worldSession.Player joined queue for bg queue {bgQueueTypeId}, {member.GUID}, NAME {member.GetName()}");
            }

            Log.Logger.Debug("Battleground: group end");
        }

        _battlegroundManager.ScheduleQueueUpdate(0, bgQueueTypeId, bracketEntry.BracketId);
    }

    [WorldPacketHandler(ClientOpcodes.BattlemasterJoinArena)]
    private void HandleBattlemasterJoinArena(BattlemasterJoinArena packet)
    {
        // ignore if we already in BG or BG queue
        if (_worldSession.Player.InBattleground)
            return;

        var arenatype = (ArenaTypes)ArenaTeam.GetTypeBySlot(packet.TeamSizeIndex);

        //check existence
        var bg = _battlegroundManager.GetBattlegroundTemplate(BattlegroundTypeId.Aa);

        if (bg == null)
        {
            Log.Logger.Error("Battleground: template bg (all arenas) not found");

            return;
        }

        if (_disableManager.IsDisabledFor(DisableType.Battleground, (uint)BattlegroundTypeId.Aa, null))
        {
            _worldSession.Player.SendSysMessage(CypherStrings.ArenaDisabled);

            return;
        }

        var bgTypeId = bg.GetTypeID();
        var bgQueueTypeId = _battlegroundManager.BGQueueTypeId((ushort)bgTypeId, BattlegroundQueueIdType.Arena, true, arenatype);
        var bracketEntry = _db2Manager.GetBattlegroundBracketByLevel(bg.MapId, _worldSession.Player.Level);

        if (bracketEntry == null)
            return;

        var grp = _worldSession.Player.Group;

        // no group found, error
        if (grp == null)
            return;

        if (grp.LeaderGUID != _worldSession.Player.GUID)
            return;

        var ateamId = _worldSession.Player.GetArenaTeamId(packet.TeamSizeIndex);
        // check real arenateam existence only here (if it was moved to group.CanJoin .. () then we would ahve to get it twice)
        var at = _arenaTeamManager.GetArenaTeamById(ateamId);

        if (at == null)
            return;

        // get the team rating for queuing
        var arenaRating = at.GetRating();
        var matchmakerRating = at.GetAverageMmr(grp);
        // the arenateam id must match for everyone in the group

        if (arenaRating <= 0)
            arenaRating = 1;

        var bgQueue = _battlegroundManager.GetBattlegroundQueue(bgQueueTypeId);

        uint avgTime = 0;
        GroupQueueInfo ginfo = null;

        var err = grp.CanJoinBattlegroundQueue(bg, bgQueueTypeId, (uint)arenatype, (uint)arenatype, true, packet.TeamSizeIndex, out var errorGuid);

        if (err == 0)
        {
            Log.Logger.Debug("Battleground: arena team id {0}, leader {1} queued with matchmaker rating {2} for type {3}", _worldSession.Player.GetArenaTeamId(packet.TeamSizeIndex), _worldSession.Player.GetName(), matchmakerRating, arenatype);

            ginfo = bgQueue.AddGroup(_worldSession.Player, grp, _worldSession.Player.Team, bracketEntry, false, arenaRating, matchmakerRating, ateamId);
            avgTime = bgQueue.GetAverageQueueWaitTime(ginfo, bracketEntry.BracketId);
        }

        if (ginfo == null)
        {
            Log.Logger.Error("Battleground: ginfo not found");

            return;
        }

        for (var refe = grp.FirstMember; refe != null; refe = refe.Next())
        {
            var member = refe.Source;

            if (member == null)
                continue;

            if (err != 0)
            {
                _battlegroundManager.BuildBattlegroundStatusFailed(out var battlefieldStatus, bgQueueTypeId, _worldSession.Player, 0, err, errorGuid);
                member.SendPacket(battlefieldStatus);

                continue;
            }

            if (!_worldSession.Player.CanJoinToBattleground(bg))
            {
                _battlegroundManager.BuildBattlegroundStatusFailed(out var battlefieldStatus, bgQueueTypeId, _worldSession.Player, 0, GroupJoinBattlegroundResult.BattlegroundJoinFailed, errorGuid);
                member.SendPacket(battlefieldStatus);

                return;
            }

            // add to queue
            var queueSlot = member.AddBattlegroundQueueId(bgQueueTypeId);

            _battlegroundManager.BuildBattlegroundStatusQueued(out var battlefieldStatusQueued, bg, member, queueSlot, ginfo.JoinTime, bgQueueTypeId, avgTime, arenatype, true);
            member.SendPacket(battlefieldStatusQueued);

            Log.Logger.Debug($"Battleground: _worldSession.Player joined queue for arena as group bg queue {bgQueueTypeId}, {member.GUID}, NAME {member.GetName()}");
        }

        _battlegroundManager.ScheduleQueueUpdate(matchmakerRating, bgQueueTypeId, bracketEntry.BracketId);
    }

    [WorldPacketHandler(ClientOpcodes.HearthAndResurrect)]
    private void HandleHearthAndResurrect(HearthAndResurrect packet)
    {
        if (_worldSession.Player.IsInFlight || packet == null)
            return;

        var bf = _battleFieldManager.GetBattlefieldToZoneId(_worldSession.Player.Location.Map, _worldSession.Player.Location.Zone);

        if (bf != null)
        {
            bf.PlayerAskToLeave(_worldSession.Player);

            return;
        }

        var atEntry = _areaTableRecords.LookupByKey(_worldSession.Player.Location.Area);

        if (atEntry == null || !atEntry.HasFlag(AreaFlags.CanHearthAndResurrect))
            return;

        _worldSession.Player.BuildPlayerRepop();
        _worldSession.Player.ResurrectPlayer(1.0f);
        _worldSession.Player.TeleportTo(_worldSession.Player.Homebind);
    }

    [WorldPacketHandler(ClientOpcodes.BattlemasterJoinSkirmish)]
    private void HandleJoinSkirmish(JoinSkirmish packet)
    {
        if (_worldSession.Player == null)
            return;

        var arenatype = packet.Bracket == BracketType.Skirmish3 ? ArenaTypes.Team3V3 : ArenaTypes.Team2V2;

        var bg = _battlegroundManager.GetBattlegroundTemplate(BattlegroundTypeId.Aa);

        if (bg == null)
            return;

        TeamFaction GetQueueTeam()
        {
            // mercenary applies only to unrated battlegrounds
            if (bg.IsRated || bg.IsArena)
                return _worldSession.Player.Team;

            if (_worldSession.Player.HasAura(193472)) // SPELL_MERCENARY_CONTRACT_HORDE
                return TeamFaction.Horde;

            return _worldSession.Player.HasAura(193475) ? // SPELL_MERCENARY_CONTRACT_ALLIANCE
                       TeamFaction.Alliance : _worldSession.Player.Team;
        }

        if (_disableManager.IsDisabledFor(DisableType.Battleground, (uint)BattlegroundTypeId.Aa, null))
        {
            _worldSession.Player.SendSysMessage(CypherStrings.ArenaDisabled);

            return;
        }

        var bgTypeId = bg.GetTypeID();
        var bgQueueTypeId = _battlegroundManager.BGQueueTypeId((ushort)bgTypeId, BattlegroundQueueIdType.Arena, true, arenatype);

        if (_worldSession.Player.InBattleground)
            return;

        var bracketEntry = _db2Manager.GetBattlegroundBracketByLevel(bg.MapId, _worldSession.Player.Level);

        if (bracketEntry == null)
            return;

        if (!packet.JoinAsGroup)
        {
            if (_worldSession.Player.IsUsingLfg)
            {
                _battlegroundManager.BuildBattlegroundStatusFailed(out var battlefieldStatusFailed, bgQueueTypeId, _worldSession.Player, 0, GroupJoinBattlegroundResult.LfgCantUseBattleground);
                _worldSession.SendPacket(battlefieldStatusFailed);

                return;
            }

            if (!_worldSession.Player.CanJoinToBattleground(bg))
            {
                _battlegroundManager.BuildBattlegroundStatusFailed(out var battlefieldStatusFailed, bgQueueTypeId, _worldSession.Player, 0, GroupJoinBattlegroundResult.LfgCantUseBattleground);
                _worldSession.SendPacket(battlefieldStatusFailed);

                return;
            }

            if (_worldSession.Player.GetBattlegroundQueueIndex(bgQueueTypeId) < 2)
                return;

            if (!_worldSession.Player.HasFreeBattlegroundQueueId)
            {
                _battlegroundManager.BuildBattlegroundStatusFailed(out var battlefieldStatusFailed, bgQueueTypeId, _worldSession.Player, 0, GroupJoinBattlegroundResult.LfgCantUseBattleground);
                _worldSession.SendPacket(battlefieldStatusFailed);

                return;
            }

            var bgQueue = _battlegroundManager.GetBattlegroundQueue(bgQueueTypeId);
            var ginfo = bgQueue.AddGroup(_worldSession.Player, null, GetQueueTeam(), bracketEntry, false, 0, 0);

            var avgTime = bgQueue.GetAverageQueueWaitTime(ginfo, bracketEntry.BracketId);
            var queueSlot = _worldSession.Player.AddBattlegroundQueueId(bgQueueTypeId);
            _battlegroundManager.BuildBattlegroundStatusQueued(out var battlefieldStatus, bg, _worldSession.Player, queueSlot, ginfo.JoinTime, bgQueueTypeId, avgTime, arenatype, false);
            _worldSession.SendPacket(battlefieldStatus);
        }
        else
        {
            var grp = _worldSession.Player.Group;

            if (grp == null)
            {
                _battlegroundManager.BuildBattlegroundStatusFailed(out var battlefieldStatuss, bgQueueTypeId, _worldSession.Player, 0, GroupJoinBattlegroundResult.LfgCantUseBattleground);
                _worldSession.Player.Session.SendPacket(battlefieldStatuss);

                return;
            }

            if (grp.LeaderGUID != _worldSession.Player.GUID)
            {
                _battlegroundManager.BuildBattlegroundStatusFailed(out var battlefieldStatuss, bgQueueTypeId, _worldSession.Player, 0, GroupJoinBattlegroundResult.LfgCantUseBattleground);
                _worldSession.Player.Session.SendPacket(battlefieldStatuss);

                return;
            }

            var err = grp.CanJoinBattlegroundQueue(bg, bgQueueTypeId, 0, bg.GetMaxPlayersPerTeam(), false, 0, out _);

            var bgQueue = _battlegroundManager.GetBattlegroundQueue(bgQueueTypeId);
            GroupQueueInfo ginfo = null;
            uint avgTime = 0;

            if (err == default)
            {
                ginfo = bgQueue.AddGroup(_worldSession.Player, null, GetQueueTeam(), bracketEntry, false, 0, 0);
                avgTime = bgQueue.GetAverageQueueWaitTime(ginfo, bracketEntry.BracketId);
            }

            if (ginfo == null)
            {
                Log.Logger.Error("Battleground: ginfo not found");
                return;
            }

            foreach (var slot in grp.MemberSlots)
            {
                var member = _objectAccessor.FindPlayer(slot.Guid);

                if (member == null)
                    continue;

                if (err != default)
                {
                    _battlegroundManager.BuildBattlegroundStatusFailed(out var battlefieldStatuss, bgQueueTypeId, _worldSession.Player, 0, GroupJoinBattlegroundResult.LfgCantUseBattleground);
                    member.Session.SendPacket(battlefieldStatuss);

                    continue;
                }

                _battlegroundManager.BuildBattlegroundStatusQueued(out var battlefieldStatus, bg, member, member.AddBattlegroundQueueId(bgQueueTypeId), ginfo.JoinTime, bgQueueTypeId, avgTime, 0, true);
                member.SendPacket(battlefieldStatus);
            }
        }

        _battlegroundManager.ScheduleQueueUpdate(0, bgQueueTypeId, bracketEntry.BracketId);
    }

    [WorldPacketHandler(ClientOpcodes.PvpLogData)]
    private void HandlePVPLogData(PVPLogDataRequest packet)
    {
        var bg = _worldSession.Player.Battleground;

        if (bg == null || packet == null)
            return;

        // Prevent players from sending BuildPvpLogDataPacket in an arena except for when sent in Battleground.EndBattleground.
        if (bg.IsArena)
            return;

        PVPMatchStatisticsMessage pvpMatchStatistics = new();
        bg.BuildPvPLogDataPacket(out pvpMatchStatistics.Data);
        _worldSession.SendPacket(pvpMatchStatistics);
    }

    [WorldPacketHandler(ClientOpcodes.ReportPvpPlayerAfk)]
    private void HandleReportPvPAFK(ReportPvPPlayerAFK reportPvPPlayerAFK)
    {
        var reportedPlayer = _objectAccessor.FindPlayer(reportPvPPlayerAFK.Offender);

        if (reportedPlayer == null)
        {
            Log.Logger.Debug("WorldSession.HandleReportPvPAFK: _worldSession.Player not found");

            return;
        }

        Log.Logger.Debug("WorldSession.HandleReportPvPAFK:  {0} [IP: {1}] reported {2}", _worldSession.Player.GetName(), _worldSession.Player.Session.RemoteAddress, reportedPlayer.GUID.ToString());

        reportedPlayer.ReportedAfkBy(_worldSession.Player);
    }

    [WorldPacketHandler(ClientOpcodes.RequestBattlefieldStatus)]
    private void HandleRequestBattlefieldStatus(RequestBattlefieldStatus packet)
    {
        // we must update all queues here

        for (byte i = 0; i < SharedConst.MaxPlayerBGQueues; ++i)
        {
            var bgQueueTypeId = _worldSession.Player.GetBattlegroundQueueTypeId(i);

            if (bgQueueTypeId == default)
                continue;

            var bgTypeId = (BattlegroundTypeId)bgQueueTypeId.BattlemasterListId;
            var arenaType = (ArenaTypes)bgQueueTypeId.TeamSize;
            var bg = _worldSession.Player.Battleground;

            if (bg != null && bg.QueueId == bgQueueTypeId && packet != null)
            {
                //i cannot check any variable from _worldSession.Player class because _worldSession.Player class doesn't know if _worldSession.Player is in 2v2 / 3v3 or 5v5 arena
                //so i must use bg pointer to get that information
                _battlegroundManager.BuildBattlegroundStatusActive(out var battlefieldStatus, bg, _worldSession.Player, i, _worldSession.Player.GetBattlegroundQueueJoinTime(bgQueueTypeId), arenaType);
                _worldSession.SendPacket(battlefieldStatus);

                continue;
            }

            //we are sending update to _worldSession.Player about queue - he can be invited there!
            //get GroupQueueInfo for queue status
            var bgQueue = _battlegroundManager.GetBattlegroundQueue(bgQueueTypeId);

            if (!bgQueue.GetPlayerGroupInfoData(_worldSession.Player.GUID, out var ginfo))
                continue;

            if (ginfo.IsInvitedToBGInstanceGUID != 0)
            {
                bg = _battlegroundManager.GetBattleground(ginfo.IsInvitedToBGInstanceGUID, bgTypeId);

                if (bg == null)
                    continue;

                _battlegroundManager.BuildBattlegroundStatusNeedConfirmation(out var battlefieldStatus, bg, _worldSession.Player, i, _worldSession.Player.GetBattlegroundQueueJoinTime(bgQueueTypeId), Time.GetMSTimeDiff(Time.MSTime, ginfo.RemoveInviteTime), arenaType);
                _worldSession.SendPacket(battlefieldStatus);
            }
            else
            {
                bg = _battlegroundManager.GetBattlegroundTemplate(bgTypeId);

                if (bg == null)
                    continue;

                // expected bracket entry
                var bracketEntry = _db2Manager.GetBattlegroundBracketByLevel(bg.MapId, _worldSession.Player.Level);

                if (bracketEntry == null)
                    continue;

                var avgTime = bgQueue.GetAverageQueueWaitTime(ginfo, bracketEntry.BracketId);
                _battlegroundManager.BuildBattlegroundStatusQueued(out var battlefieldStatus, bg, _worldSession.Player, i, _worldSession.Player.GetBattlegroundQueueJoinTime(bgQueueTypeId), bgQueueTypeId, avgTime, arenaType, ginfo.Players.Count > 1);
                _worldSession.SendPacket(battlefieldStatus);
            }
        }
    }

    [WorldPacketHandler(ClientOpcodes.RequestPvpRewards, Processing = PacketProcessing.Inplace)]
    private void HandleRequestPvpReward(RequestPVPRewards packet)
    {
        if (packet != null)
            _worldSession.Player.SendPvpRewards();
    }
}