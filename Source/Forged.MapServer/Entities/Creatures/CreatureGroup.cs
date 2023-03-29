// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.Entities.Units;
using Framework.Constants;
using Serilog;

namespace Forged.MapServer.Entities.Creatures;

public class CreatureGroup
{
    private readonly Dictionary<Creature, FormationInfo> _members = new();
    private readonly ulong _leaderSpawnId;
    private readonly bool _formed;
    private Creature _leader;
    private bool _engaging;


    public Creature Leader => _leader;

    public ulong LeaderSpawnId => _leaderSpawnId;

    public bool IsEmpty => _members.Empty();

    public bool IsFormed => _formed;

    public CreatureGroup(ulong leaderSpawnId)
    {
        _leaderSpawnId = leaderSpawnId;
    }

    public void AddMember(Creature member)
    {
        Log.Logger.Debug("CreatureGroup.AddMember: Adding {0}.", member.GUID.ToString());

        //Check if it is a leader
        if (member.SpawnId == _leaderSpawnId)
        {
            Log.Logger.Debug("{0} is formation leader. Adding group.", member.GUID.ToString());
            _leader = member;
        }

        // formation must be registered at this point
        var formationInfo = FormationMgr.GetFormationInfo(member.SpawnId);
        _members.Add(member, formationInfo);
        member.Formation = this;
    }

    public void RemoveMember(Creature member)
    {
        if (_leader == member)
            _leader = null;

        _members.Remove(member);
        member.Formation = null;
    }

    public void MemberEngagingTarget(Creature member, Unit target)
    {
        // used to prevent recursive calls
        if (_engaging)
            return;

        var groupAI = (GroupAIFlags)FormationMgr.GetFormationInfo(member.SpawnId).GroupAi;

        if (groupAI == 0)
            return;

        if (member == _leader)
        {
            if (!groupAI.HasFlag(GroupAIFlags.MembersAssistLeader))
                return;
        }
        else if (!groupAI.HasFlag(GroupAIFlags.LeaderAssistsMember))
        {
            return;
        }

        _engaging = true;

        foreach (var pair in _members)
        {
            var other = pair.Key;

            // Skip self
            if (other == member)
                continue;

            if (!other.IsAlive)
                continue;

            if (((other != _leader && groupAI.HasFlag(GroupAIFlags.MembersAssistLeader)) || (other == _leader && groupAI.HasFlag(GroupAIFlags.LeaderAssistsMember))) && other.WorldObjectCombat.IsValidAttackTarget(target))
                other.EngageWithTarget(target);
        }

        _engaging = false;
    }

    public void FormationReset(bool dismiss)
    {
        foreach (var creature in _members.Keys)
            if (creature != _leader && creature.IsAlive)
                creature.MotionMaster.MoveIdle();

        //_formed = !dismiss;
    }

    public void LeaderStartedMoving()
    {
        if (_leader == null)
            return;

        foreach (var pair in _members)
        {
            var member = pair.Key;

            if (member == _leader || !member.IsAlive || member.IsEngaged || !pair.Value.GroupAi.HasAnyFlag((uint)GroupAIFlags.IdleInFormation))
                continue;

            var angle = pair.Value.FollowAngle + MathF.PI; // for some reason, someone thought it was a great idea to invert relativ angles...
            var dist = pair.Value.FollowDist;

            if (!member.HasUnitState(UnitState.FollowFormation))
                member.MotionMaster.MoveFormation(_leader, dist, angle, pair.Value.LeaderWaypointIDs[0], pair.Value.LeaderWaypointIDs[1]);
        }
    }

    public bool CanLeaderStartMoving()
    {
        foreach (var pair in _members)
            if (pair.Key != _leader && pair.Key.IsAlive)
                if (pair.Key.IsEngaged || pair.Key.IsReturningHome)
                    return false;

        return true;
    }

    public bool IsLeader(Creature creature)
    {
        return _leader == creature;
    }

    public bool HasMember(Creature member)
    {
        return _members.ContainsKey(member);
    }
}