// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.Achievements;
using Forged.MapServer.DataStorage;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Globals;
using Forged.MapServer.Spells;
using Framework.Collections;
using Framework.Constants;
using Framework.Database;
using Framework.Util;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Forged.MapServer.Conditions;

public class DisableManager
{
    private readonly CliDB _cliDB;
    private readonly IConfiguration _configuration;
    private readonly CriteriaManager _criteriaManager;
    private readonly DB2Manager _db2Manager;
    private readonly Dictionary<DisableType, Dictionary<uint, DisableData>> _disableMap = new();
    private readonly GameObjectManager _objectManager;
    private readonly SpellManager _spellManager;
    private readonly WorldDatabase _worldDatabase;

    public DisableManager(WorldDatabase worldDatabase, IConfiguration configuration, CliDB cliDB, SpellManager spellManager,
                          DB2Manager db2Manager, CriteriaManager criteriaManager, GameObjectManager objectManager)
    {
        _worldDatabase = worldDatabase;
        _configuration = configuration;
        _cliDB = cliDB;
        _spellManager = spellManager;
        _db2Manager = db2Manager;
        _criteriaManager = criteriaManager;
        _objectManager = objectManager;
    }

    public void CheckQuestDisables()
    {
        if (!_disableMap.ContainsKey(DisableType.Quest) || _disableMap[DisableType.Quest].Count == 0)
        {
            Log.Logger.Information("Checked 0 quest disables.");

            return;
        }

        var oldMSTime = Time.MSTime;

        // check only quests, rest already done at startup
        foreach (var pair in _disableMap[DisableType.Quest])
        {
            var entry = pair.Key;

            if (_objectManager.GetQuestTemplate(entry) == null)
            {
                Log.Logger.Error("QuestId entry {0} from `disables` doesn't exist, skipped.", entry);
                _disableMap[DisableType.Quest].Remove(entry);

                continue;
            }

            if (pair.Value.Flags != 0)
                Log.Logger.Error("Disable flags specified for quest {0}, useless data.", entry);
        }

        Log.Logger.Information("Checked {0} quest disables in {1} ms", _disableMap[DisableType.Quest].Count, Time.GetMSTimeDiffToNow(oldMSTime));
    }

    public bool IsDisabledFor(DisableType type, uint entry, WorldObject refe, ushort flags = 0)
    {
        if (!_disableMap.ContainsKey(type) || _disableMap[type].Empty())
            return false;

        if (!_disableMap[type].TryGetValue(entry, out var data)) // not disabled
            return false;

        switch (type)
        {
            case DisableType.Spell:
            {
                var spellFlags = (DisableFlags)data.Flags;

                if (refe != null)
                {
                    if ((refe.IsPlayer && spellFlags.HasFlag(DisableFlags.SpellPlayer)) ||
                        (refe.IsCreature && (spellFlags.HasFlag(DisableFlags.SpellCreature) || (refe.AsUnit.IsPet && spellFlags.HasFlag(DisableFlags.SpellPet)))) ||
                        (refe.IsGameObject && spellFlags.HasFlag(DisableFlags.SpellGameobject)))
                    {
                        if (spellFlags.HasAnyFlag(DisableFlags.SpellArenas | DisableFlags.SpellBattleGrounds))
                        {
                            var map = refe.Location.Map;

                            if (map != null)
                            {
                                if (spellFlags.HasFlag(DisableFlags.SpellArenas) && map.IsBattleArena)
                                    return true; // Current map is Arena and this spell is disabled here

                                if (spellFlags.HasFlag(DisableFlags.SpellBattleGrounds) && map.IsBattleground)
                                    return true; // Current map is a Battleground and this spell is disabled here
                            }
                        }

                        if (spellFlags.HasFlag(DisableFlags.SpellMap))
                        {
                            var mapIds = data.Param0;

                            if (mapIds.Contains(refe.Location.MapId))
                                return true; // Spell is disabled on current map

                            if (!spellFlags.HasFlag(DisableFlags.SpellArea))
                                return false; // Spell is disabled on another map, but not this one, return false

                            // Spell is disabled in an area, but not explicitly our current mapId. Continue processing.
                        }

                        if (spellFlags.HasFlag(DisableFlags.SpellArea))
                        {
                            var areaIds = data.Param1;

                            if (areaIds.Contains(refe.Location.Area))
                                return true; // Spell is disabled in this area

                            return false; // Spell is disabled in another area, but not this one, return false
                        }
                        else
                        {
                            return true; // Spell disabled for all maps
                        }
                    }

                    return false;
                }
                else if (spellFlags.HasFlag(DisableFlags.SpellDeprecatedSpell)) // call not from spellcast
                {
                    return true;
                }
                else if (flags.HasAnyFlag((byte)DisableFlags.SpellLOS))
                {
                    return spellFlags.HasFlag(DisableFlags.SpellLOS);
                }

                break;
            }
            case DisableType.Map:
            case DisableType.LFGMap:
                var player = refe.AsPlayer;

                if (player != null)
                {
                    var mapEntry = _cliDB.MapStorage.LookupByKey(entry);

                    if (mapEntry.IsDungeon())
                    {
                        var disabledModes = (DisableFlags)data.Flags;
                        var targetDifficulty = player.GetDifficultyId(mapEntry);
                        _db2Manager.GetDownscaledMapDifficultyData(entry, ref targetDifficulty);

                        return targetDifficulty switch
                        {
                            Difficulty.Normal   => disabledModes.HasFlag(DisableFlags.DungeonStatusNormal),
                            Difficulty.Heroic   => disabledModes.HasFlag(DisableFlags.DungeonStatusHeroic),
                            Difficulty.Raid10HC => disabledModes.HasFlag(DisableFlags.DungeonStatusHeroic10Man),
                            Difficulty.Raid25HC => disabledModes.HasFlag(DisableFlags.DungeonStatusHeroic25Man),
                            _                   => false
                        };
                    }
                    else if (mapEntry.InstanceType == MapTypes.Common)
                    {
                        return true;
                    }
                }

                return false;
            case DisableType.Quest:
                return true;
            case DisableType.Battleground:
            case DisableType.OutdoorPVP:
            case DisableType.Criteria:
            case DisableType.MMAP:
                return true;
            case DisableType.VMAP:
                return flags.HasAnyFlag(data.Flags);
        }

        return false;
    }

