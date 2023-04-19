// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Text;
using Forged.MapServer.Achievements;
using Forged.MapServer.DataStorage;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Events;
using Forged.MapServer.Globals;
using Forged.MapServer.Maps;
using Forged.MapServer.Scenarios;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ICondition;
using Forged.MapServer.World;
using Framework.Constants;
using Serilog;

namespace Forged.MapServer.Conditions;

public class Condition
{
    private readonly AchievementGlobalMgr _achievementGlobalMgr;
    private readonly CliDB _cliDB;
    private readonly ConditionManager _conditionManager;
    private readonly GameEventManager _gameEventManager;
    private readonly GameObjectManager _objectManager;
    private readonly PlayerComputators _playerComputators;
    private readonly ScriptManager _scriptManager;
    private readonly WorldStateManager _worldStateManager;

    // So far, only used in CONDITION_SOURCE_TYPE_SMART_EVENT
    public Condition(ConditionManager conditionManager, ScriptManager scriptManager, CliDB cliDB, GameObjectManager objectManager, PlayerComputators playerComputators,
                     GameEventManager gameEventManager, WorldStateManager worldStateManager, AchievementGlobalMgr achievementGlobalMgr)
    {
        _conditionManager = conditionManager;
        _scriptManager = scriptManager;
        _cliDB = cliDB;
        _objectManager = objectManager;
        _playerComputators = playerComputators;
        _gameEventManager = gameEventManager;
        _worldStateManager = worldStateManager;
        _achievementGlobalMgr = achievementGlobalMgr;
        SourceType = ConditionSourceType.None;
        ConditionType = ConditionTypes.None;
    }

    public byte ConditionTarget { get; set; }

    public ConditionTypes ConditionType { get; set; }

    //ConditionTypeOrReference
    public uint ConditionValue1 { get; set; }

    public uint ConditionValue2 { get; set; }
    public uint ConditionValue3 { get; set; }
    public uint ElseGroup { get; set; }
    public uint ErrorTextId { get; set; }
    public uint ErrorType { get; set; }
    public bool NegativeCondition { get; set; }
    public uint ReferenceId { get; set; }
    public uint ScriptId { get; set; }
    public int SourceEntry { get; set; }
    public uint SourceGroup { get; set; }
    public uint SourceId { get; set; }

    public ConditionSourceType SourceType { get; set; } //SourceTypeOrReferenceId

    public uint GetMaxAvailableConditionTargets()
    {
        // returns number of targets which are available for given source type
        return SourceType switch
        {
            ConditionSourceType.Spell                   => 2,
            ConditionSourceType.SpellImplicitTarget     => 2,
            ConditionSourceType.CreatureTemplateVehicle => 2,
            ConditionSourceType.VehicleSpell            => 2,
            ConditionSourceType.SpellClickEvent         => 2,
            ConditionSourceType.GossipMenu              => 2,
            ConditionSourceType.GossipMenuOption        => 2,
            ConditionSourceType.SmartEvent              => 2,
            ConditionSourceType.NpcVendor               => 2,
            ConditionSourceType.SpellProc               => 2,
            _                                           => 1
        };
    }

