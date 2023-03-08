// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Arenas;
using Game.BattleFields;
using Game.BattleGrounds;
using Game.DataStorage;
using Game.Entities;
using Game.Groups;
using Game.Networking;
using Game.Networking.Packets;

namespace Game
{
    public partial class WorldSession
    {
        [WorldPacketHandler(ClientOpcodes.BattlemasterHello)]
        void HandleBattlemasterHello(Hello hello)
        {
            Creature unit = Player.GetNPCIfCanInteractWith(hello.Unit, NPCFlags.BattleMaster, NPCFlags2.None);
            if (!unit)
                return;

            // Stop the npc if moving
            uint pause = unit.MovementTemplate.GetInteractionPauseTimer();
            if (pause != 0)
                unit.PauseMovement(pause);
            unit.            HomePosition = unit.Location;

            BattlegroundTypeId bgTypeId = Global.BattlegroundMgr.GetBattleMasterBG(unit.Entry);

            if (!Player.GetBgAccessByLevel(bgTypeId))
            {
                // temp, must be gossip message...
                SendNotification(CypherStrings.YourBgLevelReqError);
                return;
            }

            Global.BattlegroundMgr.SendBattlegroundList(Player, hello.Unit, bgTypeId);
        }

        [WorldPacketHandler(ClientOpcodes.BattlemasterJoin)]
        void HandleBattlemasterJoin(BattlemasterJoin battlemasterJoin)
        {
            bool isPremade = false;

            if (battlemasterJoin.QueueIDs.Empty())
            {
                Log.outError(LogFilter.Network, $"Battleground: no bgtype received. possible cheater? {_player.GUID}");
                return;
            }

            BattlegroundQueueTypeId bgQueueTypeId = BattlegroundQueueTypeId.FromPacked(battlemasterJoin.QueueIDs[0]);
            if (!Global.BattlegroundMgr.IsValidQueueId(bgQueueTypeId))
            {
                Log.outError(LogFilter.Network, $"Battleground: invalid bg queue {bgQueueTypeId} received. possible cheater? {_player.GUID}");
                return;
            }

            BattlemasterListRecord battlemasterListEntry = CliDB.BattlemasterListStorage.LookupByKey(bgQueueTypeId.BattlemasterListId);
            if (Global.DisableMgr.IsDisabledFor(DisableType.Battleground, bgQueueTypeId.BattlemasterListId, null) || battlemasterListEntry.Flags.HasAnyFlag(BattlemasterListFlags.Disabled))
            {
                Player.SendSysMessage(CypherStrings.BgDisabled);
                return;
            }

            BattlegroundTypeId bgTypeId = (BattlegroundTypeId)bgQueueTypeId.BattlemasterListId;

            // ignore if player is already in BG
            if (Player.InBattleground())
                return;

            // get bg instance or bg template if instance not found
            Battleground bg = Global.BattlegroundMgr.GetBattlegroundTemplate(bgTypeId);
            if (!bg)
                return;

            // expected bracket entry
            PvpDifficultyRecord bracketEntry = Global.DB2Mgr.GetBattlegroundBracketByLevel(bg.GetMapId(), Player.Level);
            if (bracketEntry == null)
                return;

            GroupJoinBattlegroundResult err = GroupJoinBattlegroundResult.None;

            PlayerGroup grp = _player.Group;

            TeamFaction getQueueTeam()
            {
                // mercenary applies only to unrated battlegrounds
                if (!bg.IsRated() && !bg.IsArena())
                {
                    if (_player.HasAura(BattlegroundConst.SpellMercenaryContractHorde))
                        return TeamFaction.Horde;

                    if (_player.HasAura(BattlegroundConst.SpellMercenaryContractAlliance))
                        return TeamFaction.Alliance;
                }

                return _player.Team;
            }

            BattlefieldStatusFailed battlefieldStatusFailed;
            // check queue conditions
            if (grp == null)
            {
                if (Player.IsUsingLfg)
                {
                    Global.BattlegroundMgr.BuildBattlegroundStatusFailed(out battlefieldStatusFailed, bgQueueTypeId, Player, 0, GroupJoinBattlegroundResult.LfgCantUseBattleground);
                    SendPacket(battlefieldStatusFailed);
                    return;
                }

                // check RBAC permissions
                if (!Player.CanJoinToBattleground(bg))
                {
                    Global.BattlegroundMgr.BuildBattlegroundStatusFailed(out battlefieldStatusFailed, bgQueueTypeId, Player, 0, GroupJoinBattlegroundResult.JoinTimedOut);
                    SendPacket(battlefieldStatusFailed);
                    return;
                }

                // check Deserter debuff
                if (Player.IsDeserter())
                {
                    Global.BattlegroundMgr.BuildBattlegroundStatusFailed(out battlefieldStatusFailed, bgQueueTypeId, Player, 0, GroupJoinBattlegroundResult.Deserters);
                    SendPacket(battlefieldStatusFailed);
                    return;
                }
                
                bool isInRandomBgQueue = _player.InBattlegroundQueueForBattlegroundQueueType(Global.BattlegroundMgr.BGQueueTypeId((ushort)BattlegroundTypeId.RB, BattlegroundQueueIdType.Battleground, false, 0))
                    || _player.InBattlegroundQueueForBattlegroundQueueType(Global.BattlegroundMgr.BGQueueTypeId((ushort)BattlegroundTypeId.RandomEpic, BattlegroundQueueIdType.Battleground, false, 0));
                if (bgTypeId != BattlegroundTypeId.RB && bgTypeId != BattlegroundTypeId.RandomEpic && isInRandomBgQueue)
                {
                    // player is already in random queue
                    Global.BattlegroundMgr.BuildBattlegroundStatusFailed(out battlefieldStatusFailed, bgQueueTypeId, Player, 0, GroupJoinBattlegroundResult.InRandomBg);
                    SendPacket(battlefieldStatusFailed);
                    return;
                }

                if (_player.InBattlegroundQueue(true) && !isInRandomBgQueue && (bgTypeId == BattlegroundTypeId.RB || bgTypeId == BattlegroundTypeId.RandomEpic))
                {
                    // player is already in queue, can't start random queue
                    Global.BattlegroundMgr.BuildBattlegroundStatusFailed(out battlefieldStatusFailed, bgQueueTypeId, Player, 0, GroupJoinBattlegroundResult.InNonRandomBg);
                    SendPacket(battlefieldStatusFailed);
                    return;
                }

                // check if already in queue
                if (Player.GetBattlegroundQueueIndex(bgQueueTypeId) < SharedConst.MaxPlayerBGQueues)
                    return;  // player is already in this queue

                // check if has free queue slots
                if (!Player.HasFreeBattlegroundQueueId())
                {
                    Global.BattlegroundMgr.BuildBattlegroundStatusFailed(out battlefieldStatusFailed, bgQueueTypeId, Player, 0, GroupJoinBattlegroundResult.TooManyQueues);
                    SendPacket(battlefieldStatusFailed);
                    return;
                }

                // check Freeze debuff
                if (_player.HasAura(9454))
                    return;

                BattlegroundQueue bgQueue = Global.BattlegroundMgr.GetBattlegroundQueue(bgQueueTypeId);
                GroupQueueInfo ginfo = bgQueue.AddGroup(Player, null, getQueueTeam(), bracketEntry, isPremade, 0, 0);

                uint avgTime = bgQueue.GetAverageQueueWaitTime(ginfo, bracketEntry.GetBracketId());
                uint queueSlot = Player.AddBattlegroundQueueId(bgQueueTypeId);

                Global.BattlegroundMgr.BuildBattlegroundStatusQueued(out BattlefieldStatusQueued battlefieldStatusQueued, bg, Player, queueSlot, ginfo.JoinTime, bgQueueTypeId, avgTime, 0, false);
                SendPacket(battlefieldStatusQueued);

                Log.outDebug(LogFilter.Battleground, $"Battleground: player joined queue for bg queue {bgQueueTypeId}, {_player.GUID}, NAME {_player.GetName()}");
            }
            else
            {
                if (grp.LeaderGUID != Player.GUID)
                    return;

                err = grp.CanJoinBattlegroundQueue(bg, bgQueueTypeId, 0, bg.GetMaxPlayersPerTeam(), false, 0, out ObjectGuid errorGuid);
                isPremade = (grp.MembersCount >= bg.GetMinPlayersPerTeam());

                BattlegroundQueue bgQueue = Global.BattlegroundMgr.GetBattlegroundQueue(bgQueueTypeId);
                GroupQueueInfo ginfo = null;
                uint avgTime = 0;

                if (err == 0)
                {
                    Log.outDebug(LogFilter.Battleground, "Battleground: the following players are joining as group:");
                    ginfo = bgQueue.AddGroup(Player, grp, getQueueTeam(), bracketEntry, isPremade, 0, 0);
                    avgTime = bgQueue.GetAverageQueueWaitTime(ginfo, bracketEntry.GetBracketId());
                }

                for (GroupReference refe = grp.FirstMember; refe != null; refe = refe.Next())
                {
                    Player member = refe.Source;
                    if (!member)
                        continue;   // this should never happen

                    if (err != 0)
                    {
                        Global.BattlegroundMgr.BuildBattlegroundStatusFailed(out BattlefieldStatusFailed battlefieldStatus, bgQueueTypeId, Player, 0, err, errorGuid);
                        member.SendPacket(battlefieldStatus);
                        continue;
                    }

                    // add to queue
                    uint queueSlot = member.AddBattlegroundQueueId(bgQueueTypeId);

                    Global.BattlegroundMgr.BuildBattlegroundStatusQueued(out BattlefieldStatusQueued battlefieldStatusQueued, bg, member, queueSlot, ginfo.JoinTime, bgQueueTypeId, avgTime, 0, true);
                    member.SendPacket(battlefieldStatusQueued);
                    Log.outDebug(LogFilter.Battleground, $"Battleground: player joined queue for bg queue {bgQueueTypeId}, {member.GUID}, NAME {member.GetName()}");
                }
                Log.outDebug(LogFilter.Battleground, "Battleground: group end");
            }

            Global.BattlegroundMgr.ScheduleQueueUpdate(0, bgQueueTypeId, bracketEntry.GetBracketId());
        }

