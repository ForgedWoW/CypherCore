// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.DataStorage;
using Forged.MapServer.DataStorage.ClientReader;
using Forged.MapServer.DataStorage.Structs.C;
using Forged.MapServer.DataStorage.Structs.M;
using Forged.MapServer.Entities.Items;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Maps;
using Forged.MapServer.Maps.Grids;
using Forged.MapServer.World;
using Framework.Constants;
using Framework.Database;
using Framework.Util;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Forged.MapServer.Globals.Caching;

public class PlayerInfoCache : IObjectCache
{
    private readonly DB6Storage<ChrClassesRecord> _chrClassesRecords;
    private readonly DB6Storage<ChrRacesRecord> _chrRacesRecords;
    private readonly CliDB _cliDB;
    private readonly IConfiguration _configuration;
    private readonly DB2Manager _db2Manager;
    private readonly GridDefines _gridDefines;
    private readonly ItemTemplateCache _itemTemplateCache;
    private readonly DB6Storage<MapRecord> _mapRecords;
    private readonly DB6Storage<MovieRecord> _movieRecords;
    private readonly SceneTemplateCache _sceneTemplateCache;
    private readonly TransportManager _transportManager;
    private readonly WorldDatabase _worldDatabase;
    private readonly WorldManager _worldManager;
    private uint[] _playerXPperLevel;

    public PlayerInfoCache(IConfiguration configuration, WorldDatabase worldDatabase, GridDefines gridDefines, TransportManager transportManager,
                           SceneTemplateCache sceneTemplateCache, WorldManager worldManager, ItemTemplateCache itemTemplateCache,
                           DB6Storage<ChrRacesRecord> chrRacesRecords, DB6Storage<ChrClassesRecord> chrClassesRecords, DB6Storage<MapRecord> mapRecords,
                           DB2Manager db2Manager, DB6Storage<MovieRecord> movieRecords, CliDB cliDB)
    {
        _configuration = configuration;
        _worldDatabase = worldDatabase;
        _gridDefines = gridDefines;
        _transportManager = transportManager;
        _sceneTemplateCache = sceneTemplateCache;
        _worldManager = worldManager;
        _itemTemplateCache = itemTemplateCache;
        _chrRacesRecords = chrRacesRecords;
        _chrClassesRecords = chrClassesRecords;
        _mapRecords = mapRecords;
        _db2Manager = db2Manager;
        _movieRecords = movieRecords;
        _cliDB = cliDB;
    }

    public Dictionary<Race, Dictionary<PlayerClass, PlayerInfo>> PlayerInfos { get; } = new();

    public PlayerInfo GetPlayerInfo(Race raceId, PlayerClass classId)
    {
        if (raceId >= Race.Max)
            return null;

        if (classId >= PlayerClass.Max)
            return null;

        return !PlayerInfos.TryGetValue(raceId, classId, out var info) ? null : info;
    }

    public PlayerLevelInfo GetPlayerLevelInfo(Race race, PlayerClass @class, uint level)
    {
        if (level < 1 || race >= Race.Max || @class >= PlayerClass.Max)
            return null;

        if (!PlayerInfos.TryGetValue(race, @class, out var pInfo))
            return null;

        return level <= _configuration.GetDefaultValue("MaxPlayerLevel", SharedConst.DefaultMaxLevel) ? pInfo.LevelInfo[level - 1] : BuildPlayerLevelInfo(race, @class, level);
    }

    public uint GetXPForLevel(uint level)
    {
        return level < _playerXPperLevel.Length ? _playerXPperLevel[level] : 0;
    }

