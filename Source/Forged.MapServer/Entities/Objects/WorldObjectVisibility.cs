// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Framework.Dynamic;

namespace Forged.MapServer.Entities.Objects;

public class WorldObjectVisibility
{
    private readonly WorldObject _worldObject;
    private SmoothPhasing _smoothPhasing;
    private float? _visibilityDistanceOverride;
    public WorldObjectVisibility(WorldObject worldObject)
    {
        ServerSideVisibility.SetValue(ServerSideVisibilityType.Ghost, GhostVisibilityType.Alive | GhostVisibilityType.Ghost);
        ServerSideVisibilityDetect.SetValue(ServerSideVisibilityType.Ghost, GhostVisibilityType.Alive);
        _worldObject = worldObject;
    }

    public FlaggedArray64<InvisibilityType> Invisibility { get; set; } = new((int)InvisibilityType.Max);
    public FlaggedArray64<InvisibilityType> InvisibilityDetect { get; set; } = new((int)InvisibilityType.Max);
    public FlaggedArray32<ServerSideVisibilityType> ServerSideVisibility { get; set; } = new(2);
    public FlaggedArray32<ServerSideVisibilityType> ServerSideVisibilityDetect { get; set; } = new(2);
    public FlaggedArray32<StealthType> Stealth { get; set; } = new(2);
    public FlaggedArray32<StealthType> StealthDetect { get; set; } = new(2);
    public float VisibilityRange
    {
        get
        {
            if (IsVisibilityOverridden && !_worldObject.IsPlayer && _visibilityDistanceOverride != null)
                return _visibilityDistanceOverride.Value;

            if (IsFarVisible && !_worldObject.IsPlayer)
                return SharedConst.MaxVisibilityDistance;

            return _worldObject.Location.Map?.VisibilityRange ?? SharedConst.MaxVisibilityDistance;
        }
    }

    private bool IsFarVisible { get; set; }
    private bool IsVisibilityOverridden => _visibilityDistanceOverride.HasValue;
    public virtual bool CanAlwaysSee(WorldObject obj)
    {
        return false;
    }