        [WorldPacketHandler(ClientOpcodes.PvpLogData)]
        void HandlePVPLogData(PVPLogDataRequest packet)
        {
            Battleground bg = Player.GetBattleground();
            if (!bg)
                return;

            // Prevent players from sending BuildPvpLogDataPacket in an arena except for when sent in Battleground.EndBattleground.
            if (bg.IsArena())
                return;

            PVPMatchStatisticsMessage pvpMatchStatistics = new();
            bg.BuildPvPLogDataPacket(out pvpMatchStatistics.Data);
            SendPacket(pvpMatchStatistics);
        }

        [WorldPacketHandler(ClientOpcodes.BattlefieldList)]
        void HandleBattlefieldList(BattlefieldListRequest battlefieldList)
        {
            BattlemasterListRecord bl = CliDB.BattlemasterListStorage.LookupByKey(battlefieldList.ListID);
            if (bl == null)
            {
                Log.outDebug(LogFilter.Battleground, "BattlegroundHandler: invalid bgtype ({0}) with player (Name: {1}, GUID: {2}) received.", battlefieldList.ListID, Player.GetName(), Player.GUID.ToString());
                return;
            }

            Global.BattlegroundMgr.SendBattlegroundList(Player, ObjectGuid.Empty, (BattlegroundTypeId)battlefieldList.ListID);
        }