    public GridMapTypeMask GetSearcherTypeMaskForCondition()
    {
        // build mask of types for which condition can return true
        // this is used for speeding up gridsearches
        if (NegativeCondition)
            return GridMapTypeMask.All;

        GridMapTypeMask mask = 0;

        switch (ConditionType)
        {
            case ConditionTypes.ActiveEvent:
            case ConditionTypes.Areaid:
            case ConditionTypes.DifficultyId:
            case ConditionTypes.DistanceTo:
            case ConditionTypes.InstanceInfo:
            case ConditionTypes.Mapid:
            case ConditionTypes.NearCreature:
            case ConditionTypes.NearGameobject:
            case ConditionTypes.None:
            case ConditionTypes.PhaseId:
            case ConditionTypes.RealmAchievement:
            case ConditionTypes.TerrainSwap:
            case ConditionTypes.WorldState:
            case ConditionTypes.Zoneid:
                mask |= GridMapTypeMask.All;

                break;
            case ConditionTypes.Gender:
            case ConditionTypes.Title:
            case ConditionTypes.DrunkenState:
            case ConditionTypes.Spell:
            case ConditionTypes.QuestTaken:
            case ConditionTypes.QuestComplete:
            case ConditionTypes.QuestNone:
            case ConditionTypes.Skill:
            case ConditionTypes.QuestRewarded:
            case ConditionTypes.ReputationRank:
            case ConditionTypes.Achievement:
            case ConditionTypes.Team:
            case ConditionTypes.Item:
            case ConditionTypes.ItemEquipped:
            case ConditionTypes.PetType:
            case ConditionTypes.Taxi:
            case ConditionTypes.Queststate:
            case ConditionTypes.Gamemaster:
                mask |= GridMapTypeMask.Player;

                break;
            case ConditionTypes.UnitState:
            case ConditionTypes.Alive:
            case ConditionTypes.HpVal:
            case ConditionTypes.HpPct:
            case ConditionTypes.RelationTo:
            case ConditionTypes.ReactionTo:
            case ConditionTypes.Level:
            case ConditionTypes.Class:
            case ConditionTypes.Race:
            case ConditionTypes.Aura:
            case ConditionTypes.InWater:
            case ConditionTypes.StandState:
                mask |= GridMapTypeMask.Creature | GridMapTypeMask.Player;

                break;
            case ConditionTypes.ObjectEntryGuid:
                switch ((TypeId)ConditionValue1)
                {
                    case TypeId.Unit:
                        mask |= GridMapTypeMask.Creature;

                        break;
                    case TypeId.Player:
                        mask |= GridMapTypeMask.Player;

                        break;
                    case TypeId.GameObject:
                        mask |= GridMapTypeMask.GameObject;

                        break;
                    case TypeId.Corpse:
                        mask |= GridMapTypeMask.Corpse;

                        break;
                    case TypeId.AreaTrigger:
                        mask |= GridMapTypeMask.AreaTrigger;

                        break;
                }

                break;
            case ConditionTypes.TypeMask:
                if (Convert.ToBoolean((TypeMask)ConditionValue1 & TypeMask.Unit))
                    mask |= GridMapTypeMask.Creature | GridMapTypeMask.Player;

                if (Convert.ToBoolean((TypeMask)ConditionValue1 & TypeMask.Player))
                    mask |= GridMapTypeMask.Player;

                if (Convert.ToBoolean((TypeMask)ConditionValue1 & TypeMask.GameObject))
                    mask |= GridMapTypeMask.GameObject;

                if (Convert.ToBoolean((TypeMask)ConditionValue1 & TypeMask.Corpse))
                    mask |= GridMapTypeMask.Corpse;

                if (Convert.ToBoolean((TypeMask)ConditionValue1 & TypeMask.AreaTrigger))
                    mask |= GridMapTypeMask.AreaTrigger;

                break;
            case ConditionTypes.DailyQuestDone:
            case ConditionTypes.ObjectiveProgress:
            case ConditionTypes.BattlePetCount:
                mask |= GridMapTypeMask.Player;

                break;
            case ConditionTypes.ScenarioStep:
                mask |= GridMapTypeMask.All;

                break;
            case ConditionTypes.SceneInProgress:
                mask |= GridMapTypeMask.Player;

                break;
            case ConditionTypes.PlayerCondition:
                mask |= GridMapTypeMask.Player;

                break;
        }

        return mask;
    }

    public bool IsLoaded()
    {
        return ConditionType > ConditionTypes.None || ReferenceId != 0 || ScriptId != 0;
    }

