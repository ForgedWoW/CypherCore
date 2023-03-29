// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Forged.RealmServer.Achievements;
using Forged.RealmServer.Maps;
using Forged.RealmServer.Networking;

namespace Forged.RealmServer.Scenarios;

public class InstanceScenario : Scenario
{
	readonly InstanceMap _map;

	public InstanceScenario(InstanceMap map, ScenarioData scenarioData) : base(scenarioData)
	{
		_map = map;

		//ASSERT(_map);
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
		//Hack  todo fix me
		if (_map == null)
			return;

		_map.SendToPlayers(data);
	}

	void LoadInstanceData()
	{
		var instanceScript = _map.InstanceScript;

		if (instanceScript == null)
			return;

		List<CriteriaTree> criteriaTrees = new();

		var killCreatureCriteria = Global.CriteriaMgr.GetScenarioCriteriaByTypeAndScenario(CriteriaType.KillCreature, _data.Entry.Id);

		if (!killCreatureCriteria.Empty())
		{
			var spawnGroups = _gameObjectManager.GetInstanceSpawnGroupsForMap(_map.Id);

			if (spawnGroups != null)
			{
				Dictionary<uint, ulong> despawnedCreatureCountsById = new();

				foreach (var spawnGroup in spawnGroups)
				{
					if (instanceScript.GetBossState(spawnGroup.BossStateId) != EncounterState.Done)
						continue;

					var isDespawned = ((1 << (int)EncounterState.Done) & spawnGroup.BossStates) == 0 || spawnGroup.Flags.HasFlag(InstanceSpawnGroupFlags.BlockSpawn);

					if (isDespawned)
						foreach (var spawn in _gameObjectManager.GetSpawnMetadataForGroup(spawnGroup.SpawnGroupId))
						{
							var spawnData = spawn.ToSpawnData();

							if (spawnData != null)
								++despawnedCreatureCountsById[spawnData.Id];
						}
				}

				foreach (var criteria in killCreatureCriteria)
				{
					// count creatures in despawned spawn groups
					var progress = despawnedCreatureCountsById.LookupByKey(criteria.Entry.Asset);

					if (progress != 0)
					{
						SetCriteriaProgress(criteria, progress, null, ProgressType.Set);
						var trees = Global.CriteriaMgr.GetCriteriaTreesByCriteria(criteria.Id);

						if (trees != null)
							foreach (var tree in trees)
								criteriaTrees.Add(tree);
					}
				}
			}
		}

		foreach (var criteria in Global.CriteriaMgr.GetScenarioCriteriaByTypeAndScenario(CriteriaType.DefeatDungeonEncounter, _data.Entry.Id))
		{
			if (!instanceScript.IsEncounterCompleted(criteria.Entry.Asset))
				continue;

			SetCriteriaProgress(criteria, 1, null, ProgressType.Set);
			var trees = Global.CriteriaMgr.GetCriteriaTreesByCriteria(criteria.Id);

			if (trees != null)
				foreach (var tree in trees)
					criteriaTrees.Add(tree);
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