// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using Forged.MapServer.DataStorage;
using Forged.MapServer.DataStorage.ClientReader;
using Forged.MapServer.DataStorage.Structs.B;
using Forged.MapServer.DataStorage.Structs.E;
using Forged.MapServer.DataStorage.Structs.M;
using Forged.MapServer.DataStorage.Structs.S;
using Forged.MapServer.Maps;
using Forged.MapServer.Maps.Grids;
using Forged.MapServer.Scripting;
using Forged.MapServer.Spells;
using Framework.Constants;
using Framework.Database;
using Framework.Util;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Forged.MapServer.Globals.Caching;

public class ScriptLoader : IObjectCache
{
    private readonly DB6Storage<BroadcastTextRecord> _broadcastTextRecords;
    private readonly CliDB _cliDB;
    private readonly IConfiguration _configuration;
    private readonly CreatureTemplateCache _creatureTemplateCache;
    private readonly DB6Storage<EmotesRecord> _emotesRecords;
    private readonly GameObjectCache _gameObjectCache;
    private readonly GameObjectTemplateCache _gameObjectTemplateCache;
    private readonly GridDefines _gridDefines;
    private readonly ItemTemplateCache _itemTemplateCache;
    private readonly MapManager _mapManager;
    private readonly DB6Storage<MapRecord> _mapRecords;
    private readonly QuestTemplateCache _questTemplateCache;
    private readonly ScriptManager _scriptManager;
    private readonly SpellManager _spellManager;
    private readonly DB6Storage<SpellNameRecord> _spellNameRecords;
    private readonly WorldDatabase _worldDatabase;

    public ScriptLoader(WorldDatabase worldDatabase, ScriptManager scriptManager, SpellManager spellManager, MapManager mapManager,
                        GameObjectTemplateCache gameObjectTemplateCache, QuestTemplateCache questTemplateCache, IConfiguration configuration,
                        GridDefines gridDefines, ItemTemplateCache itemTemplateCache, CreatureTemplateCache creatureTemplateCache,
                        GameObjectCache gameObjectCache, DB6Storage<SpellNameRecord> spellNameRecords, CliDB cliDB,
                        DB6Storage<BroadcastTextRecord> broadcastTextRecords, DB6Storage<EmotesRecord> emotesRecords, DB6Storage<MapRecord> mapRecords)
    {
        _worldDatabase = worldDatabase;
        _scriptManager = scriptManager;
        _spellManager = spellManager;
        _mapManager = mapManager;
        _gameObjectTemplateCache = gameObjectTemplateCache;
        _questTemplateCache = questTemplateCache;
        _configuration = configuration;
        _gridDefines = gridDefines;
        _itemTemplateCache = itemTemplateCache;
        _creatureTemplateCache = creatureTemplateCache;
        _gameObjectCache = gameObjectCache;
        _spellNameRecords = spellNameRecords;
        _cliDB = cliDB;
        _broadcastTextRecords = broadcastTextRecords;
        _emotesRecords = emotesRecords;
        _mapRecords = mapRecords;
    }

    public string GetScriptsTableNameByType(ScriptsType type)
    {
        return type switch
        {
            ScriptsType.Spell => "spell_scripts",
            ScriptsType.Event => "event_scripts",
            ScriptsType.Waypoint => "waypoint_scripts",
            _ => ""
        };
    }

    public void Load()
    {
        LoadEventScripts();
        LoadSpellScripts();
        LoadWaypointScripts();
    }

