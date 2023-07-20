// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using Forged.MapServer.Achievements;
using Forged.MapServer.Arenas;
using Forged.MapServer.Chat;
using Forged.MapServer.Conditions;
using Forged.MapServer.DataStorage;
using Forged.MapServer.DataStorage.Structs.S;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Globals;
using Forged.MapServer.Maps;
using Forged.MapServer.Networking;
using Forged.MapServer.Networking.Packets.Achievements;
using Forged.MapServer.Networking.Packets.Scenario;
using Forged.MapServer.Phasing;
using Forged.MapServer.Spells;
using Forged.MapServer.World;
using Framework.Constants;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Forged.MapServer.Scenarios;

public class Scenario : CriteriaHandler
{
    protected ScenarioData Data;
    private readonly ObjectAccessor _objectAccessor;

    private readonly List<ObjectGuid> _players = new();
    private readonly Dictionary<ScenarioStepRecord, ScenarioStepState> _stepStates = new();
    private ScenarioStepRecord _currentstep;

    public Scenario(ScenarioData scenarioData, ObjectAccessor objectAccessor, CriteriaManager criteriaManager, WorldManager worldManager, GameObjectManager gameObjectManager, SpellManager spellManager, ArenaTeamManager arenaTeamManager,
                    DisableManager disableManager, WorldStateManager worldStateManager, CliDB cliDB, ConditionManager conditionManager, RealmManager realmManager, IConfiguration configuration,
                    LanguageManager languageManager, DB2Manager db2Manager, MapManager mapManager, AchievementGlobalMgr achievementManager, PhasingHandler phasingHandler) :
        base(criteriaManager, worldManager, gameObjectManager, spellManager, arenaTeamManager, disableManager, worldStateManager, cliDB, conditionManager, realmManager, configuration, languageManager, db2Manager, mapManager, achievementManager, phasingHandler)
    {
        Data = scenarioData;
        _objectAccessor = objectAccessor;
        _currentstep = null;

        //ASSERT(_data);

        foreach (var scenarioStep in Data.Steps.Values)
            SetStepState(scenarioStep, ScenarioStepState.NotStarted);

        var firstStep = GetFirstStep();

        if (firstStep != null)
            SetStep(firstStep);
        else
            Log.Logger.Error("Scenario.Scenario: Could not launch Scenario (id: {0}), found no valid scenario step", Data.Entry.Id);
    }

    public override void AfterCriteriaTreeUpdate(CriteriaTree tree, Player referencePlayer) { }

    public override bool CanCompleteCriteriaTree(CriteriaTree tree)
    {
        var step = tree.ScenarioStep;

        if (step == null)
            return false;

        var state = GetStepState(step);

        if (state == ScenarioStepState.Done)
            return false;

        var currentStep = GetStep();

        if (currentStep == null)
            return false;

        if (step.IsBonusObjective())
            if (step != currentStep)
                return false;

        return base.CanCompleteCriteriaTree(tree);
    }

    public override bool CanUpdateCriteriaTree(Criteria criteria, CriteriaTree tree, Player referencePlayer)
    {
        var step = tree.ScenarioStep;

        if (step == null)
            return false;

        if (step.ScenarioID != Data.Entry.Id)
            return false;

        var currentStep = GetStep();

        if (currentStep == null)
            return false;

        if (step.IsBonusObjective())
            return true;

        return currentStep == step;
    }

    public override void CompletedCriteriaTree(CriteriaTree tree, Player referencePlayer)
    {
        var step = tree.ScenarioStep;

        if (!IsCompletedStep(step))
            return;

        SetStepState(step, ScenarioStepState.Done);
        CompleteStep(step);
    }

    public virtual void CompleteScenario()
    {
        SendPacket(new ScenarioCompleted(Data.Entry.Id));
    }

