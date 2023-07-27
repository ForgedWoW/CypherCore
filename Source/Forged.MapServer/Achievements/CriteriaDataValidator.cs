// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.DataStorage;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Events;
using Forged.MapServer.Globals;
using Forged.MapServer.Globals.Caching;
using Forged.MapServer.Maps;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAchievement;
using Forged.MapServer.Spells;
using Framework.Constants;
using Serilog;

namespace Forged.MapServer.Achievements;

public class CriteriaDataValidator
{
    private readonly CliDB _cliDB;
    private readonly CriteriaManager _criteriaManager;
    private readonly GameEventManager _gameEventManager;
    private readonly GameObjectManager _objectManager;
    private readonly PlayerComputators _playerComputators;
    private readonly ItemTemplateCache _itemTemplateCache;
    private readonly ScriptManager _scriptManager;
    private readonly SpellManager _spellManager;

    public CriteriaDataValidator(GameObjectManager objectManager, SpellManager spellManager, CliDB cliDB, GameEventManager gameEventManager, 
                                 ScriptManager scriptManager, CriteriaManager criteriaManager, PlayerComputators playerComputators, ItemTemplateCache itemTemplateCache)
    {
        _objectManager = objectManager;
        _spellManager = spellManager;
        _cliDB = cliDB;
        _gameEventManager = gameEventManager;
        _scriptManager = scriptManager;
        _criteriaManager = criteriaManager;
        _playerComputators = playerComputators;
        _itemTemplateCache = itemTemplateCache;
    }

