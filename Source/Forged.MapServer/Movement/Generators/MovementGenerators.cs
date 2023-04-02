// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.Entities.Units;
using Framework.Constants;

namespace Forged.MapServer.Movement.Generators;

public abstract class MovementGenerator : IEquatable<MovementGenerator>
{
    public UnitState BaseUnitState;
    public MovementGeneratorFlags Flags;
    public MovementGeneratorMode Mode;
    public MovementGeneratorPriority Priority;
    public void AddFlag(MovementGeneratorFlags flag)
    {
        Flags |= flag;
    }

    // on current top if another movement replaces
    public virtual void Deactivate(Unit owner) { }

    public bool Equals(MovementGenerator other)
    {
        if (Mode == other.Mode && Priority == other.Priority)
            return true;

        return false;
    }

    // on movement delete
    public virtual void Finalize(Unit owner, bool active, bool movementInform) { }

    public virtual string GetDebugInfo()
    {
        return $"Mode: {Mode} Priority: {Priority} Flags: {Flags} BaseUniteState: {BaseUnitState}";
    }

    public int GetHashCode(MovementGenerator obj)
    {
        return obj.Mode.GetHashCode() ^ obj.Priority.GetHashCode();
    }

    public abstract MovementGeneratorType GetMovementGeneratorType();

    // used by Evade code for select point to evade with expected restart default movement
    public virtual bool GetResetPosition(Unit u, out float x, out float y, out float z)
    {
        x = y = z = 0.0f;

        return false;
    }

    public bool HasFlag(MovementGeneratorFlags flag)
    {
        return (Flags & flag) != 0;
    }

    // on top first update
    public virtual void Initialize(Unit owner) { }

    // timer in ms
    public virtual void Pause(uint timer = 0) { }

    public void RemoveFlag(MovementGeneratorFlags flag)
    {
        Flags &= ~flag;
    }

    // on top reassign
    public virtual void Reset(Unit owner) { }

    // timer in ms
    public virtual void Resume(uint overrideTimer = 0) { }

    public virtual void UnitSpeedChanged() { }

    // on top on MotionMaster::Update
    public abstract bool Update(Unit owner, uint diff);
}

public abstract class MovementGeneratorMedium<T> : MovementGenerator where T : Unit
{
    public bool IsActive { get; set; }

    public override void Deactivate(Unit owner)
    {
        DoDeactivate((T)owner);
    }

    public abstract void DoDeactivate(T owner);

    public abstract void DoFinalize(T owner, bool active, bool movementInform);

    public abstract void DoInitialize(T owner);

    public abstract void DoReset(T owner);

    public abstract bool DoUpdate(T owner, uint diff);

    public override void Finalize(Unit owner, bool active, bool movementInform)
    {
        DoFinalize((T)owner, active, movementInform);
    }

    public override MovementGeneratorType GetMovementGeneratorType()
    {
        return MovementGeneratorType.Max;
    }

    public override void Initialize(Unit owner)
    {
        DoInitialize((T)owner);
        IsActive = true;
    }

    public override void Reset(Unit owner)
    {
        DoReset((T)owner);
    }

    public override bool Update(Unit owner, uint diff)
    {
        return DoUpdate((T)owner, diff);
    }
}