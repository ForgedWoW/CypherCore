// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.Globals;
using Framework.Database;
using Serilog;

namespace Forged.MapServer.Pools;

public class QuestPoolManager
{
    private readonly CharacterDatabase _characterDatabase;
    private readonly List<QuestPool> _dailyPools = new();
    private readonly List<QuestPool> _monthlyPools = new();
    private readonly GameObjectManager _objectManager;
    private readonly Dictionary<uint, QuestPool> _poolLookup = new();
    private readonly List<QuestPool> _weeklyPools = new();

    private readonly WorldDatabase _worldDatabase;
    // questId -> pool

    public QuestPoolManager(CharacterDatabase characterDatabase, WorldDatabase worldDatabase, GameObjectManager objectManager)
    {
        _characterDatabase = characterDatabase;
        _worldDatabase = worldDatabase;
        _objectManager = objectManager;
    }

    public static void RegeneratePool(QuestPool pool)
    {
        var n = pool.Members.Count - 1;
        pool.ActiveQuests.Clear();

        for (uint i = 0; i < pool.NumActive; ++i)
        {
            var j = RandomHelper.URand(i, n);

            if (i != j)
                (pool.Members[i], pool.Members[j]) = (pool.Members[j], pool.Members[i]);

            foreach (var quest in pool.Members[i])
                pool.ActiveQuests.Add(quest);
        }
    }

    public void ChangeDailyQuests()
    {
        Regenerate(_dailyPools);
    }

    public void ChangeMonthlyQuests()
    {
        Regenerate(_monthlyPools);
    }

    public void ChangeWeeklyQuests()
    {
        Regenerate(_weeklyPools);
    }

    // the storage structure ends up making this kind of inefficient
    // we don't use it in practice (only in debug commands), so that's fine
    public QuestPool FindQuestPool(uint poolId)
    {
        bool Lambda(QuestPool p)
        {
            return p.PoolId == poolId;
        }

        ;

        var questPool = _dailyPools.Find(Lambda);

        if (questPool != null)
            return questPool;

        questPool = _weeklyPools.Find(Lambda);

        if (questPool != null)
            return questPool;

        questPool = _monthlyPools.Find(Lambda);

        return questPool;
    }

    public bool IsQuestActive(uint questId)
    {
        if (!_poolLookup.TryGetValue(questId, out var it)) // not pooled
            return true;

        return it.ActiveQuests.Contains(questId);
    }

    public bool IsQuestPooled(uint questId)
    {
        return _poolLookup.ContainsKey(questId);
    }

    public void LoadFromDB()
    {
        var oldMSTime = Time.MSTime;
        Dictionary<uint, Tuple<List<QuestPool>, int>> lookup = new(); // poolId -> (list, index)

        _poolLookup.Clear();
        _dailyPools.Clear();
        _weeklyPools.Clear();
        _monthlyPools.Clear();

        // load template data from world DB
        {
            var result = _worldDatabase.Query("SELECT qpm.questId, qpm.poolId, qpm.poolIndex, qpt.numActive FROM quest_pool_members qpm LEFT JOIN quest_pool_template qpt ON qpm.poolId = qpt.poolId");

            if (result.IsEmpty())
            {
                Log.Logger.Information("Loaded 0 quest pools. DB table `quest_pool_members` is empty.");

                return;
            }

            do
            {
                if (result.IsNull(2))
                {
                    Log.Logger.Error($"Table `quest_pool_members` contains reference to non-existing pool {result.Read<uint>(1)}. Skipped.");

                    continue;
                }

                var questId = result.Read<uint>(0);
                var poolId = result.Read<uint>(1);
                var poolIndex = result.Read<uint>(2);
                var numActive = result.Read<uint>(3);

                var quest = _objectManager.GetQuestTemplate(questId);

                if (quest == null)
                {
                    Log.Logger.Error("Table `quest_pool_members` contains reference to non-existing quest {0}. Skipped.", questId);

                    continue;
                }

                if (!quest.IsDailyOrWeekly && !quest.IsMonthly)
                {
                    Log.Logger.Error("Table `quest_pool_members` contains reference to quest {0}, which is neither daily, weekly nor monthly. Skipped.", questId);

                    continue;
                }

                if (!lookup.ContainsKey(poolId))
                {
                    var poolList = quest.IsDaily ? _dailyPools : quest.IsWeekly ? _weeklyPools : _monthlyPools;

                    poolList.Add(new QuestPool()
                    {
                        PoolId = poolId,
                        NumActive = numActive
                    });

                    lookup.Add(poolId, new Tuple<List<QuestPool>, int>(poolList, poolList.Count - 1));
                }

                var pair = lookup[poolId];

                var members = pair.Item1[pair.Item2].Members;
                members.Add(poolIndex, questId);
            } while (result.NextRow());
        }

        // load saved spawns from character DB
        {
            var result = _characterDatabase.Query("SELECT pool_id, quest_id FROM pool_quest_save");

            if (!result.IsEmpty())
            {
                List<uint> unknownPoolIds = new();

                do
                {
                    var poolId = result.Read<uint>(0);
                    var questId = result.Read<uint>(1);

                    if (!lookup.TryGetValue(poolId, out var it))
                    {
                        Log.Logger.Error("Table `pool_quest_save` contains reference to non-existant quest pool {0}. Deleted.", poolId);
                        unknownPoolIds.Add(poolId);

                        continue;
                    }

                    it.Item1[it.Item2].ActiveQuests.Add(questId);
                } while (result.NextRow());

                var trans0 = new SQLTransaction();

                foreach (var poolId in unknownPoolIds)
                {
                    var stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_POOL_QUEST_SAVE);
                    stmt.AddValue(0, poolId);
                    trans0.Append(stmt);
                }

                _characterDatabase.CommitTransaction(trans0);
            }
        }

