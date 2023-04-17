// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Maps.Grids;
using Forged.MapServer.Movement;
using Framework.Constants;
using Framework.Util;
using Serilog;

namespace Forged.MapServer.AI.ScriptedAI;

public class EscortAI : ScriptedAI
{
    private readonly WaypointPath _path;

    private bool _activeAttacker;
    private bool _despawnAtEnd;
    private bool _despawnAtFar;
    private bool _ended;
    private Quest.Quest _escortQuest;
    private EscortState _escortState;
    private bool _hasImmuneToNPCFlags;
    private bool _instantRespawn;
    private bool _manualPath;
    private readonly float _maxPlayerDistance;
    private TimeSpan _pauseTimer;
    private uint _playerCheckTimer;

    private ObjectGuid _playerGUID;
    //generally passed in Start() when regular escort script.

    private bool _resume;

    // if creature should respawn instantly after escort over (if not, database respawntime are used)
    private bool _returnToStart;

    // obsolete, determined by faction.
    private bool _running; // all creatures are walking by default (has Id MOVEMENTFLAG_WALK)

    // if creature can walk same path (loop) without despawn. Not for regular escort quests.
    private bool _started;

    public EscortAI(Creature creature) : base(creature)
    {
        _pauseTimer = TimeSpan.FromSeconds(2.5);
        _playerCheckTimer = 1000;
        _maxPlayerDistance = 100;
        _activeAttacker = true;
        _despawnAtEnd = true;
        _despawnAtFar = true;

        _path = new WaypointPath();
    }

    public void AddWaypoint(uint id, float x, float y, float z, float orientation, TimeSpan waitTime)
    {
        x = GridDefines.NormalizeMapCoord(x);
        y = GridDefines.NormalizeMapCoord(y);

        WaypointNode waypoint = new()
        {
            ID = id,
            X = x,
            Y = y,
            Z = z,
            Orientation = orientation,
            MoveType = _running ? WaypointMoveType.Run : WaypointMoveType.Walk,
            Delay = (uint)waitTime.TotalMilliseconds,
            EventId = 0,
            EventChance = 100
        };

        _path.Nodes.Add(waypoint);

        _manualPath = true;
    }

    public override void EnterEvadeMode(EvadeReason why = EvadeReason.Other)
    {
        Me.RemoveAllAuras();
        Me.CombatStop(true);
        Me.SetTappedBy(null);

        EngagementOver();

        if (HasEscortState(EscortState.Escorting))
        {
            AddEscortState(EscortState.Returning);
            ReturnToLastPoint();
            Log.Logger.Debug($"EscortAI.EnterEvadeMode has left combat and is now returning to last point {Me.GUID}");
        }
        else
        {
            Me.MotionMaster.MoveTargetedHome();

            if (_hasImmuneToNPCFlags)
                Me.SetImmuneToNPC(true);

            Reset();
        }
    }

    public Player GetPlayerForEscort()
    {
        return Me.ObjectAccessor.GetPlayer(Me, _playerGUID);
    }

    public bool HasEscortState(EscortState escortState)
    {
        return (_escortState & escortState) != 0;
    }

    public override void InitializeAI()
    {
        _escortState = EscortState.None;

        if (!IsCombatMovementAllowed())
            SetCombatMovement(true);

        //add a small delay before going to first waypoint, normal in near all cases
        _pauseTimer = TimeSpan.FromSeconds(2);

        if (Me.Faction != Me.Template.Faction)
            Me.RestoreFaction();

        Reset();
    }

    public bool IsActiveAttacker()
    {
        return _activeAttacker;
    }

    public override bool IsEscorted()
    {
        return !_playerGUID.IsEmpty;
    }

