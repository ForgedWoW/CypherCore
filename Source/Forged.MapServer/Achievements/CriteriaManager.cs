// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Autofac;
using Forged.MapServer.DataStorage;
using Forged.MapServer.DataStorage.Structs.A;
using Forged.MapServer.DataStorage.Structs.C;
using Forged.MapServer.DataStorage.Structs.S;
using Forged.MapServer.Globals;
using Forged.MapServer.Quest;
using Framework.Constants;
using Framework.Database;
using Game.Common;
using Serilog;

namespace Forged.MapServer.Achievements;

public class CriteriaManager
{
    private readonly ClassFactory _classFactory;
    private readonly CliDB _cliDB;
    private readonly Dictionary<uint, Criteria> _criteria = new();
    private readonly Dictionary<uint, CriteriaDataSet> _criteriaDataMap = new();
    private readonly Dictionary<uint, ModifierTreeNode> _criteriaModifiers = new();
    private readonly MultiMap<uint, Criteria>[] _criteriasByAsset = new MultiMap<uint, Criteria>[(int)CriteriaType.Count];
    private readonly MultiMap<int, Criteria>[] _criteriasByFailEvent = new MultiMap<int, Criteria>[(int)CriteriaFailEvent.Max];
    private readonly MultiMap<CriteriaStartEvent, Criteria> _criteriasByTimedType = new();

    // store criterias by type to speed up lookup
    private readonly MultiMap<CriteriaType, Criteria> _criteriasByType = new();

    private readonly MultiMap<uint, CriteriaTree> _criteriaTreeByCriteria = new();
    private readonly Dictionary<uint, CriteriaTree> _criteriaTrees = new();
    private readonly GameObjectManager _gameObjectManager;
    private readonly MultiMap<CriteriaType, Criteria> _guildCriteriasByType = new();
    private readonly MultiMap<CriteriaType, Criteria> _questObjectiveCriteriasByType = new();
    private readonly MultiMap<uint, Criteria>[] _scenarioCriteriasByTypeAndScenarioId = new MultiMap<uint, Criteria>[(int)CriteriaType.Count];
    private readonly WorldDatabase _worldDatabase;

    public CriteriaManager(CliDB cliDB, WorldDatabase worldDatabase, GameObjectManager gameObjectManager, ClassFactory classFactory)
    {
        _cliDB = cliDB;
        _worldDatabase = worldDatabase;
        _gameObjectManager = gameObjectManager;
        _classFactory = classFactory;

        for (var i = 0; i < (int)CriteriaType.Count; ++i)
        {
            _criteriasByAsset[i] = new MultiMap<uint, Criteria>();
            _scenarioCriteriasByTypeAndScenarioId[i] = new MultiMap<uint, Criteria>();
        }
    }

    public static bool IsGroupCriteriaType(CriteriaType type)
    {
        return type switch
        {
            CriteriaType.KillCreature    => true,
            CriteriaType.WinBattleground => true,
            CriteriaType.BeSpellTarget => // NYI
                true,
            CriteriaType.WinAnyRankedArena => true,
            CriteriaType.GainAura => // NYI
                true,
            CriteriaType.WinAnyBattleground => // NYI
                true,
            _ => false
        };
    }

    public static void WalkCriteriaTree(CriteriaTree tree, Action<CriteriaTree> func)
    {
        foreach (var node in tree.Children)
            WalkCriteriaTree(node, func);

        func(tree);
    }

    public Criteria GetCriteria(uint criteriaId)
    {
        return _criteria.LookupByKey(criteriaId);
    }

    public List<Criteria> GetCriteriaByFailEvent(CriteriaFailEvent failEvent, int asset)
    {
        return _criteriasByFailEvent[(int)failEvent].LookupByKey(asset);
    }

    public CriteriaDataSet GetCriteriaDataSet(Criteria criteria)
    {
        return _criteriaDataMap.LookupByKey(criteria.Id);
    }

    public CriteriaTree GetCriteriaTree(uint criteriaTreeId)
    {
        return _criteriaTrees.LookupByKey(criteriaTreeId);
    }

    public List<CriteriaTree> GetCriteriaTreesByCriteria(uint criteriaId)
    {
        return _criteriaTreeByCriteria.LookupByKey(criteriaId);
    }

    public List<Criteria> GetGuildCriteriaByType(CriteriaType type)
    {
        return _guildCriteriasByType.LookupByKey(type);
    }

