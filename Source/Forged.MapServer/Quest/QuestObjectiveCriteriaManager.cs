// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.Achievements;
using Forged.MapServer.Chrono;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Networking;
using Forged.MapServer.Networking.Packets.Achievements;
using Framework.Constants;
using Framework.Database;
using Serilog;

namespace Forged.MapServer.Quest;

internal class QuestObjectiveCriteriaManager : CriteriaHandler
{
    private readonly Player _owner;
    private readonly List<uint> _completedObjectives = new();

	public QuestObjectiveCriteriaManager(Player owner)
	{
		_owner = owner;
	}

	public void CheckAllQuestObjectiveCriteria(Player referencePlayer)
	{
		// suppress sending packets
		for (CriteriaType i = 0; i < CriteriaType.Count; ++i)
			UpdateCriteria(i, 0, 0, 0, null, referencePlayer);
	}

	public override void Reset()
	{
		foreach (var pair in _criteriaProgress)
			SendCriteriaProgressRemoved(pair.Key);

		_criteriaProgress.Clear();

		DeleteFromDB(_owner.GUID);

		// re-fill data
		CheckAllQuestObjectiveCriteria(_owner);
	}

	public static void DeleteFromDB(ObjectGuid guid)
	{
		SQLTransaction trans = new();

		var stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_QUESTSTATUS_OBJECTIVES_CRITERIA);
		stmt.AddValue(0, guid.Counter);
		trans.Append(stmt);

		stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_QUESTSTATUS_OBJECTIVES_CRITERIA_PROGRESS);
		stmt.AddValue(0, guid.Counter);
		trans.Append(stmt);

		DB.Characters.CommitTransaction(trans);
	}

	public void LoadFromDB(SQLResult objectiveResult, SQLResult criteriaResult)
	{
		if (!objectiveResult.IsEmpty())
			do
			{
				var objectiveId = objectiveResult.Read<uint>(0);

				var objective = Global.ObjectMgr.GetQuestObjective(objectiveId);

				if (objective == null)
					continue;

				_completedObjectives.Add(objectiveId);
			} while (objectiveResult.NextRow());

		if (!criteriaResult.IsEmpty())
		{
			var now = GameTime.GetGameTime();

			do
			{
				var criteriaId = criteriaResult.Read<uint>(0);
				var counter = criteriaResult.Read<ulong>(1);
				var date = criteriaResult.Read<long>(2);

				var criteria = Global.CriteriaMgr.GetCriteria(criteriaId);

				if (criteria == null)
				{
					// Removing non-existing criteria data for all characters
					Log.Logger.Error($"Non-existing quest objective criteria {criteriaId} data has been removed from the table `character_queststatus_objectives_criteria_progress`.");

					var stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_INVALID_QUEST_PROGRESS_CRITERIA);
					stmt.AddValue(0, criteriaId);
					DB.Characters.Execute(stmt);

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

				_criteriaProgress[criteriaId] = progress;
			} while (criteriaResult.NextRow());
		}
	}

	public void SaveToDB(SQLTransaction trans)
	{
		var stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_QUESTSTATUS_OBJECTIVES_CRITERIA);
		stmt.AddValue(0, _owner.GUID.Counter);
		trans.Append(stmt);

		if (!_completedObjectives.Empty())
			foreach (var completedObjectiveId in _completedObjectives)
			{
				stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_CHAR_QUESTSTATUS_OBJECTIVES_CRITERIA);
				stmt.AddValue(0, _owner.GUID.Counter);
				stmt.AddValue(1, completedObjectiveId);
				trans.Append(stmt);
			}

		if (!_criteriaProgress.Empty())
			foreach (var pair in _criteriaProgress)
			{
				if (!pair.Value.Changed)
					continue;

				stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_QUESTSTATUS_OBJECTIVES_CRITERIA_PROGRESS_BY_CRITERIA);
				stmt.AddValue(0, _owner.GUID.Counter);
				stmt.AddValue(1, pair.Key);
				trans.Append(stmt);

				if (pair.Value.Counter != 0)
				{
					stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_CHAR_QUESTSTATUS_OBJECTIVES_CRITERIA_PROGRESS);
					stmt.AddValue(0, _owner.GUID.Counter);
					stmt.AddValue(1, pair.Key);
					stmt.AddValue(2, pair.Value.Counter);
					stmt.AddValue(3, pair.Value.Date);
					trans.Append(stmt);
				}

				pair.Value.Changed = false;
			}
	}

	public void ResetCriteria(CriteriaFailEvent failEvent, uint failAsset, bool evenIfCriteriaComplete)
	{
		Log.Logger.Debug($"QuestObjectiveCriteriaMgr.ResetCriteria({failEvent}, {failAsset}, {evenIfCriteriaComplete})");

		// disable for gamemasters with GM-mode enabled
		if (_owner.IsGameMaster)
			return;

		var playerCriteriaList = Global.CriteriaMgr.GetCriteriaByFailEvent(failEvent, (int)failAsset);

		foreach (var playerCriteria in playerCriteriaList)
		{
			var trees = Global.CriteriaMgr.GetCriteriaTreesByCriteria(playerCriteria.Id);
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
		var tree = Global.CriteriaMgr.GetCriteriaTree(criteriaTreeId);

		if (tree == null)
			return;

		CriteriaManager.WalkCriteriaTree(tree, criteriaTree => { RemoveCriteriaProgress(criteriaTree.Criteria); });
	}

	public override void SendAllData(Player receiver)
	{
		foreach (var pair in _criteriaProgress)
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

	public bool HasCompletedObjective(QuestObjective questObjective)
	{
		return _completedObjectives.Contains(questObjective.Id);
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

	public override void SendCriteriaProgressRemoved(uint criteriaId)
	{
		CriteriaDeleted criteriaDeleted = new()
		{
			CriteriaID = criteriaId
		};

		SendPacket(criteriaDeleted);
	}

	public override bool CanUpdateCriteriaTree(Criteria criteria, CriteriaTree tree, Player referencePlayer)
	{
		var objective = tree.QuestObjective;

		if (objective == null)
			return false;

		if (HasCompletedObjective(objective))
		{
			Log.Logger.Verbose($"QuestObjectiveCriteriaMgr.CanUpdateCriteriaTree: (Id: {criteria.Id} Type {criteria.Entry.Type} Quest Objective {objective.Id}) Objective already completed");

			return false;
		}

		if (_owner.GetQuestStatus(objective.QuestID) != QuestStatus.Incomplete)
		{
			Log.Logger.Verbose($"QuestObjectiveCriteriaMgr.CanUpdateCriteriaTree: (Id: {criteria.Id} Type {criteria.Entry.Type} Quest Objective {objective.Id}) Not on quest");

			return false;
		}

		var quest = Global.ObjectMgr.GetQuestTemplate(objective.QuestID);

		if (_owner.Group && _owner.Group.IsRaidGroup && !quest.IsAllowedInRaid(referencePlayer.Map.DifficultyID))
		{
			Log.Logger.Verbose($"QuestObjectiveCriteriaMgr.CanUpdateCriteriaTree: (Id: {criteria.Id} Type {criteria.Entry.Type} Quest Objective {objective.Id}) Quest cannot be completed in raid group");

			return false;
		}

		var slot = _owner.FindQuestSlot(objective.QuestID);

		if (slot >= SharedConst.MaxQuestLogSize || !_owner.IsQuestObjectiveCompletable(slot, quest, objective))
		{
			Log.Logger.Verbose($"QuestObjectiveCriteriaMgr.CanUpdateCriteriaTree: (Id: {criteria.Id} Type {criteria.Entry.Type} Quest Objective {objective.Id}) Objective not completable");

			return false;
		}

		return base.CanUpdateCriteriaTree(criteria, tree, referencePlayer);
	}

	public override bool CanCompleteCriteriaTree(CriteriaTree tree)
	{
		var objective = tree.QuestObjective;

		if (objective == null)
			return false;

		return base.CanCompleteCriteriaTree(tree);
	}

	public override void CompletedCriteriaTree(CriteriaTree tree, Player referencePlayer)
	{
		var objective = tree.QuestObjective;

		if (objective == null)
			return;

		CompletedObjective(objective, referencePlayer);
	}

	public override void SendPacket(ServerPacket data)
	{
		_owner.SendPacket(data);
	}

	public override string GetOwnerInfo()
	{
		return $"{_owner.GUID} {_owner.GetName()}";
	}

	public override List<Criteria> GetCriteriaByType(CriteriaType type, uint asset)
	{
		return Global.CriteriaMgr.GetQuestObjectiveCriteriaByType(type);
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