    public virtual bool CanNeverSee(WorldObject obj)
    {
        return _worldObject.Location.Map != obj.Location.Map || !_worldObject.Location.InSamePhase(obj);
    }
    public bool CanSeeOrDetect(WorldObject obj, bool ignoreStealth = false, bool distanceCheck = false, bool checkAlert = false)
    {
        if (_worldObject == obj)
            return true;

        if (obj.IsNeverVisibleFor(_worldObject) || CanNeverSee(obj))
            return false;

        if (obj.IsAlwaysVisibleFor(_worldObject) || CanAlwaysSee(obj))
            return true;

        if (!obj.Visibility.CheckPrivateObjectOwnerVisibility(_worldObject))
            return false;

        var smoothPhasing = obj.Visibility.GetSmoothPhasing();

        if (smoothPhasing != null && smoothPhasing.IsBeingReplacedForSeer(_worldObject.GUID))
            return false;

        if (!obj.IsPrivateObject && !_worldObject.ConditionManager.IsObjectMeetingVisibilityByObjectIdConditions((uint)obj.TypeId, obj.Entry, _worldObject))
            return false;

        var corpseVisibility = false;

        if (distanceCheck)
        {
            var corpseCheck = false;
            var thisPlayer = _worldObject.AsPlayer;

            if (thisPlayer != null)
            {
                if (thisPlayer.IsDead &&
                    thisPlayer.Health > 0 && // Cheap way to check for ghost state
                    !Convert.ToBoolean((uint)(obj.Visibility.ServerSideVisibility.GetValue(ServerSideVisibilityType.Ghost) & ServerSideVisibility.GetValue(ServerSideVisibilityType.Ghost) & (uint)GhostVisibilityType.Ghost)))
                {
                    var corpse = thisPlayer.GetCorpse();

                    if (corpse != null)
                    {
                        corpseCheck = true;

                        if (corpse.Location.IsWithinDist(thisPlayer, GetSightRange(obj), false))
                            if (corpse.Location.IsWithinDist(obj, GetSightRange(obj), false))
                                corpseVisibility = true;
                    }
                }

                var target = obj.AsUnit;

                if (target)
                {
                    // Don't allow to detect vehicle accessories if you can't see vehicle
                    var vehicle = target.VehicleBase;

                    if (vehicle)
                        if (!thisPlayer.HaveAtClient(vehicle))
                            return false;
                }
            }

            var viewpoint = _worldObject;
            var player = _worldObject.AsPlayer;

            if (player != null)
                viewpoint = player.Viewpoint;

            viewpoint ??= _worldObject;

            if (!corpseCheck && !_worldObject.Location.IsWithinDist(obj, GetSightRange(viewpoint), false))
                return false;
        }

        // GM visibility off or hidden NPC
        if (obj.Visibility.ServerSideVisibility.GetValue(ServerSideVisibilityType.GM) == 0)
        {
            // Stop checking other things for GMs
            if (ServerSideVisibilityDetect.GetValue(ServerSideVisibilityType.GM) != 0)
                return true;
        }
        else
        {
            return ServerSideVisibilityDetect.GetValue(ServerSideVisibilityType.GM) >= obj.Visibility.ServerSideVisibility.GetValue(ServerSideVisibilityType.GM);
        }

        // Ghost players, Spirit Healers, and some other NPCs
        if (corpseVisibility || Convert.ToBoolean(obj.Visibility.ServerSideVisibility.GetValue(ServerSideVisibilityType.Ghost) & ServerSideVisibilityDetect.GetValue(ServerSideVisibilityType.Ghost)))
            return !obj.IsInvisibleDueToDespawn(_worldObject) && CanDetect(obj, ignoreStealth, checkAlert);

        {
            // Alive players can see dead players in some cases, but other objects can't do that
            var thisPlayer = _worldObject.AsPlayer;

            if (thisPlayer != null)
            {
                var objPlayer = obj.AsPlayer;

                if (objPlayer != null)
                {
                    if (!thisPlayer.IsGroupVisibleFor(objPlayer))
                        return false;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        return !obj.IsInvisibleDueToDespawn(_worldObject) && CanDetect(obj, ignoreStealth, checkAlert);
    }

    public bool CheckPrivateObjectOwnerVisibility(WorldObject seer)
    {
        if (!_worldObject.IsPrivateObject)
            return true;

        // Owner of this private object
        if (_worldObject.PrivateObjectOwner == seer.GUID)
            return true;

        // Another private object of the same owner
        if (_worldObject.PrivateObjectOwner == seer.PrivateObjectOwner)
            return true;

        var playerSeer = seer.AsPlayer;

        if (playerSeer != null)
            if (playerSeer.IsInGroup(_worldObject.PrivateObjectOwner))
                return true;

        return false;
    }

    public SmoothPhasing GetOrCreateSmoothPhasing()
    {
        return _smoothPhasing ??= new SmoothPhasing();
    }

    public float GetSightRange(WorldObject target = null)
    {
        if (_worldObject.IsPlayer || _worldObject.IsCreature)
        {
            if (!_worldObject.IsPlayer)
                return _worldObject.IsCreature ? _worldObject.AsCreature.SightDistance : SharedConst.SightRangeUnit;

            return target switch
            {
                { Visibility: { IsVisibilityOverridden: true, _visibilityDistanceOverride: { } }, IsPlayer: true } => target.Visibility._visibilityDistanceOverride.Value,
                { Visibility: { IsFarVisible: true, _worldObject.IsPlayer: false } }                               => SharedConst.MaxVisibilityDistance,
                _                                                                                                  => _worldObject.AsPlayer.CinematicMgr.IsOnCinematic() ? SharedConst.DefaultVisibilityInstance : _worldObject.Location.Map.VisibilityRange
            };
        }

        if (_worldObject.IsDynObject && _worldObject.IsActive)
            return _worldObject.Location.Map.VisibilityRange;

        return 0.0f;
    }

    public SmoothPhasing GetSmoothPhasing()
    {
        return _smoothPhasing;
    }

    public virtual bool IsAlwaysDetectableFor(WorldObject seer)
    {
        return false;
    }

    public virtual bool IsAlwaysVisibleFor(WorldObject seer)
    {
        return false;
    }

    public virtual bool IsInvisibleDueToDespawn(WorldObject seer)
    {
        return false;
    }

    public virtual bool IsNeverVisibleFor(WorldObject seer)
    {
        return !_worldObject.Location.IsInWorld || _worldObject.IsDestroyedObject;
    }
    public void SetFarVisible(bool on)
    {
        if (_worldObject.IsPlayer)
            return;

        IsFarVisible = on;
    }

    public void SetVisibilityDistanceOverride(VisibilityDistanceType type)
    {
        if (_worldObject.TypeId == TypeId.Player)
            return;

        var creature = _worldObject.AsCreature;

        if (creature != null)
        {
            creature.RemoveUnitFlag2(UnitFlags2.LargeAoi | UnitFlags2.GiganticAoi | UnitFlags2.InfiniteAoi);

            switch (type)
            {
                case VisibilityDistanceType.Large:
                    creature.SetUnitFlag2(UnitFlags2.LargeAoi);

                    break;
                case VisibilityDistanceType.Gigantic:
                    creature.SetUnitFlag2(UnitFlags2.GiganticAoi);

                    break;
                case VisibilityDistanceType.Infinite:
                    creature.SetUnitFlag2(UnitFlags2.InfiniteAoi);

                    break;
            }
        }

        _visibilityDistanceOverride = SharedConst.VisibilityDistances[(int)type];
    }
    private bool CanDetect(WorldObject obj, bool ignoreStealth, bool checkAlert = false)
    {
        var seer = _worldObject;

        // If a unit is possessing another one, it uses the detection of the latter
        // Pets don't have detection, they use the detection of their masters
        var thisUnit = _worldObject.AsUnit;

        if (thisUnit != null)
        {
            if (thisUnit.IsPossessing)
            {
                var charmed = thisUnit.Charmed;

                if (charmed != null)
                    seer = charmed;
            }
            else
            {
                var controller = thisUnit.CharmerOrOwner;

                if (controller != null)
                    seer = controller;
            }
        }

        if (obj.IsAlwaysDetectableFor(seer))
            return true;

        if (!ignoreStealth && !seer.Visibility.CanDetectInvisibilityOf(obj))
            return false;

        if (!ignoreStealth && !seer.Visibility.CanDetectStealthOf(obj, checkAlert))
            return false;

        return true;
    }

    private bool CanDetectInvisibilityOf(WorldObject obj)
    {
        var mask = obj.Visibility.Invisibility.GetFlags() & InvisibilityDetect.GetFlags();

        // Check for not detected types
        if (mask != obj.Visibility.Invisibility.GetFlags())
            return false;

        for (var i = 0; i < (int)InvisibilityType.Max; ++i)
        {
            if (!Convert.ToBoolean(mask & (1ul << i)))
                continue;

            var objInvisibilityValue = obj.Visibility.Invisibility.GetValue((InvisibilityType)i);
            var ownInvisibilityDetectValue = InvisibilityDetect.GetValue((InvisibilityType)i);

            // Too low value to detect
            if (ownInvisibilityDetectValue < objInvisibilityValue)
                return false;
        }

        return true;
    }

    private bool CanDetectStealthOf(WorldObject obj, bool checkAlert = false)
    {
        // Combat reach is the minimal distance (both in front and behind),
        //   and it is also used in the range calculation.
        // One stealth point increases the visibility range by 0.3 yard.

        if (obj.Visibility.Stealth.GetFlags() == 0)
            return true;

        var distance = _worldObject.Location.GetExactDist(obj.Location);
        var combatReach = 0.0f;

        var unit = _worldObject.AsUnit;

        if (unit != null)
            combatReach = unit.CombatReach;

        if (distance < combatReach)
            return true;

        // Only check back for units, it does not make sense for gameobjects
        if (unit && !_worldObject.Location.HasInArc(MathF.PI, obj.Location))
            return false;

        // Traps should detect stealth always
        var go = _worldObject.AsGameObject;

        if (go is { GoType: GameObjectTypes.Trap })
            return true;

        go = obj.AsGameObject;

        for (var i = 0; i < (int)StealthType.Max; ++i)
        {
            if (!Convert.ToBoolean((int)(obj.Visibility.Stealth.GetFlags() & (1 << i))))
                continue;

            if (unit != null && unit.HasAuraTypeWithMiscvalue(AuraType.DetectStealth, i))
                return true;

            // Starting points
            var detectionValue = 30;

            // Level difference: 5 point / level, starting from level 1.
            // There may be spells for this and the starting points too, but
            // not in the DBCs of the client.
            detectionValue += (int)(_worldObject.GetLevelForTarget(obj) - 1) * 5;

            // Apply modifiers
            detectionValue += StealthDetect.GetValue((StealthType)i);

            var owner = go?.OwnerUnit;

            if (owner != null)
                detectionValue -= (int)(owner.GetLevelForTarget(_worldObject) - 1) * 5;

            detectionValue -= obj.Visibility.Stealth.GetValue((StealthType)i);

            // Calculate max distance
            var visibilityRange = detectionValue * 0.3f + combatReach;

            // If this unit is an NPC then player detect range doesn't apply
            if (unit != null && unit.IsTypeId(TypeId.Player) && visibilityRange > SharedConst.MaxPlayerStealthDetectRange)
                visibilityRange = SharedConst.MaxPlayerStealthDetectRange;

            // When checking for alert state, look 8% further, and then 1.5 yards more than that.
            if (checkAlert)
                visibilityRange += (visibilityRange * 0.08f) + 1.5f;

            // If checking for alert, and creature's visibility range is greater than aggro distance, No alert
            var tunit = obj.AsUnit;

            if (checkAlert && unit != null && unit.AsCreature && visibilityRange >= unit.AsCreature.GetAttackDistance(tunit) + unit.AsCreature.CombatDistance)
                return false;

            if (distance > visibilityRange)
                return false;
        }

        return true;
    }
}