    public ModifierTreeNode GetModifierTree(uint modifierTreeId)
    {
        return _criteriaModifiers.LookupByKey(modifierTreeId);
    }

    public List<Criteria> GetPlayerCriteriaByType(CriteriaType type, uint asset)
    {
        if (asset != 0 && IsCriteriaTypeStoredByAsset(type))
        {
            if (_criteriasByAsset[(int)type].ContainsKey(asset))
                return _criteriasByAsset[(int)type][asset];

            return new List<Criteria>();
        }

        return _criteriasByType.LookupByKey(type);
    }

    public List<Criteria> GetQuestObjectiveCriteriaByType(CriteriaType type)
    {
        return _questObjectiveCriteriasByType[type];
    }

    public List<Criteria> GetScenarioCriteriaByTypeAndScenario(CriteriaType type, uint scenarioId)
    {
        return _scenarioCriteriasByTypeAndScenarioId[(int)type].LookupByKey(scenarioId);
    }

    public List<Criteria> GetTimedCriteriaByType(CriteriaStartEvent startEvent)
    {
        return _criteriasByTimedType.LookupByKey(startEvent);
    }

    public void LoadCriteriaData()
    {
        var oldMSTime = Time.MSTime;

        _criteriaDataMap.Clear(); // need for reload case

        var result = _worldDatabase.Query("SELECT criteria_id, type, value1, value2, ScriptName FROM criteria_data");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 additional criteria data. DB table `criteria_data` is empty.");

            return;
        }

        uint count = 0;

        do
        {
            var criteriaID = result.Read<uint>(0);

            var criteria = GetCriteria(criteriaID);

            if (criteria == null)
            {
                Log.Logger.Error("Table `criteria_data` contains data for non-existing criteria (Entry: {0}). Ignored.", criteriaID);

                continue;
            }

            var dataType = (CriteriaDataType)result.Read<byte>(1);
            var scriptName = result.Read<string>(4);
            uint scriptId = 0;

            if (!scriptName.IsEmpty())
            {
                if (dataType != CriteriaDataType.Script)
                    Log.Logger.Error("Table `criteria_data` contains a ScriptName for non-scripted data type (Entry: {0}, type {1}), useless data.", criteriaID, dataType);
                else
                    scriptId = _gameObjectManager.GetScriptId(scriptName);
            }

            var data = _classFactory.Resolve<CriteriaData>(new PositionalParameter(0, dataType),
                                                                    new PositionalParameter(1, result.Read<uint>(2)),
                                                                    new PositionalParameter(2, result.Read<uint>(3)),
                                                                    new PositionalParameter(3, scriptId));

            if (!data.IsValid(criteria))
                continue;

            // this will allocate empty data set storage
            CriteriaDataSet dataSet = new();
            dataSet.SetCriteriaId(criteriaID);

            // add real data only for not NONE data types
            if (data.DataType != CriteriaDataType.None)
                dataSet.Add(data);

            _criteriaDataMap[criteriaID] = dataSet;
            // counting data by and data types
            ++count;
        } while (result.NextRow());

        Log.Logger.Information("Loaded {0} additional criteria data in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
    }

