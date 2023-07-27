// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using System.Linq;
using Forged.MapServer.Achievements;
using Forged.MapServer.Arenas;
using Forged.MapServer.Chat;
using Forged.MapServer.Conditions;
using Forged.MapServer.DataStorage;
using Forged.MapServer.Globals;
using Forged.MapServer.Globals.Caching;
using Forged.MapServer.Maps;
using Forged.MapServer.Networking;
using Forged.MapServer.Phasing;
using Forged.MapServer.Spells;
using Forged.MapServer.World;
using Framework.Constants;
using Microsoft.Extensions.Configuration;

namespace Forged.MapServer.Scenarios;

public class InstanceScenario : Scenario
{
    private readonly InstanceMap _map;
    private readonly MapSpawnGroupCache _mapSpawnGroupCache;

    public InstanceScenario(ScenarioData scenarioData, ObjectAccessor objectAccessor, CriteriaManager criteriaManager, WorldManager worldManager, GameObjectManager gameObjectManager, 
                            SpellManager spellManager, ArenaTeamManager arenaTeamManager, DisableManager disableManager, WorldStateManager worldStateManager, CliDB cliDB, 
                            ConditionManager conditionManager, RealmManager realmManager, IConfiguration configuration, LanguageManager languageManager, DB2Manager db2Manager, 
                            MapManager mapManager, AchievementGlobalMgr achievementManager, InstanceMap map, PhasingHandler phasingHandler, ItemTemplateCache itemTemplateCache,
                            MapSpawnGroupCache mapSpawnGroupCache) :
        base(scenarioData, 
             objectAccessor, 
             criteriaManager, 
             worldManager, 
             gameObjectManager, 
             spellManager, 
             arenaTeamManager, 
             disableManager, 
             worldStateManager, cliDB, conditionManager, realmManager, configuration, languageManager, db2Manager, mapManager, achievementManager, phasingHandler, itemTemplateCache, map)
    {
        _map = map;
        _mapSpawnGroupCache = mapSpawnGroupCache;
        LoadInstanceData();

        var players = map.Players;

        foreach (var player in players)
            SendScenarioState(player);
    }

    public override string GetOwnerInfo()
    {
        return $"Instance ID {_map.InstanceId}";
    }

    public override void SendPacket(ServerPacket data)
    {
        _map?.SendToPlayers(data);
    }

    private void LoadInstanceData()
    {
        var instanceScript = _map.InstanceScript;

        if (instanceScript == null)
            return;

        List<CriteriaTree> criteriaTrees = new();

        var killCreatureCriteria = CriteriaManager.GetScenarioCriteriaByTypeAndScenario(CriteriaType.KillCreature, Data.Entry.Id);

        if (!killCreatureCriteria.Empty())
        {
            var spawnGroups = GameObjectManager.GetInstanceSpawnGroupsForMap(_map.Id);

            if (spawnGroups != null)
            {
                Dictionary<uint, ulong> despawnedCreatureCountsById = new();

                foreach (var spawnGroup in spawnGroups)
                {
                    if (instanceScript.GetBossState(spawnGroup.BossStateId) != EncounterState.Done)
                        continue;

                    var isDespawned = ((1 << (int)EncounterState.Done) & spawnGroup.BossStates) == 0 || spawnGroup.Flags.HasFlag(InstanceSpawnGroupFlags.BlockSpawn);

                    if (!isDespawned)
                        continue;

                    foreach (var spawnData in _mapSpawnGroupCache.SpawnGroupMapStorage.LookupByKey(spawnGroup.SpawnGroupId).Select(spawn => spawn.ToSpawnData()).Where(spawnData => spawnData != null))
                        ++despawnedCreatureCountsById[spawnData.Id];
                }

                foreach (var criteria in killCreatureCriteria)
                {
                    // count creatures in despawned spawn groups
                    if (!despawnedCreatureCountsById.TryGetValue(criteria.Entry.Asset, out var progress))
                        continue;

                    SetCriteriaProgress(criteria, progress, null);
                    var trees = CriteriaManager.GetCriteriaTreesByCriteria(criteria.Id);

                    if (trees == null)
                        continue;

                    criteriaTrees.AddRange(trees);
                }
            }
        }

        foreach (var criteria in CriteriaManager.GetScenarioCriteriaByTypeAndScenario(CriteriaType.DefeatDungeonEncounter, Data.Entry.Id))
        {
            if (!instanceScript.IsEncounterCompleted(criteria.Entry.Asset))
                continue;

            SetCriteriaProgress(criteria, 1, null);
            var trees = CriteriaManager.GetCriteriaTreesByCriteria(criteria.Id);

            if (trees == null)
                continue;

            criteriaTrees.AddRange(trees);
        }

        foreach (var tree in criteriaTrees)
        {
            var step = tree.ScenarioStep;

            if (step == null)
                continue;

            if (IsCompletedCriteriaTree(tree))
                SetStepState(step, ScenarioStepState.Done);
        }
    }
}