    public void LoadEventScripts()
    {
        LoadScripts(ScriptsType.Event);

        List<uint> evtScripts = new();

        // Load all possible script entries from gameobjects
        foreach (var go in _gameObjectTemplateCache.GameObjectTemplates)
        {
            var eventId = go.Value.GetEventScriptId();

            if (eventId != 0)
                evtScripts.Add(eventId);
        }

        // Load all possible script entries from spells
        foreach (var spellNameEntry in _spellNameRecords.Values)
        {
            var spell = _spellManager.GetSpellInfo(spellNameEntry.Id);

            if (spell == null)
                continue;

            evtScripts.AddRange(from spellEffectInfo in spell.Effects
                                where spellEffectInfo.IsEffectName(SpellEffectName.SendEvent)
                                where spellEffectInfo.MiscValue != 0
                                select (uint)spellEffectInfo.MiscValue);
        }

        foreach (var pathIdx in _cliDB.TaxiPathNodesByPath)
            for (uint nodeIdx = 0; nodeIdx < pathIdx.Value.Length; ++nodeIdx)
            {
                var node = pathIdx.Value[nodeIdx];

                if (node.ArrivalEventID != 0)
                    evtScripts.Add(node.ArrivalEventID);

                if (node.DepartureEventID != 0)
                    evtScripts.Add(node.DepartureEventID);
            }

        // Then check if all scripts are in above list of possible script entries
        foreach (var script in _scriptManager.EventScripts)
        {
            var id = evtScripts.Find(p => p == script.Key);

            if (id == 0)
                Log.Logger.Error("Table `event_scripts` has script (Id: {0}) not referring to any gameobject_template type 10 data2 field, type 3 data6 field, type 13 data 2 field or any spell effect {1}",
                                 script.Key,
                                 SpellEffectName.SendEvent);
        }
    }

    public void LoadSpellScripts()
    {
        LoadScripts(ScriptsType.Spell);

        // check ids
        foreach (var script in _scriptManager.SpellScripts)
        {
            var spellId = script.Key & 0x00FFFFFF;
            var spellInfo = _spellManager.GetSpellInfo(spellId);

            if (spellInfo == null)
            {
                Log.Logger.Error("Table `spell_scripts` has not existing spell (Id: {0}) as script id", spellId);

                continue;
            }

            var spellEffIndex = (byte)(script.Key >> 24 & 0x000000FF);

            if (spellEffIndex >= spellInfo.Effects.Count)
            {
                Log.Logger.Error($"Table `spell_scripts` has too high effect index {spellEffIndex} for spell (Id: {spellId}) as script id");

                continue;
            }

            //check for correct spellEffect
            if (spellInfo.GetEffect(spellEffIndex).Effect == 0 || spellInfo.GetEffect(spellEffIndex).Effect != SpellEffectName.ScriptEffect && spellInfo.GetEffect(spellEffIndex).Effect != SpellEffectName.Dummy)
                Log.Logger.Error($"Table `spell_scripts` - spell {spellId} effect {spellEffIndex} is not SPELL_EFFECT_SCRIPT_EFFECT or SPELL_EFFECT_DUMMY");
        }
    }

    public void LoadWaypointScripts()
    {
        LoadScripts(ScriptsType.Waypoint);

        List<uint> actionSet = new();

        foreach (var script in _scriptManager.WaypointScripts)
            actionSet.Add(script.Key);

        var stmt = _worldDatabase.GetPreparedStatement(WorldStatements.SEL_WAYPOINT_DATA_ACTION);
        var result = _worldDatabase.Query(stmt);

        if (!result.IsEmpty())
            do
            {
                var action = result.Read<uint>(0);

                actionSet.Remove(action);
            } while (result.NextRow());

        foreach (var id in actionSet)
            Log.Logger.Error("There is no waypoint which links to the waypoint script {0}", id);
    }

    private void LoadScripts(ScriptsType type)
    {
        var oldMSTime = Time.MSTime;

        var scripts = _scriptManager.GetScriptsMapByType(type);

        if (scripts == null)
            return;

        var tableName = GetScriptsTableNameByType(type);

        if (string.IsNullOrEmpty(tableName))
            return;

        if (_mapManager.IsScriptScheduled()) // function cannot be called when scripts are in use.
            return;

        Log.Logger.Information("Loading {0}...", tableName);

        scripts.Clear(); // need for reload support

        var isSpellScriptTable = type == ScriptsType.Spell;
        //                                         0    1       2         3         4          5    6  7  8  9
        var result = _worldDatabase.Query("SELECT id, delay, command, datalong, datalong2, dataint, x, y, z, o{0} FROM {1}", isSpellScriptTable ? ", effIndex" : "", tableName);

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 script definitions. DB table `{0}` is empty!", tableName);

            return;
        }

        uint count = 0;