    public virtual void CompleteStep(ScenarioStepRecord step)
    {
        var quest = GameObjectManager.GetQuestTemplate(step.RewardQuestID);

        if (quest != null)
            foreach (var guid in _players)
                _objectAccessor.FindPlayer(guid)?.RewardQuest(quest, LootItemType.Item, 0, null, false);

        if (step.IsBonusObjective())
            return;

        ScenarioStepRecord newStep = null;

        foreach (var scenarioStep in Data.Steps.Values)
        {
            if (scenarioStep.IsBonusObjective())
                continue;

            if (GetStepState(scenarioStep) == ScenarioStepState.Done)
                continue;

            if (newStep == null || scenarioStep.OrderIndex < newStep.OrderIndex)
                newStep = scenarioStep;
        }

        SetStep(newStep);

        if (IsComplete())
            CompleteScenario();
        else
            Log.Logger.Error("Scenario.CompleteStep: Scenario (id: {0}, step: {1}) was completed, but could not determine new step, or validate scenario completion.", step.ScenarioID, step.Id);
    }

    public override List<Criteria> GetCriteriaByType(CriteriaType type, uint asset)
    {
        return CriteriaManager.GetScenarioCriteriaByTypeAndScenario(type, Data.Entry.Id);
    }

    public ScenarioRecord GetEntry()
    {
        return Data.Entry;
    }

    public ScenarioStepRecord GetLastStep()
    {
        // Do it like this because we don't know what order they're in inside the container.
        ScenarioStepRecord lastStep = null;

        foreach (var scenarioStep in Data.Steps.Values)
        {
            if (scenarioStep.IsBonusObjective())
                continue;

            if (lastStep == null || scenarioStep.OrderIndex > lastStep.OrderIndex)
                lastStep = scenarioStep;
        }

        return lastStep;
    }

    public ScenarioStepRecord GetStep()
    {
        return _currentstep;
    }

    public virtual void OnPlayerEnter(Player player)
    {
        _players.Add(player.GUID);
        SendScenarioState(player);
    }

    public virtual void OnPlayerExit(Player player)
    {
        _players.Remove(player.GUID);
        SendBootPlayer(player);
    }

    public override void Reset()
    {
        base.Reset();
        SetStep(GetFirstStep());
    }

    public override void SendAllData(Player receiver) { }

    public override void SendCriteriaProgressRemoved(uint criteriaId) { }

    public override void SendCriteriaUpdate(Criteria criteria, CriteriaProgress progress, TimeSpan timeElapsed, bool timedCompleted)
    {
        ScenarioProgressUpdate progressUpdate = new();
        progressUpdate.CriteriaProgress.Id = criteria.Id;
        progressUpdate.CriteriaProgress.Quantity = progress.Counter;
        progressUpdate.CriteriaProgress.Player = progress.PlayerGUID;
        progressUpdate.CriteriaProgress.Date = progress.Date;

        if (criteria.Entry.StartTimer != 0)
            progressUpdate.CriteriaProgress.Flags = timedCompleted ? 1 : 0u;

        progressUpdate.CriteriaProgress.TimeFromStart = (uint)timeElapsed.TotalSeconds;
        progressUpdate.CriteriaProgress.TimeFromCreate = 0;

        SendPacket(progressUpdate);
    }

    public override void SendPacket(ServerPacket data)
    {
        foreach (var guid in _players)
            _objectAccessor.FindPlayer(guid)?.SendPacket(data);
    }

    public void SendScenarioState(Player player)
    {
        ScenarioState scenarioState = new();
        BuildScenarioState(scenarioState);
        player.SendPacket(scenarioState);
    }

    public void SetStepState(ScenarioStepRecord step, ScenarioStepState state)
    {
        _stepStates[step] = state;
    }

    public virtual void Update(uint diff) { }