        [WorldPacketHandler(ClientOpcodes.BattlefieldPort)]
        void HandleBattleFieldPort(BattlefieldPort battlefieldPort)
        {
            if (!Player.InBattlegroundQueue())
            {
                Log.outDebug(LogFilter.Battleground, "CMSG_BATTLEFIELD_PORT {0} Slot: {1}, Unk: {2}, Time: {3}, AcceptedInvite: {4}. Player not in queue!",
                    GetPlayerInfo(), battlefieldPort.Ticket.Id, battlefieldPort.Ticket.Type, battlefieldPort.Ticket.Time, battlefieldPort.AcceptedInvite);
                return;
            }

            BattlegroundQueueTypeId bgQueueTypeId = Player.GetBattlegroundQueueTypeId(battlefieldPort.Ticket.Id);
            if (bgQueueTypeId == default)
            {
                Log.outDebug(LogFilter.Battleground, "CMSG_BATTLEFIELD_PORT {0} Slot: {1}, Unk: {2}, Time: {3}, AcceptedInvite: {4}. Invalid queueSlot!",
                    GetPlayerInfo(), battlefieldPort.Ticket.Id, battlefieldPort.Ticket.Type, battlefieldPort.Ticket.Time, battlefieldPort.AcceptedInvite);
                return;
            }

            BattlegroundQueue bgQueue = Global.BattlegroundMgr.GetBattlegroundQueue(bgQueueTypeId);

            //we must use temporary variable, because GroupQueueInfo pointer can be deleted in BattlegroundQueue.RemovePlayer() function
            if (!bgQueue.GetPlayerGroupInfoData(Player.GUID, out GroupQueueInfo ginfo))
            {
                Log.outDebug(LogFilter.Battleground, "CMSG_BATTLEFIELD_PORT {0} Slot: {1}, Unk: {2}, Time: {3}, AcceptedInvite: {4}. Player not in queue (No player Group Info)!",
                    GetPlayerInfo(), battlefieldPort.Ticket.Id, battlefieldPort.Ticket.Type, battlefieldPort.Ticket.Time, battlefieldPort.AcceptedInvite);
                return;
            }
            // if action == 1, then instanceId is required
            if (ginfo.IsInvitedToBGInstanceGUID == 0 && battlefieldPort.AcceptedInvite)
            {
                Log.outDebug(LogFilter.Battleground, "CMSG_BATTLEFIELD_PORT {0} Slot: {1}, Unk: {2}, Time: {3}, AcceptedInvite: {4}. Player is not invited to any bg!",
                    GetPlayerInfo(), battlefieldPort.Ticket.Id, battlefieldPort.Ticket.Type, battlefieldPort.Ticket.Time, battlefieldPort.AcceptedInvite);
                return;
            }

            BattlegroundTypeId bgTypeId = (BattlegroundTypeId)bgQueueTypeId.BattlemasterListId;
            // BGTemplateId returns Battleground_AA when it is arena queue.
            // Do instance id search as there is no AA bg instances.
            Battleground bg = Global.BattlegroundMgr.GetBattleground(ginfo.IsInvitedToBGInstanceGUID, bgTypeId == BattlegroundTypeId.AA ? BattlegroundTypeId.None : bgTypeId);
            if (!bg)
            {
                if (battlefieldPort.AcceptedInvite)
                {
                    Log.outDebug(LogFilter.Battleground, "CMSG_BATTLEFIELD_PORT {0} Slot: {1}, Unk: {2}, Time: {3}, AcceptedInvite: {4}. Cant find BG with id {5}!",
                        GetPlayerInfo(), battlefieldPort.Ticket.Id, battlefieldPort.Ticket.Type, battlefieldPort.Ticket.Time, battlefieldPort.AcceptedInvite, ginfo.IsInvitedToBGInstanceGUID);
                    return;
                }

                bg = Global.BattlegroundMgr.GetBattlegroundTemplate(bgTypeId);
                if (!bg)
                {
                    Log.outError(LogFilter.Network, "BattlegroundHandler: bg_template not found for type id {0}.", bgTypeId);
                    return;
                }
            }

            // get real bg type
            bgTypeId = bg.GetTypeID();

            // expected bracket entry
            PvpDifficultyRecord bracketEntry = Global.DB2Mgr.GetBattlegroundBracketByLevel(bg.GetMapId(), Player.Level);
            if (bracketEntry == null)
                return;

            //some checks if player isn't cheating - it is not exactly cheating, but we cannot allow it
            if (battlefieldPort.AcceptedInvite && bgQueue.GetQueueId().TeamSize == 0)
            {
                //if player is trying to enter Battleground(not arena!) and he has deserter debuff, we must just remove him from queue
                if (!Player.IsDeserter())
                {
                    // send bg command result to show nice message
                    Global.BattlegroundMgr.BuildBattlegroundStatusFailed(out BattlefieldStatusFailed battlefieldStatus, bgQueueTypeId, Player, battlefieldPort.Ticket.Id, GroupJoinBattlegroundResult.Deserters);
                    SendPacket(battlefieldStatus);
                    battlefieldPort.AcceptedInvite = false;
                    Log.outDebug(LogFilter.Battleground, "Player {0} ({1}) has a deserter debuff, do not port him to Battleground!", Player.GetName(), Player.GUID.ToString());
                }
                //if player don't match Battlegroundmax level, then do not allow him to enter! (this might happen when player leveled up during his waiting in queue
                if (Player.Level > bg.GetMaxLevel())
                {
                    Log.outDebug(LogFilter.Network, "Player {0} ({1}) has level ({2}) higher than maxlevel ({3}) of Battleground({4})! Do not port him to Battleground!",
                        Player.GetName(), Player.GUID.ToString(), Player.Level, bg.GetMaxLevel(), bg.GetTypeID());
                    battlefieldPort.AcceptedInvite = false;
                }
            }

            if (battlefieldPort.AcceptedInvite)
            {
                // check Freeze debuff
                if (Player.HasAura(9454))
                    return;

                if (!Player.IsInvitedForBattlegroundQueueType(bgQueueTypeId))
                    return;                                 // cheating?

                if (!Player.InBattleground())
                    Player.SetBattlegroundEntryPoint();

                // resurrect the player
                if (!Player.IsAlive)
                {
                    Player.ResurrectPlayer(1.0f);
                    Player.SpawnCorpseBones();
                }
                // stop taxi flight at port
                Player.FinishTaxiFlight();

                Global.BattlegroundMgr.BuildBattlegroundStatusActive(out BattlefieldStatusActive battlefieldStatus, bg, Player, battlefieldPort.Ticket.Id, Player.GetBattlegroundQueueJoinTime(bgQueueTypeId), bg.GetArenaType());
                SendPacket(battlefieldStatus);

                // remove BattlegroundQueue status from BGmgr
                bgQueue.RemovePlayer(Player.GUID, false);
                // this is still needed here if Battleground"jumping" shouldn't add deserter debuff
                // also this is required to prevent stuck at old Battlegroundafter SetBattlegroundId set to new
                Battleground currentBg = Player.GetBattleground();
                if (currentBg)
                    currentBg.RemovePlayerAtLeave(Player.GUID, false, true);

                // set the destination instance id
                Player.SetBattlegroundId(bg.GetInstanceID(), bgTypeId);
                // set the destination team
                Player.SetBgTeam(ginfo.Team);

                Global.BattlegroundMgr.SendToBattleground(Player, ginfo.IsInvitedToBGInstanceGUID, bgTypeId);
                Log.outDebug(LogFilter.Battleground, $"Battleground: player {_player.GetName()} ({_player.GUID}) joined battle for bg {bg.GetInstanceID()}, bgtype {bg.GetTypeID()}, queue {bgQueueTypeId}.");
            }
            else // leave queue
            {
                // if player leaves rated arena match before match start, it is counted as he played but he lost
                if (bgQueue.GetQueueId().Rated && ginfo.IsInvitedToBGInstanceGUID != 0)
                {
                    ArenaTeam at = Global.ArenaTeamMgr.GetArenaTeamById((uint)ginfo.Team);
                    if (at != null)
                    {
                        Log.outDebug(LogFilter.Battleground, "UPDATING memberLost's personal arena rating for {0} by opponents rating: {1}, because he has left queue!", Player.GUID.ToString(), ginfo.OpponentsTeamRating);
                        at.MemberLost(Player, ginfo.OpponentsMatchmakerRating);
                        at.SaveToDB();
                    }
                }
                BattlefieldStatusNone battlefieldStatus = new();
                battlefieldStatus.Ticket = battlefieldPort.Ticket;
                SendPacket(battlefieldStatus);

                Player.RemoveBattlegroundQueueId(bgQueueTypeId);  // must be called this way, because if you move this call to queue.removeplayer, it causes bugs
                bgQueue.RemovePlayer(Player.GUID, true);
                // player left queue, we should update it - do not update Arena Queue
                if (bgQueue.GetQueueId().TeamSize == 0)
                    Global.BattlegroundMgr.ScheduleQueueUpdate(ginfo.ArenaMatchmakerRating, bgQueueTypeId, bracketEntry.GetBracketId());

                Log.outDebug(LogFilter.Battleground, $"Battleground: player {_player.GetName()} ({_player.GUID}) left queue for bgtype { bg.GetTypeID()}, queue {bgQueueTypeId}.");
            }
        }