    public void Load()
    {
        var time = Time.MSTime;

        // Load playercreate
        {
            //                                         0     1      2    3           4           5           6            7        8               9               10              11               12                  13              14              15
            var result = _worldDatabase.Query("SELECT race, class, map, position_x, position_y, position_z, orientation, npe_map, npe_position_x, npe_position_y, npe_position_z, npe_orientation, npe_transport_guid, intro_movie_id, intro_scene_id, npe_intro_scene_id FROM playercreateinfo");

            if (result.IsEmpty())
            {
                Log.Logger.Information("Loaded 0 player create definitions. DB table `playercreateinfo` is empty.");

                return;
            }

            uint count = 0;

            do
            {
                var currentrace = result.Read<uint>(0);
                var currentclass = result.Read<uint>(1);
                var mapId = result.Read<uint>(2);
                var positionX = result.Read<float>(3);
                var positionY = result.Read<float>(4);
                var positionZ = result.Read<float>(5);
                var orientation = result.Read<float>(6);

                if (!_chrRacesRecords.ContainsKey(currentrace))
                {
                    Log.Logger.Error($"Wrong race {currentrace} in `playercreateinfo` table, ignoring.");

                    continue;
                }

                if (!_chrClassesRecords.ContainsKey(currentclass))
                {
                    Log.Logger.Error($"Wrong class {currentclass} in `playercreateinfo` table, ignoring.");

                    continue;
                }

                // accept DB data only for valid position (and non instanceable)
                if (!_gridDefines.IsValidMapCoord(mapId, positionX, positionY, positionZ, orientation))
                {
                    Log.Logger.Error($"Wrong home position for class {currentclass} race {currentrace} pair in `playercreateinfo` table, ignoring.");

                    continue;
                }

                if (_mapRecords.LookupByKey(mapId).Instanceable())
                {
                    Log.Logger.Error($"Home position in instanceable map for class {currentclass} race {currentrace} pair in `playercreateinfo` table, ignoring.");

                    continue;
                }

                if (_db2Manager.GetChrModel((Race)currentrace, Gender.Male) == null)
                {
                    Log.Logger.Error($"Missing male model for race {currentrace}, ignoring.");

                    continue;
                }

                if (_db2Manager.GetChrModel((Race)currentrace, Gender.Female) == null)
                {
                    Log.Logger.Error($"Missing female model for race {currentrace}, ignoring.");

                    continue;
                }

                PlayerInfo info = new(_configuration);
                info.CreatePosition.Loc = new WorldLocation(mapId, positionX, positionY, positionZ, orientation);

                if (!result.IsNull(7))
                {
                    PlayerInfo.CreatePositionModel createPosition = new()
                    {
                        Loc = new WorldLocation(result.Read<uint>(7), result.Read<float>(8), result.Read<float>(9), result.Read<float>(10), result.Read<float>(11))
                    };

                    if (!result.IsNull(12))
                        createPosition.TransportGuid = result.Read<ulong>(12);

                    info.CreatePositionNpe = createPosition;

                    if (!_mapRecords.ContainsKey(info.CreatePositionNpe.Value.Loc.MapId))
                    {
                        Log.Logger.Error($"Invalid NPE map id {info.CreatePositionNpe.Value.Loc.MapId} for class {currentclass} race {currentrace} pair in `playercreateinfo` table, ignoring.");
                        info.CreatePositionNpe = null;
                    }

                    if (info.CreatePositionNpe is { TransportGuid: { } } && _transportManager.GetTransportSpawn(info.CreatePositionNpe.Value.TransportGuid.Value) == null)
                    {
                        Log.Logger.Error($"Invalid NPE transport spawn id {info.CreatePositionNpe.Value.TransportGuid.Value} for class {currentclass} race {currentrace} pair in `playercreateinfo` table, ignoring.");
                        info.CreatePositionNpe = null; // remove entire NPE data - assume user put transport offsets into npe_position fields
                    }
                }

                if (!result.IsNull(13))
                {
                    var introMovieId = result.Read<uint>(13);

                    if (_movieRecords.ContainsKey(introMovieId))
                        info.IntroMovieId = introMovieId;
                    else
                        Log.Logger.Debug($"Invalid intro movie id {introMovieId} for class {currentclass} race {currentrace} pair in `playercreateinfo` table, ignoring.");
                }

                if (!result.IsNull(14))
                {
                    var introSceneId = result.Read<uint>(14);

                    if (_sceneTemplateCache.GetSceneTemplate(introSceneId) != null)
                        info.IntroSceneId = introSceneId;
                    else
                        Log.Logger.Debug($"Invalid intro scene id {introSceneId} for class {currentclass} race {currentrace} pair in `playercreateinfo` table, ignoring.");
                }

                if (!result.IsNull(15))
                {
                    var introSceneId = result.Read<uint>(15);

                    if (_sceneTemplateCache.GetSceneTemplate(introSceneId) != null)
                        info.IntroSceneIdNpe = introSceneId;
                    else
                        Log.Logger.Debug($"Invalid NPE intro scene id {introSceneId} for class {currentclass} race {currentrace} pair in `playercreateinfo` table, ignoring.");
                }

                PlayerInfos.Add((Race)currentrace, (PlayerClass)currentclass, info);

                ++count;
            } while (result.NextRow());

            Log.Logger.Information("Loaded {0} player create definitions in {1} ms", count, Time.GetMSTimeDiffToNow(time));
        }

        time = Time.MSTime;
        // Load playercreate items
        Log.Logger.Information("Loading Player Create Items Data...");

        {
            MultiMap<uint, ItemTemplate> itemsByCharacterLoadout = new();

            foreach (var characterLoadoutItem in _cliDB.CharacterLoadoutItemStorage.Values)
            {
                var itemTemplate = _itemTemplateCache.GetItemTemplate(characterLoadoutItem.ItemID);

                if (itemTemplate != null)
                    itemsByCharacterLoadout.Add(characterLoadoutItem.CharacterLoadoutID, itemTemplate);
            }

            foreach (var characterLoadout in _cliDB.CharacterLoadoutStorage.Values)
            {
                if (!characterLoadout.IsForNewCharacter())
                    continue;

                if (itemsByCharacterLoadout.TryGetValue(characterLoadout.Id, out var items))
                    continue;

                for (var raceIndex = Race.Human; raceIndex < Race.Max; ++raceIndex)
                {
                    if (!characterLoadout.RaceMask.HasAnyFlag(SharedConst.GetMaskForRace(raceIndex)))
                        continue;

                    if (PlayerInfos.TryGetValue(raceIndex, (PlayerClass)characterLoadout.ChrClassID, out var playerInfo))
                    {
                        playerInfo.ItemContext = (ItemContext)characterLoadout.ItemContext;

                        foreach (var itemTemplate in items)
                        {
                            // BuyCount by default
                            var count = itemTemplate.BuyCount;

                            // special amount for food/drink
                            if (itemTemplate.Class == ItemClass.Consumable && (ItemSubClassConsumable)itemTemplate.SubClass == ItemSubClassConsumable.FoodDrink)
                            {
                                if (!itemTemplate.Effects.Empty())
                                    count = (SpellCategories)itemTemplate.Effects[0].SpellCategoryID switch
                                    {
                                        SpellCategories.Food => // food
                                            characterLoadout.ChrClassID == (int)PlayerClass.Deathknight ? 10 : 4u,
                                        SpellCategories.Drink => // drink
                                            2,
                                        _ => count
                                    };

                                if (itemTemplate.MaxStackSize < count)
                                    count = itemTemplate.MaxStackSize;
                            }

                            playerInfo.Items.Add(new PlayerCreateInfoItem(itemTemplate.Id, count));
                        }
                    }
                }
            }
        }

        Log.Logger.Information("Loading Player Create Items Override Data...");

        {
            //                                         0     1      2       3
            var result = _worldDatabase.Query("SELECT race, class, itemid, amount FROM playercreateinfo_item");

            if (result.IsEmpty())
                Log.Logger.Information("Loaded 0 custom player create items. DB table `playercreateinfo_item` is empty.");
            else
            {
                uint count = 0;

                do
                {
                    var currentrace = result.Read<uint>(0);

                    if (currentrace >= (int)Race.Max)
                    {
                        Log.Logger.Error("Wrong race {0} in `playercreateinfo_item` table, ignoring.", currentrace);

                        continue;
                    }

                    var currentclass = result.Read<uint>(1);

                    if (currentclass >= (int)PlayerClass.Max)
                    {
                        Log.Logger.Error("Wrong class {0} in `playercreateinfo_item` table, ignoring.", currentclass);

                        continue;
                    }

                    var itemid = result.Read<uint>(2);

                    if (_itemTemplateCache.GetItemTemplate(itemid).Id == 0)
                    {
                        Log.Logger.Error("Item id {0} (race {1} class {2}) in `playercreateinfo_item` table but not listed in `itemtemplate`, ignoring.", itemid, currentrace, currentclass);

                        continue;
                    }

                    var amount = result.Read<int>(3);

                    if (amount == 0)
                    {
                        Log.Logger.Error("Item id {0} (class {1} race {2}) have amount == 0 in `playercreateinfo_item` table, ignoring.", itemid, currentrace, currentclass);

                        continue;
                    }

                    if (currentrace == 0 || currentclass == 0)
                    {
                        var minrace = currentrace != 0 ? currentrace : 1;
                        var maxrace = currentrace != 0 ? currentrace + 1 : (int)Race.Max;
                        var minclass = currentclass != 0 ? currentclass : 1;
                        var maxclass = currentclass != 0 ? currentclass + 1 : (int)PlayerClass.Max;

                        for (var r = minrace; r < maxrace; ++r)
                            for (var c = minclass; c < maxclass; ++c)
                                PlayerCreateInfoAddItemHelper(r, c, itemid, amount);
                    }
                    else
                        PlayerCreateInfoAddItemHelper(currentrace, currentclass, itemid, amount);

                    ++count;
                } while (result.NextRow());

                Log.Logger.Information("Loaded {0} custom player create items in {1} ms", count, Time.GetMSTimeDiffToNow(time));
            }
        }

        // Load playercreate skills
        Log.Logger.Information("Loading Player Create Skill Data...");

        {
            var oldMSTime = Time.MSTime;

            foreach (var rcInfo in _cliDB.SkillRaceClassInfoStorage.Values)
                if (rcInfo.Availability == 1)
                    for (var raceIndex = Race.Human; raceIndex < Race.Max; ++raceIndex)
                        if (rcInfo.RaceMask == -1 || Convert.ToBoolean(SharedConst.GetMaskForRace(raceIndex) & rcInfo.RaceMask))
                            for (var classIndex = PlayerClass.Warrior; classIndex < PlayerClass.Max; ++classIndex)
                                if (rcInfo.ClassMask == -1 || Convert.ToBoolean((1 << ((int)classIndex - 1)) & rcInfo.ClassMask))
                                    if (PlayerInfos.TryGetValue(raceIndex, classIndex, out var info))
                                        info.Skills.Add(rcInfo);

            Log.Logger.Information("Loaded player create skills in {0} ms", Time.GetMSTimeDiffToNow(oldMSTime));
        }

        // Load playercreate custom spells
        Log.Logger.Information("Loading Player Create Custom Spell Data...");

        {
            var oldMSTime = Time.MSTime;

            var result = _worldDatabase.Query("SELECT racemask, classmask, Spell FROM playercreateinfo_spell_custom");

            if (result.IsEmpty())
                Log.Logger.Information("Loaded 0 player create custom spells. DB table `playercreateinfo_spell_custom` is empty.");
            else
            {
                uint count = 0;

                do
                {
                    var raceMask = result.Read<ulong>(0);
                    var classMask = result.Read<uint>(1);
                    var spellId = result.Read<uint>(2);

                    if (raceMask != 0 && !Convert.ToBoolean(raceMask & SharedConst.RaceMaskAllPlayable))
                    {
                        Log.Logger.Error("Wrong race mask {0} in `playercreateinfo_spell_custom` table, ignoring.", raceMask);

                        continue;
                    }

                    if (classMask != 0 && !Convert.ToBoolean((int)(classMask & (int)PlayerClass.ClassMaskAllPlayable)))
                    {
                        Log.Logger.Error("Wrong class mask {0} in `playercreateinfo_spell_custom` table, ignoring.", classMask);

                        continue;
                    }

                    for (var raceIndex = Race.Human; raceIndex < Race.Max; ++raceIndex)
                        if (raceMask == 0 || Convert.ToBoolean((ulong)SharedConst.GetMaskForRace(raceIndex) & raceMask))
                            for (var classIndex = PlayerClass.Warrior; classIndex < PlayerClass.Max; ++classIndex)
                                if (classMask == 0 || Convert.ToBoolean((int)((1 << ((int)classIndex - 1)) & classMask)))
                                    if (PlayerInfos.TryGetValue(raceIndex, classIndex, out var playerInfo))
                                    {
                                        playerInfo.CustomSpells.Add(spellId);
                                        ++count;
                                    }
                } while (result.NextRow());

                Log.Logger.Information("Loaded {0} custom player create spells in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
            }
        }

        // Load playercreate cast spell
        Log.Logger.Information("Loading Player Create Cast Spell Data...");

        {
            var oldMSTime = Time.MSTime;

            var result = _worldDatabase.Query("SELECT raceMask, classMask, spell, createMode FROM playercreateinfo_cast_spell");

            if (result.IsEmpty())
                Log.Logger.Information("Loaded 0 player create cast spells. DB table `playercreateinfo_cast_spell` is empty.");
            else
            {
                uint count = 0;

                do
                {
                    var raceMask = result.Read<ulong>(0);
                    var classMask = result.Read<uint>(1);
                    var spellId = result.Read<uint>(2);
                    var playerCreateMode = result.Read<sbyte>(3);

                    if (raceMask != 0 && (raceMask & SharedConst.RaceMaskAllPlayable) == 0)
                    {
                        Log.Logger.Error($"Wrong race mask {raceMask} in `playercreateinfo_cast_spell` table, ignoring.");

                        continue;
                    }

                    if (classMask != 0 && !classMask.HasAnyFlag((uint)PlayerClass.ClassMaskAllPlayable))
                    {
                        Log.Logger.Error($"Wrong class mask {classMask} in `playercreateinfo_cast_spell` table, ignoring.");

                        continue;
                    }

                    if (playerCreateMode is < 0 or >= (sbyte)PlayerCreateMode.Max)
                    {
                        Log.Logger.Error($"Uses invalid createMode {playerCreateMode} in `playercreateinfo_cast_spell` table, ignoring.");

                        continue;
                    }

                    for (var raceIndex = Race.Human; raceIndex < Race.Max; ++raceIndex)
                        if (raceMask == 0 || Convert.ToBoolean((ulong)SharedConst.GetMaskForRace(raceIndex) & raceMask))
                            for (var classIndex = PlayerClass.Warrior; classIndex < PlayerClass.Max; ++classIndex)
                                if (classMask == 0 || Convert.ToBoolean((int)((1 << ((int)classIndex - 1)) & classMask)))
                                    if (PlayerInfos.TryGetValue(raceIndex, classIndex, out var info))
                                    {
                                        info.CastSpells[playerCreateMode].Add(spellId);
                                        ++count;
                                    }
                } while (result.NextRow());

                Log.Logger.Information("Loaded {0} player create cast spells in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
            }
        }

        // Load playercreate actions
        time = Time.MSTime;
        Log.Logger.Information("Loading Player Create Action Data...");

        {
            //                                         0     1      2       3       4
            var result = _worldDatabase.Query("SELECT race, class, button, action, type FROM playercreateinfo_action");

            if (result.IsEmpty())
                Log.Logger.Information("Loaded 0 player create actions. DB table `playercreateinfo_action` is empty.");
            else
            {
                uint count = 0;

                do
                {
                    var currentrace = (Race)result.Read<uint>(0);

                    if (currentrace >= Race.Max)
                    {
                        Log.Logger.Error("Wrong race {0} in `playercreateinfo_action` table, ignoring.", currentrace);

                        continue;
                    }

                    var currentclass = (PlayerClass)result.Read<uint>(1);

                    if (currentclass >= PlayerClass.Max)
                    {
                        Log.Logger.Error("Wrong class {0} in `playercreateinfo_action` table, ignoring.", currentclass);

                        continue;
                    }

                    if (PlayerInfos.TryGetValue(currentrace, currentclass, out var info))
                        info.Actions.Add(new PlayerCreateInfoAction(result.Read<byte>(2), result.Read<uint>(3), result.Read<byte>(4)));

                    ++count;
                } while (result.NextRow());

                Log.Logger.Information("Loaded {0} player create actions in {1} ms", count, Time.GetMSTimeDiffToNow(time));
            }
        }

        time = Time.MSTime;
        // Loading levels data (class/race dependent)
        Log.Logger.Information("Loading Player Create Level Stats Data...");

        {
            var raceStatModifiers = new short[(int)Race.Max][];

            for (var i = 0; i < (int)Race.Max; ++i)
                raceStatModifiers[i] = new short[(int)Stats.Max];

            //                                         0     1    2    3    4
            var result = _worldDatabase.Query("SELECT race, str, agi, sta, inte FROM player_racestats");

            if (result.IsEmpty())
            {
                Log.Logger.Information("Loaded 0 level stats definitions. DB table `player_racestats` is empty.");
                _worldManager.StopNow();

                return;
            }

            do
            {
                var currentrace = (Race)result.Read<uint>(0);

                if (currentrace >= Race.Max)
                {
                    Log.Logger.Error($"Wrong race {currentrace} in `player_racestats` table, ignoring.");

                    continue;
                }

                for (var i = 0; i < (int)Stats.Max; ++i)
                    raceStatModifiers[(int)currentrace][i] = result.Read<short>(i + 1);
            } while (result.NextRow());

            //                               0      1      2    3    4    5
            result = _worldDatabase.Query("SELECT class, level, str, agi, sta, inte FROM player_classlevelstats");

            if (result.IsEmpty())
            {
                Log.Logger.Information("Loaded 0 level stats definitions. DB table `player_classlevelstats` is empty.");
                _worldManager.StopNow();

                return;
            }

            uint count = 0;

            do
            {
                var currentclass = (PlayerClass)result.Read<byte>(0);

                if (currentclass >= PlayerClass.Max)
                {
                    Log.Logger.Error("Wrong class {0} in `player_classlevelstats` table, ignoring.", currentclass);

                    continue;
                }

                var currentlevel = result.Read<uint>(1);

                if (currentlevel > _configuration.GetDefaultValue("MaxPlayerLevel", SharedConst.DefaultMaxLevel))
                {
                    if (currentlevel > 255) // hardcoded level maximum
                        Log.Logger.Error($"Wrong (> 255) level {currentlevel} in `player_classlevelstats` table, ignoring.");
                    else
                        Log.Logger.Warning($"Unused (> MaxPlayerLevel in worldserver.conf) level {currentlevel} in `player_levelstats` table, ignoring.");

                    continue;
                }

                for (var race = 0; race < raceStatModifiers.Length; ++race)
                {
                    if (!PlayerInfos.TryGetValue((Race)race, currentclass, out var playerInfo))
                        continue;

                    for (var i = 0; i < (int)Stats.Max; i++)
                        playerInfo.LevelInfo[currentlevel - 1].Stats[i] = (ushort)(result.Read<ushort>(i + 2) + raceStatModifiers[race][i]);
                }

                ++count;
            } while (result.NextRow());

            // Fill gaps and check integrity
            for (Race race = 0; race < Race.Max; ++race)
            {
                // skip non existed races
                if (!_chrRacesRecords.ContainsKey(race))
                    continue;

                for (PlayerClass @class = 0; @class < PlayerClass.Max; ++@class)
                {
                    // skip non existed classes
                    if (_chrClassesRecords.LookupByKey(@class) == null)
                        continue;

                    if (!PlayerInfos.TryGetValue(race, @class, out var playerInfo))
                        continue;

                    if (_configuration.GetDefaultValue("character:EnforceRaceAndClassExpations", true))
                    {
                        // skip expansion races if not playing with expansion
                        if (_configuration.GetDefaultValue("Expansion", (int)Expansion.Dragonflight) < (int)Expansion.BurningCrusade && race is Race.BloodElf or Race.Draenei)
                            continue;

                        // skip expansion classes if not playing with expansion
                        if (_configuration.GetDefaultValue("Expansion", (int)Expansion.Dragonflight) < (int)Expansion.WrathOfTheLichKing && @class == PlayerClass.Deathknight)
                            continue;

                        if (_configuration.GetDefaultValue("Expansion", (int)Expansion.Dragonflight) < (int)Expansion.MistsOfPandaria && race is Race.PandarenNeutral or Race.PandarenHorde or Race.PandarenAlliance)
                            continue;

                        if (_configuration.GetDefaultValue("Expansion", (int)Expansion.Dragonflight) < (int)Expansion.Legion && @class == PlayerClass.DemonHunter)
                            continue;

                        if (_configuration.GetDefaultValue("Expansion", (int)Expansion.Dragonflight) < (int)Expansion.Dragonflight && @class == PlayerClass.Evoker)
                            continue;
                    }

                    // fatal error if no level 1 data
                    if (playerInfo.LevelInfo[0].Stats[0] == 0)
                    {
                        Log.Logger.Error("Race {0} Class {1} Level 1 does not have stats data!", race, @class);
                        Environment.Exit(1);

                        return;
                    }

                    // fill level gaps
                    for (var level = 1; level < _configuration.GetDefaultValue("MaxPlayerLevel", SharedConst.DefaultMaxLevel); ++level)
                        if (playerInfo.LevelInfo[level].Stats[0] == 0)
                        {
                            Log.Logger.Error("Race {0} Class {1} Level {2} does not have stats data. Using stats data of level {3}.", race, @class, level + 1, level);
                            playerInfo.LevelInfo[level] = playerInfo.LevelInfo[level - 1];
                        }
                }
            }

            Log.Logger.Information("Loaded {0} level stats definitions in {1} ms", count, Time.GetMSTimeDiffToNow(time));
        }

        time = Time.MSTime;
        // Loading xp per level data
        Log.Logger.Information("Loading Player Create XP Data...");

        {
            _playerXPperLevel = new uint[_cliDB.XpGameTable.TableRowCount + 1];

            //                                          0      1
            var result = _worldDatabase.Query("SELECT Level, Experience FROM player_xp_for_level");

            // load the DBC's levels at first...
            for (uint level = 1; level < _cliDB.XpGameTable.TableRowCount; ++level)
                _playerXPperLevel[level] = (uint)_cliDB.XpGameTable.GetRow(level).Total;

            uint count = 0;

            // ...overwrite if needed (custom values)
            if (!result.IsEmpty())
            {
                do
                {
                    uint currentlevel = result.Read<byte>(0);
                    var currentxp = result.Read<uint>(1);

                    if (currentlevel >= _configuration.GetDefaultValue("MaxPlayerLevel", SharedConst.DefaultMaxLevel))
                    {
                        if (currentlevel > SharedConst.StrongMaxLevel) // hardcoded level maximum
                            Log.Logger.Error("Wrong (> {0}) level {1} in `player_xp_for_level` table, ignoring.", 255, currentlevel);
                        else
                        {
                            Log.Logger.Warning("Unused (> MaxPlayerLevel in worldserver.conf) level {0} in `player_xp_for_levels` table, ignoring.", currentlevel);
                            ++count; // make result loading percent "expected" correct in case disabled detail mode for example.
                        }

                        continue;
                    }

                    //PlayerXPperLevel
                    _playerXPperLevel[currentlevel] = currentxp;
                    ++count;
                } while (result.NextRow());

                // fill level gaps
                for (var level = 1; level < _configuration.GetDefaultValue("MaxPlayerLevel", SharedConst.DefaultMaxLevel); ++level)
                    if (_playerXPperLevel[level] == 0)
                    {
                        Log.Logger.Error("Level {0} does not have XP for level data. Using data of level [{1}] + 12000.", level + 1, level);
                        _playerXPperLevel[level] = _playerXPperLevel[level - 1] + 12000;
                    }
            }

            Log.Logger.Information("Loaded {0} xp for level definition(s) from database in {1} ms", count, Time.GetMSTimeDiffToNow(time));
        }
    }

    private PlayerLevelInfo BuildPlayerLevelInfo(Race race, PlayerClass @class, uint level)
    {
        // base data (last known level)
        var info = PlayerInfos[race][@class].LevelInfo[_configuration.GetDefaultValue("MaxPlayerLevel", SharedConst.DefaultMaxLevel) - 1];

        for (var lvl = _configuration.GetDefaultValue("MaxPlayerLevel", SharedConst.DefaultMaxLevel) - 1; lvl < level; ++lvl)
            switch (@class)
            {
                case PlayerClass.Warrior:
                    info.Stats[0] += (lvl > 23 ? 2 : (lvl > 1 ? 1 : 0));
                    info.Stats[1] += (lvl > 23 ? 2 : (lvl > 1 ? 1 : 0));
                    info.Stats[2] += (lvl > 36 ? 1 : (lvl > 6 && (lvl % 2) != 0 ? 1 : 0));
                    info.Stats[3] += (lvl > 9 && (lvl % 2) == 0 ? 1 : 0);

                    break;

                case PlayerClass.Paladin:
                    info.Stats[0] += (lvl > 3 ? 1 : 0);
                    info.Stats[1] += (lvl > 33 ? 2 : (lvl > 1 ? 1 : 0));
                    info.Stats[2] += (lvl > 38 ? 1 : (lvl > 7 && (lvl % 2) == 0 ? 1 : 0));
                    info.Stats[3] += (lvl > 6 && (lvl % 2) != 0 ? 1 : 0);

                    break;

                case PlayerClass.Hunter:
                    info.Stats[0] += (lvl > 4 ? 1 : 0);
                    info.Stats[1] += (lvl > 4 ? 1 : 0);
                    info.Stats[2] += (lvl > 33 ? 2 : (lvl > 1 ? 1 : 0));
                    info.Stats[3] += (lvl > 8 && (lvl % 2) != 0 ? 1 : 0);

                    break;

                case PlayerClass.Rogue:
                    info.Stats[0] += (lvl > 5 ? 1 : 0);
                    info.Stats[1] += (lvl > 4 ? 1 : 0);
                    info.Stats[2] += (lvl > 16 ? 2 : (lvl > 1 ? 1 : 0));
                    info.Stats[3] += (lvl > 8 && (lvl % 2) == 0 ? 1 : 0);

                    break;

                case PlayerClass.Priest:
                    info.Stats[0] += (lvl > 9 && (lvl % 2) == 0 ? 1 : 0);
                    info.Stats[1] += (lvl > 5 ? 1 : 0);
                    info.Stats[2] += (lvl > 38 ? 1 : (lvl > 8 && (lvl % 2) != 0 ? 1 : 0));
                    info.Stats[3] += (lvl > 22 ? 2 : (lvl > 1 ? 1 : 0));

                    break;

                case PlayerClass.Shaman:
                    info.Stats[0] += (lvl > 34 ? 1 : (lvl > 6 && (lvl % 2) != 0 ? 1 : 0));
                    info.Stats[1] += (lvl > 4 ? 1 : 0);
                    info.Stats[2] += (lvl > 7 && (lvl % 2) == 0 ? 1 : 0);
                    info.Stats[3] += (lvl > 5 ? 1 : 0);

                    break;

                case PlayerClass.Mage:
                    info.Stats[0] += (lvl > 9 && (lvl % 2) == 0 ? 1 : 0);
                    info.Stats[1] += (lvl > 5 ? 1 : 0);
                    info.Stats[2] += (lvl > 9 && (lvl % 2) == 0 ? 1 : 0);
                    info.Stats[3] += (lvl > 24 ? 2 : (lvl > 1 ? 1 : 0));

                    break;

                case PlayerClass.Warlock:
                    info.Stats[0] += (lvl > 9 && (lvl % 2) == 0 ? 1 : 0);
                    info.Stats[1] += (lvl > 38 ? 2 : (lvl > 3 ? 1 : 0));
                    info.Stats[2] += (lvl > 9 && (lvl % 2) == 0 ? 1 : 0);
                    info.Stats[3] += (lvl > 33 ? 2 : (lvl > 2 ? 1 : 0));

                    break;

                case PlayerClass.Druid:
                    info.Stats[0] += (lvl > 38 ? 2 : (lvl > 6 && (lvl % 2) != 0 ? 1 : 0));
                    info.Stats[1] += (lvl > 32 ? 2 : (lvl > 4 ? 1 : 0));
                    info.Stats[2] += (lvl > 38 ? 2 : (lvl > 8 && (lvl % 2) != 0 ? 1 : 0));
                    info.Stats[3] += (lvl > 38 ? 3 : (lvl > 4 ? 1 : 0));

                    break;
            }

        return info;
    }

    private void PlayerCreateInfoAddItemHelper(uint race, uint @class, uint itemId, int count)
    {
        if (!PlayerInfos.TryGetValue((Race)race, (PlayerClass)@class, out var playerInfo))
            return;

        if (count > 0)
            playerInfo.Items.Add(new PlayerCreateInfoItem(itemId, (uint)count));
        else
        {
            if (count < -1)
                Log.Logger.Error("Invalid count {0} specified on item {1} be removed from original player create info (use -1)!", count, itemId);

            playerInfo.Items.RemoveAll(item => item.ItemId == itemId);
        }
    }
}