    public bool IsValid(CriteriaData criteriaData, Criteria criteria)
    {
        if (criteriaData.DataType >= CriteriaDataType.Max)
        {
            Log.Logger.Error("Table `criteria_data` for criteria (Entry: {0}) has wrong data type ({1}), ignored.", criteria.Id, criteriaData.DataType);

            return false;
        }

        switch (criteria.Entry.Type)
        {
            case CriteriaType.KillCreature:
            case CriteriaType.KillAnyCreature:
            case CriteriaType.WinBattleground:
            case CriteriaType.MaxDistFallenWithoutDying:
            case CriteriaType.CompleteQuest: // only hardcoded list
            case CriteriaType.CastSpell:
            case CriteriaType.WinAnyRankedArena:
            case CriteriaType.DoEmote:
            case CriteriaType.KillPlayer:
            case CriteriaType.WinDuel:
            case CriteriaType.GetLootByType:
            case CriteriaType.LandTargetedSpellOnTarget:
            case CriteriaType.BeSpellTarget:
            case CriteriaType.GainAura:
            case CriteriaType.EquipItemInSlot:
            case CriteriaType.RollNeed:
            case CriteriaType.RollGreed:
            case CriteriaType.TrackedWorldStateUIModified:
            case CriteriaType.EarnHonorableKill:
            case CriteriaType.CompleteDailyQuest: // only Children's Week achievements
            case CriteriaType.UseItem:            // only Children's Week achievements
            case CriteriaType.DeliveredKillingBlow:
            case CriteriaType.ReachLevel:
            case CriteriaType.Login:
            case CriteriaType.LootAnyItem:
            case CriteriaType.ObtainAnyItem:
                break;

            default:
                if (criteriaData.DataType != CriteriaDataType.Script)
                {
                    Log.Logger.Error("Table `criteria_data` has data for non-supported criteria type (Entry: {0} Type: {1}), ignored.", criteria.Id, criteria.Entry.Type);

                    return false;
                }

                break;
        }

        switch (criteriaData.DataType)
        {
            case CriteriaDataType.None:
            case CriteriaDataType.InstanceScript:
                return true;

            case CriteriaDataType.TCreature:
                if (criteriaData.Creature.Id != 0 && _objectManager.CreatureTemplateCache.GetCreatureTemplate(criteriaData.Creature.Id) != null)
                    return true;

                Log.Logger.Error("Table `criteria_data` (Entry: {0} Type: {1}) for data type CRITERIA_DATA_TYPE_CREATURE ({2}) has non-existing creature id in value1 ({3}), ignored.",
                                 criteria.Id,
                                 criteria.Entry.Type,
                                 criteriaData.DataType,
                                 criteriaData.Creature.Id);

                return false;

            case CriteriaDataType.TPlayerClassRace:
                if (criteriaData.ClassRace is { ClassId: 0, RaceId: 0 })
                {
                    Log.Logger.Error("Table `criteria_data` (Entry: {0} Type: {1}) for data type CRITERIA_DATA_TYPE_T_PLAYER_CLASS_RACE ({2}) must not have 0 in either value field, ignored.",
                                     criteria.Id,
                                     criteria.Entry.Type,
                                     criteriaData.DataType);

                    return false;
                }

                if (criteriaData.ClassRace.ClassId != 0 && ((1 << (int)(criteriaData.ClassRace.ClassId - 1)) & (int)PlayerClass.ClassMaskAllPlayable) == 0)
                {
                    Log.Logger.Error("Table `criteria_data` (Entry: {0} Type: {1}) for data type CRITERIA_DATA_TYPE_T_PLAYER_CLASS_RACE ({2}) has non-existing class in value1 ({3}), ignored.",
                                     criteria.Id,
                                     criteria.Entry.Type,
                                     criteriaData.DataType,
                                     criteriaData.ClassRace.ClassId);

                    return false;
                }

                if (criteriaData.ClassRace.RaceId == 0 || (SharedConst.GetMaskForRace((Race)criteriaData.ClassRace.RaceId) & (long)SharedConst.RaceMaskAllPlayable) != 0)
                    return true;

                Log.Logger.Error("Table `criteria_data` (Entry: {0} Type: {1}) for data type CRITERIA_DATA_TYPE_T_PLAYER_CLASS_RACE ({2}) has non-existing race in value2 ({3}), ignored.",
                                 criteria.Id,
                                 criteria.Entry.Type,
                                 criteriaData.DataType,
                                 criteriaData.ClassRace.RaceId);

                return false;

            case CriteriaDataType.TPlayerLessHealth:
                if (criteriaData.Health.Percent is >= 1 and <= 100)
                    return true;

                Log.Logger.Error("Table `criteria_data` (Entry: {0} Type: {1}) for data type CRITERIA_DATA_TYPE_PLAYER_LESS_HEALTH ({2}) has wrong percent value in value1 ({3}), ignored.",
                                 criteria.Id,
                                 criteria.Entry.Type,
                                 criteriaData.DataType,
                                 criteriaData.Health.Percent);

                return false;

            case CriteriaDataType.SAura:
            case CriteriaDataType.TAura:
            {
                var spellEntry = _spellManager.GetSpellInfo(criteriaData.Aura.SpellId);

                if (spellEntry == null)
                {
                    Log.Logger.Error("Table `criteria_data` (Entry: {0} Type: {1}) for data type {2} has wrong spell id in value1 ({3}), ignored.",
                                     criteria.Id,
                                     criteria.Entry.Type,
                                     criteriaData.DataType,
                                     criteriaData.Aura.SpellId);

                    return false;
                }

                if (spellEntry.Effects.Count <= criteriaData.Aura.EffectIndex)
                {
                    Log.Logger.Error("Table `criteria_data` (Entry: {0} Type: {1}) for data type {2} has wrong spell effect index in value2 ({3}), ignored.",
                                     criteria.Id,
                                     criteria.Entry.Type,
                                     criteriaData.DataType,
                                     criteriaData.Aura.EffectIndex);

                    return false;
                }

                if (spellEntry.GetEffect(criteriaData.Aura.EffectIndex).ApplyAuraName != 0)
                    return true;

                Log.Logger.Error("Table `criteria_data` (Entry: {0} Type: {1}) for data type {2} has non-aura spell effect (ID: {3} Effect: {4}), ignores.",
                                 criteria.Id,
                                 criteria.Entry.Type,
                                 criteriaData.DataType,
                                 criteriaData.Aura.SpellId,
                                 criteriaData.Aura.EffectIndex);

                return false;
            }
            case CriteriaDataType.Value:
                if (criteriaData.Value.ComparisonType < (int)ComparisionType.Max)
                    return true;

                Log.Logger.Error("Table `criteria_data` (Entry: {0} Type: {1}) for data type CRITERIA_DATA_TYPE_VALUE ({2}) has wrong ComparisionType in value2 ({3}), ignored.",
                                 criteria.Id,
                                 criteria.Entry.Type,
                                 criteriaData.DataType,
                                 criteriaData.Value.ComparisonType);

                return false;

            case CriteriaDataType.TLevel:
                if (criteriaData.Level.Min <= SharedConst.GTMaxLevel)
                    return true;

                Log.Logger.Error("Table `criteria_data` (Entry: {0} Type: {1}) for data type CRITERIA_DATA_TYPE_T_LEVEL ({2}) has wrong minlevel in value1 ({3}), ignored.",
                                 criteria.Id,
                                 criteria.Entry.Type,
                                 criteriaData.DataType,
                                 criteriaData.Level.Min);

                return false;

            case CriteriaDataType.TGender:
                if (criteriaData.Gender.Gender <= (int)Gender.None)
                    return true;

                Log.Logger.Error("Table `criteria_data` (Entry: {0} Type: {1}) for data type CRITERIA_DATA_TYPE_T_GENDER ({2}) has wrong gender in value1 ({3}), ignored.",
                                 criteria.Id,
                                 criteria.Entry.Type,
                                 criteriaData.DataType,
                                 criteriaData.Gender.Gender);

                return false;

            case CriteriaDataType.Script:
                if (criteriaData.ScriptId != 0)
                    return true;

                Log.Logger.Error("Table `criteria_data` (Entry: {0} Type: {1}) for data type CRITERIA_DATA_TYPE_SCRIPT ({2}) does not have ScriptName set, ignored.",
                                 criteria.Id,
                                 criteria.Entry.Type,
                                 criteriaData.DataType);

                return false;

            case CriteriaDataType.MapPlayerCount:
                if (criteriaData.MapPlayers.MaxCount > 0)
                    return true;

                Log.Logger.Error("Table `criteria_data` (Entry: {0} Type: {1}) for data type CRITERIA_DATA_TYPE_MAP_PLAYER_COUNT ({2}) has wrong max players count in value1 ({3}), ignored.",
                                 criteria.Id,
                                 criteria.Entry.Type,
                                 criteriaData.DataType,
                                 criteriaData.MapPlayers.MaxCount);

                return false;

            case CriteriaDataType.TTeam:
                if (criteriaData.TeamId.Team is (int)TeamFaction.Alliance or (int)TeamFaction.Horde)
                    return true;

                Log.Logger.Error("Table `criteria_data` (Entry: {0} Type: {1}) for data type CRITERIA_DATA_TYPE_T_TEAM ({2}) has unknown team in value1 ({3}), ignored.",
                                 criteria.Id,
                                 criteria.Entry.Type,
                                 criteriaData.DataType,
                                 criteriaData.TeamId.Team);

                return false;

            case CriteriaDataType.SDrunk:
                if (criteriaData.Drunk.State < 4)
                    return true;

                Log.Logger.Error("Table `criteria_data` (Entry: {0} Type: {1}) for data type CRITERIA_DATA_TYPE_S_DRUNK ({2}) has unknown drunken state in value1 ({3}), ignored.",
                                 criteria.Id,
                                 criteria.Entry.Type,
                                 criteriaData.DataType,
                                 criteriaData.Drunk.State);

                return false;

            case CriteriaDataType.Holiday:
                if (_cliDB.HolidaysStorage.ContainsKey(criteriaData.Holiday.Id))
                    return true;

                Log.Logger.Error("Table `criteria_data`(Entry: {0} Type: {1}) for data type CRITERIA_DATA_TYPE_HOLIDAY ({2}) has unknown holiday in value1 ({3}), ignored.",
                                 criteria.Id,
                                 criteria.Entry.Type,
                                 criteriaData.DataType,
                                 criteriaData.Holiday.Id);

                return false;

            case CriteriaDataType.GameEvent:
            {
                var events = _gameEventManager.GetEventMap();

                if (criteriaData.GameEvent.Id >= 1 && criteriaData.GameEvent.Id < events.Length)
                    return true;

                Log.Logger.Error("Table `criteria_data` (Entry: {0} Type: {1}) for data type CRITERIA_DATA_TYPE_GAME_EVENT ({2}) has unknown game_event in value1 ({3}), ignored.",
                                 criteria.Id,
                                 criteria.Entry.Type,
                                 criteriaData.DataType,
                                 criteriaData.GameEvent.Id);

                return false;
            }
            case CriteriaDataType.BgLossTeamScore:
                return true; // not check correctness node indexes
            case CriteriaDataType.SEquippedItem:
                if (criteriaData.EquippedItem.ItemQuality < (uint)ItemQuality.Max)
                    return true;

                Log.Logger.Error("Table `achievement_criteria_requirement` (Entry: {0} Type: {1}) for requirement ACHIEVEMENT_CRITERIA_REQUIRE_S_EQUIPED_ITEM ({2}) has unknown quality state in value1 ({3}), ignored.",
                                 criteria.Id,
                                 criteria.Entry.Type,
                                 criteriaData.DataType,
                                 criteriaData.EquippedItem.ItemQuality);

                return false;

            case CriteriaDataType.MapId:
                if (_cliDB.MapStorage.ContainsKey(criteriaData.MapId.Id))
                    return true;

                Log.Logger.Error("Table `criteria_data` (Entry: {0} Type: {1}) for data type CRITERIA_DATA_TYPE_MAP_ID ({2}) contains an unknown map entry in value1 ({3}), ignored.",
                                 criteria.Id,
                                 criteria.Entry.Type,
                                 criteriaData.DataType,
                                 criteriaData.MapId.Id);

                return false;

            case CriteriaDataType.SPlayerClassRace:
                if (criteriaData.ClassRace is { ClassId: 0, RaceId: 0 })
                {
                    Log.Logger.Error("Table `criteria_data` (Entry: {0} Type: {1}) for data type CRITERIA_DATA_TYPE_S_PLAYER_CLASS_RACE ({2}) must not have 0 in either value field, ignored.",
                                     criteria.Id,
                                     criteria.Entry.Type,
                                     criteriaData.DataType);

                    return false;
                }

                if (criteriaData.ClassRace.ClassId != 0 && ((1 << (int)(criteriaData.ClassRace.ClassId - 1)) & (int)PlayerClass.ClassMaskAllPlayable) == 0)
                {
                    Log.Logger.Error("Table `criteria_data` (Entry: {0} Type: {1}) for data type CRITERIA_DATA_TYPE_S_PLAYER_CLASS_RACE ({2}) has non-existing class in value1 ({3}), ignored.",
                                     criteria.Id,
                                     criteria.Entry.Type,
                                     criteriaData.DataType,
                                     criteriaData.ClassRace.ClassId);

                    return false;
                }

                if (criteriaData.ClassRace.RaceId == 0 || ((ulong)SharedConst.GetMaskForRace((Race)criteriaData.ClassRace.RaceId) & SharedConst.RaceMaskAllPlayable) != 0)
                    return true;

                Log.Logger.Error("Table `criteria_data` (Entry: {0} Type: {1}) for data type CRITERIA_DATA_TYPE_S_PLAYER_CLASS_RACE ({2}) has non-existing race in value2 ({3}), ignored.",
                                 criteria.Id,
                                 criteria.Entry.Type,
                                 criteriaData.DataType,
                                 criteriaData.ClassRace.RaceId);

                return false;

            case CriteriaDataType.SKnownTitle:
                if (_cliDB.CharTitlesStorage.ContainsKey(criteriaData.KnownTitle.Id))
                    return true;

                Log.Logger.Error("Table `criteria_data` (Entry: {0} Type: {1}) for data type CRITERIA_DATA_TYPE_S_KNOWN_TITLE ({2}) contains an unknown title_id in value1 ({3}), ignore.",
                                 criteria.Id,
                                 criteria.Entry.Type,
                                 criteriaData.DataType,
                                 criteriaData.KnownTitle.Id);

                return false;

            case CriteriaDataType.SItemQuality:
                if (criteriaData.itemQuality.Quality < (uint)ItemQuality.Max)
                    return true;

                Log.Logger.Error("Table `criteria_data` (Entry: {0} Type: {1}) for data type CRITERIA_DATA_TYPE_S_ITEM_QUALITY ({2}) contains an unknown quality state value in value1 ({3}), ignored.",
                                 criteria.Id,
                                 criteria.Entry.Type,
                                 criteriaData.DataType,
                                 criteriaData.itemQuality.Quality);

                return false;

            default:
                Log.Logger.Error("Table `criteria_data` (Entry: {0} Type: {1}) contains data of a non-supported data type ({2}), ignored.", criteria.Id, criteria.Entry.Type, criteriaData.DataType);

                return false;
        }
    }