        // post-processing and sanity checks
        var trans = new SQLTransaction();

        foreach (var pair in lookup)
        {
            if (pair.Value.Item1 == null)
                continue;

            var pool = pair.Value.Item1[pair.Value.Item2];

            if (pool.Members.Count < pool.NumActive)
            {
                Log.Logger.Error($"Table `quest_pool_template` contains quest pool {pool.PoolId} requesting {pool.NumActive} spawns, but only has {pool.Members.Count} members. Requested spawns reduced.");
                pool.NumActive = (uint)pool.Members.Count;
            }

            var doRegenerate = pool.ActiveQuests.Empty();

            if (!doRegenerate)
            {
                List<uint> accountedFor = new();
                uint activeCount = 0;

                for (var i = (uint)pool.Members.Count; i-- != 0;)
                {
                    var member = pool.Members[i];

                    if (member.Empty())
                    {
                        Log.Logger.Error($"Table `quest_pool_members` contains no entries at index {i} for quest pool {pool.PoolId}. Index removed.");
                        pool.Members.Remove(i);

                        continue;
                    }

                    // check if the first member is active
                    var status = pool.ActiveQuests.Contains(member[0]);

                    // temporarily remove any spawns that are accounted for
                    if (status)
                    {
                        accountedFor.Add(member[0]);
                        pool.ActiveQuests.Remove(member[0]);
                    }

                    // now check if all other members also have the same status, and warn if not
                    foreach (var id in member)
                    {
                        var otherStatus = pool.ActiveQuests.Contains(id);

                        if (status != otherStatus)
                            Log.Logger.Warning($"Table `pool_quest_save` {(status ? "does not have" : "has")} quest {id} (in pool {pool.PoolId}, index {i}) saved, but its index is{(status ? "" : " not")} " +
                                               $"active (because quest {member[0]} is{(status ? "" : " not")} in the table). Set quest {id} to {(status ? "" : "in")}active.");

                        if (otherStatus)
                            pool.ActiveQuests.Remove(id);

                        if (status)
                            accountedFor.Add(id);
                    }

                    if (status)
                        ++activeCount;
                }

                // warn for any remaining active spawns (not part of the pool)
                foreach (var quest in pool.ActiveQuests)
                    Log.Logger.Warning($"Table `pool_quest_save` has saved quest {quest} for pool {pool.PoolId}, but that quest is not part of the pool. Skipped.");

                // only the previously-found spawns should actually be active
                pool.ActiveQuests = accountedFor;

                if (activeCount != pool.NumActive)
                {
                    doRegenerate = true;
                    Log.Logger.Error($"Table `pool_quest_save` has {activeCount} active members saved for pool {pool.PoolId}, which requests {pool.NumActive} active members. Pool spawns re-generated.");
                }
            }

            if (doRegenerate)
            {
                RegeneratePool(pool);
                SaveToDB(pool, trans);
            }

            foreach (var memberKey in pool.Members.Keys)
            {
                foreach (var quest in pool.Members[memberKey])
                {
                    if (_poolLookup.ContainsKey(quest))
                    {
                        Log.Logger.Error($"Table `quest_pool_members` lists quest {quest} as member of pool {pool.PoolId}, but it is already a member of pool {_poolLookup[quest].PoolId}. Skipped.");

                        continue;
                    }

                    _poolLookup[quest] = pool;
                }
            }
        }

        _characterDatabase.CommitTransaction(trans);

        Log.Logger.Information($"Loaded {_dailyPools.Count} daily, {_weeklyPools.Count} weekly and {_monthlyPools.Count} monthly quest pools in {Time.GetMSTimeDiffToNow(oldMSTime)} ms");
    }

    public void SaveToDB(QuestPool pool, SQLTransaction trans)
    {
        var delStmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_POOL_QUEST_SAVE);
        delStmt.AddValue(0, pool.PoolId);
        trans.Append(delStmt);

        foreach (var questId in pool.ActiveQuests)
        {
            var insStmt = _characterDatabase.GetPreparedStatement(CharStatements.INS_POOL_QUEST_SAVE);
            insStmt.AddValue(0, pool.PoolId);
            insStmt.AddValue(1, questId);
            trans.Append(insStmt);
        }
    }

    private void Regenerate(List<QuestPool> pools)
    {
        var trans = new SQLTransaction();

        foreach (var pool in pools)
        {
            RegeneratePool(pool);
            SaveToDB(pool, trans);
        }

        _characterDatabase.CommitTransaction(trans);
    }
}