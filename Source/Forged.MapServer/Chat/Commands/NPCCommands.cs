﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Chrono;
using Forged.MapServer.Entities;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.LootManagement;
using Forged.MapServer.Maps;
using Forged.MapServer.Movement.Generators;
using Forged.MapServer.Phasing;
using Framework.Collections;
using Framework.Constants;
using Framework.Database;
using Framework.IO;
using Framework.Util;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Forged.MapServer.Chat.Commands;

[CommandGroup("npc")]
internal class NPCCommands
{
    private static void _IterateNotNormalLootMap(CommandHandler handler, MultiMap<ObjectGuid, NotNormalLootItem> map, List<LootItem> items)
    {
        foreach (var key in map.Keys)
        {
            if (map[key].Empty())
                continue;

            var list = map[key];

            var player = handler.ObjectAccessor.FindConnectedPlayer(key);
            handler.SendSysMessage(CypherStrings.CommandNpcShowlootSublabel, player != null ? player.GetName() : $"Offline player (GUID {key})", list.Count);

            foreach (var it in list)
            {
                var item = items[it.LootListId];

                if (!it.IsLooted && !item.IsLooted)
                    _ShowLootEntry(handler, item.Itemid, item.Count, true);
            }
        }
    }

    private static void _ShowLootEntry(CommandHandler handler, uint itemId, byte itemCount, bool alternateString = false)
    {
        var name = "Unknown item";

        var itemTemplate = handler.ObjectManager.GetItemTemplate(itemId);

        if (itemTemplate != null)
            name = itemTemplate.GetName(handler.SessionDbcLocale);

        handler.SendSysMessage(alternateString ? CypherStrings.CommandNpcShowlootEntry2 : CypherStrings.CommandNpcShowlootEntry,
                               itemCount,
                               ItemConst.ItemQualityColors[(int)(itemTemplate != null ? itemTemplate.Quality : ItemQuality.Poor)],
                               itemId,
                               name,
                               itemId);
    }

    [Command("despawngroup", RBACPermissions.CommandNpcDespawngroup)]
    private static bool HandleNpcDespawnGroup(CommandHandler handler, string[] opts)
    {
        if (opts.Empty())
            return false;

        var deleteRespawnTimes = false;
        uint groupId = 0;

        // Decode arguments
        foreach (var variant in opts)
            if (!uint.TryParse(variant, out groupId))
                deleteRespawnTimes = true;

        var player = handler.Session.Player;

        if (!player.Location.Map.SpawnGroupDespawn(groupId, deleteRespawnTimes, out var despawnedCount))
        {
            handler.SendSysMessage(CypherStrings.SpawngroupBadgroup, groupId);

            return false;
        }

        handler.SendSysMessage($"Despawned a total of {despawnedCount} objects.");

        return true;
    }

    [Command("evade", RBACPermissions.CommandNpcEvade)]
    private static bool HandleNpcEvadeCommand(CommandHandler handler, EvadeReason? why, string force)
    {
        var creatureTarget = handler.SelectedCreature;

        if (creatureTarget == null || creatureTarget.IsPet)
        {
            handler.SendSysMessage(CypherStrings.SelectCreature);

            return false;
        }

        if (!creatureTarget.IsAIEnabled)
        {
            handler.SendSysMessage(CypherStrings.CreatureNotAiEnabled);

            return false;
        }

        if (force.Equals("force", StringComparison.OrdinalIgnoreCase))
            creatureTarget.ClearUnitState(UnitState.Evade);

        creatureTarget.AI.EnterEvadeMode(why.GetValueOrDefault(EvadeReason.Other));

        return true;
    }