        [WorldPacketHandler(ClientOpcodes.BattlefieldLeave)]
        void HandleBattlefieldLeave(BattlefieldLeave packet)
        {
            // not allow leave Battlegroundin combat
            if (Player.IsInCombat)
            {
                Battleground bg = Player.GetBattleground();
                if (bg)
                    if (bg.GetStatus() != BattlegroundStatus.WaitLeave)
                        return;
            }

            Player.LeaveBattleground();
        }

        [WorldPacketHandler(ClientOpcodes.RequestBattlefieldStatus)]
        void HandleRequestBattlefieldStatus(RequestBattlefieldStatus packet)
        {
            // we must update all queues here
            Battleground bg = null;
            for (byte i = 0; i < SharedConst.MaxPlayerBGQueues; ++i)
            {
                BattlegroundQueueTypeId bgQueueTypeId = Player.GetBattlegroundQueueTypeId(i);
                if (bgQueueTypeId == default)
                    continue;

                BattlegroundTypeId bgTypeId = (BattlegroundTypeId)bgQueueTypeId.BattlemasterListId;
                ArenaTypes arenaType = (ArenaTypes)bgQueueTypeId.TeamSize;
                bg = _player.GetBattleground();
                if (bg && bg.GetQueueId() == bgQueueTypeId)
                {
                    //i cannot check any variable from player class because player class doesn't know if player is in 2v2 / 3v3 or 5v5 arena
                    //so i must use bg pointer to get that information
                    Global.BattlegroundMgr.BuildBattlegroundStatusActive(out BattlefieldStatusActive battlefieldStatus, bg, _player, i, _player.GetBattlegroundQueueJoinTime(bgQueueTypeId), arenaType);
                    SendPacket(battlefieldStatus);
                    continue;
                }

                //we are sending update to player about queue - he can be invited there!
                //get GroupQueueInfo for queue status
                BattlegroundQueue bgQueue = Global.BattlegroundMgr.GetBattlegroundQueue(bgQueueTypeId);
                if (!bgQueue.GetPlayerGroupInfoData(Player.GUID, out GroupQueueInfo ginfo))
                    continue;

                if (ginfo.IsInvitedToBGInstanceGUID != 0)
                {
                    bg = Global.BattlegroundMgr.GetBattleground(ginfo.IsInvitedToBGInstanceGUID, bgTypeId);
                    if (!bg)
                        continue;

                    Global.BattlegroundMgr.BuildBattlegroundStatusNeedConfirmation(out BattlefieldStatusNeedConfirmation battlefieldStatus, bg, Player, i, Player.GetBattlegroundQueueJoinTime(bgQueueTypeId), Time.GetMSTimeDiff(Time.MSTime, ginfo.RemoveInviteTime), arenaType);
                    SendPacket(battlefieldStatus);
                }
                else
                {
                    bg = Global.BattlegroundMgr.GetBattlegroundTemplate(bgTypeId);
                    if (!bg)
                        continue;

                    // expected bracket entry
                    PvpDifficultyRecord bracketEntry = Global.DB2Mgr.GetBattlegroundBracketByLevel(bg.GetMapId(), Player.Level);
                    if (bracketEntry == null)
                        continue;

                    uint avgTime = bgQueue.GetAverageQueueWaitTime(ginfo, bracketEntry.GetBracketId());
                    Global.BattlegroundMgr.BuildBattlegroundStatusQueued(out BattlefieldStatusQueued battlefieldStatus, bg, Player, i, Player.GetBattlegroundQueueJoinTime(bgQueueTypeId), bgQueueTypeId, avgTime, arenaType, ginfo.Players.Count > 1);
                    SendPacket(battlefieldStatus);
                }
            }
        }

