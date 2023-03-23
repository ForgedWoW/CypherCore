// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using System.Linq;
using Framework.Configuration;
using Framework.Database;
using Game.Common.Entities.Creatures;
using Game.Entities;

namespace Game.Common.Entities.Creatures;

public class FormationMgr
{
	static readonly Dictionary<ulong, FormationInfo> CreatureGroupMap = new();

	public static void AddCreatureToGroup(ulong leaderSpawnId, Creature creature)
	{
		var map = creature.Map;

		var creatureGroup = map.CreatureGroupHolder.LookupByKey(leaderSpawnId);

		if (creatureGroup != null)
		{
			//Add member to an existing group
			Log.outDebug(LogFilter.Unit, "Group found: {0}, inserting creature GUID: {1}, Group InstanceID {2}", leaderSpawnId, creature.GUID.ToString(), creature.InstanceId);

			// With dynamic spawn the creature may have just respawned
			// we need to find previous instance of creature and delete it from the formation, as it'll be invalidated
			var bounds = map.CreatureBySpawnIdStore.LookupByKey(creature.SpawnId);

			foreach (var other in bounds)
			{
				if (other == creature)
					continue;

				if (creatureGroup.HasMember(other))
					creatureGroup.RemoveMember(other);
			}

			creatureGroup.AddMember(creature);
		}
		else
		{
			//Create new group
			Log.outDebug(LogFilter.Unit, "Group not found: {0}. Creating new group.", leaderSpawnId);
			CreatureGroup group = new(leaderSpawnId);
			map.CreatureGroupHolder[leaderSpawnId] = group;
			group.AddMember(creature);
		}
	}

	public static void RemoveCreatureFromGroup(CreatureGroup group, Creature member)
	{
		Log.outDebug(LogFilter.Unit, "Deleting member GUID: {0} from group {1}", group.LeaderSpawnId, member.SpawnId);
		group.RemoveMember(member);

		if (group.IsEmpty)
		{
			var map = member.Map;

			Log.outDebug(LogFilter.Unit, "Deleting group with InstanceID {0}", member.InstanceId);
			map.CreatureGroupHolder.Remove(group.LeaderSpawnId);
		}
	}

	public static void LoadCreatureFormations()
	{
		var oldMSTime = Time.MSTime;

		//Get group data
		var result = DB.World.Query("SELECT leaderGUID, memberGUID, dist, angle, groupAI, point_1, point_2 FROM creature_formations ORDER BY leaderGUID");

		if (result.IsEmpty())
		{
			Log.outInfo(LogFilter.ServerLoading, "Loaded 0 creatures in formations. DB table `creature_formations` is empty!");

			return;
		}

		uint count = 0;
		List<ulong> leaderSpawnIds = new();

		do
		{
			//Load group member data
			FormationInfo member = new();
			member.LeaderSpawnId = result.Read<ulong>(0);
			var memberSpawnId = result.Read<ulong>(1);
			member.FollowDist = 0f;
			member.FollowAngle = 0f;

			//If creature is group leader we may skip loading of dist/angle
			if (member.LeaderSpawnId != memberSpawnId)
			{
				member.FollowDist = result.Read<float>(2);
				member.FollowAngle = result.Read<float>(3) * MathFunctions.PI / 180;
			}

			member.GroupAi = result.Read<uint>(4);

			for (var i = 0; i < 2; ++i)
				member.LeaderWaypointIDs[i] = result.Read<ushort>(5 + i);

			// check data correctness
			{
				if (Global.ObjectMgr.GetCreatureData(member.LeaderSpawnId) == null)
				{
					if (ConfigMgr.GetDefaultValue("load.autoclean", false))
						DB.World.Execute($"DELETE FROM creature_formations WHERE leaderGUID = {member.LeaderSpawnId}");
					else
						Log.outError(LogFilter.Sql, $"creature_formations table leader guid {member.LeaderSpawnId} incorrect (not exist)");

					continue;
				}

				if (Global.ObjectMgr.GetCreatureData(memberSpawnId) == null)
				{
					if (ConfigMgr.GetDefaultValue("load.autoclean", false))
						DB.World.Execute($"DELETE FROM creature_formations WHERE memberGUID = {memberSpawnId}");
					else
						Log.outError(LogFilter.Sql, $"creature_formations table member guid {memberSpawnId} incorrect (not exist)");

					continue;
				}

				leaderSpawnIds.Add(member.LeaderSpawnId);
			}

			CreatureGroupMap.Add(memberSpawnId, member);
			++count;
		} while (result.NextRow());

		foreach (var leaderSpawnId in leaderSpawnIds)
			if (!CreatureGroupMap.ContainsKey(leaderSpawnId))
			{
				Log.outError(LogFilter.Sql, $"creature_formation contains leader spawn {leaderSpawnId} which is not included on its formation, removing");

				foreach (var itr in CreatureGroupMap.ToList())
					if (itr.Value.LeaderSpawnId == leaderSpawnId)
						CreatureGroupMap.Remove(itr.Key);
			}

		Log.outInfo(LogFilter.ServerLoading, "Loaded {0} creatures in formations in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
	}

	public static FormationInfo GetFormationInfo(ulong spawnId)
	{
		return CreatureGroupMap.LookupByKey(spawnId);
	}

	public static void AddFormationMember(ulong spawnId, float followAng, float followDist, ulong leaderSpawnId, uint groupAI)
	{
		FormationInfo member = new();
		member.LeaderSpawnId = leaderSpawnId;
		member.FollowDist = followDist;
		member.FollowAngle = followAng;
		member.GroupAi = groupAI;

		for (var i = 0; i < 2; ++i)
			member.LeaderWaypointIDs[i] = 0;

		CreatureGroupMap.Add(spawnId, member);
	}
}