    [Command("info", RBACPermissions.CommandNpcInfo)]
    private static bool HandleNpcInfoCommand(CommandHandler handler)
    {
        var target = handler.SelectedCreature;

        if (target == null)
        {
            handler.SendSysMessage(CypherStrings.SelectCreature);

            return false;
        }

        var cInfo = target.Template;

        var faction = target.Faction;
        var npcflags = (ulong)target.UnitData.NpcFlags[1] << 32 | target.UnitData.NpcFlags[0];
        var mechanicImmuneMask = cInfo.MechanicImmuneMask;
        var displayid = target.DisplayId;
        var nativeid = target.NativeDisplayId;
        var entry = target.Entry;

        var curRespawnDelay = target.RespawnCompatibilityMode ? target.RespawnTimeEx - GameTime.CurrentTime : target.Location.Map.GetCreatureRespawnTime(target.SpawnId) - GameTime.CurrentTime;

        curRespawnDelay = curRespawnDelay switch
        {
            < 0 => 0,
            _   => curRespawnDelay
        };

        var curRespawnDelayStr = Time.SecsToTimeString((ulong)curRespawnDelay, TimeFormat.ShortText);
        var defRespawnDelayStr = Time.SecsToTimeString(target.RespawnDelay, TimeFormat.ShortText);

        handler.SendSysMessage(CypherStrings.NpcinfoChar, target.GetName(), target.SpawnId, target.GUID.ToString(), entry, faction, npcflags, displayid, nativeid);

        if (target.CreatureData != null && target.CreatureData.SpawnGroupData.GroupId != 0)
        {
            var groupData = target.CreatureData.SpawnGroupData;
            handler.SendSysMessage(CypherStrings.SpawninfoGroupId, groupData.Name, groupData.GroupId, groupData.Flags, target.Location.Map.IsSpawnGroupActive(groupData.GroupId));
        }

        handler.SendSysMessage(CypherStrings.SpawninfoCompatibilityMode, target.RespawnCompatibilityMode);
        handler.SendSysMessage(CypherStrings.NpcinfoLevel, target.Level);
        handler.SendSysMessage(CypherStrings.NpcinfoEquipment, target.CurrentEquipmentId, target.OriginalEquipmentId);
        handler.SendSysMessage(CypherStrings.NpcinfoHealth, target.GetCreateHealth(), target.MaxHealth, target.Health);
        handler.SendSysMessage(CypherStrings.NpcinfoMovementData, target.MovementTemplate.ToString());

        handler.SendSysMessage(CypherStrings.NpcinfoUnitFieldFlags, (uint)target.UnitData.Flags);

        foreach (UnitFlags value in Enum.GetValues(typeof(UnitFlags)))
            if (target.HasUnitFlag(value))
                handler.SendSysMessage("{0} (0x{1:X})", value, value);

        handler.SendSysMessage(CypherStrings.NpcinfoUnitFieldFlags2, (uint)target.UnitData.Flags2);

        foreach (UnitFlags2 value in Enum.GetValues(typeof(UnitFlags2)))
            if (target.HasUnitFlag2(value))
                handler.SendSysMessage("{0} (0x{1:X})", value, value);

        handler.SendSysMessage(CypherStrings.NpcinfoUnitFieldFlags3, (uint)target.UnitData.Flags3);

        foreach (UnitFlags3 value in Enum.GetValues(typeof(UnitFlags3)))
            if (target.HasUnitFlag3(value))
                handler.SendSysMessage("{0} (0x{1:X})", value, value);

        handler.SendSysMessage(CypherStrings.NpcinfoDynamicFlags, target.DynamicFlags);
        handler.SendSysMessage(CypherStrings.CommandRawpawntimes, defRespawnDelayStr, curRespawnDelayStr);
        handler.SendSysMessage(CypherStrings.NpcinfoLoot, cInfo.LootId, cInfo.PickPocketId, cInfo.SkinLootId);
        handler.SendSysMessage(CypherStrings.NpcinfoDungeonId, target.InstanceId);

        var data = handler.ObjectManager.GetCreatureData(target.SpawnId);

        if (data != null)
            handler.SendSysMessage(CypherStrings.NpcinfoPhases, data.PhaseId, data.PhaseGroup);

        handler.ClassFactory.Resolve<PhasingHandler>().PrintToChat(handler, target);

        handler.SendSysMessage(CypherStrings.NpcinfoArmor, target.GetArmor());
        handler.SendSysMessage(CypherStrings.NpcinfoPosition, target.Location.X, target.Location.Y, target.Location.Z);
        handler.SendSysMessage(CypherStrings.ObjectinfoAiInfo, target.GetAIName(), target.GetScriptName());
        handler.SendSysMessage(CypherStrings.ObjectinfoStringIds, target.StringIds[0], target.StringIds[1], target.StringIds[2]);
        handler.SendSysMessage(CypherStrings.NpcinfoReactstate, target.ReactState);
        var ai = target.AI;

        if (ai != null)
            handler.SendSysMessage(CypherStrings.ObjectinfoAiType, nameof(ai));

        handler.SendSysMessage(CypherStrings.NpcinfoFlagsExtra, cInfo.FlagsExtra);

        foreach (uint value in Enum.GetValues(typeof(CreatureFlagsExtra)))
            if (cInfo.FlagsExtra.HasAnyFlag((CreatureFlagsExtra)value))
                handler.SendSysMessage("{0} (0x{1:X})", (CreatureFlagsExtra)value, value);

        handler.SendSysMessage(CypherStrings.NpcinfoNpcFlags, target.UnitData.NpcFlags[0]);

        foreach (uint value in Enum.GetValues(typeof(NPCFlags)))
            if (npcflags.HasAnyFlag(value))
                handler.SendSysMessage("{0} (0x{1:X})", (NPCFlags)value, value);

        handler.SendSysMessage(CypherStrings.NpcinfoMechanicImmune, mechanicImmuneMask);

        foreach (int value in Enum.GetValues(typeof(Mechanics)))
            if (Convert.ToBoolean(mechanicImmuneMask & (1ul << (value - 1))))
                handler.SendSysMessage("{0} (0x{1:X})", (Mechanics)value, value);

        return true;
    }

    [Command("move", RBACPermissions.CommandNpcMove)]
    private static bool HandleNpcMoveCommand(CommandHandler handler, ulong? spawnId)
    {
        var creature = handler.SelectedCreature;
        var player = handler.Session.Player;

        if (player == null)
            return false;

        if (!spawnId.HasValue && creature == null)
            return false;

        var lowguid = spawnId ?? creature.SpawnId;

        // Attempting creature load from DB data
        var data = handler.ObjectManager.GetCreatureData(lowguid);

        if (data == null)
        {
            handler.SendSysMessage(CypherStrings.CommandCreatguidnotfound, lowguid);

            return false;
        }

        if (player.Location.MapId != data.MapId)
        {
            handler.SendSysMessage(CypherStrings.CommandCreatureatsamemap, lowguid);

            return false;
        }

        handler.ObjectManager.RemoveCreatureFromGrid(data);
        data.SpawnPoint.Relocate(player.Location);
        handler.ObjectManager.AddCreatureToGrid(data);

        // update position in DB
        var stmt = handler.ClassFactory.Resolve<WorldDatabase>().GetPreparedStatement(WorldStatements.UPD_CREATURE_POSITION);
        stmt.AddValue(0, player.Location.X);
        stmt.AddValue(1, player.Location.Y);
        stmt.AddValue(2, player.Location.Z);
        stmt.AddValue(3, player.Location.Orientation);
        stmt.AddValue(4, lowguid);

        handler.ClassFactory.Resolve<WorldDatabase>().Execute(stmt);

        // respawn selected creature at the new location
        creature?.DespawnOrUnsummon(TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(1));

        handler.SendSysMessage(CypherStrings.CommandCreaturemoved);

        return true;
    }