    public void LoadCriteriaList()
    {
        var oldMSTime = Time.MSTime;

        Dictionary<uint /*criteriaTreeID*/, AchievementRecord> achievementCriteriaTreeIds = new();

        foreach (var achievement in _cliDB.AchievementStorage.Values)
            if (achievement.CriteriaTree != 0)
                achievementCriteriaTreeIds[achievement.CriteriaTree] = achievement;

        Dictionary<uint, ScenarioStepRecord> scenarioCriteriaTreeIds = new();

        foreach (var scenarioStep in _cliDB.ScenarioStepStorage.Values)
            if (scenarioStep.CriteriaTreeId != 0)
                scenarioCriteriaTreeIds[scenarioStep.CriteriaTreeId] = scenarioStep;

        Dictionary<uint /*criteriaTreeID*/, QuestObjective> questObjectiveCriteriaTreeIds = new();

        foreach (var pair in _gameObjectManager.GetQuestTemplates())
        {
            foreach (var objective in pair.Value.Objectives)
            {
                if (objective.Type != QuestObjectiveType.CriteriaTree)
                    continue;

                if (objective.ObjectID != 0)
                    questObjectiveCriteriaTreeIds[(uint)objective.ObjectID] = objective;
            }
        }

        // Load criteria tree nodes
        foreach (var tree in _cliDB.CriteriaTreeStorage.Values)
        {
            // Find linked achievement
            var achievement = GetEntry(achievementCriteriaTreeIds, tree);
            var scenarioStep = GetEntry(scenarioCriteriaTreeIds, tree);
            var questObjective = GetEntry(questObjectiveCriteriaTreeIds, tree);

            if (achievement == null && scenarioStep == null && questObjective == null)
                continue;

            CriteriaTree criteriaTree = new()
            {
                Id = tree.Id,
                Achievement = achievement,
                ScenarioStep = scenarioStep,
                QuestObjective = questObjective,
                Entry = tree
            };

            _criteriaTrees[criteriaTree.Entry.Id] = criteriaTree;
        }

        // Build tree
        foreach (var pair in _criteriaTrees)
        {
            var parent = _criteriaTrees.LookupByKey(pair.Value.Entry.Parent);

            parent?.Children.Add(pair.Value);

            if (_cliDB.CriteriaStorage.HasRecord(pair.Value.Entry.CriteriaID))
                _criteriaTreeByCriteria.Add(pair.Value.Entry.CriteriaID, pair.Value);
        }

        for (var i = 0; i < (int)CriteriaFailEvent.Max; ++i)
            _criteriasByFailEvent[i] = new MultiMap<int, Criteria>();

        // Load criteria
        uint criterias = 0;
        uint guildCriterias = 0;
        uint scenarioCriterias = 0;
        uint questObjectiveCriterias = 0;

        foreach (var criteriaEntry in _cliDB.CriteriaStorage.Values)
        {
            if (_criteriaTreeByCriteria.TryGetValue(criteriaEntry.Id, out var treeList))
                continue;

            Criteria criteria = new()
            {
                Id = criteriaEntry.Id,
                Entry = criteriaEntry,
                Modifier = _criteriaModifiers.LookupByKey(criteriaEntry.ModifierTreeId)
            };

            _criteria[criteria.Id] = criteria;

            List<uint> scenarioIds = new();

            foreach (var tree in treeList)
            {
                tree.Criteria = criteria;

                var achievement = tree.Achievement;

                if (achievement != null)
                {
                    if (achievement.Flags.HasAnyFlag(AchievementFlags.Guild))
                        criteria.FlagsCu |= CriteriaFlagsCu.Guild;
                    else if (achievement.Flags.HasAnyFlag(AchievementFlags.Account))
                        criteria.FlagsCu |= CriteriaFlagsCu.Account;
                    else
                        criteria.FlagsCu |= CriteriaFlagsCu.Player;
                }
                else if (tree.ScenarioStep != null)
                {
                    criteria.FlagsCu |= CriteriaFlagsCu.Scenario;
                    scenarioIds.Add(tree.ScenarioStep.ScenarioID);
                }
                else if (tree.QuestObjective != null)
                {
                    criteria.FlagsCu |= CriteriaFlagsCu.QuestObjective;
                }
            }

            if (criteria.FlagsCu.HasAnyFlag(CriteriaFlagsCu.Player | CriteriaFlagsCu.Account))
            {
                ++criterias;
                _criteriasByType.Add(criteriaEntry.Type, criteria);

                if (IsCriteriaTypeStoredByAsset(criteriaEntry.Type))
                {
                    if (criteriaEntry.Type != CriteriaType.RevealWorldMapOverlay)
                    {
                        _criteriasByAsset[(int)criteriaEntry.Type].Add(criteriaEntry.Asset, criteria);
                    }
                    else
                    {
                        if (!_cliDB.WorldMapOverlayStorage.TryGetValue(criteriaEntry.Asset, out var worldOverlayEntry))
                            break;

                        for (byte j = 0; j < SharedConst.MaxWorldMapOverlayArea; ++j)
                            if (worldOverlayEntry.AreaID[j] != 0)
                            {
                                var valid = true;

                                for (byte i = 0; i < j; ++i)
                                    if (worldOverlayEntry.AreaID[j] == worldOverlayEntry.AreaID[i])
                                        valid = false;

                                if (valid)
                                    _criteriasByAsset[(int)criteriaEntry.Type].Add(worldOverlayEntry.AreaID[j], criteria);
                            }
                    }
                }
            }

            if (criteria.FlagsCu.HasAnyFlag(CriteriaFlagsCu.Guild))
            {
                ++guildCriterias;
                _guildCriteriasByType.Add(criteriaEntry.Type, criteria);
            }

            if (criteria.FlagsCu.HasAnyFlag(CriteriaFlagsCu.Scenario))
            {
                ++scenarioCriterias;

                foreach (var scenarioId in scenarioIds)
                    _scenarioCriteriasByTypeAndScenarioId[(int)criteriaEntry.Type].Add(scenarioId, criteria);
            }

            if (criteria.FlagsCu.HasAnyFlag(CriteriaFlagsCu.QuestObjective))
            {
                ++questObjectiveCriterias;
                _questObjectiveCriteriasByType.Add(criteriaEntry.Type, criteria);
            }

            if (criteriaEntry.StartTimer != 0)
                _criteriasByTimedType.Add((CriteriaStartEvent)criteriaEntry.StartEvent, criteria);

            if (criteriaEntry.FailEvent != 0)
                _criteriasByFailEvent[criteriaEntry.FailEvent].Add((int)criteriaEntry.FailAsset, criteria);
        }

        Log.Logger.Information($"Loaded {criterias} criteria, {guildCriterias} guild criteria, {scenarioCriterias} scenario criteria and {questObjectiveCriterias} quest objective criteria in {Time.GetMSTimeDiffToNow(oldMSTime)} ms.");
    }