    public bool IsPathfindingEnabled(uint mapId)
    {
        return _configuration.GetDefaultValue("mmap.EnablePathFinding", true) && !IsDisabledFor(DisableType.MMAP, mapId, null);
    }

    public bool IsVMAPDisabledFor(uint entry, byte flags)
    {
        return IsDisabledFor(DisableType.VMAP, entry, null, flags);
    }

    public void LoadDisables()
    {
        var oldMSTime = Time.MSTime;

        // reload case
        _disableMap.Clear();

        var result = _worldDatabase.Query("SELECT sourceType, entry, flags, params_0, params_1 FROM disables");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 disables. DB table `disables` is empty!");

            return;
        }

        uint totalCount = 0;

        do
        {
            var type = (DisableType)result.Read<uint>(0);

            if (type >= DisableType.Max)
            {
                Log.Logger.Error("Invalid type {0} specified in `disables` table, skipped.", type);

                continue;
            }

            var entry = result.Read<uint>(1);
            var flags = (DisableFlags)result.Read<ushort>(2);
            var params0 = result.Read<string>(3);
            var params1 = result.Read<string>(4);

            DisableData data = new()
            {
                Flags = (ushort)flags
            };

            switch (type)
            {
                case DisableType.Spell:
                    if (!(_spellManager.HasSpellInfo(entry) || flags.HasFlag(DisableFlags.SpellDeprecatedSpell)))
                    {
                        Log.Logger.Error("Spell entry {0} from `disables` doesn't exist in dbc, skipped.", entry);

                        continue;
                    }

                    if (flags is 0 or > DisableFlags.MaxSpell)
                    {
                        Log.Logger.Error("Disable flags for spell {0} are invalid, skipped.", entry);

                        continue;
                    }

                    if (flags.HasFlag(DisableFlags.SpellMap))
                    {
                        var array = new StringArray(params0, ',');

                        for (byte i = 0; i < array.Length;)
                            if (uint.TryParse(array[i++], out var id))
                                data.Param0.Add(id);
                    }

                    if (flags.HasFlag(DisableFlags.SpellArea))
                    {
                        var array = new StringArray(params1, ',');

                        for (byte i = 0; i < array.Length;)
                            if (uint.TryParse(array[i++], out var id))
                                data.Param1.Add(id);
                    }

                    break;
                // checked later
                case DisableType.Quest:
                    break;
                case DisableType.Map:
                case DisableType.LFGMap:
                {
                    if (!_cliDB.MapStorage.TryGetValue(entry, out var mapEntry))
                    {
                        Log.Logger.Error("Map entry {0} from `disables` doesn't exist in dbc, skipped.", entry);

                        continue;
                    }

                    var isFlagInvalid = false;

                    switch (mapEntry.InstanceType)
                    {
                        case MapTypes.Common:
                            if (flags != 0)
                                isFlagInvalid = true;

                            break;
                        case MapTypes.Instance:
                        case MapTypes.Raid:
                            if (flags.HasFlag(DisableFlags.DungeonStatusHeroic) && _db2Manager.GetMapDifficultyData(entry, Difficulty.Heroic) == null)
                                flags &= ~DisableFlags.DungeonStatusHeroic;

                            if (flags.HasFlag(DisableFlags.DungeonStatusHeroic10Man) && _db2Manager.GetMapDifficultyData(entry, Difficulty.Raid10HC) == null)
                                flags &= ~DisableFlags.DungeonStatusHeroic10Man;

                            if (flags.HasFlag(DisableFlags.DungeonStatusHeroic25Man) && _db2Manager.GetMapDifficultyData(entry, Difficulty.Raid25HC) == null)
                                flags &= ~DisableFlags.DungeonStatusHeroic25Man;

                            if (flags == 0)
                                isFlagInvalid = true;

                            break;
                        case MapTypes.Battleground:
                        case MapTypes.Arena:
                            Log.Logger.Error("Battlegroundmap {0} specified to be disabled in map case, skipped.", entry);

                            continue;
                    }

                    if (isFlagInvalid)
                    {
                        Log.Logger.Error("Disable flags for map {0} are invalid, skipped.", entry);

                        continue;
                    }

                    break;
                }
                case DisableType.Battleground:
                    if (!_cliDB.BattlemasterListStorage.ContainsKey(entry))
                    {
                        Log.Logger.Error("Battlegroundentry {0} from `disables` doesn't exist in dbc, skipped.", entry);

                        continue;
                    }

                    if (flags != 0)
                        Log.Logger.Error("Disable flags specified for Battleground{0}, useless data.", entry);

                    break;
                case DisableType.OutdoorPVP:
                    if (entry > (int)OutdoorPvPTypes.Max)
                    {
                        Log.Logger.Error("OutdoorPvPTypes value {0} from `disables` is invalid, skipped.", entry);

                        continue;
                    }

                    if (flags != 0)
                        Log.Logger.Error("Disable flags specified for outdoor PvP {0}, useless data.", entry);

                    break;
                case DisableType.Criteria:
                    if (_criteriaManager.GetCriteria(entry) == null)
                    {
                        Log.Logger.Error("Criteria entry {0} from `disables` doesn't exist in dbc, skipped.", entry);

                        continue;
                    }

                    if (flags != 0)
                        Log.Logger.Error("Disable flags specified for Criteria {0}, useless data.", entry);

                    break;
                case DisableType.VMAP:
                {
                    if (!_cliDB.MapStorage.TryGetValue(entry, out var mapEntry))
                    {
                        Log.Logger.Error("Map entry {0} from `disables` doesn't exist in dbc, skipped.", entry);

                        continue;
                    }

                    switch (mapEntry.InstanceType)
                    {
                        case MapTypes.Common:
                            if (flags.HasFlag(DisableFlags.VmapAreaFlag))
                                Log.Logger.Information("Areaflag disabled for world map {0}.", entry);

                            if (flags.HasFlag(DisableFlags.VmapLiquidStatus))
                                Log.Logger.Information("Liquid status disabled for world map {0}.", entry);

                            break;
                        case MapTypes.Instance:
                        case MapTypes.Raid:
                            if (flags.HasFlag(DisableFlags.VmapHeight))
                                Log.Logger.Information("Height disabled for instance map {0}.", entry);

                            if (flags.HasFlag(DisableFlags.VmapLOS))
                                Log.Logger.Information("LoS disabled for instance map {0}.", entry);

                            break;
                        case MapTypes.Battleground:
                            if (flags.HasFlag(DisableFlags.VmapHeight))
                                Log.Logger.Information("Height disabled for Battlegroundmap {0}.", entry);

                            if (flags.HasFlag(DisableFlags.VmapLOS))
                                Log.Logger.Information("LoS disabled for Battlegroundmap {0}.", entry);

                            break;
                        case MapTypes.Arena:
                            if (flags.HasFlag(DisableFlags.VmapHeight))
                                Log.Logger.Information("Height disabled for arena map {0}.", entry);

                            if (flags.HasFlag(DisableFlags.VmapLOS))
                                Log.Logger.Information("LoS disabled for arena map {0}.", entry);

                            break;
                    }

                    break;
                }
                case DisableType.MMAP:
                {
                    if (!_cliDB.MapStorage.TryGetValue(entry, out var mapEntry))
                    {
                        Log.Logger.Error("Map entry {0} from `disables` doesn't exist in dbc, skipped.", entry);

                        continue;
                    }

                    switch (mapEntry.InstanceType)
                    {
                        case MapTypes.Common:
                            Log.Logger.Information("Pathfinding disabled for world map {0}.", entry);

                            break;
                        case MapTypes.Instance:
                        case MapTypes.Raid:
                            Log.Logger.Information("Pathfinding disabled for instance map {0}.", entry);

                            break;
                        case MapTypes.Battleground:
                            Log.Logger.Information("Pathfinding disabled for Battlegroundmap {0}.", entry);

                            break;
                        case MapTypes.Arena:
                            Log.Logger.Information("Pathfinding disabled for arena map {0}.", entry);

                            break;
                    }

                    break;
                }
            }

            if (!_disableMap.ContainsKey(type))
                _disableMap[type] = new Dictionary<uint, DisableData>();

            _disableMap[type].Add(entry, data);
            ++totalCount;
        } while (result.NextRow());

        Log.Logger.Information("Loaded {0} disables in {1} ms", totalCount, Time.GetMSTimeDiffToNow(oldMSTime));
    }

    public class DisableData
    {
        public ushort Flags { get; set; }
        public List<uint> Param0 { get; set; } = new();
        public List<uint> Param1 { get; set; } = new();
    }
}