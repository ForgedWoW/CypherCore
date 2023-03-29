// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using System.Linq;
using Forged.MapServer.Globals;
using Framework.Database;
using Framework.Util;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Forged.MapServer.Entities.Creatures;

public class FormationMgr
{
    private readonly IConfiguration _configuration;
    private readonly WorldDatabase _worldDatabase;
    private readonly GameObjectManager _objectManager;
    private readonly Dictionary<ulong, FormationInfo> _creatureGroupMap = new();

    public FormationMgr(IConfiguration configuration, WorldDatabase worldDatabase, GameObjectManager objectManager)
    {
        _configuration = configuration;
        _worldDatabase = worldDatabase;
        _objectManager = objectManager;
    }

    public void AddCreatureToGroup(ulong leaderSpawnId, Creature creature)
    {
        var map = creature.Location.Map;

        var creatureGroup = map.CreatureGroupHolder.LookupByKey(leaderSpawnId);

        if (creatureGroup != null)
        {
            //Add member to an existing group
            Log.Logger.Debug("Group found: {0}, inserting creature GUID: {1}, Group InstanceID {2}", leaderSpawnId, creature.GUID.ToString(), creature.InstanceId);

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
            Log.Logger.Debug("Group not found: {0}. Creating new group.", leaderSpawnId);
            CreatureGroup group = new(leaderSpawnId);
            map.CreatureGroupHolder[leaderSpawnId] = group;
            group.AddMember(creature);
        }
    }

    public void RemoveCreatureFromGroup(CreatureGroup group, Creature member)
    {
        Log.Logger.Debug("Deleting member GUID: {0} from group {1}", group.LeaderSpawnId, member.SpawnId);
        group.RemoveMember(member);

        if (group.IsEmpty)
        {
            var map = member.Location.Map;

            Log.Logger.Debug("Deleting group with InstanceID {0}", member.InstanceId);
            map.CreatureGroupHolder.Remove(group.LeaderSpawnId);
        }
    }

    public void LoadCreatureFormations()
    {
        var oldMSTime = Time.MSTime;

        //Get group data
        var result = _worldDatabase.Query("SELECT leaderGUID, memberGUID, dist, angle, groupAI, point_1, point_2 FROM creature_formations ORDER BY leaderGUID");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 creatures in formations. DB table `creature_formations` is empty!");

            return;
        }

        uint count = 0;
        List<ulong> leaderSpawnIds = new();

        do
        {
            //Load group member data
            FormationInfo member = new()
            {
                LeaderSpawnId = result.Read<ulong>(0)
            };

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
                if (_objectManager.GetCreatureData(member.LeaderSpawnId) == null)
                {
                    if (_configuration.GetDefaultValue("load.autoclean", false))
                        _worldDatabase.Execute($"DELETE FROM creature_formations WHERE leaderGUID = {member.LeaderSpawnId}");
                    else
                        Log.Logger.Error($"creature_formations table leader guid {member.LeaderSpawnId} incorrect (not exist)");

                    continue;
                }

                if (_objectManager.GetCreatureData(memberSpawnId) == null)
                {
                    if (_configuration.GetDefaultValue("load.autoclean", false))
                        _worldDatabase.Execute($"DELETE FROM creature_formations WHERE memberGUID = {memberSpawnId}");
                    else
                        Log.Logger.Error($"creature_formations table member guid {memberSpawnId} incorrect (not exist)");

                    continue;
                }

                leaderSpawnIds.Add(member.LeaderSpawnId);
            }

            _creatureGroupMap.Add(memberSpawnId, member);
            ++count;
        } while (result.NextRow());

        foreach (var leaderSpawnId in leaderSpawnIds)
            if (!_creatureGroupMap.ContainsKey(leaderSpawnId))
            {
                Log.Logger.Error($"creature_formation contains leader spawn {leaderSpawnId} which is not included on its formation, removing");

                foreach (var itr in _creatureGroupMap.ToList())
                    if (itr.Value.LeaderSpawnId == leaderSpawnId)
                        _creatureGroupMap.Remove(itr.Key);
            }

        Log.Logger.Information("Loaded {0} creatures in formations in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
    }

    public FormationInfo GetFormationInfo(ulong spawnId)
    {
        return _creatureGroupMap.LookupByKey(spawnId);
    }

    public void AddFormationMember(ulong spawnId, float followAng, float followDist, ulong leaderSpawnId, uint groupAI)
    {
        FormationInfo member = new()
        {
            LeaderSpawnId = leaderSpawnId,
            FollowDist = followDist,
            FollowAngle = followAng,
            GroupAi = groupAI
        };

        for (var i = 0; i < 2; ++i)
            member.LeaderWaypointIDs[i] = 0;

        _creatureGroupMap.Add(spawnId, member);
    }
}