        do
        {
            ScriptInfo tmp = new()
            {
                type = type,
                id = result.Read<uint>(0)
            };

            if (isSpellScriptTable)
                tmp.id |= result.Read<uint>(10) << 24;

            tmp.delay = result.Read<uint>(1);
            tmp.command = (ScriptCommands)result.Read<uint>(2);

            unsafe
            {
                tmp.Raw.nData[0] = result.Read<uint>(3);
                tmp.Raw.nData[1] = result.Read<uint>(4);
                tmp.Raw.nData[2] = (uint)result.Read<int>(5);
                tmp.Raw.fData[0] = result.Read<float>(6);
                tmp.Raw.fData[1] = result.Read<float>(7);
                tmp.Raw.fData[2] = result.Read<float>(8);
                tmp.Raw.fData[3] = result.Read<float>(9);
            }

            // generic command args check
            switch (tmp.command)
            {
                case ScriptCommands.Talk:
                    {
                        if (tmp.Talk.ChatType > ChatMsg.RaidBossWhisper)
                        {
                            if (_configuration.GetDefaultValue("load:autoclean", false))
                                _worldDatabase.Execute($"DELETE FROM {tableName} WHERE id = {tmp.id}");
                            else
                                Log.Logger.Error("Table `{0}` has invalid talk type (datalong = {1}) in SCRIPT_COMMAND_TALK for script id {2}",
                                                 tableName,
                                                 tmp.Talk.ChatType,
                                                 tmp.id);

                            continue;
                        }

                        if (!_broadcastTextRecords.ContainsKey((uint)tmp.Talk.TextID))
                        {
                            if (_configuration.GetDefaultValue("load:autoclean", false))
                                _worldDatabase.Execute($"DELETE FROM {tableName} WHERE id = {tmp.id}");
                            else
                                Log.Logger.Error("Table `{0}` has invalid talk text id (dataint = {1}) in SCRIPT_COMMAND_TALK for script id {2}",
                                                 tableName,
                                                 tmp.Talk.TextID,
                                                 tmp.id);

                            continue;
                        }

                        break;
                    }

                case ScriptCommands.Emote:
                    {
                        if (!_emotesRecords.ContainsKey(tmp.Emote.EmoteID))
                        {
                            if (_configuration.GetDefaultValue("load:autoclean", false))
                                _worldDatabase.Execute($"DELETE FROM {tableName} WHERE id = {tmp.id}");
                            else
                                Log.Logger.Error("Table `{0}` has invalid emote id (datalong = {1}) in SCRIPT_COMMAND_EMOTE for script id {2}",
                                                 tableName,
                                                 tmp.Emote.EmoteID,
                                                 tmp.id);

                            continue;
                        }

                        break;
                    }

                case ScriptCommands.TeleportTo:
                    {
                        if (!_mapRecords.ContainsKey(tmp.TeleportTo.MapID))
                        {
                            if (_configuration.GetDefaultValue("load:autoclean", false))
                                _worldDatabase.Execute($"DELETE FROM {tableName} WHERE id = {tmp.id}");
                            else
                                Log.Logger.Error("Table `{0}` has invalid map (Id: {1}) in SCRIPT_COMMAND_TELEPORT_TO for script id {2}",
                                                 tableName,
                                                 tmp.TeleportTo.MapID,
                                                 tmp.id);

                            continue;
                        }

                        if (!_gridDefines.IsValidMapCoord(tmp.TeleportTo.DestX, tmp.TeleportTo.DestY, tmp.TeleportTo.DestZ, tmp.TeleportTo.Orientation))
                        {
                            if (_configuration.GetDefaultValue("load:autoclean", false))
                                _worldDatabase.Execute($"DELETE FROM {tableName} WHERE id = {tmp.id}");
                            else
                                Log.Logger.Error("Table `{0}` has invalid coordinates (X: {1} Y: {2} Z: {3} O: {4}) in SCRIPT_COMMAND_TELEPORT_TO for script id {5}",
                                                 tableName,
                                                 tmp.TeleportTo.DestX,
                                                 tmp.TeleportTo.DestY,
                                                 tmp.TeleportTo.DestZ,
                                                 tmp.TeleportTo.Orientation,
                                                 tmp.id);

                            continue;
                        }

                        break;
                    }

                case ScriptCommands.QuestExplored:
                    {
                        var quest = _questTemplateCache.GetQuestTemplate(tmp.QuestExplored.QuestID);

                        if (quest == null)
                        {
                            if (_configuration.GetDefaultValue("load:autoclean", false))
                                _worldDatabase.Execute($"DELETE FROM {tableName} WHERE id = {tmp.id}");
                            else
                                Log.Logger.Error("Table `{0}` has invalid quest (ID: {1}) in SCRIPT_COMMAND_QUEST_EXPLORED in `datalong` for script id {2}",
                                                 tableName,
                                                 tmp.QuestExplored.QuestID,
                                                 tmp.id);

                            continue;
                        }

                        if (!quest.HasSpecialFlag(QuestSpecialFlags.ExplorationOrEvent))
                        {
                            Log.Logger.Error("Table `{0}` has quest (ID: {1}) in SCRIPT_COMMAND_QUEST_EXPLORED in `datalong` for script id {2}, but quest not have Id QUEST_SPECIAL_FLAGS_EXPLORATION_OR_EVENT in quest flags. Script command or quest flags wrong. QuestId modified to require objective.",
                                             tableName,
                                             tmp.QuestExplored.QuestID,
                                             tmp.id);

                            // this will prevent quest completing without objective
                            quest.SetSpecialFlag(QuestSpecialFlags.ExplorationOrEvent);

                            // continue; - quest objective requirement set and command can be allowed
                        }

                        if (tmp.QuestExplored.Distance > SharedConst.DefaultVisibilityDistance)
                        {
                            Log.Logger.Error("Table `{0}` has too large distance ({1}) for exploring objective complete in `datalong2` in SCRIPT_COMMAND_QUEST_EXPLORED in `datalong` for script id {2}",
                                             tableName,
                                             tmp.QuestExplored.Distance,
                                             tmp.id);

                            continue;
                        }

                        if (tmp.QuestExplored.Distance != 0 && tmp.QuestExplored.Distance > SharedConst.DefaultVisibilityDistance)
                        {
                            if (_configuration.GetDefaultValue("load:autoclean", false))
                                _worldDatabase.Execute($"DELETE FROM {tableName} WHERE id = {tmp.id}");
                            else
                                Log.Logger.Error("Table `{0}` has too large distance ({1}) for exploring objective complete in `datalong2` in SCRIPT_COMMAND_QUEST_EXPLORED in `datalong` for script id {2}, max distance is {3} or 0 for disable distance check",
                                                 tableName,
                                                 tmp.QuestExplored.Distance,
                                                 tmp.id,
                                                 SharedConst.DefaultVisibilityDistance);

                            continue;
                        }

                        if (tmp.QuestExplored.Distance != 0 && tmp.QuestExplored.Distance < SharedConst.InteractionDistance)
                        {
                            if (_configuration.GetDefaultValue("load:autoclean", false))
                                _worldDatabase.Execute($"DELETE FROM {tableName} WHERE id = {tmp.id}");
                            else
                                Log.Logger.Error("Table `{0}` has too small distance ({1}) for exploring objective complete in `datalong2` in SCRIPT_COMMAND_QUEST_EXPLORED in `datalong` for script id {2}, min distance is {3} or 0 for disable distance check",
                                                 tableName,
                                                 tmp.QuestExplored.Distance,
                                                 tmp.id,
                                                 SharedConst.InteractionDistance);

                            continue;
                        }

                        break;
                    }

                case ScriptCommands.KillCredit:
                    {
                        if (_creatureTemplateCache.GetCreatureTemplate(tmp.KillCredit.CreatureEntry) == null)
                        {
                            if (_configuration.GetDefaultValue("load:autoclean", false))
                                _worldDatabase.Execute($"DELETE FROM {tableName} WHERE id = {tmp.id}");
                            else
                                Log.Logger.Error("Table `{0}` has invalid creature (Entry: {1}) in SCRIPT_COMMAND_KILL_CREDIT for script id {2}",
                                                 tableName,
                                                 tmp.KillCredit.CreatureEntry,
                                                 tmp.id);

                            continue;
                        }

                        break;
                    }

                case ScriptCommands.RespawnGameobject:
                    {
                        var data = _gameObjectCache.GetGameObjectData(tmp.RespawnGameObject.GOGuid);

                        if (data == null)
                        {
                            if (_configuration.GetDefaultValue("load:autoclean", false))
                                _worldDatabase.Execute($"DELETE FROM {tableName} WHERE id = {tmp.id}");
                            else
                                Log.Logger.Error("Table `{0}` has invalid gameobject (GUID: {1}) in SCRIPT_COMMAND_RESPAWN_GAMEOBJECT for script id {2}",
                                                 tableName,
                                                 tmp.RespawnGameObject.GOGuid,
                                                 tmp.id);

                            continue;
                        }

                        var info = _gameObjectTemplateCache.GetGameObjectTemplate(data.Id);

                        if (info == null)
                        {
                            if (_configuration.GetDefaultValue("load:autoclean", false))
                                _worldDatabase.Execute($"DELETE FROM {tableName} WHERE id = {tmp.id}");
                            else
                                Log.Logger.Error("Table `{0}` has gameobject with invalid entry (GUID: {1} Entry: {2}) in SCRIPT_COMMAND_RESPAWN_GAMEOBJECT for script id {3}",
                                                 tableName,
                                                 tmp.RespawnGameObject.GOGuid,
                                                 data.Id,
                                                 tmp.id);

                            continue;
                        }

                        if (info.type is GameObjectTypes.FishingNode or GameObjectTypes.FishingHole or GameObjectTypes.Door or GameObjectTypes.Button or GameObjectTypes.Trap)
                        {
                            if (_configuration.GetDefaultValue("load:autoclean", false))
                                _worldDatabase.Execute($"DELETE FROM {tableName} WHERE id = {tmp.id}");
                            else
                                Log.Logger.Error("Table `{0}` have gameobject type ({1}) unsupported by command SCRIPT_COMMAND_RESPAWN_GAMEOBJECT for script id {2}",
                                                 tableName,
                                                 info.entry,
                                                 tmp.id);

                            continue;
                        }

                        break;
                    }

                case ScriptCommands.TempSummonCreature:
                    {
                        if (!_gridDefines.IsValidMapCoord(tmp.TempSummonCreature.PosX, tmp.TempSummonCreature.PosY, tmp.TempSummonCreature.PosZ, tmp.TempSummonCreature.Orientation))
                        {
                            if (_configuration.GetDefaultValue("load:autoclean", false))
                                _worldDatabase.Execute($"DELETE FROM {tableName} WHERE id = {tmp.id}");
                            else
                                Log.Logger.Error("Table `{0}` has invalid coordinates (X: {1} Y: {2} Z: {3} O: {4}) in SCRIPT_COMMAND_TEMP_SUMMON_CREATURE for script id {5}",
                                                 tableName,
                                                 tmp.TempSummonCreature.PosX,
                                                 tmp.TempSummonCreature.PosY,
                                                 tmp.TempSummonCreature.PosZ,
                                                 tmp.TempSummonCreature.Orientation,
                                                 tmp.id);

                            continue;
                        }

                        if (_creatureTemplateCache.GetCreatureTemplate(tmp.TempSummonCreature.CreatureEntry) == null)
                        {
                            if (_configuration.GetDefaultValue("load:autoclean", false))
                                _worldDatabase.Execute($"DELETE FROM {tableName} WHERE id = {tmp.id}");
                            else
                                Log.Logger.Error("Table `{0}` has invalid creature (Entry: {1}) in SCRIPT_COMMAND_TEMP_SUMMON_CREATURE for script id {2}",
                                                 tableName,
                                                 tmp.TempSummonCreature.CreatureEntry,
                                                 tmp.id);

                            continue;
                        }

                        break;
                    }

                case ScriptCommands.OpenDoor:
                case ScriptCommands.CloseDoor:
                    {
                        var data = _gameObjectCache.GetGameObjectData(tmp.ToggleDoor.GOGuid);

                        if (data == null)
                        {
                            if (_configuration.GetDefaultValue("load:autoclean", false))
                                _worldDatabase.Execute($"DELETE FROM {tableName} WHERE id = {tmp.id}");
                            else
                                Log.Logger.Error("Table `{0}` has invalid gameobject (GUID: {1}) in {2} for script id {3}",
                                                 tableName,
                                                 tmp.ToggleDoor.GOGuid,
                                                 tmp.command,
                                                 tmp.id);

                            continue;
                        }

                        var info = _gameObjectTemplateCache.GetGameObjectTemplate(data.Id);

                        if (info == null)
                        {
                            if (_configuration.GetDefaultValue("load:autoclean", false))
                                _worldDatabase.Execute($"DELETE FROM {tableName} WHERE id = {tmp.id}");
                            else
                                Log.Logger.Error("Table `{0}` has gameobject with invalid entry (GUID: {1} Entry: {2}) in {3} for script id {4}",
                                                 tableName,
                                                 tmp.ToggleDoor.GOGuid,
                                                 data.Id,
                                                 tmp.command,
                                                 tmp.id);

                            continue;
                        }

                        if (info.type != GameObjectTypes.Door)
                        {
                            if (_configuration.GetDefaultValue("load:autoclean", false))
                                _worldDatabase.Execute($"DELETE FROM {tableName} WHERE id = {tmp.id}");
                            else
                                Log.Logger.Error("Table `{0}` has gameobject type ({1}) non supported by command {2} for script id {3}",
                                                 tableName,
                                                 info.entry,
                                                 tmp.command,
                                                 tmp.id);

                            continue;
                        }

                        break;
                    }

                case ScriptCommands.RemoveAura:
                    {
                        if (!_spellManager.HasSpellInfo(tmp.RemoveAura.SpellID))
                        {
                            if (_configuration.GetDefaultValue("load:autoclean", false))
                                _worldDatabase.Execute($"DELETE FROM {tableName} WHERE id = {tmp.id}");
                            else
                                Log.Logger.Error("Table `{0}` using non-existent spell (id: {1}) in SCRIPT_COMMAND_REMOVE_AURA for script id {2}",
                                                 tableName,
                                                 tmp.RemoveAura.SpellID,
                                                 tmp.id);

                            continue;
                        }

                        if (Convert.ToBoolean((int)tmp.RemoveAura.Flags & ~0x1)) // 1 bits (0, 1)
                        {
                            if (_configuration.GetDefaultValue("load:autoclean", false))
                                _worldDatabase.Execute($"DELETE FROM {tableName} WHERE id = {tmp.id}");
                            else
                                Log.Logger.Error("Table `{0}` using unknown flags in datalong2 ({1}) in SCRIPT_COMMAND_REMOVE_AURA for script id {2}",
                                                 tableName,
                                                 tmp.RemoveAura.Flags,
                                                 tmp.id);

                            continue;
                        }

                        break;
                    }

                case ScriptCommands.CastSpell:
                    {
                        if (!_spellManager.HasSpellInfo(tmp.CastSpell.SpellID))
                        {
                            if (_configuration.GetDefaultValue("load:autoclean", false))
                                _worldDatabase.Execute($"DELETE FROM {tableName} WHERE id = {tmp.id}");
                            else
                                Log.Logger.Error("Table `{0}` using non-existent spell (id: {1}) in SCRIPT_COMMAND_CAST_SPELL for script id {2}",
                                                 tableName,
                                                 tmp.CastSpell.SpellID,
                                                 tmp.id);

                            continue;
                        }

                        if ((int)tmp.CastSpell.Flags > 4) // targeting type
                        {
                            if (_configuration.GetDefaultValue("load:autoclean", false))
                                _worldDatabase.Execute($"DELETE FROM {tableName} WHERE id = {tmp.id}");
                            else
                                Log.Logger.Error("Table `{0}` using unknown target in datalong2 ({1}) in SCRIPT_COMMAND_CAST_SPELL for script id {2}",
                                                 tableName,
                                                 tmp.CastSpell.Flags,
                                                 tmp.id);

                            continue;
                        }

                        if ((int)tmp.CastSpell.Flags != 4 && Convert.ToBoolean(tmp.CastSpell.CreatureEntry & ~0x1)) // 1 bit (0, 1)
                        {
                            if (_configuration.GetDefaultValue("load:autoclean", false))
                                _worldDatabase.Execute($"DELETE FROM {tableName} WHERE id = {tmp.id}");
                            else
                                Log.Logger.Error("Table `{0}` using unknown flags in dataint ({1}) in SCRIPT_COMMAND_CAST_SPELL for script id {2}",
                                                 tableName,
                                                 tmp.CastSpell.CreatureEntry,
                                                 tmp.id);

                            continue;
                        }

                        if ((int)tmp.CastSpell.Flags == 4 && _creatureTemplateCache.GetCreatureTemplate((uint)tmp.CastSpell.CreatureEntry) == null)
                        {
                            if (_configuration.GetDefaultValue("load:autoclean", false))
                                _worldDatabase.Execute($"DELETE FROM {tableName} WHERE id = {tmp.id}");
                            else
                                Log.Logger.Error("Table `{0}` using invalid creature entry in dataint ({1}) in SCRIPT_COMMAND_CAST_SPELL for script id {2}",
                                                 tableName,
                                                 tmp.CastSpell.CreatureEntry,
                                                 tmp.id);

                            continue;
                        }

                        break;
                    }

                case ScriptCommands.CreateItem:
                    {
                        if (_itemTemplateCache.GetItemTemplate(tmp.CreateItem.ItemEntry) == null)
                        {
                            if (_configuration.GetDefaultValue("load:autoclean", false))
                                _worldDatabase.Execute($"DELETE FROM {tableName} WHERE id = {tmp.id}");
                            else
                                Log.Logger.Error("Table `{0}` has nonexistent item (entry: {1}) in SCRIPT_COMMAND_CREATE_ITEM for script id {2}",
                                                 tableName,
                                                 tmp.CreateItem.ItemEntry,
                                                 tmp.id);

                            continue;
                        }

                        if (tmp.CreateItem.Amount == 0)
                        {
                            if (_configuration.GetDefaultValue("load:autoclean", false))
                                _worldDatabase.Execute($"DELETE FROM {tableName} WHERE id = {tmp.id}");
                            else
                                Log.Logger.Error("Table `{0}` SCRIPT_COMMAND_CREATE_ITEM but amount is {1} for script id {2}",
                                                 tableName,
                                                 tmp.CreateItem.Amount,
                                                 tmp.id);

                            continue;
                        }

                        break;
                    }
                case ScriptCommands.PlayAnimkit:
                    {
                        if (!_cliDB.AnimKitStorage.ContainsKey(tmp.PlayAnimKit.AnimKitID))
                        {
                            if (_configuration.GetDefaultValue("load:autoclean", false))
                                _worldDatabase.Execute($"DELETE FROM {tableName} WHERE id = {tmp.id}");
                            else
                                Log.Logger.Error("Table `{0}` has invalid AnimKid id (datalong = {1}) in SCRIPT_COMMAND_PLAY_ANIMKIT for script id {2}",
                                                 tableName,
                                                 tmp.PlayAnimKit.AnimKitID,
                                                 tmp.id);

                            continue;
                        }

                        break;
                    }
                case ScriptCommands.FieldSetDeprecated:
                case ScriptCommands.FlagSetDeprecated:
                case ScriptCommands.FlagRemoveDeprecated:
                    {
                        if (_configuration.GetDefaultValue("load:autoclean", false))
                            _worldDatabase.Execute($"DELETE FROM {tableName} WHERE id = {tmp.id}");
                        else
                            Log.Logger.Error($"Table `{tableName}` uses deprecated direct updatefield modify command {tmp.command} for script id {tmp.id}");

                        continue;
                    }
            }

            if (!scripts.ContainsKey(tmp.id))
                scripts[tmp.id] = new MultiMap<uint, ScriptInfo>();

            scripts[tmp.id].Add(tmp.delay, tmp);

            ++count;
        } while (result.NextRow());

        Log.Logger.Information("Loaded {0} script definitions in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
    }
}