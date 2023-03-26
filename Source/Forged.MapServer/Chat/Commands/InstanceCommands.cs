// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Chrono;
using Forged.MapServer.DataStorage;
using Forged.MapServer.Maps.Instances;
using Framework.Constants;

namespace Forged.MapServer.Chat.Commands;

[CommandGroup("instance")]
internal class InstanceCommands
{
	[Command("getbossstate", RBACPermissions.CommandInstanceGetBossState)]
    private static bool HandleInstanceGetBossStateCommand(CommandHandler handler, uint encounterId, PlayerIdentifier player)
	{
		// Character name must be provided when using this from console.
		if (player == null || handler.Session == null)
		{
			handler.SendSysMessage(CypherStrings.CmdSyntax);

			return false;
		}

		if (player == null)
			player = PlayerIdentifier.FromSelf(handler);

		if (player.IsConnected())
		{
			handler.SendSysMessage(CypherStrings.PlayerNotFound);

			return false;
		}

		var map = player.GetConnectedPlayer().Map.ToInstanceMap;

		if (map == null)
		{
			handler.SendSysMessage(CypherStrings.NotDungeon);

			return false;
		}

		if (map.InstanceScript == null)
		{
			handler.SendSysMessage(CypherStrings.NoInstanceData);

			return false;
		}

		if (encounterId > map.InstanceScript.GetEncounterCount())
		{
			handler.SendSysMessage(CypherStrings.BadValue);

			return false;
		}

		var state = map.InstanceScript.GetBossState(encounterId);
		handler.SendSysMessage(CypherStrings.CommandInstGetBossState, encounterId, state);

		return true;
	}

	[Command("listbinds", RBACPermissions.CommandInstanceListbinds)]
    private static bool HandleInstanceListBindsCommand(CommandHandler handler)
	{
		var player = handler.SelectedPlayer;

		if (player == null)
			player = handler.Session.Player;

		var now = GameTime.GetDateAndTime();
		var instanceLocks = Global.InstanceLockMgr.GetInstanceLocksForPlayer(player.GUID);

		foreach (var instanceLock in instanceLocks)
		{
			MapDb2Entries entries = new(instanceLock.GetMapId(), instanceLock.GetDifficultyId());
			var timeleft = !instanceLock.IsExpired() ? Time.secsToTimeString((ulong)(instanceLock.GetEffectiveExpiryTime() - now).TotalSeconds) : "-";

			handler.SendSysMessage(CypherStrings.CommandListBindInfo,
									entries.Map.Id,
									entries.Map.MapName[Global.WorldMgr.DefaultDbcLocale],
									entries.MapDifficulty.DifficultyID,
									CliDB.DifficultyStorage.LookupByKey(entries.MapDifficulty.DifficultyID).Name,
									instanceLock.GetInstanceId(),
									handler.GetCypherString(instanceLock.IsExpired() ? CypherStrings.Yes : CypherStrings.No),
									handler.GetCypherString(instanceLock.IsExtended() ? CypherStrings.Yes : CypherStrings.No),
									timeleft);
		}

		handler.SendSysMessage(CypherStrings.CommandListBindPlayerBinds, instanceLocks.Count);

		return true;
	}

	[Command("setbossstate", RBACPermissions.CommandInstanceSetBossState)]
    private static bool HandleInstanceSetBossStateCommand(CommandHandler handler, uint encounterId, EncounterState state, PlayerIdentifier player)
	{
		// Character name must be provided when using this from console.
		if (player == null || handler.Session == null)
		{
			handler.SendSysMessage(CypherStrings.CmdSyntax);

			return false;
		}

		if (player == null)
			player = PlayerIdentifier.FromSelf(handler);

		if (!player.IsConnected())
		{
			handler.SendSysMessage(CypherStrings.PlayerNotFound);

			return false;
		}

		var map = player.GetConnectedPlayer().Map.ToInstanceMap;

		if (map == null)
		{
			handler.SendSysMessage(CypherStrings.NotDungeon);

			return false;
		}

		if (map.InstanceScript == null)
		{
			handler.SendSysMessage(CypherStrings.NoInstanceData);

			return false;
		}

		// Reject improper values.
		if (encounterId > map.InstanceScript.GetEncounterCount())
		{
			handler.SendSysMessage(CypherStrings.BadValue);

			return false;
		}

		map.InstanceScript.SetBossState(encounterId, state);
		handler.SendSysMessage(CypherStrings.CommandInstSetBossState, encounterId, state);

		return true;
	}