    public override void JustDied(Unit killer)
    {
        if (!HasEscortState(EscortState.Escorting) || _playerGUID.IsEmpty || _escortQuest == null)
            return;

        var player = GetPlayerForEscort();

        if (player)
        {
            var group = player.Group;

            if (group)
                for (var groupRef = group.FirstMember; groupRef != null; groupRef = groupRef.Next())
                {
                    var member = groupRef.Source;

                    if (member)
                        if (member.Location.IsInMap(player))
                            member.FailQuest(_escortQuest.Id);
                }
            else
                player.FailQuest(_escortQuest.Id);
        }
    }

    public override void MoveInLineOfSight(Unit who)
    {
        if (who == null)
            return;

        if (HasEscortState(EscortState.Escorting) && AssistPlayerInCombatAgainst(who))
            return;

        base.MoveInLineOfSight(who);
    }

    public override void MovementInform(MovementGeneratorType moveType, uint id)
    {
        // no action allowed if there is no escort
        if (!HasEscortState(EscortState.Escorting))
            return;

        switch (moveType)
        {
            //Combat start position reached, continue waypoint movement
            case MovementGeneratorType.Point:
            {
                if (_pauseTimer == TimeSpan.Zero)
                    _pauseTimer = TimeSpan.FromSeconds(2);

                switch (id)
                {
                    case EscortPointIds.LAST_POINT:
                        Log.Logger.Debug($"EscortAI::MovementInform has returned to original position before combat ({Me.GUID})");

                        Me.SetWalk(!_running);
                        RemoveEscortState(EscortState.Returning);

                        break;

                    case EscortPointIds.HOME:
                        Log.Logger.Debug($"EscortAI::MovementInform: returned to home location and restarting waypoint path ({Me.GUID})");
                        _started = false;

                        break;
                }

                break;
            }
            case MovementGeneratorType.Waypoint:
            {
                var waypoint = _path.Nodes[(int)id];

                Log.Logger.Debug($"EscortAI::MovementInform: waypoint node {waypoint.ID} reached ({Me.GUID})");

                // last point
                if (id == _path.Nodes.Count - 1)
                {
                    _started = false;
                    _ended = true;
                    _pauseTimer = TimeSpan.FromSeconds(1);
                }

                break;
            }
        }
    }

    public void SetActiveAttacker(bool attack)
    {
        _activeAttacker = attack;
    }

    public void SetDespawnAtEnd(bool despawn)
    {
        _despawnAtEnd = despawn;
    }

    public void SetDespawnAtFar(bool despawn)
    {
        _despawnAtFar = despawn;
    }

    public void SetEscortPaused(bool on)
    {
        if (!HasEscortState(EscortState.Escorting))
            return;

        if (on)
        {
            AddEscortState(EscortState.Paused);
            var movementGenerator = Me.MotionMaster.GetCurrentMovementGenerator(MovementSlot.Default);

            movementGenerator?.Pause();
        }
        else
        {
            RemoveEscortState(EscortState.Paused);
            _resume = true;
        }
    }

    public void SetPauseTimer(TimeSpan timer)
    {
        _pauseTimer = timer;
    }

    public void SetRun(bool on = true)
    {
        if (on == _running)
            return;

        foreach (var node in _path.Nodes)
            node.MoveType = on ? WaypointMoveType.Run : WaypointMoveType.Walk;

        Me.SetWalk(!on);

        _running = on;
    }