    [Command("near", RBACPermissions.CommandNpcNear)]
    private static bool HandleNpcNearCommand(CommandHandler handler, float? dist)
    {
        var distance = dist.GetValueOrDefault(10.0f);
        uint count = 0;

        var player = handler.Player;

        var stmt = handler.ClassFactory.Resolve<WorldDatabase>().GetPreparedStatement(WorldStatements.SEL_CREATURE_NEAREST);
        stmt.AddValue(0, player.Location.X);
        stmt.AddValue(1, player.Location.Y);
        stmt.AddValue(2, player.Location.Z);
        stmt.AddValue(3, player.Location.MapId);
        stmt.AddValue(4, player.Location.X);
        stmt.AddValue(5, player.Location.Y);
        stmt.AddValue(6, player.Location.Z);
        stmt.AddValue(7, distance * distance);
        var result = handler.ClassFactory.Resolve<WorldDatabase>().Query(stmt);

        if (!result.IsEmpty())
            do
            {
                var guid = result.Read<ulong>(0);
                var entry = result.Read<uint>(1);
                var x = result.Read<float>(2);
                var y = result.Read<float>(3);
                var z = result.Read<float>(4);
                var mapId = result.Read<ushort>(5);

                var creatureTemplate = handler.ObjectManager.GetCreatureTemplate(entry);

                if (creatureTemplate == null)
                    continue;

                handler.SendSysMessage(CypherStrings.CreatureListChat, guid, guid, creatureTemplate.Name, x, y, z, mapId, "", "");

                ++count;
            } while (result.NextRow());

        handler.SendSysMessage(CypherStrings.CommandNearNpcMessage, distance, count);

        return true;
    }

    [Command("playemote", RBACPermissions.CommandNpcPlayemote)]
    private static bool HandleNpcPlayEmoteCommand(CommandHandler handler, uint emote)
    {
        var target = handler.SelectedCreature;

        if (target == null)
        {
            handler.SendSysMessage(CypherStrings.SelectCreature);

            return false;
        }

        target.EmoteState = (Emote)emote;

        return true;
    }

    [Command("say", RBACPermissions.CommandNpcSay)]
    private static bool HandleNpcSayCommand(CommandHandler handler, Tail text)
    {
        if (text.IsEmpty())
            return false;

        var creature = handler.SelectedCreature;

        if (creature == null)
        {
            handler.SendSysMessage(CypherStrings.SelectCreature);

            return false;
        }

        creature.Say(text, Language.Universal);

        // make some emotes
        switch (((string)text).LastOrDefault())
        {
            case '?':
                creature.HandleEmoteCommand(Emote.OneshotQuestion);

                break;

            case '!':
                creature.HandleEmoteCommand(Emote.OneshotExclamation);

                break;

            default:
                creature.HandleEmoteCommand(Emote.OneshotTalk);

                break;
        }

        return true;
    }

    [Command("showloot", RBACPermissions.CommandNpcShowloot)]
    private static bool HandleNpcShowLootCommand(CommandHandler handler, string all)
    {
        var creatureTarget = handler.SelectedCreature;

        if (creatureTarget == null || creatureTarget.IsPet)
        {
            handler.SendSysMessage(CypherStrings.SelectCreature);

            return false;
        }

        var loot = creatureTarget.Loot;

        if (!creatureTarget.IsDead || loot == null || loot.IsLooted())
        {
            handler.SendSysMessage(CypherStrings.CommandNotDeadOrNoLoot, creatureTarget.GetName());

            return false;
        }

        handler.SendSysMessage(CypherStrings.CommandNpcShowlootHeader, creatureTarget.GetName(), creatureTarget.Entry);
        handler.SendSysMessage(CypherStrings.CommandNpcShowlootMoney, loot.Gold / MoneyConstants.Gold, loot.Gold % MoneyConstants.Gold / MoneyConstants.Silver, loot.Gold % MoneyConstants.Silver);

        if (all.Equals("all", StringComparison.OrdinalIgnoreCase)) // nonzero from strcmp <. not equal
        {
            handler.SendSysMessage(CypherStrings.CommandNpcShowlootLabel, "Standard items", loot.Items.Count);

            foreach (var item in loot.Items)
                if (!item.IsLooted)
                    _ShowLootEntry(handler, item.Itemid, item.Count);
        }
        else
        {
            handler.SendSysMessage(CypherStrings.CommandNpcShowlootLabel, "Standard items", loot.Items.Count);

            foreach (var item in loot.Items)
                if (!item.IsLooted && !item.Freeforall && item.Conditions.Empty())
                    _ShowLootEntry(handler, item.Itemid, item.Count);

            if (!loot.PlayerFFAItems.Empty())
            {
                handler.SendSysMessage(CypherStrings.CommandNpcShowlootLabel2, "FFA items per allowed player");
                _IterateNotNormalLootMap(handler, loot.PlayerFFAItems, loot.Items);
            }
        }

        return true;
    }

    [Command("spawngroup", RBACPermissions.CommandNpcSpawngroup)]
    private static bool HandleNpcSpawnGroup(CommandHandler handler, string[] opts)
    {
        if (opts.Empty())
            return false;

        var ignoreRespawn = false;
        var force = false;
        uint groupId = 0;

        // Decode arguments
        foreach (var variant in opts)
            switch (variant)
            {
                case "force":
                    force = true;

                    break;

                case "ignorerespawn":
                    ignoreRespawn = true;

                    break;

                default:
                    uint.TryParse(variant, out groupId);

                    break;
            }

        var player = handler.Session.Player;

        List<WorldObject> creatureList = new();

        if (!player.Location.Map.SpawnGroupSpawn(groupId, ignoreRespawn, force, creatureList))
        {
            handler.SendSysMessage(CypherStrings.SpawngroupBadgroup, groupId);

            return false;
        }

        handler.SendSysMessage(CypherStrings.SpawngroupSpawncount, creatureList.Count);

        return true;
    }