        [WorldPacketHandler(ClientOpcodes.BattlemasterJoinArena)]
        void HandleBattlemasterJoinArena(BattlemasterJoinArena packet)
        {
            // ignore if we already in BG or BG queue
            if (Player.InBattleground())
                return;

            ArenaTypes arenatype = (ArenaTypes)ArenaTeam.GetTypeBySlot(packet.TeamSizeIndex);

            //check existence
            Battleground bg = Global.BattlegroundMgr.GetBattlegroundTemplate(BattlegroundTypeId.AA);
            if (!bg)
            {
                Log.outError(LogFilter.Network, "Battleground: template bg (all arenas) not found");
                return;
            }

            if (Global.DisableMgr.IsDisabledFor(DisableType.Battleground, (uint)BattlegroundTypeId.AA, null))
            {
                Player.SendSysMessage(CypherStrings.ArenaDisabled);
                return;
            }

            BattlegroundTypeId bgTypeId = bg.GetTypeID();
            BattlegroundQueueTypeId bgQueueTypeId = Global.BattlegroundMgr.BGQueueTypeId((ushort)bgTypeId, BattlegroundQueueIdType.Arena, true, arenatype);
            PvpDifficultyRecord bracketEntry = Global.DB2Mgr.GetBattlegroundBracketByLevel(bg.GetMapId(), Player.Level);
            if (bracketEntry == null)
                return;

            PlayerGroup grp = Player.Group;
            // no group found, error
            if (!grp)
                return;
            if (grp.LeaderGUID != Player.GUID)
                return;

            uint ateamId = Player.GetArenaTeamId(packet.TeamSizeIndex);
            // check real arenateam existence only here (if it was moved to group.CanJoin .. () then we would ahve to get it twice)
            ArenaTeam at = Global.ArenaTeamMgr.GetArenaTeamById(ateamId);
            if (at == null)
                return;

            // get the team rating for queuing
            uint arenaRating = at.GetRating();
            uint matchmakerRating = at.GetAverageMMR(grp);
            // the arenateam id must match for everyone in the group

            if (arenaRating <= 0)
                arenaRating = 1;

            BattlegroundQueue bgQueue = Global.BattlegroundMgr.GetBattlegroundQueue(bgQueueTypeId);

            uint avgTime = 0;
            GroupQueueInfo ginfo = null;

            var err = grp.CanJoinBattlegroundQueue(bg, bgQueueTypeId, (uint)arenatype, (uint)arenatype, true, packet.TeamSizeIndex, out ObjectGuid errorGuid);
            if (err == 0)
            {
                Log.outDebug(LogFilter.Battleground, "Battleground: arena team id {0}, leader {1} queued with matchmaker rating {2} for type {3}", Player.GetArenaTeamId(packet.TeamSizeIndex), Player.GetName(), matchmakerRating, arenatype);

                ginfo = bgQueue.AddGroup(Player, grp, _player.Team, bracketEntry, false, arenaRating, matchmakerRating, ateamId);
                avgTime = bgQueue.GetAverageQueueWaitTime(ginfo, bracketEntry.GetBracketId());
            }

            for (GroupReference refe = grp.FirstMember; refe != null; refe = refe.Next())
            {
                Player member = refe.Source;
                if (!member)
                    continue;

                if (err != 0)
                {
                    Global.BattlegroundMgr.BuildBattlegroundStatusFailed(out BattlefieldStatusFailed battlefieldStatus, bgQueueTypeId, Player, 0, err, errorGuid);
                    member.SendPacket(battlefieldStatus);
                    continue;
                }

                if (!Player.CanJoinToBattleground(bg))
                {
                    Global.BattlegroundMgr.BuildBattlegroundStatusFailed(out BattlefieldStatusFailed battlefieldStatus, bgQueueTypeId, Player, 0, GroupJoinBattlegroundResult.BattlegroundJoinFailed, errorGuid);
                    member.SendPacket(battlefieldStatus);
                    return;
                }

                // add to queue
                uint queueSlot = member.AddBattlegroundQueueId(bgQueueTypeId);

                Global.BattlegroundMgr.BuildBattlegroundStatusQueued(out BattlefieldStatusQueued battlefieldStatusQueued, bg, member, queueSlot, ginfo.JoinTime, bgQueueTypeId, avgTime, arenatype, true);
                member.SendPacket(battlefieldStatusQueued);

                Log.outDebug(LogFilter.Battleground, $"Battleground: player joined queue for arena as group bg queue {bgQueueTypeId}, {member.GUID}, NAME {member.GetName()}");
            }

            Global.BattlegroundMgr.ScheduleQueueUpdate(matchmakerRating, bgQueueTypeId, bracketEntry.GetBracketId());
        }

