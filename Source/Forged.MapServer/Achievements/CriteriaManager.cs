// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Framework.Database;
using Game;
using Game.DataStorage;
using Game.Common.DataStorage.Structs.A;
using Game.Common.DataStorage.Structs.C;
using Game.Common.DataStorage.Structs.S;

namespace Forged.MapServer.Achievements;

public class CriteriaManager : Singleton<CriteriaManager>
{
    readonly Dictionary<uint, CriteriaDataSet> _criteriaDataMap = new();
    readonly Dictionary<uint, CriteriaTree> _criteriaTrees = new();
    readonly Dictionary<uint, Criteria> _criteria = new();
    readonly Dictionary<uint, ModifierTreeNode> _criteriaModifiers = new();
    readonly MultiMap<uint, CriteriaTree> _criteriaTreeByCriteria = new();

    // store criterias by type to speed up lookup
    readonly MultiMap<CriteriaType, Criteria> _criteriasByType = new();
    readonly MultiMap<uint, Criteria>[] _criteriasByAsset = new MultiMap<uint, Criteria>[(int)CriteriaType.Count];
    readonly MultiMap<CriteriaType, Criteria> _guildCriteriasByType = new();
    readonly MultiMap<uint, Criteria>[] _scenarioCriteriasByTypeAndScenarioId = new MultiMap<uint, Criteria>[(int)CriteriaType.Count];
    readonly MultiMap<CriteriaType, Criteria> _questObjectiveCriteriasByType = new();
    readonly MultiMap<CriteriaStartEvent, Criteria> _criteriasByTimedType = new();
    readonly MultiMap<int, Criteria>[] _criteriasByFailEvent = new MultiMap<int, Criteria>[(int)CriteriaFailEvent.Max];

    CriteriaManager()
    {
        for (var i = 0; i < (int)CriteriaType.Count; ++i)
        {
            _criteriasByAsset[i] = new MultiMap<uint, Criteria>();
            _scenarioCriteriasByTypeAndScenarioId[i] = new MultiMap<uint, Criteria>();
        }
    }

    public void LoadCriteriaModifiersTree()
    {
        var oldMSTime = Time.MSTime;

        if (CliDB.ModifierTreeStorage.Empty())
        {
            Log.outInfo(LogFilter.ServerLoading, "Loaded 0 criteria modifiers.");

            return;
        }

        // Load modifier tree nodes
        foreach (var tree in CliDB.ModifierTreeStorage.Values)
        {
            ModifierTreeNode node = new();
            node.Entry = tree;
            _criteriaModifiers[node.Entry.Id] = node;
        }

        // Build tree
        foreach (var treeNode in _criteriaModifiers.Values)
        {
            var parentNode = _criteriaModifiers.LookupByKey(treeNode.Entry.Parent);

            if (parentNode != null)
                parentNode.Children.Add(treeNode);
        }

        Log.outInfo(LogFilter.ServerLoading, "Loaded {0} criteria modifiers in {1} ms", _criteriaModifiers.Count, Time.GetMSTimeDiffToNow(oldMSTime));
    }