    [Command("tame", RBACPermissions.CommandNpcTame)]
    private static bool HandleNpcTameCommand(CommandHandler handler)
    {
        var creatureTarget = handler.SelectedCreature;

        if (creatureTarget == null || creatureTarget.IsPet)
        {
            handler.SendSysMessage(CypherStrings.SelectCreature);

            return false;
        }

        var player = handler.Session.Player;

        if (!player.PetGUID.IsEmpty)
        {
            handler.SendSysMessage(CypherStrings.YouAlreadyHavePet);

            return false;
        }

        var cInfo = creatureTarget.Template;

        if (!cInfo.IsTameable(player.CanTameExoticPets))
        {
            handler.SendSysMessage(CypherStrings.CreatureNonTameable, cInfo.Entry);

            return false;
        }

        // Everything looks OK, create new pet
        var pet = player.CreateTamedPetFrom(creatureTarget);

        if (pet == null)
        {
            handler.SendSysMessage(CypherStrings.CreatureNonTameable, cInfo.Entry);

            return false;
        }

        // place pet before player
        var pos = new Position();
        player.Location.GetClosePoint(pos, creatureTarget.CombatReach, SharedConst.ContactDistance);
        pos.Orientation = MathFunctions.PI - player.Location.Orientation;
        pet.Location.Relocate(pos);

        // set pet to defensive mode by default (some classes can't control controlled pets in fact).
        pet.
            // set pet to defensive mode by default (some classes can't control controlled pets in fact).
            ReactState = ReactStates.Defensive;

        // calculate proper level
        var level = Math.Max(player.Level - 5, creatureTarget.Level);

        // prepare visual effect for levelup
        pet.SetLevel(level - 1);

        // add to world
        pet.Location.
            // add to world
            Map.AddToMap(pet.AsCreature);

        // visual effect for levelup
        pet.SetLevel(level);

        // caster have pet now
        player.SetMinion(pet, true);

        pet.SavePetToDB(PetSaveMode.AsCurrent);
        player.PetSpellInitialize();

        return true;
    }

    [Command("textemote", RBACPermissions.CommandNpcTextemote)]
    private static bool HandleNpcTextEmoteCommand(CommandHandler handler, Tail text)
    {
        var creature = handler.SelectedCreature;

        if (creature == null)
        {
            handler.SendSysMessage(CypherStrings.SelectCreature);

            return false;
        }

        creature.TextEmote(text);

        return true;
    }

    [Command("whisper", RBACPermissions.CommandNpcWhisper)]
    private static bool HandleNpcWhisperCommand(CommandHandler handler, string recv, Tail text)
    {
        if (text.IsEmpty())
        {
            handler.SendSysMessage(CypherStrings.CmdSyntax);

            return false;
        }

        var creature = handler.SelectedCreature;

        if (creature == null)
        {
            handler.SendSysMessage(CypherStrings.SelectCreature);

            return false;
        }

        // check online security
        var receiver = handler.ObjectAccessor.FindPlayerByName(recv);

        if (handler.HasLowerSecurity(receiver, ObjectGuid.Empty))
            return false;

        creature.Whisper(text, Language.Universal, receiver);

        return true;
    }

    [Command("yell", RBACPermissions.CommandNpcYell)]
    private static bool HandleNpcYellCommand(CommandHandler handler, Tail text)
    {
        if (text.IsEmpty())
            return false;

        var creature = handler.SelectedCreature;

        if (creature == null )
        {
            handler.SendSysMessage(CypherStrings.SelectCreature);

            return false;
        }

        creature.Yell(text);

        // make an emote
        creature.HandleEmoteCommand(Emote.OneshotShout);

        return true;
    }

    [CommandGroup("add")]
    private class AddCommands
    {
        [Command("", RBACPermissions.CommandNpcAdd)]
        private static bool HandleNpcAddCommand(CommandHandler handler, uint id)
        {
            if (handler.Session.Player.GameObjectManager.GetCreatureTemplate(id) == null)
                return false;

            var chr = handler.Session.Player;
            var map = chr.Location.Map;

            var trans = chr.GetTransport<Transport>();

            if (trans != null)
            {
                var guid = handler.Session.Player.GameObjectManager.GenerateCreatureSpawnId();
                var data = handler.Session.Player.GameObjectManager.NewOrExistCreatureData(guid);
                data.SpawnId = guid;
                data.SpawnGroupData = handler.Session.Player.GameObjectManager.GetDefaultSpawnGroup();
                data.Id = id;
                data.SpawnPoint.Relocate(chr.MovementInfo.Transport.Pos.X, chr.MovementInfo.Transport.Pos.Y, chr.MovementInfo.Transport.Pos.Z, chr.MovementInfo.Transport.Pos.Orientation);
                data.SpawnGroupData = new SpawnGroupTemplateData();

                var creaturePassenger = trans.CreateNPCPassenger(guid, data);

                if (creaturePassenger != null)
                {
                    creaturePassenger.SaveToDB((uint)trans.Template.MoTransport.SpawnMap,
                                               new List<Difficulty>
                                               {
                                                   map.DifficultyID
                                               });

                    handler.Session.Player.GameObjectManager.AddCreatureToGrid(data);
                }

                return true;
            }

            var creature = handler.ClassFactory.Resolve<CreatureFactory>().CreateCreature(id, map, chr.Location);

            if (creature == null)
                return false;

            handler.ClassFactory.Resolve<PhasingHandler>().InheritPhaseShift(creature, chr);

            creature.SaveToDB(map.Id,
                              new List<Difficulty>
                              {
                                  map.DifficultyID
                              });

            var dbGUID = creature.SpawnId;

            // To call _LoadGoods(); _LoadQuests(); CreateTrainerSpells()
            // current "creature" variable is deleted and created fresh new, otherwise old values might trigger asserts or cause undefined behavior
            creature.CleanupsBeforeDelete();
            creature = handler.ClassFactory.Resolve<CreatureFactory>().CreateCreatureFromDB(dbGUID, map, true, true);

            if (creature == null)
                return false;

            handler.Session.Player.GameObjectManager.AddCreatureToGrid(handler.Session.Player.GameObjectManager.GetCreatureData(dbGUID));

            return true;
        }