        [WorldPacketHandler(ClientOpcodes.ReportPvpPlayerAfk)]
        void HandleReportPvPAFK(ReportPvPPlayerAFK reportPvPPlayerAFK)
        {
            Player reportedPlayer = Global.ObjAccessor.FindPlayer(reportPvPPlayerAFK.Offender);
            if (!reportedPlayer)
            {
                Log.outDebug(LogFilter.Battleground, "WorldSession.HandleReportPvPAFK: player not found");
                return;
            }

            Log.outDebug(LogFilter.BattlegroundReportPvpAfk, "WorldSession.HandleReportPvPAFK:  {0} [IP: {1}] reported {2}", _player.GetName(), _player.Session.RemoteAddress, reportedPlayer.GUID.ToString());

            reportedPlayer.ReportedAfkBy(Player);
        }

        [WorldPacketHandler(ClientOpcodes.RequestRatedPvpInfo)]
        void HandleRequestRatedPvpInfo(RequestRatedPvpInfo packet)
        {
            RatedPvpInfo ratedPvpInfo = new();
            SendPacket(ratedPvpInfo);
        }

        [WorldPacketHandler(ClientOpcodes.GetPvpOptionsEnabled, Processing = PacketProcessing.Inplace)]
        void HandleGetPVPOptionsEnabled(GetPVPOptionsEnabled packet)
        {
            // This packet is completely irrelevant, it triggers PVP_TYPES_ENABLED lua event but that is not handled in interface code as of 6.1.2
            PVPOptionsEnabled pvpOptionsEnabled = new();
            pvpOptionsEnabled.PugBattlegrounds = true;
            SendPacket(new PVPOptionsEnabled());
        }

