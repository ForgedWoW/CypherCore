// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Globals;
using Forged.MapServer.Globals.Caching;
using Forged.MapServer.Maps;
using Forged.MapServer.Maps.Grids;
using Forged.MapServer.Phasing;
using Framework.Constants;
using Framework.Database;

namespace Forged.MapServer.Chat.Commands;

[CommandGroup("tele")]
internal class TeleCommands
{
    private static bool DoNameTeleport(CommandHandler handler, PlayerIdentifier player, uint mapId, Position pos, string locationName)
    {
        if (!handler.ClassFactory.Resolve<GridDefines>().IsValidMapCoord(mapId, pos) || handler.ObjectManager.IsTransportMap(mapId))
        {
            handler.SendSysMessage(CypherStrings.InvalidTargetCoord, pos.X, pos.Y, mapId);

            return false;
        }

        var target = player.GetConnectedPlayer();

        if (target != null)
        {
            // check online security
            if (handler.HasLowerSecurity(target, ObjectGuid.Empty))
                return false;

            var chrNameLink = handler.PlayerLink(target.GetName());

            if (target.IsBeingTeleported)
            {
                handler.SendSysMessage(CypherStrings.IsTeleported, chrNameLink);

                return false;
            }

            handler.SendSysMessage(CypherStrings.TeleportingTo, chrNameLink, "", locationName);

            if (handler.NeedReportToTarget(target))
                target.SendSysMessage(CypherStrings.TeleportedToBy, handler.NameLink);

            // stop flight if need
            if (target.IsInFlight)
                target.FinishTaxiFlight();
            else
                target.SaveRecallPosition(); // save only in non-flight case

            target.TeleportTo(new WorldLocation(mapId, pos));
        }
        else
        {
            // check offline security
            if (handler.HasLowerSecurity(null, player.GetGUID()))
                return false;

            var nameLink = handler.PlayerLink(player.GetName());

            handler.SendSysMessage(CypherStrings.TeleportingTo, nameLink, handler.GetCypherString(CypherStrings.Offline), locationName);

            handler.ClassFactory.Resolve<PlayerComputators>().SavePositionInDB(new WorldLocation(mapId, pos), handler.ClassFactory.Resolve<TerrainManager>().GetZoneId(handler.ClassFactory.Resolve<PhasingHandler>().EmptyPhaseShift, new WorldLocation(mapId, pos)), player.GetGUID());
        }

        return true;
    }

    [Command("add", RBACPermissions.CommandTeleAdd)]
    private static bool HandleTeleAddCommand(CommandHandler handler, string name)
    {
        var player = handler.Player;

        if (player == null)
            return false;

        if (handler.ClassFactory.Resolve<GameTeleObjectCache>().GetGameTeleExactName(name) != null)
        {
            handler.SendSysMessage(CypherStrings.CommandTpAlreadyexist);

            return false;
        }

        GameTele tele = new()
        {
            PosX = player.Location.X,
            PosY = player.Location.Y,
            PosZ = player.Location.Z,
            Orientation = player.Location.Orientation,
            MapId = player.Location.MapId,
            Name = name,
            NameLow = name.ToLowerInvariant()
        };

        if (handler.ClassFactory.Resolve<GameTeleObjectCache>().AddGameTele(tele))
            handler.SendSysMessage(CypherStrings.CommandTpAdded);
        else
        {
            handler.SendSysMessage(CypherStrings.CommandTpAddedErr);

            return false;
        }

        return true;
    }