    public bool Meets(ConditionSourceInfo sourceInfo)
    {
        var map = sourceInfo.ConditionMap;
        var condMeets = false;
        var needsObject = false;

        switch (ConditionType)
        {
            case ConditionTypes.None:
                condMeets = true; // empty condition, always met

                break;
            case ConditionTypes.ActiveEvent:
                condMeets = _gameEventManager.IsActiveEvent((ushort)ConditionValue1);

                break;
            case ConditionTypes.InstanceInfo:
            {
                if (map.IsDungeon)
                {
                    var instance = ((InstanceMap)map).InstanceScript;

                    if (instance != null)
                        condMeets = (InstanceInfo)ConditionValue3 switch
                        {
                            InstanceInfo.Data => instance.GetData(ConditionValue1) == ConditionValue2,
                            //case INSTANCE_INFO_GUID_DATA:
                            //    condMeets = instance->GetGuidData(ConditionValue1) == ObjectGuid(uint64(ConditionValue2));
                            //    break;
                            InstanceInfo.BossState => instance.GetBossState(ConditionValue1) == (EncounterState)ConditionValue2,
                            InstanceInfo.Data64    => instance.GetData64(ConditionValue1) == ConditionValue2,
                            _                      => false
                        };
                }

                break;
            }
            case ConditionTypes.Mapid:
                condMeets = map.Id == ConditionValue1;

                break;
            case ConditionTypes.WorldState:
            {
                condMeets = _worldStateManager.GetValue((int)ConditionValue1, map) == ConditionValue2;

                break;
            }
            case ConditionTypes.RealmAchievement:
            {
                var achievement = _cliDB.AchievementStorage.LookupByKey(ConditionValue1);

                if (achievement != null && _achievementGlobalMgr.IsRealmCompleted(achievement))
                    condMeets = true;

                break;
            }
            case ConditionTypes.DifficultyId:
            {
                condMeets = (uint)map.DifficultyID == ConditionValue1;

                break;
            }
            case ConditionTypes.ScenarioStep:
            {
                var instanceMap = map.ToInstanceMap;

                Scenario scenario = instanceMap?.InstanceScenario;

                var step = scenario?.GetStep();

                if (step != null)
                    condMeets = step.Id == ConditionValue1;

                break;
            }
            default:
                needsObject = true;

                break;
        }

        var obj = sourceInfo.ConditionTargets[ConditionTarget];

        // object not present, return false
        if (needsObject && obj == null)
        {
            Log.Logger.Debug("Condition object not found for condition (Entry: {0} Type: {1} Group: {2})", SourceEntry, SourceType, SourceGroup);

            return false;
        }

        var player = obj?.AsPlayer;
        var unit = obj?.AsUnit;

        switch (ConditionType)
        {
            case ConditionTypes.Aura:
                if (unit != null)
                    condMeets = unit.HasAuraEffect(ConditionValue1, (byte)ConditionValue2);

                break;
            case ConditionTypes.Item:
                if (player != null)
                {
                    var checkBank = ConditionValue3 != 0;
                    condMeets = player.HasItemCount(ConditionValue1, ConditionValue2, checkBank);
                }

                break;
            case ConditionTypes.ItemEquipped:
                if (player != null)
                    condMeets = player.HasItemOrGemWithIdEquipped(ConditionValue1, 1);

                break;
            case ConditionTypes.Zoneid:
                if (obj != null)
                    condMeets = obj.Location.Zone == ConditionValue1;

                break;
            case ConditionTypes.ReputationRank:
                if (player != null)
                    if (_cliDB.FactionStorage.TryGetValue(ConditionValue1, out var faction))
                        condMeets = Convert.ToBoolean(ConditionValue2 & (1 << (int)player.ReputationMgr.GetRank(faction)));

                break;
            case ConditionTypes.Achievement:
                if (player != null)
                    condMeets = player.HasAchieved(ConditionValue1);

                break;
            case ConditionTypes.Team:
                if (player != null)
                    condMeets = (uint)player.Team == ConditionValue1;

                break;
            case ConditionTypes.Class:
                if (unit != null)
                    condMeets = Convert.ToBoolean(unit.ClassMask & ConditionValue1);

                break;
            case ConditionTypes.Race:
                if (unit != null)
                    condMeets = Convert.ToBoolean(SharedConst.GetMaskForRace(unit.Race) & ConditionValue1);

                break;
            case ConditionTypes.Gender:
                if (player != null)
                    condMeets = player.NativeGender == (Gender)ConditionValue1;

                break;
            case ConditionTypes.Skill:
                if (player != null)
                    condMeets = player.HasSkill((SkillType)ConditionValue1) && player.GetBaseSkillValue((SkillType)ConditionValue1) >= ConditionValue2;

                break;
            case ConditionTypes.QuestRewarded:
                if (player != null)
                    condMeets = player.GetQuestRewardStatus(ConditionValue1);

                break;
            case ConditionTypes.QuestTaken:
                if (player != null)
                {
                    var status = player.GetQuestStatus(ConditionValue1);
                    condMeets = status == QuestStatus.Incomplete;
                }

                break;
            case ConditionTypes.QuestComplete:
                if (player != null)
                {
                    var status = player.GetQuestStatus(ConditionValue1);
                    condMeets = status == QuestStatus.Complete && !player.GetQuestRewardStatus(ConditionValue1);
                }

                break;
            case ConditionTypes.QuestNone:
                if (player != null)
                {
                    var status = player.GetQuestStatus(ConditionValue1);
                    condMeets = status == QuestStatus.None;
                }

                break;
            case ConditionTypes.Areaid:
                if (obj != null)
                    condMeets = obj.Location.Area == ConditionValue1;

                break;
            case ConditionTypes.Spell:
                if (player != null)
                    condMeets = player.HasSpell(ConditionValue1);

                break;
            case ConditionTypes.Level:
                if (unit != null)
                    condMeets = MathFunctions.CompareValues((ComparisionType)ConditionValue2, unit.Level, ConditionValue1);

                break;
            case ConditionTypes.DrunkenState:
                if (player != null)
                    condMeets = (uint)_playerComputators.GetDrunkenstateByValue(player.DrunkValue) >= ConditionValue1;

                break;
            case ConditionTypes.NearCreature:
                if (obj != null)
                    condMeets = obj.Location.FindNearestCreature(ConditionValue1, ConditionValue2, ConditionValue3 == 0) != null;

                break;
            case ConditionTypes.NearGameobject:
                if (obj != null)
                    condMeets = obj.Location.FindNearestGameObject(ConditionValue1, ConditionValue2) != null;

                break;
            case ConditionTypes.ObjectEntryGuid:
                if (obj != null && (uint)obj.TypeId == ConditionValue1)
                {
                    condMeets = ConditionValue2 == 0 || obj.Entry == ConditionValue2;

                    if (ConditionValue3 != 0)
                        switch (obj.TypeId)
                        {
                            case TypeId.Unit:
                                condMeets &= obj.AsCreature.SpawnId == ConditionValue3;

                                break;
                            case TypeId.GameObject:
                                condMeets &= obj.AsGameObject.SpawnId == ConditionValue3;

                                break;
                        }
                }

                break;
            case ConditionTypes.TypeMask:
                if (obj != null)
                    condMeets = Convert.ToBoolean((TypeMask)ConditionValue1 & obj.ObjectTypeMask);

                break;
            case ConditionTypes.RelationTo:
            {
                var toObject = sourceInfo.ConditionTargets[ConditionValue1];

                var toUnit = toObject?.AsUnit;

                if (toUnit != null && unit != null)
                    condMeets = (RelationType)ConditionValue2 switch
                    {
                        RelationType.Self          => unit == toUnit,
                        RelationType.InParty       => unit.IsInPartyWith(toUnit),
                        RelationType.InRaidOrParty => unit.IsInRaidWith(toUnit),
                        RelationType.OwnedBy       => unit.OwnerGUID == toUnit.GUID,
                        RelationType.PassengerOf   => unit.IsOnVehicle(toUnit),
                        RelationType.CreatedBy     => unit.CreatorGUID == toUnit.GUID,
                        _                          => condMeets
                    };

                break;
            }
            case ConditionTypes.ReactionTo:
            {
                var toObject = sourceInfo.ConditionTargets[ConditionValue1];

                var toUnit = toObject?.AsUnit;

                if (toUnit != null && unit != null)
                    condMeets = Convert.ToBoolean((1 << (int)unit.WorldObjectCombat.GetReactionTo(toUnit)) & ConditionValue2);

                break;
            }
            case ConditionTypes.DistanceTo:
            {
                var toObject = sourceInfo.ConditionTargets[ConditionValue1];

                if (toObject != null && obj != null)
                    condMeets = MathFunctions.CompareValues((ComparisionType)ConditionValue3, obj.Location.GetDistance(toObject), ConditionValue2);

                break;
            }
            case ConditionTypes.Alive:
                if (unit != null)
                    condMeets = unit.IsAlive;

                break;
            case ConditionTypes.HpVal:
                if (unit != null)
                    condMeets = MathFunctions.CompareValues((ComparisionType)ConditionValue2, unit.Health, ConditionValue1);

                break;
            case ConditionTypes.HpPct:
                if (unit != null)
                    condMeets = MathFunctions.CompareValues((ComparisionType)ConditionValue2, unit.HealthPct, ConditionValue1);

                break;
            case ConditionTypes.PhaseId:
                if (obj != null)
                    condMeets = obj.Location.PhaseShift.HasPhase(ConditionValue1);

                break;
            case ConditionTypes.Title:
                if (player != null)
                    condMeets = player.HasTitle(ConditionValue1);

                break;
            case ConditionTypes.UnitState:
                if (unit != null)
                    condMeets = unit.HasUnitState((UnitState)ConditionValue1);

                break;
            case ConditionTypes.CreatureType:
            {
                if (obj == null)
                    break;

                var creature = obj.AsCreature;

                if (creature != null)
                    condMeets = (uint)creature.Template.CreatureType == ConditionValue1;

                break;
            }
            case ConditionTypes.InWater:
                if (unit != null)
                    condMeets = unit.Location.IsInWater;

                break;
            case ConditionTypes.TerrainSwap:
                if (obj != null)
                    condMeets = obj.Location.PhaseShift.HasVisibleMapId(ConditionValue1);

                break;
            case ConditionTypes.StandState:
            {
                if (unit != null)
                {
                    if (ConditionValue1 == 0)
                        condMeets = unit.StandState == (UnitStandStateType)ConditionValue2;
                    else
                        condMeets = ConditionValue2 switch
                        {
                            0 => unit.IsStandState,
                            1 => unit.IsSitState,
                            _ => condMeets
                        };
                }

                break;
            }
            case ConditionTypes.DailyQuestDone:
            {
                if (player != null)
                    condMeets = player.IsDailyQuestDone(ConditionValue1);

                break;
            }
            case ConditionTypes.Charmed:
            {
                if (unit != null)
                    condMeets = unit.IsCharmed;

                break;
            }
            case ConditionTypes.PetType:
            {
                var pet = player?.CurrentPet;

                if (pet != null)
                    condMeets = ((1 << (int)pet.PetType) & ConditionValue1) != 0;

                break;
            }
            case ConditionTypes.Taxi:
            {
                if (player != null)
                    condMeets = player.IsInFlight;

                break;
            }
            case ConditionTypes.Queststate:
            {
                if (player != null)
                    if (
                        (Convert.ToBoolean(ConditionValue2 & (1 << (int)QuestStatus.None)) && player.GetQuestStatus(ConditionValue1) == QuestStatus.None) ||
                        (Convert.ToBoolean(ConditionValue2 & (1 << (int)QuestStatus.Complete)) && player.GetQuestStatus(ConditionValue1) == QuestStatus.Complete) ||
                        (Convert.ToBoolean(ConditionValue2 & (1 << (int)QuestStatus.Incomplete)) && player.GetQuestStatus(ConditionValue1) == QuestStatus.Incomplete) ||
                        (Convert.ToBoolean(ConditionValue2 & (1 << (int)QuestStatus.Failed)) && player.GetQuestStatus(ConditionValue1) == QuestStatus.Failed) ||
                        (Convert.ToBoolean(ConditionValue2 & (1 << (int)QuestStatus.Rewarded)) && player.GetQuestRewardStatus(ConditionValue1))
                    )
                        condMeets = true;

                break;
            }
            case ConditionTypes.ObjectiveProgress:
            {
                if (player != null)
                {
                    var questObj = _objectManager.GetQuestObjective(ConditionValue1);

                    if (questObj == null)
                        break;

                    var quest = _objectManager.GetQuestTemplate(questObj.QuestID);

                    if (quest == null)
                        break;

                    var slot = player.FindQuestSlot(questObj.QuestID);

                    if (slot >= SharedConst.MaxQuestLogSize)
                        break;

                    condMeets = player.GetQuestSlotObjectiveData(slot, questObj) == ConditionValue3;
                }

                break;
            }
            case ConditionTypes.Gamemaster:
            {
                if (player != null)
                    condMeets = ConditionValue1 == 1 ? player.CanBeGameMaster : player.IsGameMaster;

                break;
            }
            case ConditionTypes.BattlePetCount:
            {
                if (player != null)
                    condMeets = MathFunctions.CompareValues((ComparisionType)ConditionValue3, player.Session.BattlePetMgr.GetPetCount(_cliDB.BattlePetSpeciesStorage.LookupByKey(ConditionValue1), player.GUID), ConditionValue2);

                break;
            }
            case ConditionTypes.SceneInProgress:
            {
                if (player != null)
                    condMeets = player.SceneMgr.GetActiveSceneCount(ConditionValue1) > 0;

                break;
            }
            case ConditionTypes.PlayerCondition:
            {
                if (player != null)
                    if (_cliDB.PlayerConditionStorage.TryGetValue(ConditionValue1, out var playerCondition))
                        condMeets = _conditionManager.IsPlayerMeetingCondition(player, playerCondition);

                break;
            }
        }

        if (NegativeCondition)
            condMeets = !condMeets;

        if (!condMeets)
            sourceInfo.LastFailedCondition = this;

        return condMeets && _scriptManager.RunScriptRet<IConditionCheck>(p => p.OnConditionCheck(this, sourceInfo), ScriptId, true); // Returns true by default.;
    }

    public string ToString(bool ext = false)
    {
        StringBuilder ss = new();
        ss.Append($"[Condition SourceType: {SourceType}");

        if (SourceType < ConditionSourceType.Max)
        {
            if (_conditionManager.StaticSourceTypeData.Length > (int)SourceType)
                ss.AppendFormat(" ({0})", _conditionManager.StaticSourceTypeData[(int)SourceType]);
        }
        else
            ss.Append(" (Unknown)");

        if (_conditionManager.CanHaveSourceGroupSet(SourceType))
            ss.Append($", SourceGroup: {SourceGroup}");

        ss.Append($", SourceEntry: {SourceEntry}");

        if (_conditionManager.CanHaveSourceIdSet(SourceType))
            ss.Append($", SourceId: {SourceId}");

        if (ext)
        {
            ss.Append($", ConditionType: {ConditionType}");

            if (ConditionType < ConditionTypes.Max)
                ss.AppendFormat(" ({0})", _conditionManager.StaticConditionTypeData[(int)ConditionType].Name);
            else
                ss.Append(" (Unknown)");
        }

        ss.Append(']');

        return ss.ToString();
    }
}