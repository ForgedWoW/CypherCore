// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.DataStorage.ClientReader;
using Forged.MapServer.DataStorage.Structs.C;
using Forged.MapServer.DataStorage.Structs.F;
using Forged.MapServer.DataStorage.Structs.V;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Movement;
using Forged.MapServer.Scripting;
using Forged.MapServer.Spells;
using Framework.Constants;
using Framework.Database;
using Framework.Util;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Forged.MapServer.Globals.Caching;

public class CreatureTemplateCache : IObjectCache
{
    private readonly IConfiguration _configuration;
    private readonly DB6Storage<CreatureDisplayInfoRecord> _creatureDisplayInfoRecords;
    private readonly DB6Storage<CreatureFamilyRecord> _creatureFamilyRecords;
    private readonly CreatureModelCache _creatureModelCache;
    private readonly Dictionary<uint, CreatureSummonedData> _creatureSummonedDataStorage = new();
    private readonly DB6Storage<CreatureTypeRecord> _creatureTypeRecords;
    private readonly List<uint>[] _difficultyEntries = new List<uint>[SharedConst.MaxCreatureDifficulties];
    private readonly DB6Storage<FactionTemplateRecord> _factionTemplateRecords;
    private readonly GameObjectManager _gameObjectManager;
    private readonly List<uint>[] _hasDifficultyEntries = new List<uint>[SharedConst.MaxCreatureDifficulties];
    private readonly ScriptManager _scriptManager;
    private readonly SpellManager _spellManager;
    private readonly DB6Storage<VehicleRecord> _vehicleRecords;
    private readonly WorldDatabase _worldDatabase;

    public CreatureTemplateCache(GameObjectManager gameObjectManager, WorldDatabase worldDatabase, IConfiguration configuration, ScriptManager scriptManager,
                                 DB6Storage<CreatureDisplayInfoRecord> creatureDisplayInfoRecords, SpellManager spellManager, DB6Storage<VehicleRecord> vehicleRecords,
                                 DB6Storage<CreatureTypeRecord> creatureTypeRecords, DB6Storage<CreatureFamilyRecord> creatureFamilyRecords, DB6Storage<FactionTemplateRecord> factionTemplateRecords,
                                 CreatureModelCache creatureModelCache)
    {
        _gameObjectManager = gameObjectManager;
        _worldDatabase = worldDatabase;
        _configuration = configuration;
        _scriptManager = scriptManager;
        _creatureDisplayInfoRecords = creatureDisplayInfoRecords;
        _spellManager = spellManager;
        _vehicleRecords = vehicleRecords;
        _creatureTypeRecords = creatureTypeRecords;
        _creatureFamilyRecords = creatureFamilyRecords;
        _factionTemplateRecords = factionTemplateRecords;
        _creatureModelCache = creatureModelCache;

        for (var i = 0; i < SharedConst.MaxCreatureDifficulties; ++i)
        {
            _difficultyEntries[i] = new List<uint>();
            _hasDifficultyEntries[i] = new List<uint>();
        }
    }

    public Dictionary<uint, CreatureTemplate> CreatureTemplates { get; } = new();