	[Command("stats", RBACPermissions.CommandInstanceStats, true)]
    private static bool HandleInstanceStatsCommand(CommandHandler handler)
	{
		handler.SendSysMessage("instances loaded: {0}", Global.MapMgr.GetNumInstances());
		handler.SendSysMessage("players in instances: {0}", Global.MapMgr.GetNumPlayersInInstances());

		var statistics = Global.InstanceLockMgr.GetStatistics();

		handler.SendSysMessage(CypherStrings.CommandInstStatSaves, statistics.InstanceCount);
		handler.SendSysMessage(CypherStrings.CommandInstStatPlayersbound, statistics.PlayerCount);

		return true;
	}

	[Command("unbind", RBACPermissions.CommandInstanceUnbind)]
    private static bool HandleInstanceUnbindCommand(CommandHandler handler, [VariantArg(typeof(uint), typeof(string))] object mapArg, uint? difficultyArg)
	{
		var player = handler.SelectedPlayer;

		if (player == null)
			player = handler.Session.Player;

		uint? mapId = null;
		Difficulty? difficulty = null;

		if (mapArg is uint)
			mapId = (uint)mapArg;

		if (difficultyArg.HasValue && CliDB.DifficultyStorage.ContainsKey(difficultyArg.Value))
			difficulty = (Difficulty)difficultyArg;

		List<InstanceLock> locksReset = new();
		List<InstanceLock> locksNotReset = new();

		Global.InstanceLockMgr.ResetInstanceLocksForPlayer(player.GUID, mapId, difficulty, locksReset, locksNotReset);

		var now = GameTime.GetDateAndTime();

		foreach (var instanceLock in locksReset)
		{
			MapDb2Entries entries = new(instanceLock.GetMapId(), instanceLock.GetDifficultyId());
			var timeleft = !instanceLock.IsExpired() ? Time.secsToTimeString((ulong)(instanceLock.GetEffectiveExpiryTime() - now).TotalSeconds) : "-";

			handler.SendSysMessage(CypherStrings.CommandInstUnbindUnbinding,
									entries.Map.Id,
									entries.Map.MapName[Global.WorldMgr.DefaultDbcLocale],
									entries.MapDifficulty.DifficultyID,
									CliDB.DifficultyStorage.LookupByKey(entries.MapDifficulty.DifficultyID).Name,
									instanceLock.GetInstanceId(),
									handler.GetCypherString(instanceLock.IsExpired() ? CypherStrings.Yes : CypherStrings.No),
									handler.GetCypherString(instanceLock.IsExtended() ? CypherStrings.Yes : CypherStrings.No),
									timeleft);
		}

		handler.SendSysMessage(CypherStrings.CommandInstUnbindUnbound, locksReset.Count);

		foreach (var instanceLock in locksNotReset)
		{
			MapDb2Entries entries = new(instanceLock.GetMapId(), instanceLock.GetDifficultyId());
			var timeleft = !instanceLock.IsExpired() ? Time.secsToTimeString((ulong)(instanceLock.GetEffectiveExpiryTime() - now).TotalSeconds) : "-";

			handler.SendSysMessage(CypherStrings.CommandInstUnbindFailed,
									entries.Map.Id,
									entries.Map.MapName[Global.WorldMgr.DefaultDbcLocale],
									entries.MapDifficulty.DifficultyID,
									CliDB.DifficultyStorage.LookupByKey(entries.MapDifficulty.DifficultyID).Name,
									instanceLock.GetInstanceId(),
									handler.GetCypherString(instanceLock.IsExpired() ? CypherStrings.Yes : CypherStrings.No),
									handler.GetCypherString(instanceLock.IsExtended() ? CypherStrings.Yes : CypherStrings.No),
									timeleft);
		}

		player.SendRaidInfo();

		return true;
	}
}