        [Command("formation", RBACPermissions.CommandNpcAddFormation)]
        private static bool HandleNpcAddFormationCommand(CommandHandler handler, ulong leaderGUID)
        {
            var creature = handler.SelectedCreature;

            if (creature == null || creature.SpawnId == 0)
            {
                handler.SendSysMessage(CypherStrings.SelectCreature);

                return false;
            }

            var lowguid = creature.SpawnId;

            if (creature.Formation != null)
            {
                handler.SendSysMessage("Selected creature is already member of group {0}", creature.Formation.LeaderSpawnId);

                return false;
            }

            if (lowguid == 0)
                return false;

            var chr = handler.Session.Player;
            var followAngle = (creature.Location.GetAbsoluteAngle(chr.Location) - chr.Location.Orientation) * 180.0f / MathF.PI;
            var followDist = MathF.Sqrt(MathF.Pow(chr.Location.X - creature.Location.X, 2f) + MathF.Pow(chr.Location.Y - creature.Location.Y, 2f));
            uint groupAI = 0;
            handler.ClassFactory.Resolve<FormationMgr>().AddFormationMember(lowguid, followAngle, followDist, leaderGUID, groupAI);
            creature.SearchFormation();

            var stmt = handler.ClassFactory.Resolve<WorldDatabase>().GetPreparedStatement(WorldStatements.INS_CREATURE_FORMATION);
            stmt.AddValue(0, leaderGUID);
            stmt.AddValue(1, lowguid);
            stmt.AddValue(2, followAngle);
            stmt.AddValue(3, followDist);
            stmt.AddValue(4, groupAI);

            handler.ClassFactory.Resolve<WorldDatabase>().Execute(stmt);

            handler.SendSysMessage("Creature {0} added to formation with leader {1}", lowguid, leaderGUID);

            return true;
        }

        [Command("move", RBACPermissions.CommandNpcAddMove)]
        private static bool HandleNpcAddMoveCommand(CommandHandler handler, ulong lowGuid)
        {
            // attempt check creature existence by DB data
            var data = handler.ObjectManager.GetCreatureData(lowGuid);

            if (data == null)
            {
                handler.SendSysMessage(CypherStrings.CommandCreatguidnotfound, lowGuid);

                return false;
            }

            // Update movement type
            var stmt = handler.ClassFactory.Resolve<WorldDatabase>().GetPreparedStatement(WorldStatements.UPD_CREATURE_MOVEMENT_TYPE);
            stmt.AddValue(0, (byte)MovementGeneratorType.Waypoint);
            stmt.AddValue(1, lowGuid);
            handler.ClassFactory.Resolve<WorldDatabase>().Execute(stmt);

            handler.SendSysMessage(CypherStrings.WaypointAdded);

            return true;
        }

        [Command("temp", RBACPermissions.CommandNpcAddTemp)]
        private static bool HandleNpcAddTempSpawnCommand(CommandHandler handler, [OptionalArg] string lootStr, uint id)
        {
            var loot = false;

            if (!lootStr.IsEmpty())
            {
                if (lootStr.Equals("loot", StringComparison.OrdinalIgnoreCase))
                    loot = true;
                else if (lootStr.Equals("noloot", StringComparison.OrdinalIgnoreCase))
                    loot = false;
                else
                    return false;
            }

            if (handler.ObjectManager.GetCreatureTemplate(id) == null)
                return false;

            var chr = handler.Session.Player;
            chr.SummonCreature(id, chr.Location, loot ? TempSummonType.CorpseTimedDespawn : TempSummonType.CorpseDespawn, TimeSpan.FromSeconds(30));

            return true;
        }

        [Command("item", RBACPermissions.CommandNpcAddItem)]
        private static bool HandleNpcAddVendorItemCommand(CommandHandler handler, uint itemId, uint? mc, uint? it, uint? ec, [OptionalArg] string bonusListIds)
        {
            if (itemId == 0)
            {
                handler.SendSysMessage(CypherStrings.CommandNeeditemsend);

                return false;
            }

            var vendor = handler.SelectedCreature;

            if (vendor == null)
            {
                handler.SendSysMessage(CypherStrings.SelectCreature);

                return false;
            }

            var maxcount = mc.GetValueOrDefault(0);
            var incrtime = it.GetValueOrDefault(0);
            var extendedcost = ec.GetValueOrDefault(0);
            var vendorEntry = vendor.Entry;

            VendorItem vItem = new()
            {
                Item = itemId,
                Maxcount = maxcount,
                Incrtime = incrtime,
                ExtendedCost = extendedcost,
                Type = ItemVendorType.Item
            };

            if (!bonusListIds.IsEmpty())
            {
                var bonusListIDsTok = new StringArray(bonusListIds, ';');

                if (!bonusListIDsTok.IsEmpty())
                    foreach (string token in bonusListIDsTok)
                        if (uint.TryParse(token, out var id))
                            vItem.BonusListIDs.Add(id);
            }

            if (!handler.ObjectManager.IsVendorItemValid(vendorEntry, vItem, handler.Session.Player))
                return false;

            handler.ObjectManager.AddVendorItem(vendorEntry, vItem);

            var itemTemplate = handler.ObjectManager.GetItemTemplate(itemId);

            handler.SendSysMessage(CypherStrings.ItemAddedToList, itemId, itemTemplate.GetName(), maxcount, incrtime, extendedcost);

            return true;
        }
    }