        [WorldPacketHandler(ClientOpcodes.RequestPvpRewards, Processing = PacketProcessing.Inplace)]
        void HandleRequestPvpReward(RequestPVPRewards packet)
        {
            Player.SendPvpRewards();
        }

        [WorldPacketHandler(ClientOpcodes.AreaSpiritHealerQuery)]
        void HandleAreaSpiritHealerQuery(AreaSpiritHealerQuery areaSpiritHealerQuery)
        {
            Creature unit = ObjectAccessor.GetCreature(Player, areaSpiritHealerQuery.HealerGuid);
            if (!unit)
                return;

            if (!unit.IsSpiritService)                            // it's not spirit service
                return;

            Battleground bg = Player.GetBattleground();
            if (bg != null)
                Global.BattlegroundMgr.SendAreaSpiritHealerQuery(Player, bg, areaSpiritHealerQuery.HealerGuid);

            BattleField bf = Global.BattleFieldMgr.GetBattlefieldToZoneId(Player.Map, Player.Zone);
            if (bf != null)
                bf.SendAreaSpiritHealerQuery(Player, areaSpiritHealerQuery.HealerGuid);
        }

        [WorldPacketHandler(ClientOpcodes.AreaSpiritHealerQueue)]
        void HandleAreaSpiritHealerQueue(AreaSpiritHealerQueue areaSpiritHealerQueue)
        {
            Creature unit = ObjectAccessor.GetCreature(Player, areaSpiritHealerQueue.HealerGuid);
            if (!unit)
                return;

            if (!unit.IsSpiritService)                            // it's not spirit service
                return;

            Battleground bg = Player.GetBattleground();
            if (bg)
                bg.AddPlayerToResurrectQueue(areaSpiritHealerQueue.HealerGuid, Player.GUID);

            BattleField bf = Global.BattleFieldMgr.GetBattlefieldToZoneId(Player.Map, Player.Zone);
            if (bf != null)
                bf.AddPlayerToResurrectQueue(areaSpiritHealerQueue.HealerGuid, Player.GUID);
        }

        [WorldPacketHandler(ClientOpcodes.HearthAndResurrect)]
        void HandleHearthAndResurrect(HearthAndResurrect packet)
        {
            if (Player.IsInFlight)
                return;

            BattleField bf = Global.BattleFieldMgr.GetBattlefieldToZoneId(Player.Map, Player.Zone);
            if (bf != null)
            {
                bf.PlayerAskToLeave(_player);
                return;
            }

            AreaTableRecord atEntry = CliDB.AreaTableStorage.LookupByKey(Player.Area);
            if (atEntry == null || !atEntry.HasFlag(AreaFlags.CanHearthAndResurrect))
                return;

            Player.BuildPlayerRepop();
            Player.ResurrectPlayer(1.0f);
            Player.TeleportTo(Player.Homebind);
        }