    [Command("", RBACPermissions.CommandTele)]
    private static bool HandleTeleCommand(CommandHandler handler, GameTele tele)
    {
        if (tele == null)
        {
            handler.SendSysMessage(CypherStrings.CommandTeleNotfound);

            return false;
        }

        var player = handler.Player;

        if (player.IsInCombat && !handler.Session.HasPermission(RBACPermissions.CommandTeleName))
        {
            handler.SendSysMessage(CypherStrings.YouInCombat);

            return false;
        }

        var map = handler.CliDB.MapStorage.LookupByKey(tele.MapId);

        if (map == null || (map.IsBattlegroundOrArena() && (player.Location.MapId != tele.MapId || !player.IsGameMaster)))
        {
            handler.SendSysMessage(CypherStrings.CannotTeleToBg);

            return false;
        }

        // stop flight if need
        if (player.IsInFlight)
            player.FinishTaxiFlight();
        else
            player.SaveRecallPosition(); // save only in non-flight case

        player.TeleportTo(tele.MapId, tele.PosX, tele.PosY, tele.PosZ, tele.Orientation);

        return true;
    }

    [Command("del", RBACPermissions.CommandTeleDel, true)]
    private static bool HandleTeleDelCommand(CommandHandler handler, GameTele tele)
    {
        if (tele == null)
        {
            handler.SendSysMessage(CypherStrings.CommandTeleNotfound);

            return false;
        }

        handler.ClassFactory.Resolve<GameTeleObjectCache>().DeleteGameTele(tele.Name);
        handler.SendSysMessage(CypherStrings.CommandTpDeleted);

        return true;
    }

    [Command("group", RBACPermissions.CommandTeleGroup)]
    private static bool HandleTeleGroupCommand(CommandHandler handler, GameTele tele)
    {
        if (tele == null)
        {
            handler.SendSysMessage(CypherStrings.CommandTeleNotfound);

            return false;
        }

        var target = handler.SelectedPlayer;

        if (target == null)
        {
            handler.SendSysMessage(CypherStrings.NoCharSelected);

            return false;
        }

        // check online security
        if (handler.HasLowerSecurity(target, ObjectGuid.Empty))
            return false;

        var map = handler.CliDB.MapStorage.LookupByKey(tele.MapId);

        if (map == null || map.IsBattlegroundOrArena())
        {
            handler.SendSysMessage(CypherStrings.CannotTeleToBg);

            return false;
        }

        var nameLink = handler.GetNameLink(target);

        var grp = target.Group;

        if (grp == null)
        {
            handler.SendSysMessage(CypherStrings.NotInGroup, nameLink);

            return false;
        }

        for (var refe = grp.FirstMember; refe != null; refe = refe.Next())
        {
            var player = refe.Source;

            if (player?.Session == null)
                continue;

            // check online security
            if (handler.HasLowerSecurity(player, ObjectGuid.Empty))
                return false;

            var plNameLink = handler.GetNameLink(player);

            if (player.IsBeingTeleported)
            {
                handler.SendSysMessage(CypherStrings.IsTeleported, plNameLink);

                continue;
            }

            handler.SendSysMessage(CypherStrings.TeleportingTo, plNameLink, "", tele.Name);

            if (handler.NeedReportToTarget(player))
                player.SendSysMessage(CypherStrings.TeleportedToBy, nameLink);

            // stop flight if need
            if (player.IsInFlight)
                player.FinishTaxiFlight();
            else
                player.SaveRecallPosition(); // save only in non-flight case

            player.TeleportTo(tele.MapId, tele.PosX, tele.PosY, tele.PosZ, tele.Orientation);
        }

        return true;
    }