    [CommandGroup("delete")]
    private class DeleteCommands
    {
        [Command("", RBACPermissions.CommandNpcDelete)]
        private static bool HandleNpcDeleteCommand(CommandHandler handler, ulong? spawnIdArg)
        {
            ulong spawnId;

            if (spawnIdArg.HasValue)
                spawnId = spawnIdArg.Value;
            else
            {
                var creature = handler.SelectedCreature;

                if (creature == null || creature.IsPet || creature.IsTotem)
                {
                    handler.SendSysMessage(CypherStrings.SelectCreature);

                    return false;
                }

                var summon = creature.ToTempSummon();

                if (summon != null)
                {
                    summon.UnSummon();
                    handler.SendSysMessage(CypherStrings.CommandDelcreatmessage);

                    return true;
                }

                spawnId = creature.SpawnId;
            }

            if (handler.ClassFactory.Resolve<CreatureFactory>().DeleteFromDB(spawnId))
            {
                handler.SendSysMessage(CypherStrings.CommandDelcreatmessage);

                return true;
            }

            handler.SendSysMessage(CypherStrings.CommandCreatguidnotfound, spawnId);

            return false;
        }

        [Command("item", RBACPermissions.CommandNpcDeleteItem)]
        private static bool HandleNpcDeleteVendorItemCommand(CommandHandler handler, uint itemId)
        {
            var vendor = handler.SelectedCreature;

            if (vendor == null || !vendor.IsVendor)
            {
                handler.SendSysMessage(CypherStrings.CommandVendorselection);

                return false;
            }

            if (itemId == 0)
                return false;

            if (!handler.ObjectManager.RemoveVendorItem(vendor.Entry, itemId, ItemVendorType.Item))
            {
                handler.SendSysMessage(CypherStrings.ItemNotInList, itemId);

                return false;
            }

            var itemTemplate = handler.ObjectManager.GetItemTemplate(itemId);
            handler.SendSysMessage(CypherStrings.ItemDeletedFromList, itemId, itemTemplate.GetName());

            return true;
        }
    }

    [CommandGroup("follow")]
    private class FollowCommands
    {
        [Command("", RBACPermissions.CommandNpcFollow)]
        private static bool HandleNpcFollowCommand(CommandHandler handler)
        {
            var player = handler.Session.Player;
            var creature = handler.SelectedCreature;

            if (creature == null)
            {
                handler.SendSysMessage(CypherStrings.SelectCreature);

                return false;
            }

            // Follow player - Using pet's default dist and angle
            creature.
                // Follow player - Using pet's default dist and angle
                MotionMaster.MoveFollow(player, SharedConst.PetFollowDist, creature.FollowAngle);

            handler.SendSysMessage(CypherStrings.CreatureFollowYouNow, creature.GetName());

            return true;
        }

        [Command("stop", RBACPermissions.CommandNpcFollowStop)]
        private static bool HandleNpcUnFollowCommand(CommandHandler handler)
        {
            var player = handler.Player;
            var creature = handler.SelectedCreature;

            if (creature == null)
            {
                handler.SendSysMessage(CypherStrings.SelectCreature);

                return false;
            }

            var movement = creature.MotionMaster.GetMovementGenerator(a =>
            {
                if (a.GetMovementGeneratorType() == MovementGeneratorType.Follow)
                {
                    var followMovement = a as FollowMovementGenerator;

                    return followMovement != null && followMovement.GetTarget() == player;
                }

                return false;
            });

            if (movement != null)
            {
                handler.SendSysMessage(CypherStrings.CreatureNotFollowYou, creature.GetName());

                return false;
            }

            creature.MotionMaster.Remove(movement);
            handler.SendSysMessage(CypherStrings.CreatureNotFollowYouNow, creature.GetName());

            return true;
        }
    }

    [CommandGroup("set")]
    private class SetCommands
    {
        [Command("allowmove", RBACPermissions.CommandNpcSetAllowmove)]
        private static bool HandleNpcSetAllowMovementCommand(CommandHandler handler)
        {
            /*
            if (handler.WorldManager.getAllowMovement())
            {
                handler.WorldManager.SetAllowMovement(false);
                handler.SendSysMessage(LANG_CREATURE_MOVE_DISABLED);
            }
            else
            {
                handler.WorldManager.SetAllowMovement(true);
                handler.SendSysMessage(LANG_CREATURE_MOVE_ENABLED);
            }
            */
            return true;
        }

        [Command("data", RBACPermissions.CommandNpcSetData)]
        private static bool HandleNpcSetDataCommand(CommandHandler handler, uint data1, uint data2)
        {
            var creature = handler.SelectedCreature;

            if (creature == null)
            {
                handler.SendSysMessage(CypherStrings.SelectCreature);

                return false;
            }

            creature.AI.SetData(data1, data2);
            var aIorScript = creature.GetAIName() != "" ? "AI type: " + creature.GetAIName() : creature.GetScriptName() != "" ? "Script Name: " + creature.GetScriptName() : "No AI or Script Name Set";
            handler.SendSysMessage(CypherStrings.NpcSetdata, creature.GUID, creature.Entry, creature.GetName(), data1, data2, aIorScript);

            return true;
        }