    public bool Meets(CriteriaData criteriaData, uint criteriaId, Player source, WorldObject target, uint miscValue1 = 0, uint miscValue2 = 0)
    {
        switch (criteriaData.DataType)
        {
            case CriteriaDataType.None:
                return true;

            case CriteriaDataType.TCreature:
                if (target == null || !target.IsTypeId(TypeId.Unit))
                    return false;

                return target.Entry == criteriaData.Creature.Id;

            case CriteriaDataType.TPlayerClassRace:
                if (target == null || !target.IsTypeId(TypeId.Player))
                    return false;

                if (criteriaData.ClassRace.ClassId != 0 && criteriaData.ClassRace.ClassId != (uint)target.AsPlayer.Class)
                    return false;

                if (criteriaData.ClassRace.RaceId != 0 && criteriaData.ClassRace.RaceId != (uint)target.AsPlayer.Race)
                    return false;

                return true;

            case CriteriaDataType.SPlayerClassRace:
                if (source == null || !source.IsTypeId(TypeId.Player))
                    return false;

                if (criteriaData.ClassRace.ClassId != 0 && criteriaData.ClassRace.ClassId != (uint)source.AsPlayer.Class)
                    return false;

                if (criteriaData.ClassRace.RaceId != 0 && criteriaData.ClassRace.RaceId != (uint)source.AsPlayer.Race)
                    return false;

                return true;

            case CriteriaDataType.TPlayerLessHealth:
                if (target == null || !target.IsTypeId(TypeId.Player))
                    return false;

                return !target.AsPlayer.HealthAbovePct((int)criteriaData.Health.Percent);

            case CriteriaDataType.SAura:
                return source.HasAuraEffect(criteriaData.Aura.SpellId, (byte)criteriaData.Aura.EffectIndex);

            case CriteriaDataType.TAura:
            {
                var unitTarget = target?.AsUnit;

                if (unitTarget == null)
                    return false;

                return unitTarget.HasAuraEffect(criteriaData.Aura.SpellId, criteriaData.Aura.EffectIndex);
            }
            case CriteriaDataType.Value:
                return MathFunctions.CompareValues((ComparisionType)criteriaData.Value.ComparisonType, miscValue1, criteriaData.Value.Value);

            case CriteriaDataType.TLevel:
                if (target == null)
                    return false;

                return target.GetLevelForTarget(source) >= criteriaData.Level.Min;

            case CriteriaDataType.TGender:
            {
                var unitTarget = target?.AsUnit;

                if (unitTarget == null)
                    return false;

                return unitTarget.Gender == (Gender)criteriaData.Gender.Gender;
            }
            case CriteriaDataType.Script:
            {
                Unit unitTarget = null;

                if (target != null)
                    unitTarget = target.AsUnit;

                return _scriptManager.RunScriptRet<IAchievementCriteriaOnCheck>(p => p.OnCheck(source.AsPlayer, unitTarget?.AsUnit), criteriaData.ScriptId);
            }
            case CriteriaDataType.MapPlayerCount:
                return source.Location.Map.PlayersCountExceptGMs <= criteriaData.MapPlayers.MaxCount;

            case CriteriaDataType.TTeam:
                if (target == null || !target.IsTypeId(TypeId.Player))
                    return false;

                return (uint)target.AsPlayer.Team == criteriaData.TeamId.Team;

            case CriteriaDataType.SDrunk:
                return _playerComputators.GetDrunkenstateByValue(source.DrunkValue) >= (DrunkenState)criteriaData.Drunk.State;

            case CriteriaDataType.Holiday:
                return _gameEventManager.IsHolidayActive((HolidayIds)criteriaData.Holiday.Id);

            case CriteriaDataType.GameEvent:
                return _gameEventManager.IsEventActive((ushort)criteriaData.GameEvent.Id);

            case CriteriaDataType.BgLossTeamScore:
            {
                var bg = source.Battleground;

                if (bg == null)
                    return false;

                var score = (int)bg.GetTeamScore(bg.GetPlayerTeam(source.GUID) == TeamFaction.Alliance ? TeamIds.Horde : TeamIds.Alliance);

                return score >= criteriaData.BattlegroundScore.Min && score <= criteriaData.BattlegroundScore.Max;
            }
            case CriteriaDataType.InstanceScript:
            {
                if (!source.Location.IsInWorld)
                    return false;

                var map = source.Location.Map;

                if (!map.IsDungeon)
                {
                    Log.Logger.Error("Achievement system call AchievementCriteriaDataType.InstanceScript ({0}) for achievement criteria {1} for non-dungeon/non-raid map {2}",
                                     CriteriaDataType.InstanceScript,
                                     criteriaId,
                                     map.Id);

                    return false;
                }

                var instance = ((InstanceMap)map).InstanceScript;

                if (instance == null)
                {
                    Log.Logger.Error("Achievement system call criteria_data_INSTANCE_SCRIPT ({0}) for achievement criteria {1} for map {2} but map does not have a instance script",
                                     CriteriaDataType.InstanceScript,
                                     criteriaId,
                                     map.Id);

                    return false;
                }

                Unit unitTarget = null;

                if (target != null)
                    unitTarget = target.AsUnit;

                return instance.CheckAchievementCriteriaMeet(criteriaId, source, unitTarget, miscValue1);
            }
            case CriteriaDataType.SEquippedItem:
            {
                var entry = _criteriaManager.GetCriteria(criteriaId);

                var itemId = entry.Entry.Type == CriteriaType.EquipItemInSlot ? miscValue2 : miscValue1;
                var itemTemplate = _itemTemplateCache.GetItemTemplate(itemId);

                if (itemTemplate == null)
                    return false;

                return itemTemplate.BaseItemLevel >= criteriaData.EquippedItem.ItemLevel && (uint)itemTemplate.Quality >= criteriaData.EquippedItem.ItemQuality;
            }
            case CriteriaDataType.MapId:
                return source.Location.MapId == criteriaData.MapId.Id;

            case CriteriaDataType.SKnownTitle:
            {
                if (_cliDB.CharTitlesStorage.TryGetValue(criteriaData.KnownTitle.Id, out var titleInfo))
                    return source != null && source.HasTitle(titleInfo.MaskID);

                return false;
            }
            case CriteriaDataType.SItemQuality:
            {
                var pProto = _itemTemplateCache.GetItemTemplate(miscValue1);

                if (pProto == null)
                    return false;

                return (uint)pProto.Quality == criteriaData.itemQuality.Quality;
            }
        }

        return false;
    }
}