        [WorldPacketHandler(ClientOpcodes.BattlemasterJoinSkirmish)]
        public void HandleJoinSkirmish(JoinSkirmish packet)
        {
            Player player = Player;
            if (player == null)
            {
                return;
            }

            bool isPremade = false;
            PlayerGroup grp = null;

            ArenaTypes arenatype = (packet.Bracket == BracketType.SKIRMISH_3 ? ArenaTypes.Team3v3 : ArenaTypes.Team2v2);
            
            Battleground bg = BattlegroundManager.Instance.GetBattlegroundTemplate(BattlegroundTypeId.AA);
            if (bg == null)
            {
                return;
            }

            var getQueueTeam = () =>
            {
                // mercenary applies only to unrated battlegrounds
                if (!bg.IsRated() && !bg.IsArena())
                {
                    if (_player.HasAura(193472)) // SPELL_MERCENARY_CONTRACT_HORDE
                    {
                        return TeamFaction.Horde;
                    }

                    if (_player.HasAura(193475)) // SPELL_MERCENARY_CONTRACT_ALLIANCE
                    {
                        return TeamFaction.Alliance;
                    }
                }

                return _player.Team;
            };

            if (DisableManager.Instance.IsDisabledFor(DisableType.Battleground, (uint)BattlegroundTypeId.AA, null))
            {
                player.SendSysMessage(CypherStrings.ArenaDisabled);
                return;
            }

            BattlegroundTypeId bgTypeId = bg.GetTypeID();
            BattlegroundQueueTypeId bgQueueTypeId = BattlegroundManager.Instance.BGQueueTypeId((ushort)bgTypeId, BattlegroundQueueIdType.Arena, true, arenatype);

            if (player.InBattleground())
            {
                return;
            }

            PvpDifficultyRecord bracketEntry = Global.DB2Mgr.GetBattlegroundBracketByLevel(bg.GetMapId(), _player.Level);
            if (bracketEntry == null)
            {
                return;
            }

            GroupJoinBattlegroundResult err = GroupJoinBattlegroundResult.None;

            if (!packet.JoinAsGroup)
            {
                if (player.IsUsingLfg)
                {
                    Global.BattlegroundMgr.BuildBattlegroundStatusFailed(out var battlefieldStatusFailed, bgQueueTypeId, _player, 0, GroupJoinBattlegroundResult.LfgCantUseBattleground);
                    SendPacket(battlefieldStatusFailed);
                    return;
                }

                if (!player.CanJoinToBattleground(bg))
                {
                    Global.BattlegroundMgr.BuildBattlegroundStatusFailed(out var battlefieldStatusFailed, bgQueueTypeId, _player, 0, GroupJoinBattlegroundResult.LfgCantUseBattleground);
                    SendPacket(battlefieldStatusFailed);
                    return;
                }

                if (player.GetBattlegroundQueueIndex(bgQueueTypeId) < 2)
                {
                    return;
                }

                if (!player.HasFreeBattlegroundQueueId())
                {
                    Global.BattlegroundMgr.BuildBattlegroundStatusFailed(out var battlefieldStatusFailed, bgQueueTypeId, _player, 0, GroupJoinBattlegroundResult.LfgCantUseBattleground);
                    SendPacket(battlefieldStatusFailed);
                    return;
                }

                BattlegroundQueue bgQueue = BattlegroundManager.Instance.GetBattlegroundQueue(bgQueueTypeId);
                GroupQueueInfo ginfo = bgQueue.AddGroup(_player, grp, getQueueTeam(), bracketEntry, isPremade, 0, 0);

                uint avgTime = bgQueue.GetAverageQueueWaitTime(ginfo, bracketEntry.GetBracketId());
                uint queueSlot = player.AddBattlegroundQueueId(bgQueueTypeId);

                BattlefieldStatusQueued battlefieldStatus = new BattlefieldStatusQueued();
                BattlegroundManager.Instance.BuildBattlegroundStatusQueued(out battlefieldStatus, bg, player, queueSlot, ginfo.JoinTime, bgQueueTypeId, avgTime, arenatype, false);
                SendPacket(battlefieldStatus);
            }
            else
            {
                grp = player.Group;

                if (grp == null)
                {
                    return;
                }

                if (grp.LeaderGUID != player.GUID)
                {
                    return;
                }

                err = grp.CanJoinBattlegroundQueue(bg, bgQueueTypeId, 0, bg.GetMaxPlayersPerTeam(), false, 0, out var errorGuid);

                BattlegroundQueue bgQueue = BattlegroundManager.Instance.GetBattlegroundQueue(bgQueueTypeId);
                GroupQueueInfo ginfo = null;
                uint avgTime = 0;

                if (err == default)
                {
                    ginfo = bgQueue.AddGroup(_player, null, getQueueTeam(), bracketEntry, isPremade, 0, 0);
                    avgTime = bgQueue.GetAverageQueueWaitTime(ginfo, bracketEntry.GetBracketId());
                }

                foreach (var slot in grp.MemberSlots)
                {
                    Player member = Global.ObjAccessor.FindPlayer(slot.Guid);
                    if (member == null)
                    {
                        continue;
                    }

                    if (err != default)
                    {
                        BattlegroundManager.Instance.BuildBattlegroundStatusFailed(out var battlefieldStatuss, bgQueueTypeId, _player, 0, GroupJoinBattlegroundResult.LfgCantUseBattleground);
                        member.                        Session.SendPacket(battlefieldStatuss);
                        continue;
                    }

                    BattlegroundManager.Instance.BuildBattlegroundStatusQueued(out var battlefieldStatus, bg, member, member.AddBattlegroundQueueId(bgQueueTypeId), ginfo.JoinTime, bgQueueTypeId, avgTime, 0, true);
                    member.SendPacket(battlefieldStatus);
                }
            }

            BattlegroundManager.Instance.ScheduleQueueUpdate(0, bgQueueTypeId, bracketEntry.GetBracketId());
        }
    }
}