        [Command("entry", RBACPermissions.CommandNpcSetEntry)]
        private static bool HandleNpcSetEntryCommand(CommandHandler handler, uint newEntryNum)
        {
            if (newEntryNum == 0)
                return false;

            var unit = handler.SelectedUnit;

            if (unit == null || !unit.IsTypeId(TypeId.Unit))
            {
                handler.SendSysMessage(CypherStrings.SelectCreature);

                return false;
            }

            var creature = unit.AsCreature;

            if (creature.UpdateEntry(newEntryNum))
                handler.SendSysMessage(CypherStrings.Done);
            else
                handler.SendSysMessage(CypherStrings.Error);

            return true;
        }

        [Command("factionid", RBACPermissions.CommandNpcSetFactionid)]
        private static bool HandleNpcSetFactionIdCommand(CommandHandler handler, uint factionId)
        {
            if (!handler.CliDB.FactionTemplateStorage.ContainsKey(factionId))
            {
                handler.SendSysMessage(CypherStrings.WrongFaction, factionId);

                return false;
            }

            var creature = handler.SelectedCreature;

            if (creature == null)
            {
                handler.SendSysMessage(CypherStrings.SelectCreature);

                return false;
            }

            creature.Faction = factionId;

            // Faction is set in creature_template - not inside creature

            // Update in memory..
            var cinfo = creature.Template;

            if (cinfo != null)
                cinfo.Faction = factionId;

            // ..and DB
            var stmt = handler.ClassFactory.Resolve<WorldDatabase>().GetPreparedStatement(WorldStatements.UPD_CREATURE_FACTION);

            stmt.AddValue(0, factionId);
            stmt.AddValue(1, factionId);
            stmt.AddValue(2, creature.Entry);

            handler.ClassFactory.Resolve<WorldDatabase>().Execute(stmt);

            return true;
        }

        [Command("Id", RBACPermissions.CommandNpcSetFlag)]
        private static bool HandleNpcSetFlagCommand(CommandHandler handler, NPCFlags npcFlags, NPCFlags2 npcFlags2)
        {
            var creature = handler.SelectedCreature;

            if (creature == null)
            {
                handler.SendSysMessage(CypherStrings.SelectCreature);

                return false;
            }

            creature.ReplaceAllNpcFlags(npcFlags);
            creature.ReplaceAllNpcFlags2(npcFlags2);

            var stmt = handler.ClassFactory.Resolve<WorldDatabase>().GetPreparedStatement(WorldStatements.UPD_CREATURE_NPCFLAG);
            stmt.AddValue(0, (ulong)npcFlags | ((ulong)npcFlags2 << 32));
            stmt.AddValue(1, creature.Entry);
            handler.ClassFactory.Resolve<WorldDatabase>().Execute(stmt);

            handler.SendSysMessage(CypherStrings.ValueSavedRejoin);

            return true;
        }

        [Command("level", RBACPermissions.CommandNpcSetLevel)]
        private static bool HandleNpcSetLevelCommand(CommandHandler handler, byte lvl)
        {
            if (lvl < 1 || lvl > handler.Configuration.GetDefaultValue("MaxPlayerLevel", SharedConst.DefaultMaxLevel) + 3)
            {
                handler.SendSysMessage(CypherStrings.BadValue);

                return false;
            }

            var creature = handler.SelectedCreature;

            if (creature == null || creature.IsPet)
            {
                handler.SendSysMessage(CypherStrings.SelectCreature);

                return false;
            }

            creature.SetMaxHealth((uint)(100 + 30 * lvl));
            creature.SetHealth((uint)(100 + 30 * lvl));
            creature.SetLevel(lvl);
            creature.SaveToDB();

            return true;
        }

        [Command("link", RBACPermissions.CommandNpcSetLink)]
        private static bool HandleNpcSetLinkCommand(CommandHandler handler, ulong linkguid)
        {
            var creature = handler.SelectedCreature;

            if (creature != null)
            {
                handler.SendSysMessage(CypherStrings.SelectCreature);

                return false;
            }

            if (creature.SpawnId == 0)
            {
                handler.SendSysMessage("Selected creature {0} isn't in creature table", creature.GUID.ToString());

                return false;
            }

            if (!handler.ObjectManager.SetCreatureLinkedRespawn(creature.SpawnId, linkguid))
            {
                handler.SendSysMessage("Selected creature can't link with guid '{0}'", linkguid);

                return false;
            }

            handler.SendSysMessage("LinkGUID '{0}' added to creature with DBTableGUID: '{1}'", linkguid, creature.SpawnId);

            return true;
        }

        [Command("model", RBACPermissions.CommandNpcSetModel)]
        private static bool HandleNpcSetModelCommand(CommandHandler handler, uint displayId)
        {
            var creature = handler.SelectedCreature;

            if (creature == null || creature.IsPet)
            {
                handler.SendSysMessage(CypherStrings.SelectCreature);

                return false;
            }

            if (!handler.CliDB.CreatureDisplayInfoStorage.ContainsKey(displayId))
            {
                handler.SendSysMessage(CypherStrings.CommandInvalidParam, displayId);

                return false;
            }

            creature.SetDisplayId(displayId);
            creature.SetNativeDisplayId(displayId);

            creature.SaveToDB();

            return true;
        }