    public void LoadCriteriaModifiersTree()
    {
        var oldMSTime = Time.MSTime;

        if (_cliDB.ModifierTreeStorage.Empty())
        {
            Log.Logger.Information("Loaded 0 criteria modifiers.");

            return;
        }

        // Load modifier tree nodes
        foreach (var tree in _cliDB.ModifierTreeStorage.Values)
        {
            ModifierTreeNode node = new()
            {
                Entry = tree
            };

            _criteriaModifiers[node.Entry.Id] = node;
        }

        // Build tree
        foreach (var treeNode in _criteriaModifiers.Values)
        {
            var parentNode = _criteriaModifiers.LookupByKey(treeNode.Entry.Parent);

            parentNode?.Children.Add(treeNode);
        }

        Log.Logger.Information("Loaded {0} criteria modifiers in {1} ms", _criteriaModifiers.Count, Time.GetMSTimeDiffToNow(oldMSTime));
    }

    private T GetEntry<T>(Dictionary<uint, T> map, CriteriaTreeRecord tree) where T : new()
    {
        var cur = tree;
        var obj = map.LookupByKey(tree.Id);

        while (obj == null)
        {
            if (cur.Parent == 0)
                break;

            cur = _cliDB.CriteriaTreeStorage.LookupByKey(cur.Parent);

            if (cur == null)
                break;

            obj = map.LookupByKey(cur.Id);
        }

        if (obj == null)
            return default;

        return obj;
    }

    private bool IsCriteriaTypeStoredByAsset(CriteriaType type)
    {
        return type switch
        {
            CriteriaType.KillCreature                => true,
            CriteriaType.WinBattleground             => true,
            CriteriaType.SkillRaised                 => true,
            CriteriaType.EarnAchievement             => true,
            CriteriaType.CompleteQuestsInZone        => true,
            CriteriaType.ParticipateInBattleground   => true,
            CriteriaType.KilledByCreature            => true,
            CriteriaType.CompleteQuest               => true,
            CriteriaType.BeSpellTarget               => true,
            CriteriaType.CastSpell                   => true,
            CriteriaType.TrackedWorldStateUIModified => true,
            CriteriaType.PVPKillInArea               => true,
            CriteriaType.LearnOrKnowSpell            => true,
            CriteriaType.AcquireItem                 => true,
            CriteriaType.AchieveSkillStep            => true,
            CriteriaType.UseItem                     => true,
            CriteriaType.LootItem                    => true,
            CriteriaType.RevealWorldMapOverlay       => true,
            CriteriaType.ReputationGained            => true,
            CriteriaType.EquipItemInSlot             => true,
            CriteriaType.DeliverKillingBlowToClass   => true,
            CriteriaType.DeliverKillingBlowToRace    => true,
            CriteriaType.DoEmote                     => true,
            CriteriaType.EquipItem                   => true,
            CriteriaType.UseGameobject               => true,
            CriteriaType.GainAura                    => true,
            CriteriaType.CatchFishInFishingHole      => true,
            CriteriaType.LearnSpellFromSkillLine     => true,
            CriteriaType.GetLootByType               => true,
            CriteriaType.LandTargetedSpellOnTarget   => true,
            CriteriaType.LearnTradeskillSkillLine    => true,
            _                                        => false
        };
    }
}