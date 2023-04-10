// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Chrono;
using Forged.MapServer.DataStorage;
using Forged.MapServer.Maps;
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

        player = player switch
        {
            null => PlayerIdentifier.FromSelf(handler),
            _    => player
        };

        if (player.IsConnected())
        {
            handler.SendSysMessage(CypherStrings.PlayerNotFound);

            return false;
        }

        var map = player.GetConnectedPlayer().Location.Map.ToInstanceMap;

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

        player = player switch
        {
            null => handler.Session.Player,
            _    => player
        };

        var now = GameTime.DateAndTime;
        var instanceLocks = handler.ClassFactory.Resolve<InstanceLockManager>().GetInstanceLocksForPlayer(player.GUID);

        foreach (var instanceLock in instanceLocks)
        {
            MapDb2Entries entries = new(instanceLock.GetMapId(), instanceLock.GetDifficultyId());
            var timeleft = !instanceLock.IsExpired() ? Time.SecsToTimeString((ulong)(instanceLock.GetEffectiveExpiryTime() - now).TotalSeconds) : "-";

            handler.SendSysMessage(CypherStrings.CommandListBindInfo,
                                   entries.Map.Id,
                                   entries.Map.MapName[handler.WorldManager.DefaultDbcLocale],
                                   entries.MapDifficulty.DifficultyID,
                                   handler.CliDB.DifficultyStorage.LookupByKey(entries.MapDifficulty.DifficultyID).Name,
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

        player = player switch
        {
            null => PlayerIdentifier.FromSelf(handler),
            _    => player
        };

        if (!player.IsConnected())
        {
            handler.SendSysMessage(CypherStrings.PlayerNotFound);

            return false;
        }

        var map = player.GetConnectedPlayer().Location.Map.ToInstanceMap;

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
        handler.SendSysMessage("instances loaded: {0}", handler.ClassFactory.Resolve<MapManager>().GetNumInstances());
        handler.SendSysMessage("players in instances: {0}", handler.ClassFactory.Resolve<MapManager>().GetNumPlayersInInstances());

        var statistics = handler.ClassFactory.Resolve<InstanceLockManager>().GetStatistics();

        handler.SendSysMessage(CypherStrings.CommandInstStatSaves, statistics.InstanceCount);
        handler.SendSysMessage(CypherStrings.CommandInstStatPlayersbound, statistics.PlayerCount);

        return true;
    }

    [Command("unbind", RBACPermissions.CommandInstanceUnbind)]
    private static bool HandleInstanceUnbindCommand(CommandHandler handler, [VariantArg(typeof(uint), typeof(string))] object mapArg, uint? difficultyArg)
    {
        var player = handler.SelectedPlayer;

        player = player switch
        {
            null => handler.Session.Player,
            _    => player
        };

        uint? mapId = null;
        Difficulty? difficulty = null;

        mapId = mapArg switch
        {
            uint arg => arg,
            _        => mapId
        };

        if (difficultyArg.HasValue && handler.CliDB.DifficultyStorage.ContainsKey(difficultyArg.Value))
            difficulty = (Difficulty)difficultyArg;

        List<InstanceLock> locksReset = new();
        List<InstanceLock> locksNotReset = new();

        handler.ClassFactory.Resolve<InstanceLockManager>().ResetInstanceLocksForPlayer(player.GUID, mapId, difficulty, locksReset, locksNotReset);

        var now = GameTime.DateAndTime;

        foreach (var instanceLock in locksReset)
        {
            MapDb2Entries entries = new(instanceLock.GetMapId(), instanceLock.GetDifficultyId());
            var timeleft = !instanceLock.IsExpired() ? Time.SecsToTimeString((ulong)(instanceLock.GetEffectiveExpiryTime() - now).TotalSeconds) : "-";

            handler.SendSysMessage(CypherStrings.CommandInstUnbindUnbinding,
                                   entries.Map.Id,
                                   entries.Map.MapName[handler.WorldManager.DefaultDbcLocale],
                                   entries.MapDifficulty.DifficultyID,
                                   handler.CliDB.DifficultyStorage.LookupByKey(entries.MapDifficulty.DifficultyID).Name,
                                   instanceLock.GetInstanceId(),
                                   handler.GetCypherString(instanceLock.IsExpired() ? CypherStrings.Yes : CypherStrings.No),
                                   handler.GetCypherString(instanceLock.IsExtended() ? CypherStrings.Yes : CypherStrings.No),
                                   timeleft);
        }

        handler.SendSysMessage(CypherStrings.CommandInstUnbindUnbound, locksReset.Count);

        foreach (var instanceLock in locksNotReset)
        {
            MapDb2Entries entries = new(instanceLock.GetMapId(), instanceLock.GetDifficultyId());
            var timeleft = !instanceLock.IsExpired() ? Time.SecsToTimeString((ulong)(instanceLock.GetEffectiveExpiryTime() - now).TotalSeconds) : "-";

            handler.SendSysMessage(CypherStrings.CommandInstUnbindFailed,
                                   entries.Map.Id,
                                   entries.Map.MapName[handler.WorldManager.DefaultDbcLocale],
                                   entries.MapDifficulty.DifficultyID,
                                   handler.CliDB.DifficultyStorage.LookupByKey(entries.MapDifficulty.DifficultyID).Name,
                                   instanceLock.GetInstanceId(),
                                   handler.GetCypherString(instanceLock.IsExpired() ? CypherStrings.Yes : CypherStrings.No),
                                   handler.GetCypherString(instanceLock.IsExtended() ? CypherStrings.Yes : CypherStrings.No),
                                   timeleft);
        }

        player.SendRaidInfo();

        return true;
    }
}