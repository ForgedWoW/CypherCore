// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Entities.Units;
using Framework.Constants;
using Serilog;

namespace Forged.MapServer.AI.ScriptedAI;

internal class FollowerAI : ScriptedAI
{
    private ObjectGuid _leaderGUID;
    private uint _updateFollowTimer;
    private FollowState _followState;
    private uint _questForFollow;

    public FollowerAI(Creature creature) : base(creature)
    {
        _updateFollowTimer = 2500;
        _followState = FollowState.None;
    }

    public override void MoveInLineOfSight(Unit who)
    {
        if (HasFollowState(FollowState.Inprogress) && !ShouldAssistPlayerInCombatAgainst(who))
            return;

        base.MoveInLineOfSight(who);
    }

    public override void JustDied(Unit killer)
    {
        if (!HasFollowState(FollowState.Inprogress) || _leaderGUID.IsEmpty || _questForFollow == 0)
            return;

        // @todo need a better check for quests with time limit.
        var player = GetLeaderForFollower();

        if (player)
        {
            var group = player.Group;

            if (group)
                for (var groupRef = group.FirstMember; groupRef != null; groupRef = groupRef.Next())
                {
                    var member = groupRef.Source;

                    if (member)
                        if (member.Location.IsInMap(player))
                            member.FailQuest(_questForFollow);
                }
            else
                player.FailQuest(_questForFollow);
        }
    }

    public override void JustReachedHome()
    {
        if (!HasFollowState(FollowState.Inprogress))
            return;

        var player = GetLeaderForFollower();

        if (player != null)
        {
            if (HasFollowState(FollowState.Paused))
                return;

            Me.MotionMaster.MoveFollow(player, SharedConst.PetFollowDist, SharedConst.PetFollowAngle);
        }
        else
        {
            Me.DespawnOrUnsummon();
        }
    }

    public override void OwnerAttackedBy(Unit attacker)
    {
        if (!Me.HasReactState(ReactStates.Passive) && ShouldAssistPlayerInCombatAgainst(attacker))
            Me.EngageWithTarget(attacker);
    }

    public override void UpdateAI(uint uiDiff)
    {
        if (HasFollowState(FollowState.Inprogress) && !Me.IsEngaged)
        {
            if (_updateFollowTimer <= uiDiff)
            {
                if (HasFollowState(FollowState.Complete) && !HasFollowState(FollowState.PostEvent))
                {
                    Log.Logger.Debug($"FollowerAI::UpdateAI: is set completed, despawns. ({Me.GUID})");
                    Me.DespawnOrUnsummon();

                    return;
                }

                var maxRangeExceeded = true;
                var questAbandoned = (_questForFollow != 0);

                var player = GetLeaderForFollower();

                if (player)
                {
                    var group = player.Group;

                    if (group)
                    {
                        for (var groupRef = group.FirstMember; groupRef != null && (maxRangeExceeded || questAbandoned); groupRef = groupRef.Next())
                        {
                            var member = groupRef.Source;

                            if (member == null)
                                continue;

                            if (maxRangeExceeded && Me.Location.IsWithinDistInMap(member, 100.0f))
                                maxRangeExceeded = false;

                            if (questAbandoned)
                            {
                                var status = member.GetQuestStatus(_questForFollow);

                                if ((status == QuestStatus.Complete) || (status == QuestStatus.Incomplete))
                                    questAbandoned = false;
                            }
                        }
                    }
                    else
                    {
                        if (Me.Location.IsWithinDistInMap(player, 100.0f))
                            maxRangeExceeded = false;

                        if (questAbandoned)
                        {
                            var status = player.GetQuestStatus(_questForFollow);

                            if ((status == QuestStatus.Complete) || (status == QuestStatus.Incomplete))
                                questAbandoned = false;
                        }
                    }
                }

                if (maxRangeExceeded || questAbandoned)
                {
                    Log.Logger.Debug($"FollowerAI::UpdateAI: failed because player/group was to far away or not found ({Me.GUID})");
                    Me.DespawnOrUnsummon();

                    return;
                }

                _updateFollowTimer = 1000;
            }
            else
            {
                _updateFollowTimer -= uiDiff;
            }
        }

        UpdateFollowerAI(uiDiff);
    }

