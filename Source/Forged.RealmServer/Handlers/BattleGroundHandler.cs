// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Forged.RealmServer.Arenas;
using Forged.RealmServer.BattleGrounds;
using Forged.RealmServer.DataStorage;
using Forged.RealmServer.Entities;
using Forged.RealmServer.Groups;
using Forged.RealmServer.Networking;
using Forged.RealmServer.Networking.Packets;

namespace Forged.RealmServer;

public partial class WorldSession
{
	[WorldPacketHandler(ClientOpcodes.BattlemasterHello)]
	void HandleBattlemasterHello(Hello hello)
	{
		var unit = Player.GetNPCIfCanInteractWith(hello.Unit, NPCFlags.BattleMaster, NPCFlags2.None);

		if (!unit)
			return;

		// Stop the npc if moving
		var pause = unit.MovementTemplate.GetInteractionPauseTimer();

		if (pause != 0)
			unit.PauseMovement(pause);

		unit.HomePosition = unit.Location;

		var bgTypeId = Global.BattlegroundMgr.GetBattleMasterBG(unit.Entry);

		if (!Player.GetBgAccessByLevel(bgTypeId))
		{
			// temp, must be gossip message...
			SendNotification(CypherStrings.YourBgLevelReqError);

			return;
		}

		Global.BattlegroundMgr.SendBattlegroundList(Player, hello.Unit, bgTypeId);
	}

	[WorldPacketHandler(ClientOpcodes.PvpLogData)]
	void HandlePVPLogData(PVPLogDataRequest packet)
	{
		var bg = Player.Battleground;

		if (!bg)
			return;

		// Prevent players from sending BuildPvpLogDataPacket in an arena except for when sent in Battleground.EndBattleground.
		if (bg.IsArena())
			return;

		PVPMatchStatisticsMessage pvpMatchStatistics = new();
		bg.BuildPvPLogDataPacket(out pvpMatchStatistics.Data);
		SendPacket(pvpMatchStatistics);
	}

	[WorldPacketHandler(ClientOpcodes.BattlemasterJoinArena)]
	void HandleBattlemasterJoinArena(BattlemasterJoinArena packet)
	{
		// ignore if we already in BG or BG queue
		if (Player.InBattleground)
			return;

		var arenatype = (ArenaTypes)ArenaTeam.GetTypeBySlot(packet.TeamSizeIndex);

		//check existence
		var bg = Global.BattlegroundMgr.GetBattlegroundTemplate(BattlegroundTypeId.AA);

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

		var bgTypeId = bg.GetTypeID();
		var bgQueueTypeId = Global.BattlegroundMgr.BGQueueTypeId((ushort)bgTypeId, BattlegroundQueueIdType.Arena, true, arenatype);
		var bracketEntry = Global.DB2Mgr.GetBattlegroundBracketByLevel(bg.GetMapId(), Player.Level);

		if (bracketEntry == null)
			return;

		var grp = Player.Group;

		// no group found, error
		if (!grp)
			return;

		if (grp.LeaderGUID != Player.GUID)
			return;

		var ateamId = Player.GetArenaTeamId(packet.TeamSizeIndex);
		// check real arenateam existence only here (if it was moved to group.CanJoin .. () then we would ahve to get it twice)
		var at = Global.ArenaTeamMgr.GetArenaTeamById(ateamId);

		if (at == null)
			return;

		// get the team rating for queuing
		var arenaRating = at.GetRating();
		var matchmakerRating = at.GetAverageMMR(grp);
		// the arenateam id must match for everyone in the group

		if (arenaRating <= 0)
			arenaRating = 1;

		var bgQueue = Global.BattlegroundMgr.GetBattlegroundQueue(bgQueueTypeId);

		uint avgTime = 0;
		GroupQueueInfo ginfo = null;

		var err = grp.CanJoinBattlegroundQueue(bg, bgQueueTypeId, (uint)arenatype, (uint)arenatype, true, packet.TeamSizeIndex, out var errorGuid);

		if (err == 0)
		{
			Log.outDebug(LogFilter.Battleground, "Battleground: arena team id {0}, leader {1} queued with matchmaker rating {2} for type {3}", Player.GetArenaTeamId(packet.TeamSizeIndex), Player.GetName(), matchmakerRating, arenatype);

			ginfo = bgQueue.AddGroup(Player, grp, _player.Team, bracketEntry, false, arenaRating, matchmakerRating, ateamId);
			avgTime = bgQueue.GetAverageQueueWaitTime(ginfo, bracketEntry.GetBracketId());
		}

		for (var refe = grp.FirstMember; refe != null; refe = refe.Next())
		{
			var member = refe.Source;

			if (!member)
				continue;

			if (err != 0)
			{
				Global.BattlegroundMgr.BuildBattlegroundStatusFailed(out var battlefieldStatus, bgQueueTypeId, Player, 0, err, errorGuid);
				member.SendPacket(battlefieldStatus);

				continue;
			}

			if (!Player.CanJoinToBattleground(bg))
			{
				Global.BattlegroundMgr.BuildBattlegroundStatusFailed(out var battlefieldStatus, bgQueueTypeId, Player, 0, GroupJoinBattlegroundResult.BattlegroundJoinFailed, errorGuid);
				member.SendPacket(battlefieldStatus);

				return;
			}

			// add to queue
			var queueSlot = member.AddBattlegroundQueueId(bgQueueTypeId);

			Global.BattlegroundMgr.BuildBattlegroundStatusQueued(out var battlefieldStatusQueued, bg, member, queueSlot, ginfo.JoinTime, bgQueueTypeId, avgTime, arenatype, true);
			member.SendPacket(battlefieldStatusQueued);

			Log.outDebug(LogFilter.Battleground, $"Battleground: player joined queue for arena as group bg queue {bgQueueTypeId}, {member.GUID}, NAME {member.GetName()}");
		}

		Global.BattlegroundMgr.ScheduleQueueUpdate(matchmakerRating, bgQueueTypeId, bracketEntry.GetBracketId());
	}