    [CommandGroup("name")]
    private class TeleNameCommands
    {
        [Command("", RBACPermissions.CommandTeleName, true)]
        private static bool HandleTeleNameCommand(CommandHandler handler, [OptionalArg] PlayerIdentifier player, [VariantArg(typeof(GameTele), typeof(string))] object where)
        {
            player = player switch
            {
                null => PlayerIdentifier.FromTargetOrSelf(handler),
                _    => player
            };

            if (player == null)
                return false;

            var target = player.GetConnectedPlayer();

            if (where is string && where.Equals("$home")) // References target's homebind
            {
                if (target != null)
                    target.TeleportTo(target.Homebind);
                else
                {
                    var stmt = handler.ClassFactory.Resolve<CharacterDatabase>().GetPreparedStatement(CharStatements.SEL_CHAR_HOMEBIND);
                    stmt.AddValue(0, player.GetGUID().Counter);
                    var result = handler.ClassFactory.Resolve<CharacterDatabase>().Query(stmt);

                    if (!result.IsEmpty())
                    {
                        WorldLocation loc = new(result.Read<ushort>(0), result.Read<float>(2), result.Read<float>(3), result.Read<float>(4));
                        uint zoneId = result.Read<ushort>(1);

                        handler.ClassFactory.Resolve<PlayerComputators>().SavePositionInDB(loc, zoneId, player.GetGUID());
                    }
                }

                return true;
            }

            // id, or string, or [name] Shift-click form |color|Htele:id|h[name]|h|r
            var tele = where as GameTele;

            return DoNameTeleport(handler, player, tele.MapId, new Position(tele.PosX, tele.PosY, tele.PosZ, tele.Orientation), tele.Name);
        }

        [CommandGroup("npc")]
        private class TeleNameNpcCommands
        {
            [Command("id", RBACPermissions.CommandTeleName, true)]
            private static bool HandleTeleNameNpcIdCommand(CommandHandler handler, PlayerIdentifier player, uint creatureId)
            {
                if (player == null)
                    return false;

                CreatureData spawnpoint = null;

                foreach (var (id, creatureData) in handler.ObjectManager.AllCreatureData)
                {
                    if (id != creatureId)
                        continue;

                    if (spawnpoint == null)
                        spawnpoint = creatureData;
                    else
                    {
                        handler.SendSysMessage(CypherStrings.CommandGocreatmultiple);

                        break;
                    }
                }

                if (spawnpoint == null)
                {
                    handler.SendSysMessage(CypherStrings.CommandGocreatnotfound);

                    return false;
                }

                var creatureTemplate = handler.ObjectManager.CreatureTemplateCache.GetCreatureTemplate(creatureId);

                return DoNameTeleport(handler, player, spawnpoint.MapId, spawnpoint.SpawnPoint, creatureTemplate.Name);
            }

            [Command("name", RBACPermissions.CommandTeleName, true)]
            private static bool HandleTeleNameNpcNameCommand(CommandHandler handler, PlayerIdentifier player, Tail name)
            {
                string normalizedName = name;

                if (player == null)
                    return false;

                WorldDatabase.EscapeString(ref normalizedName);

                var result = handler.ClassFactory.Resolve<WorldDatabase>().Query($"SELECT c.position_x, c.position_y, c.position_z, c.orientation, c.map, ct.name FROM creature c INNER JOIN creature_template ct ON c.id = ct.entry WHERE ct.name LIKE '{normalizedName}'");

                if (result.IsEmpty())
                {
                    handler.SendSysMessage(CypherStrings.CommandGocreatnotfound);

                    return false;
                }

                if (result.NextRow())
                    handler.SendSysMessage(CypherStrings.CommandGocreatmultiple);

                return DoNameTeleport(handler, player, result.Read<ushort>(4), new Position(result.Read<float>(0), result.Read<float>(1), result.Read<float>(2), result.Read<float>(3)), result.Read<string>(5));
            }

            [Command("guid", RBACPermissions.CommandTeleName, true)]
            private static bool HandleTeleNameNpcSpawnIdCommand(CommandHandler handler, PlayerIdentifier player, ulong spawnId)
            {
                if (player == null)
                    return false;

                var spawnpoint = handler.ObjectManager.GetCreatureData(spawnId);

                if (spawnpoint == null)
                {
                    handler.SendSysMessage(CypherStrings.CommandGocreatnotfound);

                    return false;
                }

                var creatureTemplate = handler.ObjectManager.CreatureTemplateCache.GetCreatureTemplate(spawnpoint.Id);

                return DoNameTeleport(handler, player, spawnpoint.MapId, spawnpoint.SpawnPoint, creatureTemplate.Name);
            }
        }
    }
}