    /// todo get rid of this many variables passed in function.
    public void Start(bool isActiveAttacker = true, bool run = false, ObjectGuid playerGUID = default, Quest.Quest quest = null, bool instantRespawn = false, bool canLoopPath = false, bool resetWaypoints = true)
    {
        // Queue respawn from the point it starts
        var cdata = Me.CreatureData;

        if (cdata != null)
            if (Me.Configuration.GetDefaultValue("Respawn:DynamicEscortNPC", false) && cdata.SpawnGroupData.Flags.HasFlag(SpawnGroupFlags.EscortQuestNpc))
                Me.SaveRespawnTime(Me.RespawnDelay);

        if (Me.IsEngaged)
        {
            Log.Logger.Error($"EscortAI::Start: (script: {Me.GetScriptName()} attempts to Start while in combat ({Me.GUID})");

            return;
        }

        if (HasEscortState(EscortState.Escorting))
        {
            Log.Logger.Error($"EscortAI::Start: (script: {Me.GetScriptName()} attempts to Start while already escorting ({Me.GUID})");

            return;
        }

        _running = run;

        if (!_manualPath && resetWaypoints)
            FillPointMovementListForCreature();

        if (_path.Nodes.Empty())
        {
            Log.Logger.Error($"EscortAI::Start: (script: {Me.GetScriptName()} starts with 0 waypoints (possible missing entry in script_waypoint. QuestId: {quest?.Id ?? 0} ({Me.GUID})");

            return;
        }

        // set variables
        _activeAttacker = isActiveAttacker;
        _playerGUID = playerGUID;
        _escortQuest = quest;
        _instantRespawn = instantRespawn;
        _returnToStart = canLoopPath;

        if (_returnToStart && _instantRespawn)
            Log.Logger.Error($"EscortAI::Start: (script: {Me.GetScriptName()} is set to return home after waypoint end and instant respawn at waypoint end. Creature will never despawn ({Me.GUID})");

        Me.MotionMaster.MoveIdle();
        Me.MotionMaster.Clear(MovementGeneratorPriority.Normal);

        //disable npcflags
        Me.ReplaceAllNpcFlags(NPCFlags.None);
        Me.ReplaceAllNpcFlags2(NPCFlags2.None);

        if (Me.IsImmuneToNPC())
        {
            _hasImmuneToNPCFlags = true;
            Me.SetImmuneToNPC(false);
        }

        Log.Logger.Debug($"EscortAI::Start: (script: {Me.GetScriptName()}, started with {_path.Nodes.Count} waypoints. ActiveAttacker = {_activeAttacker}, Run = {_running}, Player = {_playerGUID} ({Me.GUID})");

        // set initial speed
        Me.SetWalk(!_running);

        _started = false;
        AddEscortState(EscortState.Escorting);
    }

    public override void UpdateAI(uint diff)
    {
        //Waypoint Updating
        if (HasEscortState(EscortState.Escorting) && !Me.IsEngaged && !HasEscortState(EscortState.Returning))
        {
            if (_pauseTimer.TotalMilliseconds <= diff)
            {
                if (!HasEscortState(EscortState.Paused))
                {
                    _pauseTimer = TimeSpan.Zero;

                    if (_ended)
                    {
                        _ended = false;
                        Me.MotionMaster.MoveIdle();

                        if (_despawnAtEnd)
                        {
                            Log.Logger.Debug($"EscortAI::UpdateAI: reached end of waypoints, despawning at end ({Me.GUID})");

                            if (_returnToStart)
                            {
                                var respawnPosition = Me.RespawnPosition;
                                Me.MotionMaster.MovePoint(EscortPointIds.HOME, respawnPosition);
                                Log.Logger.Debug($"EscortAI::UpdateAI: returning to spawn location: {respawnPosition} ({Me.GUID})");
                            }
                            else if (_instantRespawn)
                                Me.Respawn();
                            else
                                Me.DespawnOrUnsummon();
                        }

                        Log.Logger.Debug($"EscortAI::UpdateAI: reached end of waypoints ({Me.GUID})");
                        RemoveEscortState(EscortState.Escorting);

                        return;
                    }

                    if (!_started)
                    {
                        _started = true;
                        Me.MotionMaster.MovePath(_path, false);
                    }
                    else if (_resume)
                    {
                        _resume = false;
                        var movementGenerator = Me.MotionMaster.GetCurrentMovementGenerator(MovementSlot.Default);

                        movementGenerator?.Resume();
                    }
                }
            }
            else
                _pauseTimer -= TimeSpan.FromMilliseconds(diff);
        }

        //Check if player or any member of his group is within range
        if (_despawnAtFar && HasEscortState(EscortState.Escorting) && !_playerGUID.IsEmpty && !Me.IsEngaged && !HasEscortState(EscortState.Returning))
        {
            if (_playerCheckTimer <= diff)
            {
                if (!IsPlayerOrGroupInRange())
                {
                    Log.Logger.Debug($"EscortAI::UpdateAI: failed because player/group was to far away or not found ({Me.GUID})");

                    var isEscort = false;
                    var creatureData = Me.CreatureData;

                    if (creatureData != null)
                        isEscort = Me.Configuration.GetDefaultValue("Respawn:DynamicEscortNPC", false) && creatureData.SpawnGroupData.Flags.HasAnyFlag(SpawnGroupFlags.EscortQuestNpc);

                    if (_instantRespawn)
                    {
                        if (!isEscort)
                            Me.DespawnOrUnsummon(TimeSpan.Zero, TimeSpan.FromSeconds(1));
                        else
                            Me.Location.Map.Respawn(SpawnObjectType.Creature, Me.SpawnId);
                    }
                    else
                        Me.DespawnOrUnsummon();

                    return;
                }

                _playerCheckTimer = 1000;
            }
            else
                _playerCheckTimer -= diff;
        }

        UpdateEscortAI(diff);
    }

