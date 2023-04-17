// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.Entities.Units;
using Framework.Constants;

namespace Forged.MapServer.Movement.Generators;

public class RotateMovementGenerator : MovementGenerator
{
    private readonly RotateDirection _direction;
    private readonly uint _id;
    private readonly uint _maxDuration;
    private uint _duration;

    public RotateMovementGenerator(uint id, uint time, RotateDirection direction)
    {
        _id = id;
        _duration = time;
        _maxDuration = time;
        _direction = direction;

        Mode = MovementGeneratorMode.Default;
        Priority = MovementGeneratorPriority.Normal;
        Flags = MovementGeneratorFlags.InitializationPending;
        BaseUnitState = UnitState.Rotating;
    }

    public override void Deactivate(Unit owner)
    {
        AddFlag(MovementGeneratorFlags.Deactivated);
    }

    public override void Finalize(Unit owner, bool active, bool movementInform)
    {
        AddFlag(MovementGeneratorFlags.Finalized);

        if (movementInform && owner.IsCreature)
            owner.AsCreature.AI.MovementInform(MovementGeneratorType.Rotate, _id);
    }

    public override MovementGeneratorType GetMovementGeneratorType()
    {
        return MovementGeneratorType.Rotate;
    }

    public override void Initialize(Unit owner)
    {
        RemoveFlag(MovementGeneratorFlags.InitializationPending | MovementGeneratorFlags.Deactivated);
        AddFlag(MovementGeneratorFlags.Initialized);

        owner.StopMoving();

        /*
        *  TODO: This code should be handled somewhere else, like MovementInform
        *
        *  if (owner->GetVictim())
        *      owner->SetInFront(owner->GetVictim());
        *
        *  owner->AttackStop();
        */
    }

    public override void Reset(Unit owner)
    {
        RemoveFlag(MovementGeneratorFlags.Deactivated);
        Initialize(owner);
    }

    public override bool Update(Unit owner, uint diff)
    {
        if (owner == null)
            return false;

        var angle = owner.Location.Orientation;
        angle += diff * MathFunctions.TWO_PI / _maxDuration * (_direction == RotateDirection.Left ? 1.0f : -1.0f);
        angle = Math.Clamp(angle, 0.0f, MathF.PI * 2);

        MoveSplineInit init = new(owner);
        init.MoveTo(owner.Location, false);

        if (!owner.GetTransGUID().IsEmpty)
            init.DisableTransportPathTransformations();

        init.SetFacing(angle);
        init.Launch();

        if (_duration > diff)
            _duration -= diff;
        else
        {
            AddFlag(MovementGeneratorFlags.InformEnabled);

            return false;
        }

        return true;
    }
}