	[WorldPacketHandler(ClientOpcodes.ReportPvpPlayerAfk)]
	void HandleReportPvPAFK(ReportPvPPlayerAFK reportPvPPlayerAFK)
	{
		var reportedPlayer = Global.ObjAccessor.FindPlayer(reportPvPPlayerAFK.Offender);

		if (!reportedPlayer)
		{
			Log.outDebug(LogFilter.Battleground, "WorldSession.HandleReportPvPAFK: player not found");

			return;
		}

		Log.outDebug(LogFilter.BattlegroundReportPvpAfk, "WorldSession.HandleReportPvPAFK:  {0} [IP: {1}] reported {2}", _player.GetName(), _player.Session.RemoteAddress, reportedPlayer.GUID.ToString());

		reportedPlayer.ReportedAfkBy(Player);
	}

	[WorldPacketHandler(ClientOpcodes.GetPvpOptionsEnabled, Processing = PacketProcessing.Inplace)]
	void HandleGetPVPOptionsEnabled(GetPVPOptionsEnabled packet)
	{
		// This packet is completely irrelevant, it triggers PVP_TYPES_ENABLED lua event but that is not handled in interface code as of 6.1.2
		PVPOptionsEnabled pvpOptionsEnabled = new();
		pvpOptionsEnabled.PugBattlegrounds = true;
		SendPacket(new PVPOptionsEnabled());
	}

	[WorldPacketHandler(ClientOpcodes.HearthAndResurrect)]
	void HandleHearthAndResurrect(HearthAndResurrect packet)
	{
		if (Player.IsInFlight)
			return;

		var bf = Global.BattleFieldMgr.GetBattlefieldToZoneId(Player.Map, Player.Zone);

		if (bf != null)
		{
			bf.PlayerAskToLeave(_player);

			return;
		}

		var atEntry = CliDB.AreaTableStorage.LookupByKey(Player.Area);

		if (atEntry == null || !atEntry.HasFlag(AreaFlags.CanHearthAndResurrect))
			return;

		Player.BuildPlayerRepop();
		Player.ResurrectPlayer(1.0f);
		Player.TeleportTo(Player.Homebind);
	}
}