    public void CheckCreatureTemplate(CreatureTemplate cInfo)
    {
        if (cInfo == null)
            return;

        var ok = true; // bool to allow continue outside this loop

        for (uint diff = 0; diff < SharedConst.MaxCreatureDifficulties && ok; ++diff)
        {
            if (cInfo.DifficultyEntry[diff] == 0)
                continue;

            ok = false; // will be set to true at the end of this loop again

            var difficultyInfo = GetCreatureTemplate(cInfo.DifficultyEntry[diff]);

            if (difficultyInfo == null)
            {
                Log.Logger.Error("Creature (Entry: {0}) has `difficulty_entry_{1}`={2} but creature entry {3} does not exist.",
                                 cInfo.Entry,
                                 diff + 1,
                                 cInfo.DifficultyEntry[diff],
                                 cInfo.DifficultyEntry[diff]);

                continue;
            }

            var ok2 = true;

            for (uint diff2 = 0; diff2 < SharedConst.MaxCreatureDifficulties && ok2; ++diff2)
            {
                ok2 = false;

                if (_difficultyEntries[diff2].Contains(cInfo.Entry))
                {
                    Log.Logger.Error("Creature (Entry: {0}) is listed as `difficulty_entry_{1}` of another creature, but itself lists {2} in `difficulty_entry_{3}`.",
                                     cInfo.Entry,
                                     diff2 + 1,
                                     cInfo.DifficultyEntry[diff],
                                     diff + 1);

                    continue;
                }

                if (_difficultyEntries[diff2].Contains(cInfo.DifficultyEntry[diff]))
                {
                    Log.Logger.Error("Creature (Entry: {0}) already listed as `difficulty_entry_{1}` for another entry.", cInfo.DifficultyEntry[diff], diff2 + 1);

                    continue;
                }

                if (_hasDifficultyEntries[diff2].Contains(cInfo.DifficultyEntry[diff]))
                {
                    Log.Logger.Error("Creature (Entry: {0}) has `difficulty_entry_{1}`={2} but creature entry {3} has itself a value in `difficulty_entry_{4}`.",
                                     cInfo.Entry,
                                     diff + 1,
                                     cInfo.DifficultyEntry[diff],
                                     cInfo.DifficultyEntry[diff],
                                     diff2 + 1);

                    continue;
                }

                ok2 = true;
            }

            if (!ok2)
                continue;

            if (cInfo.HealthScalingExpansion > difficultyInfo.HealthScalingExpansion)
                Log.Logger.Error("Creature (Id: {0}, Expansion {1}) has different `HealthScalingExpansion` in difficulty {2} mode (Id: {3}, Expansion: {4}).",
                                 cInfo.Entry,
                                 cInfo.HealthScalingExpansion,
                                 diff + 1,
                                 cInfo.DifficultyEntry[diff],
                                 difficultyInfo.HealthScalingExpansion);

            if (cInfo.Minlevel > difficultyInfo.Minlevel)
                Log.Logger.Error("Creature (Entry: {0}, minlevel: {1}) has lower `minlevel` in difficulty {2} mode (Entry: {3}, minlevel: {4}).",
                                 cInfo.Entry,
                                 cInfo.Minlevel,
                                 diff + 1,
                                 cInfo.DifficultyEntry[diff],
                                 difficultyInfo.Minlevel);

            if (cInfo.Maxlevel > difficultyInfo.Maxlevel)
                Log.Logger.Error("Creature (Entry: {0}, maxlevel: {1}) has lower `maxlevel` in difficulty {2} mode (Entry: {3}, maxlevel: {4}).",
                                 cInfo.Entry,
                                 cInfo.Maxlevel,
                                 diff + 1,
                                 cInfo.DifficultyEntry[diff],
                                 difficultyInfo.Maxlevel);

            if (cInfo.Faction != difficultyInfo.Faction)
                Log.Logger.Error("Creature (Entry: {0}, faction: {1}) has different `faction` in difficulty {2} mode (Entry: {3}, faction: {4}).",
                                 cInfo.Entry,
                                 cInfo.Faction,
                                 diff + 1,
                                 cInfo.DifficultyEntry[diff],
                                 difficultyInfo.Faction);

            if (cInfo.UnitClass != difficultyInfo.UnitClass)
            {
                Log.Logger.Error("Creature (Entry: {0}, class: {1}) has different `unit_class` in difficulty {2} mode (Entry: {3}, class: {4}).",
                                 cInfo.Entry,
                                 cInfo.UnitClass,
                                 diff + 1,
                                 cInfo.DifficultyEntry[diff],
                                 difficultyInfo.UnitClass);

                continue;
            }

            if (cInfo.Npcflag != difficultyInfo.Npcflag)
            {
                Log.Logger.Error("Creature (Entry: {0}) has different `npcflag` in difficulty {1} mode (Entry: {2}).", cInfo.Entry, diff + 1, cInfo.DifficultyEntry[diff]);
                Log.Logger.Error("Possible FIX: UPDATE `creature_template` SET `npcflag`=`npcflag`^{0} WHERE `entry`={1};", cInfo.Npcflag ^ difficultyInfo.Npcflag, cInfo.DifficultyEntry[diff]);

                continue;
            }

            if (cInfo.DmgSchool != difficultyInfo.DmgSchool)
            {
                Log.Logger.Error("Creature (Entry: {0}, `dmgschool`: {1}) has different `dmgschool` in difficulty {2} mode (Entry: {3}, `dmgschool`: {4}).",
                                 cInfo.Entry,
                                 cInfo.DmgSchool,
                                 diff + 1,
                                 cInfo.DifficultyEntry[diff],
                                 difficultyInfo.DmgSchool);

                Log.Logger.Error("Possible FIX: UPDATE `creature_template` SET `dmgschool`={0} WHERE `entry`={1};", cInfo.DmgSchool, cInfo.DifficultyEntry[diff]);
            }

            if (cInfo.UnitFlags2 != difficultyInfo.UnitFlags2)
            {
                Log.Logger.Error("Creature (Entry: {0}, `unit_flags2`: {1}) has different `unit_flags2` in difficulty {2} mode (Entry: {3}, `unit_flags2`: {4}).",
                                 cInfo.Entry,
                                 cInfo.UnitFlags2,
                                 diff + 1,
                                 cInfo.DifficultyEntry[diff],
                                 difficultyInfo.UnitFlags2);

                Log.Logger.Error("Possible FIX: UPDATE `creature_template` SET `unit_flags2`=`unit_flags2`^{0} WHERE `entry`={1};", cInfo.UnitFlags2 ^ difficultyInfo.UnitFlags2, cInfo.DifficultyEntry[diff]);
            }

            if (cInfo.Family != difficultyInfo.Family)
                Log.Logger.Error("Creature (Entry: {0}, family: {1}) has different `family` in difficulty {2} mode (Entry: {3}, family: {4}).",
                                 cInfo.Entry,
                                 cInfo.Family,
                                 diff + 1,
                                 cInfo.DifficultyEntry[diff],
                                 difficultyInfo.Family);

            if (cInfo.TrainerClass != difficultyInfo.TrainerClass)
            {
                Log.Logger.Error("Creature (Entry: {0}) has different `trainer_class` in difficulty {1} mode (Entry: {2}).", cInfo.Entry, diff + 1, cInfo.DifficultyEntry[diff]);

                continue;
            }

            if (cInfo.CreatureType != difficultyInfo.CreatureType)
                Log.Logger.Error("Creature (Entry: {0}, type: {1}) has different `type` in difficulty {2} mode (Entry: {3}, type: {4}).",
                                 cInfo.Entry,
                                 cInfo.CreatureType,
                                 diff + 1,
                                 cInfo.DifficultyEntry[diff],
                                 difficultyInfo.CreatureType);

            if (cInfo.VehicleId == 0 && difficultyInfo.VehicleId != 0)
                Log.Logger.Error("Non-vehicle Creature (Entry: {0}, VehicleId: {1}) has `VehicleId` set in difficulty {2} mode (Entry: {3}, VehicleId: {4}).",
                                 cInfo.Entry,
                                 cInfo.VehicleId,
                                 diff + 1,
                                 cInfo.DifficultyEntry[diff],
                                 difficultyInfo.VehicleId);

            if (cInfo.RegenHealth != difficultyInfo.RegenHealth)
            {
                Log.Logger.Error("Creature (Entry: {0}, RegenHealth: {1}) has different `RegenHealth` in difficulty {2} mode (Entry: {3}, RegenHealth: {4}).",
                                 cInfo.Entry,
                                 cInfo.RegenHealth,
                                 diff + 1,
                                 cInfo.DifficultyEntry[diff],
                                 difficultyInfo.RegenHealth);

                Log.Logger.Error("Possible FIX: UPDATE `creature_template` SET `RegenHealth`={0} WHERE `entry`={1};", cInfo.RegenHealth, cInfo.DifficultyEntry[diff]);
            }

            var differenceMask = cInfo.MechanicImmuneMask & ~difficultyInfo.MechanicImmuneMask;

            if (differenceMask != 0)
            {
                Log.Logger.Error("Creature (Entry: {0}, mechanic_immune_mask: {1}) has weaker immunities in difficulty {2} mode (Entry: {3}, mechanic_immune_mask: {4}).",
                                 cInfo.Entry,
                                 cInfo.MechanicImmuneMask,
                                 diff + 1,
                                 cInfo.DifficultyEntry[diff],
                                 difficultyInfo.MechanicImmuneMask);

                Log.Logger.Error("Possible FIX: UPDATE `creature_template` SET `mechanic_immune_mask`=`mechanic_immune_mask`|{0} WHERE `entry`={1};", differenceMask, cInfo.DifficultyEntry[diff]);
            }

            differenceMask = (uint)((cInfo.FlagsExtra ^ difficultyInfo.FlagsExtra) & ~CreatureFlagsExtra.InstanceBind);

            if (differenceMask != 0)
            {
                Log.Logger.Error("Creature (Entry: {0}, flags_extra: {1}) has different `flags_extra` in difficulty {2} mode (Entry: {3}, flags_extra: {4}).",
                                 cInfo.Entry,
                                 cInfo.FlagsExtra,
                                 diff + 1,
                                 cInfo.DifficultyEntry[diff],
                                 difficultyInfo.FlagsExtra);

                Log.Logger.Error("Possible FIX: UPDATE `creature_template` SET `flags_extra`=`flags_extra`^{0} WHERE `entry`={1};", differenceMask, cInfo.DifficultyEntry[diff]);
            }

            if (difficultyInfo.AIName.IsEmpty())
            {
                Log.Logger.Error("Creature (Entry: {0}) lists difficulty {1} mode entry {2} with `AIName` filled in. `AIName` of difficulty 0 mode creature is always used instead.",
                                 cInfo.Entry,
                                 diff + 1,
                                 cInfo.DifficultyEntry[diff]);

                continue;
            }

            if (difficultyInfo.ScriptID != 0)
            {
                Log.Logger.Error("Creature (Entry: {0}) lists difficulty {1} mode entry {2} with `ScriptName` filled in. `ScriptName` of difficulty 0 mode creature is always used instead.",
                                 cInfo.Entry,
                                 diff + 1,
                                 cInfo.DifficultyEntry[diff]);

                continue;
            }

            _hasDifficultyEntries[diff].Add(cInfo.Entry);
            _difficultyEntries[diff].Add(cInfo.DifficultyEntry[diff]);
            ok = true;
        }

        if (cInfo.MinGold > cInfo.MaxGold)
        {
            Log.Logger.Verbose($"Creature (Entry: {cInfo.Entry}) has `mingold` {cInfo.MinGold} which is greater than `maxgold` {cInfo.MaxGold}, setting `maxgold` to {cInfo.MinGold}.");
            cInfo.MaxGold = cInfo.MinGold;
        }

        if (!_factionTemplateRecords.ContainsKey(cInfo.Faction))
        {
            Log.Logger.Verbose("Creature (Entry: {0}) has non-existing faction template ({1}). This can lead to crashes, set to faction 35", cInfo.Entry, cInfo.Faction);
            cInfo.Faction = 35;
        }

        for (var k = 0; k < SharedConst.MaxCreatureKillCredit; ++k)
            if (cInfo.KillCredit[k] != 0)
                if (GetCreatureTemplate(cInfo.KillCredit[k]) == null)
                {
                    Log.Logger.Verbose("Creature (Entry: {0}) lists non-existing creature entry {1} in `KillCredit{2}`.", cInfo.Entry, cInfo.KillCredit[k], k + 1);
                    cInfo.KillCredit[k] = 0;
                }

        if (cInfo.Models.Empty())
            Log.Logger.Error($"Creature (Entry: {cInfo.Entry}) does not have any existing display id in creature_template_model.");

        if (cInfo.UnitClass == 0 || (1 << (int)cInfo.UnitClass - 1 & (int)PlayerClass.ClassMaskAllCreatures) == 0)
        {
            Log.Logger.Verbose("Creature (Entry: {0}) has invalid unit_class ({1}) in creature_template. Set to 1 (UNIT_CLASS_WARRIOR).", cInfo.Entry, cInfo.UnitClass);
            cInfo.UnitClass = (uint)PlayerClass.Warrior;
        }

        if (cInfo.DmgSchool >= (uint)SpellSchools.Max)
        {
            Log.Logger.Verbose("Creature (Entry: {0}) has invalid spell school value ({1}) in `dmgschool`.", cInfo.Entry, cInfo.DmgSchool);
            cInfo.DmgSchool = (uint)SpellSchools.Normal;
        }

        if (cInfo.BaseAttackTime == 0)
            cInfo.BaseAttackTime = SharedConst.BaseAttackTime;

        if (cInfo.RangeAttackTime == 0)
            cInfo.RangeAttackTime = SharedConst.BaseAttackTime;

        if (cInfo.SpeedWalk == 0.0f)
        {
            Log.Logger.Verbose("Creature (Entry: {0}) has wrong value ({1}) in speed_walk, set to 1.", cInfo.Entry, cInfo.SpeedWalk);
            cInfo.SpeedWalk = 1.0f;
        }

        if (cInfo.SpeedRun == 0.0f)
        {
            Log.Logger.Verbose("Creature (Entry: {0}) has wrong value ({1}) in speed_run, set to 1.14286.", cInfo.Entry, cInfo.SpeedRun);
            cInfo.SpeedRun = 1.14286f;
        }

        if (cInfo.CreatureType != 0 && !_creatureTypeRecords.ContainsKey((uint)cInfo.CreatureType))
        {
            Log.Logger.Verbose("Creature (Entry: {0}) has invalid creature type ({1}) in `type`.", cInfo.Entry, cInfo.CreatureType);
            cInfo.CreatureType = CreatureType.Humanoid;
        }

        if (cInfo.Family != 0 && !_creatureFamilyRecords.ContainsKey(cInfo.Family))
        {
            Log.Logger.Verbose("Creature (Entry: {0}) has invalid creature family ({1}) in `family`.", cInfo.Entry, cInfo.Family);
            cInfo.Family = CreatureFamily.None;
        }

        CheckCreatureMovement("creature_template_movement", cInfo.Entry, cInfo.Movement);

        if (cInfo.HoverHeight < 0.0f)
        {
            Log.Logger.Verbose("Creature (Entry: {0}) has wrong value ({1}) in `HoverHeight`", cInfo.Entry, cInfo.HoverHeight);
            cInfo.HoverHeight = 1.0f;
        }

        if (cInfo.VehicleId != 0)
            if (!_vehicleRecords.ContainsKey(cInfo.VehicleId))
            {
                Log.Logger.Verbose("Creature (Entry: {0}) has a non-existing VehicleId ({1}). This *WILL* cause the client to freeze!", cInfo.Entry, cInfo.VehicleId);
                cInfo.VehicleId = 0;
            }

        for (byte j = 0; j < SharedConst.MaxCreatureSpells; ++j)
            if (cInfo.Spells[j] != 0 && !_spellManager.HasSpellInfo(cInfo.Spells[j]))
            {
                Log.Logger.Verbose("Creature (Entry: {0}) has non-existing Spell{1} ({2}), set to 0.", cInfo.Entry, j + 1, cInfo.Spells[j]);
                cInfo.Spells[j] = 0;
            }

        if (cInfo.MovementType >= (uint)MovementGeneratorType.MaxDB)
        {
            Log.Logger.Verbose("Creature (Entry: {0}) has wrong movement generator type ({1}), ignored and set to IDLE.", cInfo.Entry, cInfo.MovementType);
            cInfo.MovementType = (uint)MovementGeneratorType.Idle;
        }

        if (cInfo.HealthScalingExpansion is < (int)Expansion.LevelCurrent or >= (int)Expansion.Max)
        {
            Log.Logger.Verbose("Table `creature_template` lists creature (Id: {0}) with invalid `HealthScalingExpansion` {1}. Ignored and set to 0.", cInfo.Entry, cInfo.HealthScalingExpansion);
            cInfo.HealthScalingExpansion = 0;
        }

        if (cInfo.RequiredExpansion > (int)Expansion.Max)
        {
            Log.Logger.Verbose("Table `creature_template` lists creature (Entry: {0}) with `RequiredExpansion` {1}. Ignored and set to 0.", cInfo.Entry, cInfo.RequiredExpansion);
            cInfo.RequiredExpansion = 0;
        }

        var badFlags = (uint)(cInfo.FlagsExtra & ~CreatureFlagsExtra.DBAllowed);

        if (badFlags != 0)
        {
            Log.Logger.Verbose("Table `creature_template` lists creature (Entry: {0}) with disallowed `flags_extra` {1}, removing incorrect Id.", cInfo.Entry, badFlags);
            cInfo.FlagsExtra &= CreatureFlagsExtra.DBAllowed;
        }

        var disallowedUnitFlags = (uint)(cInfo.UnitFlags & ~UnitFlags.Allowed);

        if (disallowedUnitFlags != 0)
        {
            Log.Logger.Verbose($"Table `creature_template` lists creature (Entry: {cInfo.Entry}) with disallowed `unit_flags` {disallowedUnitFlags}, removing incorrect Id.");
            cInfo.UnitFlags &= UnitFlags.Allowed;
        }

        var disallowedUnitFlags2 = cInfo.UnitFlags2 & ~(uint)UnitFlags2.Allowed;

        if (disallowedUnitFlags2 != 0)
        {
            Log.Logger.Verbose($"Table `creature_template` lists creature (Entry: {cInfo.Entry}) with disallowed `unit_flags2` {disallowedUnitFlags2}, removing incorrect Id.");
            cInfo.UnitFlags2 &= (uint)UnitFlags2.Allowed;
        }

        var disallowedUnitFlags3 = cInfo.UnitFlags3 & ~(uint)UnitFlags3.Allowed;

        if (disallowedUnitFlags3 != 0)
        {
            Log.Logger.Verbose($"Table `creature_template` lists creature (Entry: {cInfo.Entry}) with disallowed `unit_flags2` {disallowedUnitFlags3}, removing incorrect Id.");
            cInfo.UnitFlags3 &= (uint)UnitFlags3.Allowed;
        }

        if (cInfo.DynamicFlags != 0)
        {
            Log.Logger.Verbose($"Table `creature_template` lists creature (Entry: {cInfo.Entry}) with `dynamicflags` > 0. Ignored and set to 0.");
            cInfo.DynamicFlags = 0;
        }

        var levels = cInfo.GetMinMaxLevel();

        if (levels[0] < 1 || levels[0] > SharedConst.StrongMaxLevel)
        {
            Log.Logger.Verbose($"Creature (ID: {cInfo.Entry}): Calculated minLevel {cInfo.Minlevel} is not within [1, 255], value has been set to {(cInfo.HealthScalingExpansion == (int)Expansion.LevelCurrent ? SharedConst.MaxLevel : 1)}.");
            cInfo.Minlevel = (short)(cInfo.HealthScalingExpansion == (int)Expansion.LevelCurrent ? 0 : 1);
        }

        if (levels[1] < 1 || levels[1] > SharedConst.StrongMaxLevel)
        {
            Log.Logger.Verbose($"Creature (ID: {cInfo.Entry}): Calculated maxLevel {cInfo.Maxlevel} is not within [1, 255], value has been set to {(cInfo.HealthScalingExpansion == (int)Expansion.LevelCurrent ? SharedConst.MaxLevel : 1)}.");
            cInfo.Maxlevel = (short)(cInfo.HealthScalingExpansion == (int)Expansion.LevelCurrent ? 0 : 1);
        }

        cInfo.ModDamage *= GetDamageMod(cInfo.Rank);

        float GetDamageMod(CreatureEliteType rank)
        {
            return rank switch // define rates for each elite rank
            {
                CreatureEliteType.Normal => _configuration.GetDefaultValue("Rate:Creature:Normal:Damage", 1.0f),
                CreatureEliteType.Elite => _configuration.GetDefaultValue("Rate:Creature:Elite:Elite:Damage", 1.0f),
                CreatureEliteType.RareElite => _configuration.GetDefaultValue("Rate:Creature:Elite:RAREELITE:Damage", 1.0f),
                CreatureEliteType.WorldBoss => _configuration.GetDefaultValue("Rate:Creature:Elite:WORLDBOSS:Damage", 1.0f),
                CreatureEliteType.Rare => _configuration.GetDefaultValue("Rate:Creature:Elite:RARE:Damage", 1.0f),
                _ => _configuration.GetDefaultValue("Rate:Creature:Elite:Elite:Damage", 1.0f)
            };
        }

        if (cInfo.GossipMenuId != 0 && !cInfo.Npcflag.HasAnyFlag((ulong)NPCFlags.Gossip))
            Log.Logger.Information($"Creature (Entry: {cInfo.Entry}) has assigned gossip menu {cInfo.GossipMenuId}, but npcflag does not include UNIT_NPC_FLAG_GOSSIP.");
        else if (cInfo.GossipMenuId == 0 && cInfo.Npcflag.HasAnyFlag((ulong)NPCFlags.Gossip))
            Log.Logger.Information($"Creature (Entry: {cInfo.Entry}) has npcflag UNIT_NPC_FLAG_GOSSIP, but gossip menu is unassigned.");
    }