    private void BuildScenarioState(ScenarioState scenarioState)
    {
        scenarioState.ScenarioID = (int)Data.Entry.Id;
        var step = GetStep();

        if (step != null)
            scenarioState.CurrentStep = (int)step.Id;

        scenarioState.CriteriaProgress = GetCriteriasProgress();
        scenarioState.BonusObjectives = GetBonusObjectivesData();

        // Don't know exactly what this is for, but seems to contain list of scenario steps that we're either on or that are completed
        foreach (var state in _stepStates)
        {
            if (state.Key.IsBonusObjective())
                continue;

            switch (state.Value)
            {
                case ScenarioStepState.InProgress:
                case ScenarioStepState.Done:
                    break;

                case ScenarioStepState.NotStarted:
                default:
                    continue;
            }

            scenarioState.PickedSteps.Add(state.Key.Id);
        }

        scenarioState.ScenarioComplete = IsComplete();
    }

    private List<BonusObjectiveData> GetBonusObjectivesData()
    {
        List<BonusObjectiveData> bonusObjectivesData = new();

        foreach (var scenarioStep in Data.Steps.Values)
        {
            if (!scenarioStep.IsBonusObjective())
                continue;

            if (CriteriaManager.GetCriteriaTree(scenarioStep.CriteriaTreeId) == null)
                continue;

            BonusObjectiveData bonusObjectiveData;
            bonusObjectiveData.BonusObjectiveID = (int)scenarioStep.Id;
            bonusObjectiveData.ObjectiveComplete = GetStepState(scenarioStep) == ScenarioStepState.Done;
            bonusObjectivesData.Add(bonusObjectiveData);
        }

        return bonusObjectivesData;
    }

    private List<CriteriaProgressPkt> GetCriteriasProgress()
    {
        List<CriteriaProgressPkt> criteriasProgress = new();

        if (CriteriaProgress.Empty())
            return criteriasProgress;

        criteriasProgress.AddRange(CriteriaProgress.Select(pair => new CriteriaProgressPkt
        {
            Id = pair.Key,
            Quantity = pair.Value.Counter,
            Date = pair.Value.Date,
            Player = pair.Value.PlayerGUID
        }));

        return criteriasProgress;
    }

    private ScenarioStepRecord GetFirstStep()
    {
        // Do it like this because we don't know what order they're in inside the container.
        ScenarioStepRecord firstStep = null;

        foreach (var scenarioStep in Data.Steps.Values)
        {
            if (scenarioStep.IsBonusObjective())
                continue;

            if (firstStep == null || scenarioStep.OrderIndex < firstStep.OrderIndex)
                firstStep = scenarioStep;
        }

        return firstStep;
    }

    private ScenarioStepState GetStepState(ScenarioStepRecord step)
    {
        if (!_stepStates.ContainsKey(step))
            return ScenarioStepState.Invalid;

        return _stepStates[step];
    }

    private bool IsComplete()
    {
        foreach (var scenarioStep in Data.Steps.Values)
        {
            if (scenarioStep.IsBonusObjective())
                continue;

            if (GetStepState(scenarioStep) != ScenarioStepState.Done)
                return false;
        }

        return true;
    }

    private bool IsCompletedStep(ScenarioStepRecord step)
    {
        var tree = CriteriaManager.GetCriteriaTree(step.CriteriaTreeId);

        return tree != null && IsCompletedCriteriaTree(tree);
    }

    private void SendBootPlayer(Player player)
    {
        ScenarioVacate scenarioBoot = new()
        {
            ScenarioID = (int)Data.Entry.Id
        };

        player.SendPacket(scenarioBoot);
    }

    private void SetStep(ScenarioStepRecord step)
    {
        _currentstep = step;

        if (step != null)
            SetStepState(step, ScenarioStepState.InProgress);

        ScenarioState scenarioState = new();
        BuildScenarioState(scenarioState);
        SendPacket(scenarioState);
    }

    ~Scenario()
    {
        foreach (var player in _players.Select(guid => _objectAccessor.FindPlayer(guid)).Where(player => player != null))
            SendBootPlayer(player);

        _players.Clear();
    }
}