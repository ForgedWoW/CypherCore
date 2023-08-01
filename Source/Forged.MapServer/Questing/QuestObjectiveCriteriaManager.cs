// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.Achievements;
using Forged.MapServer.Arenas;
using Forged.MapServer.Chat;
using Forged.MapServer.Chrono;
using Forged.MapServer.Conditions;
using Forged.MapServer.DataStorage;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Globals;
using Forged.MapServer.Globals.Caching;
using Forged.MapServer.Maps;
using Forged.MapServer.Networking;
using Forged.MapServer.Networking.Packets.Achievements;
using Forged.MapServer.Phasing;
using Forged.MapServer.Spells;
using Forged.MapServer.World;
using Framework.Constants;
using Framework.Database;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Forged.MapServer.Questing;

internal class QuestObjectiveCriteriaManager : CriteriaHandler
{
    private readonly CharacterDatabase _characterDatabase;
    private readonly List<uint> _completedObjectives = new();
    private readonly Player _owner;

    public QuestObjectiveCriteriaManager(Player owner, CharacterDatabase characterDatabase, CriteriaManager criteriaManager, WorldManager worldManager, GameObjectManager gameObjectManager, SpellManager spellManager, ArenaTeamManager arenaTeamManager,
                                         DisableManager disableManager, WorldStateManager worldStateManager, CliDB cliDB, ConditionManager conditionManager, RealmManager realmManager, IConfiguration configuration,
                                         LanguageManager languageManager, DB2Manager db2Manager, MapManager mapManager, AchievementGlobalMgr achievementManager, PhasingHandler phasingHandler, ItemTemplateCache itemTemplateCache) :
        base(criteriaManager, worldManager, gameObjectManager, spellManager, arenaTeamManager, disableManager, worldStateManager, cliDB, conditionManager, realmManager, configuration, languageManager, db2Manager, mapManager, achievementManager, phasingHandler, itemTemplateCache)
    {
        _owner = owner;
        _characterDatabase = characterDatabase;
    }

    public override bool CanCompleteCriteriaTree(CriteriaTree tree)
    {
        var objective = tree.QuestObjective;

        return objective != null && base.CanCompleteCriteriaTree(tree);
    }

    public override bool CanUpdateCriteriaTree(Criteria criteria, CriteriaTree tree, Player referencePlayer)
    {
        var objective = tree.QuestObjective;

        if (objective == null)
            return false;

        if (HasCompletedObjective(objective))
        {
            Log.Logger.Verbose($"QuestObjectiveCriteriaMgr.CanUpdateCriteriaTree: (Id: {criteria.Id} Type {criteria.Entry.Type} QuestId Objective {objective.Id}) Objective already completed");

            return false;
        }

        if (_owner.GetQuestStatus(objective.QuestID) != QuestStatus.Incomplete)
        {
            Log.Logger.Verbose($"QuestObjectiveCriteriaMgr.CanUpdateCriteriaTree: (Id: {criteria.Id} Type {criteria.Entry.Type} QuestId Objective {objective.Id}) Not on quest");

            return false;
        }

        var quest = GameObjectManager.QuestTemplateCache.GetQuestTemplate(objective.QuestID);

        if (_owner.Group is { IsRaidGroup: true } && !quest.IsAllowedInRaid(referencePlayer.Location.Map.DifficultyID))
        {
            Log.Logger.Verbose($"QuestObjectiveCriteriaMgr.CanUpdateCriteriaTree: (Id: {criteria.Id} Type {criteria.Entry.Type} QuestId Objective {objective.Id}) QuestId cannot be completed in raid group");

            return false;
        }

        var slot = _owner.FindQuestSlot(objective.QuestID);

        if (slot < SharedConst.MaxQuestLogSize && _owner.IsQuestObjectiveCompletable(slot, quest, objective))
            return base.CanUpdateCriteriaTree(criteria, tree, referencePlayer);

        Log.Logger.Verbose($"QuestObjectiveCriteriaMgr.CanUpdateCriteriaTree: (Id: {criteria.Id} Type {criteria.Entry.Type} QuestId Objective {objective.Id}) Objective not completable");

        return false;
    }