    public CreatureSummonedData GetCreatureSummonedData(uint entryId)
    {
        return _creatureSummonedDataStorage.LookupByKey(entryId);
    }

    public CreatureTemplate GetCreatureTemplate(uint entry)
    {
        return CreatureTemplates.LookupByKey(entry);
    }

    public void Load()
    {
        var time = Time.MSTime;

        var stmt = _worldDatabase.GetPreparedStatement(WorldStatements.SEL_CREATURE_TEMPLATE);
        stmt.AddValue(0, 0);
        stmt.AddValue(1, 1);

        var result = _worldDatabase.Query(stmt);

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 creatures. DB table `creature_template` is empty.");

            return;
        }

        do
        {
            LoadCreatureTemplate(result.GetFields());
        } while (result.NextRow());

        LoadCreatureTemplateResistances();
        LoadCreatureTemplateSpells();

        // We load the creature models after loading but before checking
        LoadCreatureTemplateModels();

        LoadCreatureSummonedData();

        // Checking needs to be done after loading because of the difficulty self referencing
        foreach (var template in CreatureTemplates.Values)
            CheckCreatureTemplate(template);

        LoadScriptWaypoints();
        Log.Logger.Information("Loaded {0} creature definitions in {1} ms", CreatureTemplates.Count, Time.GetMSTimeDiffToNow(time));
    }

    public void LoadCreatureTemplate(SQLFields fields)
    {
        var entry = fields.Read<uint>(0);

        CreatureTemplate creature = new(_configuration, _gameObjectManager)
        {
            Entry = entry
        };

        for (var i = 0; i < SharedConst.MaxCreatureDifficulties; ++i)
            creature.DifficultyEntry[i] = fields.Read<uint>(1 + i);

        for (var i = 0; i < 2; ++i)
            creature.KillCredit[i] = fields.Read<uint>(4 + i);

        creature.Name = fields.Read<string>(6);
        creature.FemaleName = fields.Read<string>(7);
        creature.SubName = fields.Read<string>(8);
        creature.TitleAlt = fields.Read<string>(9);
        creature.IconName = fields.Read<string>(10);
        creature.GossipMenuId = fields.Read<uint>(11);
        creature.Minlevel = fields.Read<short>(12);
        creature.Maxlevel = fields.Read<short>(13);
        creature.HealthScalingExpansion = fields.Read<int>(14);
        creature.RequiredExpansion = fields.Read<uint>(15);
        creature.VignetteID = fields.Read<uint>(16);
        creature.Faction = fields.Read<uint>(17);
        creature.Npcflag = fields.Read<ulong>(18);
        creature.SpeedWalk = fields.Read<float>(19);
        creature.SpeedRun = fields.Read<float>(20);
        creature.Scale = fields.Read<float>(21);
        creature.Rank = (CreatureEliteType)fields.Read<uint>(22);
        creature.DmgSchool = fields.Read<uint>(23);
        creature.BaseAttackTime = fields.Read<uint>(24);
        creature.RangeAttackTime = fields.Read<uint>(25);
        creature.BaseVariance = fields.Read<float>(26);
        creature.RangeVariance = fields.Read<float>(27);
        creature.UnitClass = fields.Read<uint>(28);
        creature.UnitFlags = (UnitFlags)fields.Read<uint>(29);
        creature.UnitFlags2 = fields.Read<uint>(30);
        creature.UnitFlags3 = fields.Read<uint>(31);
        creature.DynamicFlags = fields.Read<uint>(32);
        creature.Family = (CreatureFamily)fields.Read<uint>(33);
        creature.TrainerClass = (PlayerClass)fields.Read<byte>(34);
        creature.CreatureType = (CreatureType)fields.Read<byte>(35);
        creature.TypeFlags = (CreatureTypeFlags)fields.Read<uint>(36);
        creature.TypeFlags2 = fields.Read<uint>(37);
        creature.LootId = fields.Read<uint>(38);
        creature.PickPocketId = fields.Read<uint>(39);
        creature.SkinLootId = fields.Read<uint>(40);

        for (var i = (int)SpellSchools.Holy; i < (int)SpellSchools.Max; ++i)
            creature.Resistance[i] = 0;

        for (var i = 0; i < SharedConst.MaxCreatureSpells; ++i)
            creature.Spells[i] = 0;

        creature.VehicleId = fields.Read<uint>(41);
        creature.MinGold = fields.Read<uint>(42);
        creature.MaxGold = fields.Read<uint>(43);
        creature.AIName = fields.Read<string>(44);
        creature.MovementType = fields.Read<uint>(45);

        if (!fields.IsNull(46))
            creature.Movement.Ground = (CreatureGroundMovementType)fields.Read<byte>(46);

        if (!fields.IsNull(47))
            creature.Movement.Swim = fields.Read<bool>(47);

        if (!fields.IsNull(48))
            creature.Movement.Flight = (CreatureFlightMovementType)fields.Read<byte>(48);

        if (!fields.IsNull(49))
            creature.Movement.Rooted = fields.Read<bool>(49);

        if (!fields.IsNull(50))
            creature.Movement.Chase = (CreatureChaseMovementType)fields.Read<byte>(50);

        if (!fields.IsNull(51))
            creature.Movement.Random = (CreatureRandomMovementType)fields.Read<byte>(51);

        if (!fields.IsNull(52))
            creature.Movement.InteractionPauseTimer = fields.Read<uint>(52);

        creature.HoverHeight = fields.Read<float>(53);
        creature.ModHealth = fields.Read<float>(54);
        creature.ModHealthExtra = fields.Read<float>(55);
        creature.ModMana = fields.Read<float>(56);
        creature.ModManaExtra = fields.Read<float>(57);
        creature.ModArmor = fields.Read<float>(58);
        creature.ModDamage = fields.Read<float>(59);
        creature.ModExperience = fields.Read<float>(60);
        creature.RacialLeader = fields.Read<bool>(61);
        creature.MovementId = fields.Read<uint>(62);
        creature.CreatureDifficultyID = fields.Read<int>(63);
        creature.WidgetSetID = fields.Read<int>(64);
        creature.WidgetSetUnitConditionID = fields.Read<int>(65);
        creature.RegenHealth = fields.Read<bool>(66);
        creature.MechanicImmuneMask = fields.Read<ulong>(67);
        creature.SpellSchoolImmuneMask = fields.Read<uint>(68);
        creature.FlagsExtra = (CreatureFlagsExtra)fields.Read<uint>(69);
        creature.ScriptID = _scriptManager.GetScriptId(fields.Read<string>(70));
        creature.StringId = fields.Read<string>(71);

        CreatureTemplates[entry] = creature;
    }

    private void CheckCreatureMovement(string table, ulong id, CreatureMovementData creatureMovement)
    {
        if (creatureMovement.Ground >= CreatureGroundMovementType.Max)
        {
            Log.Logger.Error($"`{table}`.`Ground` wrong value ({creatureMovement.Ground}) for Id {id}, setting to Run.");
            creatureMovement.Ground = CreatureGroundMovementType.Run;
        }

        if (creatureMovement.Flight >= CreatureFlightMovementType.Max)
        {
            Log.Logger.Error($"`{table}`.`Flight` wrong value ({creatureMovement.Flight}) for Id {id}, setting to None.");
            creatureMovement.Flight = CreatureFlightMovementType.None;
        }

        if (creatureMovement.Chase >= CreatureChaseMovementType.Max)
        {
            Log.Logger.Error($"`{table}`.`Chase` wrong value ({creatureMovement.Chase}) for Id {id}, setting to Run.");
            creatureMovement.Chase = CreatureChaseMovementType.Run;
        }

        if (creatureMovement.Random >= CreatureRandomMovementType.Max)
        {
            Log.Logger.Error($"`{table}`.`Random` wrong value ({creatureMovement.Random}) for Id {id}, setting to Walk.");
            creatureMovement.Random = CreatureRandomMovementType.Walk;
        }
    }

    private void LoadCreatureSummonedData()
    {
        var oldMSTime = Time.MSTime;

        //                                         0           1                            2                     3
        var result = _worldDatabase.Query("SELECT CreatureID, CreatureIDVisibleToSummoner, GroundMountDisplayID, FlyingMountDisplayID FROM creature_summoned_data");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 creature summoned data definitions. DB table `creature_summoned_data` is empty.");

            return;
        }

        do
        {
            var creatureId = result.Read<uint>(0);

            if (GetCreatureTemplate(creatureId) == null)
            {
                Log.Logger.Debug($"Table `creature_summoned_data` references non-existing creature {creatureId}, skipped");

                continue;
            }

            if (!_creatureSummonedDataStorage.ContainsKey(creatureId))
                _creatureSummonedDataStorage[creatureId] = new CreatureSummonedData();

            var summonedData = _creatureSummonedDataStorage[creatureId];

            if (!result.IsNull(1))
            {
                summonedData.CreatureIdVisibleToSummoner = result.Read<uint>(1);

                if (GetCreatureTemplate(summonedData.CreatureIdVisibleToSummoner.Value) == null)
                {
                    Log.Logger.Debug($"Table `creature_summoned_data` references non-existing creature {summonedData.CreatureIdVisibleToSummoner.Value} in CreatureIDVisibleToSummoner for creature {creatureId}, set to 0");
                    summonedData.CreatureIdVisibleToSummoner = null;
                }
            }

            if (!result.IsNull(2))
            {
                summonedData.GroundMountDisplayId = result.Read<uint>(2);

                if (!_creatureDisplayInfoRecords.ContainsKey(summonedData.GroundMountDisplayId.Value))
                {
                    Log.Logger.Debug($"Table `creature_summoned_data` references non-existing display id {summonedData.GroundMountDisplayId.Value} in GroundMountDisplayID for creature {creatureId}, set to 0");
                    summonedData.CreatureIdVisibleToSummoner = null;
                }
            }

            if (!result.IsNull(3))
            {
                summonedData.FlyingMountDisplayId = result.Read<uint>(3);

                if (!_creatureDisplayInfoRecords.ContainsKey(summonedData.FlyingMountDisplayId.Value))
                {
                    Log.Logger.Debug($"Table `creature_summoned_data` references non-existing display id {summonedData.FlyingMountDisplayId.Value} in FlyingMountDisplayID for creature {creatureId}, set to 0");
                    summonedData.GroundMountDisplayId = null;
                }
            }
        } while (result.NextRow());

        Log.Logger.Information($"Loaded {_creatureSummonedDataStorage.Count} creature summoned data definitions in {Time.GetMSTimeDiffToNow(oldMSTime)} ms");
    }

    private void LoadCreatureTemplateModels()
    {
        var oldMSTime = Time.MSTime;
        //                                         0           1                  2             3
        var result = _worldDatabase.Query("SELECT CreatureID, CreatureDisplayID, DisplayScale, Probability FROM creature_template_model ORDER BY Idx ASC");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 creature template model definitions. DB table `creature_template_model` is empty.");

            return;
        }

        uint count = 0;

        do
        {
            var creatureId = result.Read<uint>(0);
            var creatureDisplayId = result.Read<uint>(1);
            var displayScale = result.Read<float>(2);
            var probability = result.Read<float>(3);

            var cInfo = GetCreatureTemplate(creatureId);

            if (cInfo == null)
            {
                if (_configuration.GetDefaultValue("load:autoclean", false))
                    _worldDatabase.Execute($"DELETE FROM creature_template_model WHERE CreatureID = {creatureId}");
                else
                    Log.Logger.Debug($"Creature template (Entry: {creatureId}) does not exist but has a record in `creature_template_model`");

                continue;
            }

            if (!_creatureDisplayInfoRecords.TryGetValue(creatureDisplayId, out _))
            {
                Log.Logger.Debug($"Creature (Entry: {creatureId}) lists non-existing CreatureDisplayID id ({creatureDisplayId}), this can crash the client.");

                continue;
            }

            var modelInfo = _creatureModelCache.GetCreatureModelInfo(creatureDisplayId);

            if (modelInfo == null)
                Log.Logger.Debug($"No model data exist for `CreatureDisplayID` = {creatureDisplayId} listed by creature (Entry: {creatureId}).");

            if (displayScale <= 0.0f)
                displayScale = 1.0f;

            cInfo.Models.Add(new CreatureModel(creatureDisplayId, displayScale, probability));
            ++count;
        } while (result.NextRow());

        Log.Logger.Information($"Loaded {count} creature template models in {Time.GetMSTimeDiffToNow(oldMSTime)} ms");
    }

    private void LoadCreatureTemplateResistances()
    {
        var oldMSTime = Time.MSTime;

        //                                         0           1       2
        var result = _worldDatabase.Query("SELECT CreatureID, School, Resistance FROM creature_template_resistance");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 creature template resistance definitions. DB table `creature_template_resistance` is empty.");

            return;
        }

        uint count = 0;

        do
        {
            var creatureID = result.Read<uint>(0);
            var school = (SpellSchools)result.Read<byte>(1);

            if (school is SpellSchools.Normal or >= SpellSchools.Max)
            {
                Log.Logger.Error($"creature_template_resistance has resistance definitions for creature {creatureID} but this school {school} doesn't exist");

                continue;
            }

            if (!CreatureTemplates.TryGetValue(creatureID, out var creatureTemplate))
            {
                Log.Logger.Error($"creature_template_resistance has resistance definitions for creature {creatureID} but this creature doesn't exist");

                continue;
            }

            creatureTemplate.Resistance[(int)school] = result.Read<short>(2);

            ++count;
        } while (result.NextRow());

        Log.Logger.Information($"Loaded {count} creature template resistances in {Time.GetMSTimeDiffToNow(oldMSTime)} ms");
    }

    private void LoadCreatureTemplateSpells()
    {
        var oldMSTime = Time.MSTime;

        //                                         0           1       2
        var result = _worldDatabase.Query("SELECT CreatureID, `Index`, Spell FROM creature_template_spell");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 creature template spell definitions. DB table `creature_template_spell` is empty.");

            return;
        }

        uint count = 0;

        do
        {
            var creatureID = result.Read<uint>(0);
            var index = result.Read<byte>(1);

            if (index >= SharedConst.MaxCreatureSpells)
            {
                Log.Logger.Error($"creature_template_spell has spell definitions for creature {creatureID} with a incorrect index {index}");

                continue;
            }

            if (!CreatureTemplates.TryGetValue(creatureID, out var creatureTemplate))
            {
                Log.Logger.Error($"creature_template_spell has spell definitions for creature {creatureID} but this creature doesn't exist");

                continue;
            }

            creatureTemplate.Spells[index] = result.Read<uint>(2);

            ++count;
        } while (result.NextRow());

        Log.Logger.Information($"Loaded {count} creature template spells in {Time.GetMSTimeDiffToNow(oldMSTime)} ms");
    }

    private void LoadScriptWaypoints()
    {
        var oldMSTime = Time.MSTime;

        // Drop Existing Waypoint list
        _scriptManager.WaypointStore.Clear();

        ulong entryCount = 0;

        // Load Waypoints
        var result = _worldDatabase.Query("SELECT COUNT(entry) FROM script_waypoint GROUP BY entry");

        if (!result.IsEmpty())
            entryCount = result.Read<uint>(0);

        Log.Logger.Information($"Loading Script Waypoints for {entryCount} creature(s)...");

        //                                0       1         2           3           4           5
        result = _worldDatabase.Query("SELECT entry, pointid, location_x, location_y, location_z, waittime FROM script_waypoint ORDER BY pointid");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 Script Waypoints. DB table `script_waypoint` is empty.");

            return;
        }

        uint count = 0;

        do
        {
            var entry = result.Read<uint>(0);
            var id = result.Read<uint>(1);
            var x = result.Read<float>(2);
            var y = result.Read<float>(3);
            var z = result.Read<float>(4);
            var waitTime = result.Read<uint>(5);

            var info = GetCreatureTemplate(entry);

            if (info == null)
            {
                Log.Logger.Error($"SystemMgr: DB table script_waypoint has waypoint for non-existant creature entry {entry}");

                continue;
            }

            if (info.ScriptID == 0)
                Log.Logger.Error($"SystemMgr: DB table script_waypoint has waypoint for creature entry {entry}, but creature does not have ScriptName defined and then useless.");

            if (!_scriptManager.WaypointStore.ContainsKey(entry))
                _scriptManager.WaypointStore[entry] = new WaypointPath();

            var path = _scriptManager.WaypointStore[entry];
            path.ID = entry;
            path.Nodes.Add(new WaypointNode(id, x, y, z, null, waitTime));

            ++count;
        } while (result.NextRow());

        Log.Logger.Information("Loaded {0} Script Waypoint nodes in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
    }
}