        [Command("movetype", RBACPermissions.CommandNpcSetMovetype)]
        private static bool HandleNpcSetMoveTypeCommand(CommandHandler handler, ulong? lowGuid, string type, string nodel)
        {
            // 3 arguments:
            // GUID (optional - you can also select the creature)
            // stay|random|way (determines the kind of movement)
            // NODEL (optional - tells the system NOT to delete any waypoints)
            //        this is very handy if you want to do waypoints, that are
            //        later switched on/off according to special events (like escort
            //        quests, etc)
            var doNotDelete = !nodel.IsEmpty();

            ulong lowguid = 0;
            Creature creature = null;

            if (!lowGuid.HasValue) // case .setmovetype $move_type (with selected creature)
            {
                creature = handler.SelectedCreature;

                if (creature == null || creature.IsPet)
                    return false;

                lowguid = creature.SpawnId;
            }
            else
            {
                lowguid = lowGuid.Value;

                if (lowguid != 0)
                    creature = handler.GetCreatureFromPlayerMapByDbGuid(lowguid);

                // attempt check creature existence by DB data
                if (creature == null)
                {
                    var data = handler.ObjectManager.GetCreatureData(lowguid);

                    if (data == null)
                    {
                        handler.SendSysMessage(CypherStrings.CommandCreatguidnotfound, lowguid);

                        return false;
                    }
                }
                else
                    lowguid = creature.SpawnId;
            }

            // now lowguid is low guid really existed creature
            // and creature point (maybe) to this creature or NULL

            MovementGeneratorType moveType;

            switch (type)
            {
                case "stay":
                    moveType = MovementGeneratorType.Idle;

                    break;

                case "random":
                    moveType = MovementGeneratorType.Random;

                    break;

                case "way":
                    moveType = MovementGeneratorType.Waypoint;

                    break;

                default:
                    return false;
            }

            if (creature != null)
            {
                // update movement type
                if (!doNotDelete)
                    creature.LoadPath(0);

                creature.SetDefaultMovementType(moveType);
                creature.MotionMaster.Initialize();

                if (creature.IsAlive) // dead creature will reset movement generator at respawn
                {
                    creature.SetDeathState(DeathState.JustDied);
                    creature.Respawn();
                }

                creature.SaveToDB();
            }

            if (!doNotDelete)
                handler.SendSysMessage(CypherStrings.MoveTypeSet, type);
            else
                handler.SendSysMessage(CypherStrings.MoveTypeSetNodel, type);

            return true;
        }

        [Command("phase", RBACPermissions.CommandNpcSetPhase)]
        private static bool HandleNpcSetPhaseCommand(CommandHandler handler, uint phaseId)
        {
            if (phaseId == 0)
            {
                handler.SendSysMessage(CypherStrings.PhaseNotfound);

                return false;
            }

            var creature = handler.SelectedCreature;

            if (creature == null || creature.IsPet)
            {
                handler.SendSysMessage(CypherStrings.SelectCreature);

                return false;
            }

            PhasingHandler phasingHandler = handler.ClassFactory.Resolve<PhasingHandler>();
            phasingHandler.ResetPhaseShift(creature);
            phasingHandler.AddPhase(creature, phaseId, true);
            creature.Location.DBPhase = (int)phaseId;

            creature.SaveToDB();

            return true;
        }

        [Command("phasegroup", RBACPermissions.CommandNpcSetPhase)]
        private static bool HandleNpcSetPhaseGroup(CommandHandler handler, StringArguments args)
        {
            if (args.Empty())
                return false;

            var phaseGroupId = args.NextInt32();

            var creature = handler.SelectedCreature;

            if (creature == null || creature.IsPet)
            {
                handler.SendSysMessage(CypherStrings.SelectCreature);

                return false;
            }

            PhasingHandler phasingHandler = handler.ClassFactory.Resolve<PhasingHandler>();
            phasingHandler.ResetPhaseShift(creature);
            phasingHandler.AddPhaseGroup(creature, (uint)phaseGroupId, true);
            creature.Location.DBPhase = -phaseGroupId;

            creature.SaveToDB();

            return true;
        }

        [Command("spawntime", RBACPermissions.CommandNpcSetSpawntime)]
        private static bool HandleNpcSetSpawnTimeCommand(CommandHandler handler, uint spawnTime)
        {
            var creature = handler.SelectedCreature;

            if (creature == null)
                return false;

            var stmt = handler.ClassFactory.Resolve<WorldDatabase>().GetPreparedStatement(WorldStatements.UPD_CREATURE_SPAWN_TIME_SECS);
            stmt.AddValue(0, spawnTime);
            stmt.AddValue(1, creature.SpawnId);
            handler.ClassFactory.Resolve<WorldDatabase>().Execute(stmt);

            creature.RespawnDelay = spawnTime;
            handler.SendSysMessage(CypherStrings.CommandSpawntime, spawnTime);

            return true;
        }

        [Command("wanderdistance", RBACPermissions.CommandNpcSetSpawndist)]
        private static bool HandleNpcSetWanderDistanceCommand(CommandHandler handler, float option)
        {
            if (option < 0.0f)
            {
                handler.SendSysMessage(CypherStrings.BadValue);

                return false;
            }

            var mtype = option switch
            {
                > 0.0f => MovementGeneratorType.Random,
                _      => MovementGeneratorType.Idle
            };

            var creature = handler.SelectedCreature;
            ulong guidLow;

            if (creature != null)
                guidLow = creature.SpawnId;
            else
                return false;

            creature.WanderDistance = option;
            creature.SetDefaultMovementType(mtype);
            creature.MotionMaster.Initialize();

            if (creature.IsAlive) // dead creature will reset movement generator at respawn
            {
                creature.SetDeathState(DeathState.JustDied);
                creature.Respawn();
            }

            var stmt = handler.ClassFactory.Resolve<WorldDatabase>().GetPreparedStatement(WorldStatements.UPD_CREATURE_WANDER_DISTANCE);
            stmt.AddValue(0, option);
            stmt.AddValue(1, (byte)mtype);
            stmt.AddValue(2, guidLow);

            handler.ClassFactory.Resolve<WorldDatabase>().Execute(stmt);

            handler.SendSysMessage(CypherStrings.CommandWanderDistance, option);

            return true;
        }
    }
}