    public virtual void UpdateEscortAI(uint diff)
    {
        if (!UpdateVictim())
            return;

        DoMeleeAttackIfReady();
    }

    private void AddEscortState(EscortState escortState)
    {
        _escortState |= escortState;
    }

    // used in EnterEvadeMode override
    //see followerAI
    private bool AssistPlayerInCombatAgainst(Unit who)
    {
        if (!who || !who.Victim)
            return false;

        if (Me.HasReactState(ReactStates.Passive))
            return false;

        //experimental (unknown) Id not present
        if (!Me.Template.TypeFlags.HasAnyFlag(CreatureTypeFlags.CanAssist))
            return false;

        //not a player
        if (!who.Victim.CharmerOrOwnerPlayerOrPlayerItself)
            return false;

        //never attack friendly
        if (Me.WorldObjectCombat.IsValidAssistTarget(who.Victim))
            return false;

        //too far away and no free sight?
        if (Me.Location.IsWithinDistInMap(who, GetMaxPlayerDistance()) && Me.Location.IsWithinLOSInMap(who))
        {
            Me.EngageWithTarget(who);

            return true;
        }

        return false;
    }

    private void FillPointMovementListForCreature()
    {
        var path = Me.WaypointManager.GetPath(Me.Entry);

        if (path == null)
            return;

        foreach (var node in path.Nodes.Select(value => value.Copy()))
        {
            node.X = GridDefines.NormalizeMapCoord(node.X);
            node.Y = GridDefines.NormalizeMapCoord(node.Y);
            node.MoveType = _running ? WaypointMoveType.Run : WaypointMoveType.Walk;

            _path.Nodes.Add(node);
        }
    }

    private float GetMaxPlayerDistance()
    {
        return _maxPlayerDistance;
    }

    private bool IsPlayerOrGroupInRange()
    {
        var player = GetPlayerForEscort();

        if (player)
        {
            var group = player.Group;

            if (group)
                for (var groupRef = group.FirstMember; groupRef != null; groupRef = groupRef.Next())
                {
                    var member = groupRef.Source;

                    if (member)
                        if (Me.Location.IsWithinDistInMap(member, GetMaxPlayerDistance()))
                            return true;
                }
            else if (Me.Location.IsWithinDistInMap(player, GetMaxPlayerDistance()))
                return true;
        }

        return false;
    }

    private void RemoveEscortState(EscortState escortState)
    {
        _escortState &= ~escortState;
    }

    private void ReturnToLastPoint()
    {
        Me.MotionMaster.MovePoint(0xFFFFFF, Me.HomePosition);
    }
}