    public void LoadCriteriaList()
    {
        var oldMSTime = Time.MSTime;

        Dictionary<uint /*criteriaTreeID*/, AchievementRecord> achievementCriteriaTreeIds = new();

        foreach (var achievement in CliDB.AchievementStorage.Values)
            if (achievement.CriteriaTree != 0)
                achievementCriteriaTreeIds[achievement.CriteriaTree] = achievement;

        Dictionary<uint, ScenarioStepRecord> scenarioCriteriaTreeIds = new();

        foreach (var scenarioStep in CliDB.ScenarioStepStorage.Values)
            if (scenarioStep.CriteriaTreeId != 0)
                scenarioCriteriaTreeIds[scenarioStep.CriteriaTreeId] = scenarioStep;

        Dictionary<uint /*criteriaTreeID*/, QuestObjective> questObjectiveCriteriaTreeIds = new();

        foreach (var pair in Global.ObjectMgr.GetQuestTemplates())
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
        foreach (var tree in CliDB.CriteriaTreeStorage.Values)
        {
            // Find linked achievement
            var achievement = GetEntry(achievementCriteriaTreeIds, tree);
            var scenarioStep = GetEntry(scenarioCriteriaTreeIds, tree);
            var questObjective = GetEntry(questObjectiveCriteriaTreeIds, tree);

            if (achievement == null && scenarioStep == null && questObjective == null)
                continue;

            CriteriaTree criteriaTree = new();
            criteriaTree.Id = tree.Id;
            criteriaTree.Achievement = achievement;
            criteriaTree.ScenarioStep = scenarioStep;
            criteriaTree.QuestObjective = questObjective;
            criteriaTree.Entry = tree;

            _criteriaTrees[criteriaTree.Entry.Id] = criteriaTree;
        }

        // Build tree
        foreach (var pair in _criteriaTrees)
        {
            var parent = _criteriaTrees.LookupByKey(pair.Value.Entry.Parent);

            if (parent != null)
                parent.Children.Add(pair.Value);

            if (CliDB.CriteriaStorage.HasRecord(pair.Value.Entry.CriteriaID))
                _criteriaTreeByCriteria.Add(pair.Value.Entry.CriteriaID, pair.Value);
        }

        for (var i = 0; i < (int)CriteriaFailEvent.Max; ++i)
            _criteriasByFailEvent[i] = new MultiMap<int, Criteria>();

        // Load criteria
        uint criterias = 0;
        uint guildCriterias = 0;
        uint scenarioCriterias = 0;
        uint questObjectiveCriterias = 0;

        foreach (var criteriaEntry in CliDB.CriteriaStorage.Values)
        {
            var treeList = _criteriaTreeByCriteria.LookupByKey(criteriaEntry.Id);

            if (treeList.Empty())
                continue;

            Criteria criteria = new();
            criteria.Id = criteriaEntry.Id;
            criteria.Entry = criteriaEntry;
            criteria.Modifier = _criteriaModifiers.LookupByKey(criteriaEntry.ModifierTreeId);

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
                        var worldOverlayEntry = CliDB.WorldMapOverlayStorage.LookupByKey(criteriaEntry.Asset);

                        if (worldOverlayEntry == null)
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

        Log.outInfo(LogFilter.ServerLoading, $"Loaded {criterias} criteria, {guildCriterias} guild criteria, {scenarioCriterias} scenario criteria and {questObjectiveCriterias} quest objective criteria in {Time.GetMSTimeDiffToNow(oldMSTime)} ms.");
    }

    public void LoadCriteriaData()
    {
        var oldMSTime = Time.MSTime;

        _criteriaDataMap.Clear(); // need for reload case

        var result = DB.World.Query("SELECT criteria_id, type, value1, value2, ScriptName FROM criteria_data");

        if (result.IsEmpty())
        {
            Log.outInfo(LogFilter.ServerLoading, "Loaded 0 additional criteria data. DB table `criteria_data` is empty.");

            return;
        }

        uint count = 0;

        do
        {
            var criteria_id = result.Read<uint>(0);

            var criteria = GetCriteria(criteria_id);

            if (criteria == null)
            {
                Log.outError(LogFilter.Sql, "Table `criteria_data` contains data for non-existing criteria (Entry: {0}). Ignored.", criteria_id);

                continue;
            }

            var dataType = (CriteriaDataType)result.Read<byte>(1);
            var scriptName = result.Read<string>(4);
            uint scriptId = 0;

            if (!scriptName.IsEmpty())
            {
                if (dataType != CriteriaDataType.Script)
                    Log.outError(LogFilter.Sql, "Table `criteria_data` contains a ScriptName for non-scripted data type (Entry: {0}, type {1}), useless data.", criteria_id, dataType);
                else
                    scriptId = Global.ObjectMgr.GetScriptId(scriptName);
            }

            CriteriaData data = new(dataType, result.Read<uint>(2), result.Read<uint>(3), scriptId);

            if (!data.IsValid(criteria))
                continue;

            // this will allocate empty data set storage
            CriteriaDataSet dataSet = new();
            dataSet.SetCriteriaId(criteria_id);

            // add real data only for not NONE data types
            if (data.DataType != CriteriaDataType.None)
                dataSet.Add(data);

            _criteriaDataMap[criteria_id] = dataSet;
            // counting data by and data types
            ++count;
        } while (result.NextRow());

        Log.outInfo(LogFilter.ServerLoading, "Loaded {0} additional criteria data in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
    }

    public CriteriaTree GetCriteriaTree(uint criteriaTreeId)
    {
        return _criteriaTrees.LookupByKey(criteriaTreeId);
    }

    public Criteria GetCriteria(uint criteriaId)
    {
        return _criteria.LookupByKey(criteriaId);
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

    public List<Criteria> GetScenarioCriteriaByTypeAndScenario(CriteriaType type, uint scenarioId)
    {
        return _scenarioCriteriasByTypeAndScenarioId[(int)type].LookupByKey(scenarioId);
    }

    public List<Criteria> GetGuildCriteriaByType(CriteriaType type)
    {
        return _guildCriteriasByType.LookupByKey(type);
    }

    public List<Criteria> GetQuestObjectiveCriteriaByType(CriteriaType type)
    {
        return _questObjectiveCriteriasByType[type];
    }

    public List<CriteriaTree> GetCriteriaTreesByCriteria(uint criteriaId)
    {
        return _criteriaTreeByCriteria.LookupByKey(criteriaId);
    }

    public List<Criteria> GetTimedCriteriaByType(CriteriaStartEvent startEvent)
    {
        return _criteriasByTimedType.LookupByKey(startEvent);
    }

    public List<Criteria> GetCriteriaByFailEvent(CriteriaFailEvent failEvent, int asset)
    {
        return _criteriasByFailEvent[(int)failEvent].LookupByKey(asset);
    }

    public CriteriaDataSet GetCriteriaDataSet(Criteria criteria)
    {
        return _criteriaDataMap.LookupByKey(criteria.Id);
    }

    public static bool IsGroupCriteriaType(CriteriaType type)
    {
        switch (type)
        {
            case CriteriaType.KillCreature:
            case CriteriaType.WinBattleground:
            case CriteriaType.BeSpellTarget: // NYI
            case CriteriaType.WinAnyRankedArena:
            case CriteriaType.GainAura:           // NYI
            case CriteriaType.WinAnyBattleground: // NYI
                return true;
            default:
                break;
        }

        return false;
    }

    public static void WalkCriteriaTree(CriteriaTree tree, Action<CriteriaTree> func)
    {
        foreach (var node in tree.Children)
            WalkCriteriaTree(node, func);

        func(tree);
    }

    T GetEntry<T>(Dictionary<uint, T> map, CriteriaTreeRecord tree) where T : new()
    {
        var cur = tree;
        var obj = map.LookupByKey(tree.Id);

        while (obj == null)
        {
            if (cur.Parent == 0)
                break;

            cur = CliDB.CriteriaTreeStorage.LookupByKey(cur.Parent);

            if (cur == null)
                break;

            obj = map.LookupByKey(cur.Id);
        }

        if (obj == null)
            return default;

        return obj;
    }

    bool IsCriteriaTypeStoredByAsset(CriteriaType type)
    {
        switch (type)
        {
            case CriteriaType.KillCreature:
            case CriteriaType.WinBattleground:
            case CriteriaType.SkillRaised:
            case CriteriaType.EarnAchievement:
            case CriteriaType.CompleteQuestsInZone:
            case CriteriaType.ParticipateInBattleground:
            case CriteriaType.KilledByCreature:
            case CriteriaType.CompleteQuest:
            case CriteriaType.BeSpellTarget:
            case CriteriaType.CastSpell:
            case CriteriaType.TrackedWorldStateUIModified:
            case CriteriaType.PVPKillInArea:
            case CriteriaType.LearnOrKnowSpell:
            case CriteriaType.AcquireItem:
            case CriteriaType.AchieveSkillStep:
            case CriteriaType.UseItem:
            case CriteriaType.LootItem:
            case CriteriaType.RevealWorldMapOverlay:
            case CriteriaType.ReputationGained:
            case CriteriaType.EquipItemInSlot:
            case CriteriaType.DeliverKillingBlowToClass:
            case CriteriaType.DeliverKillingBlowToRace:
            case CriteriaType.DoEmote:
            case CriteriaType.EquipItem:
            case CriteriaType.UseGameobject:
            case CriteriaType.GainAura:
            case CriteriaType.CatchFishInFishingHole:
            case CriteriaType.LearnSpellFromSkillLine:
            case CriteriaType.GetLootByType:
            case CriteriaType.LandTargetedSpellOnTarget:
            case CriteriaType.LearnTradeskillSkillLine:
                return true;
            default:
                return false;
        }
    }
}