    public void CheckAllQuestObjectiveCriteria(Player referencePlayer)
    {
        // suppress sending packets
        for (CriteriaType i = 0; i < CriteriaType.Count; ++i)
            UpdateCriteria(i, 0, 0, 0, null, referencePlayer);
    }

    public override void CompletedCriteriaTree(CriteriaTree tree, Player referencePlayer)
    {
        var objective = tree.QuestObjective;

        if (objective == null)
            return;

        CompletedObjective(objective, referencePlayer);
    }

    public void DeleteFromDB(ObjectGuid guid)
    {
        SQLTransaction trans = new();

        var stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_QUESTSTATUS_OBJECTIVES_CRITERIA);
        stmt.AddValue(0, guid.Counter);
        trans.Append(stmt);

        stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_QUESTSTATUS_OBJECTIVES_CRITERIA_PROGRESS);
        stmt.AddValue(0, guid.Counter);
        trans.Append(stmt);

        _characterDatabase.CommitTransaction(trans);
    }

    public override List<Criteria> GetCriteriaByType(CriteriaType type, uint asset)
    {
        return CriteriaManager.GetQuestObjectiveCriteriaByType(type);
    }

    public override string GetOwnerInfo()
    {
        return $"{_owner.GUID} {_owner.GetName()}";
    }

    public bool HasCompletedObjective(QuestObjective questObjective)
    {
        return _completedObjectives.Contains(questObjective.Id);
    }

    public void LoadFromDB(SQLResult objectiveResult, SQLResult criteriaResult)
    {
        if (!objectiveResult.IsEmpty())
            do
            {
                var objectiveId = objectiveResult.Read<uint>(0);

                var objective = GameObjectManager.QuestTemplateCache.GetQuestObjective(objectiveId);

                if (objective == null)
                    continue;

                _completedObjectives.Add(objectiveId);
            } while (objectiveResult.NextRow());

        if (criteriaResult.IsEmpty())
            return;

        var now = GameTime.CurrentTime;

        do
        {
            var criteriaId = criteriaResult.Read<uint>(0);
            var counter = criteriaResult.Read<ulong>(1);
            var date = criteriaResult.Read<long>(2);

            var criteria = CriteriaManager.GetCriteria(criteriaId);

            if (criteria == null)
            {
                // Removing non-existing criteria data for all characters
                Log.Logger.Error($"Non-existing quest objective criteria {criteriaId} data has been removed from the table `character_queststatus_objectives_criteria_progress`.");

                var stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_INVALID_QUEST_PROGRESS_CRITERIA);
                stmt.AddValue(0, criteriaId);
                _characterDatabase.Execute(stmt);

                continue;
            }

            if (criteria.Entry.StartTimer != 0 && date + criteria.Entry.StartTimer < now)
                continue;

            CriteriaProgress progress = new()
            {
                Counter = counter,
                Date = date,
                Changed = false
            };

            CriteriaProgress[criteriaId] = progress;
        } while (criteriaResult.NextRow());
    }

    public override void Reset()
    {
        foreach (var pair in CriteriaProgress)
            SendCriteriaProgressRemoved(pair.Key);

        CriteriaProgress.Clear();

        DeleteFromDB(_owner.GUID);

        // re-fill data
        CheckAllQuestObjectiveCriteria(_owner);
    }

    public void ResetCriteria(CriteriaFailEvent failEvent, uint failAsset, bool evenIfCriteriaComplete)
    {
        Log.Logger.Debug($"QuestObjectiveCriteriaMgr.ResetCriteria({failEvent}, {failAsset}, {evenIfCriteriaComplete})");

        // disable for gamemasters with GM-mode enabled
        if (_owner.IsGameMaster)
            return;

        var playerCriteriaList = CriteriaManager.GetCriteriaByFailEvent(failEvent, (int)failAsset);

        foreach (var playerCriteria in playerCriteriaList)
        {
            var trees = CriteriaManager.GetCriteriaTreesByCriteria(playerCriteria.Id);
            var allComplete = true;

            foreach (var tree in trees)
                // don't update already completed criteria if not forced
                if (!(IsCompletedCriteriaTree(tree) && !evenIfCriteriaComplete))
                {
                    allComplete = false;

                    break;
                }

            if (allComplete)
                continue;

            RemoveCriteriaProgress(playerCriteria);
        }
    }

    public void ResetCriteriaTree(uint criteriaTreeId)
    {
        var tree = CriteriaManager.GetCriteriaTree(criteriaTreeId);

        if (tree == null)
            return;

        CriteriaManager.WalkCriteriaTree(tree, criteriaTree => { RemoveCriteriaProgress(criteriaTree.Criteria); });
    }

    public void SaveToDB(SQLTransaction trans)
    {
        var stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_QUESTSTATUS_OBJECTIVES_CRITERIA);
        stmt.AddValue(0, _owner.GUID.Counter);
        trans.Append(stmt);

        if (!_completedObjectives.Empty())
            foreach (var completedObjectiveId in _completedObjectives)
            {
                stmt = _characterDatabase.GetPreparedStatement(CharStatements.INS_CHAR_QUESTSTATUS_OBJECTIVES_CRITERIA);
                stmt.AddValue(0, _owner.GUID.Counter);
                stmt.AddValue(1, completedObjectiveId);
                trans.Append(stmt);
            }

        if (!CriteriaProgress.Empty())
            foreach (var pair in CriteriaProgress)
            {
                if (!pair.Value.Changed)
                    continue;

                stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_QUESTSTATUS_OBJECTIVES_CRITERIA_PROGRESS_BY_CRITERIA);
                stmt.AddValue(0, _owner.GUID.Counter);
                stmt.AddValue(1, pair.Key);
                trans.Append(stmt);

                if (pair.Value.Counter != 0)
                {
                    stmt = _characterDatabase.GetPreparedStatement(CharStatements.INS_CHAR_QUESTSTATUS_OBJECTIVES_CRITERIA_PROGRESS);
                    stmt.AddValue(0, _owner.GUID.Counter);
                    stmt.AddValue(1, pair.Key);
                    stmt.AddValue(2, pair.Value.Counter);
                    stmt.AddValue(3, pair.Value.Date);
                    trans.Append(stmt);
                }

                pair.Value.Changed = false;
            }
    }

    public override void SendAllData(Player receiver)
    {
        foreach (var pair in CriteriaProgress)
        {
            CriteriaUpdate criteriaUpdate = new()
            {
                CriteriaID = pair.Key,
                Quantity = pair.Value.Counter,
                PlayerGUID = _owner.GUID,
                Flags = 0,
                CurrentTime = pair.Value.Date,
                CreationTime = 0
            };

            SendPacket(criteriaUpdate);
        }
    }

    public override void SendCriteriaProgressRemoved(uint criteriaId)
    {
        CriteriaDeleted criteriaDeleted = new()
        {
            CriteriaID = criteriaId
        };

        SendPacket(criteriaDeleted);
    }

    public override void SendCriteriaUpdate(Criteria criteria, CriteriaProgress progress, TimeSpan timeElapsed, bool timedCompleted)
    {
        CriteriaUpdate criteriaUpdate = new()
        {
            CriteriaID = criteria.Id,
            Quantity = progress.Counter,
            PlayerGUID = _owner.GUID,
            Flags = 0
        };

        if (criteria.Entry.StartTimer != 0)
            criteriaUpdate.Flags = timedCompleted ? 1 : 0u; // 1 is for keeping the counter at 0 in client

        criteriaUpdate.CurrentTime = progress.Date;
        criteriaUpdate.ElapsedTime = (uint)timeElapsed.TotalSeconds;
        criteriaUpdate.CreationTime = 0;

        SendPacket(criteriaUpdate);
    }

    public override void SendPacket(ServerPacket data)
    {
        _owner.SendPacket(data);
    }

    private void CompletedObjective(QuestObjective questObjective, Player referencePlayer)
    {
        if (HasCompletedObjective(questObjective))
            return;

        _owner.KillCreditCriteriaTreeObjective(questObjective);

        Log.Logger.Information($"QuestObjectiveCriteriaMgr.CompletedObjective({questObjective.Id}). {GetOwnerInfo()}");

        _completedObjectives.Add(questObjective.Id);
    }
}