    public void StartFollow(Player player, uint factionForFollower = 0, Quest.Quest quest = null)
    {
        var cdata = Me.CreatureData;

        if (cdata != null)
            if (GetDefaultValue("Respawn.DynamicEscortNPC", false) && cdata.SpawnGroupData.Flags.HasFlag(SpawnGroupFlags.EscortQuestNpc))
                Me.SaveRespawnTime(Me.RespawnDelay);

        if (Me.IsEngaged)
        {
            Log.Logger.Debug($"FollowerAI::StartFollow: attempt to StartFollow while in combat. ({Me.GUID})");

            return;
        }

        if (HasFollowState(FollowState.Inprogress))
        {
            Log.Logger.Error($"FollowerAI::StartFollow: attempt to StartFollow while already following. ({Me.GUID})");

            return;
        }

        //set variables
        _leaderGUID = player.GUID;

        if (factionForFollower != 0)
            Me.Faction = factionForFollower;

        _questForFollow = quest.Id;

        Me.MotionMaster.Clear(MovementGeneratorPriority.Normal);
        Me.PauseMovement();

        Me.ReplaceAllNpcFlags(NPCFlags.None);
        Me.ReplaceAllNpcFlags2(NPCFlags2.None);

        AddFollowState(FollowState.Inprogress);

        Me.MotionMaster.MoveFollow(player, SharedConst.PetFollowDist, SharedConst.PetFollowAngle);

        Log.Logger.Debug($"FollowerAI::StartFollow: start follow {player.GetName()} - {_leaderGUID} ({Me.GUID})");
    }

    public void SetFollowPaused(bool paused)
    {
        if (!HasFollowState(FollowState.Inprogress) || HasFollowState(FollowState.Complete))
            return;

        if (paused)
        {
            AddFollowState(FollowState.Paused);

            if (Me.HasUnitState(UnitState.Follow))
                Me.MotionMaster.Remove(MovementGeneratorType.Follow);
        }
        else
        {
            RemoveFollowState(FollowState.Paused);

            var leader = GetLeaderForFollower();

            if (leader != null)
                Me.MotionMaster.MoveFollow(leader, SharedConst.PetFollowDist, SharedConst.PetFollowAngle);
        }
    }

    public void SetFollowComplete(bool withEndEvent = false)
    {
        if (Me.HasUnitState(UnitState.Follow))
            Me.MotionMaster.Remove(MovementGeneratorType.Follow);

        if (withEndEvent)
        {
            AddFollowState(FollowState.PostEvent);
        }
        else
        {
            if (HasFollowState(FollowState.PostEvent))
                RemoveFollowState(FollowState.PostEvent);
        }

        AddFollowState(FollowState.Complete);
    }

    public override bool IsEscorted()
    {
        return HasFollowState(FollowState.Inprogress);
    }

    private void UpdateFollowerAI(uint diff)
    {
        if (!UpdateVictim())
            return;

        DoMeleeAttackIfReady();
    }

    private Player GetLeaderForFollower()
    {
        var player = Global.ObjAccessor.GetPlayer(Me, _leaderGUID);

        if (player)
        {
            if (player.IsAlive)
            {
                return player;
            }
            else
            {
                var group = player.Group;

                if (group)
                    for (var groupRef = group.FirstMember; groupRef != null; groupRef = groupRef.Next())
                    {
                        var member = groupRef.Source;

                        if (member && Me.Location.IsWithinDistInMap(member, 100.0f) && member.IsAlive)
                        {
                            Log.Logger.Debug($"FollowerAI::GetLeaderForFollower: GetLeader changed and returned new leader. ({Me.GUID})");
                            _leaderGUID = member.GUID;

                            return member;
                        }
                    }
            }
        }

        Log.Logger.Debug($"FollowerAI::GetLeaderForFollower: GetLeader can not find suitable leader. ({Me.GUID})");

        return null;
    }

    //This part provides assistance to a player that are attacked by who, even if out of normal aggro range
    //It will cause me to attack who that are attacking _any_ player (which has been confirmed may happen also on offi)
    //The flag (type_flag) is unconfirmed, but used here for further research and is a good candidate.
    private bool ShouldAssistPlayerInCombatAgainst(Unit who)
    {
        if (!who || !who.Victim)
            return false;

        //experimental (unknown) flag not present
        if (!Me.Template.TypeFlags.HasAnyFlag(CreatureTypeFlags.CanAssist))
            return false;

        if (!who.IsInAccessiblePlaceFor(Me))
            return false;

        if (!CanAIAttack(who))
            return false;

        // we cannot attack in evade mode
        if (Me.IsInEvadeMode)
            return false;

        // or if enemy is in evade mode
        if (who.TypeId == TypeId.Unit && who.AsCreature.IsInEvadeMode)
            return false;

        //never attack friendly
        if (Me.IsFriendlyTo(who))
            return false;

        //too far away and no free sight?
        if (!Me.Location.IsWithinDistInMap(who, 100.0f) || !Me.Location.IsWithinLOSInMap(who))
            return false;

        return true;
    }

    private bool HasFollowState(FollowState uiFollowState)
    {
        return (_followState & uiFollowState) != 0;
    }

    private void AddFollowState(FollowState uiFollowState)
    {
        _followState |= uiFollowState;
    }

    private void RemoveFollowState(FollowState uiFollowState)
    {
        _followState &= ~uiFollowState;
    }
}