// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Units;
using Framework.Constants;

namespace Forged.MapServer.Movement.Generators;

public class DistractMovementGenerator : MovementGenerator
{
    private readonly float _orientation;

    private uint _timer;

    public DistractMovementGenerator(uint timer, float orientation)
    {
        _timer = timer;
        _orientation = orientation;

        Mode = MovementGeneratorMode.Default;
        Priority = MovementGeneratorPriority.Highest;
        Flags = MovementGeneratorFlags.InitializationPending;
        BaseUnitState = UnitState.Distracted;
    }

    public override void Deactivate(Unit owner)
    {
        AddFlag(MovementGeneratorFlags.Deactivated);
    }

    public override void Finalize(Unit owner, bool active, bool movementInform)
    {
        AddFlag(MovementGeneratorFlags.Finalized);

        // TODO: This code should be handled somewhere else
        // If this is a creature, then return orientation to original position (for idle movement creatures)
        if (!movementInform || !HasFlag(MovementGeneratorFlags.InformEnabled) || !owner.IsCreature)
            return;

        var angle = owner.AsCreature.HomePosition.Orientation;
        owner.SetFacingTo(angle);
    }

    public override MovementGeneratorType GetMovementGeneratorType()
    {
        return MovementGeneratorType.Distract;
    }

    public override void Initialize(Unit owner)
    {
        RemoveFlag(MovementGeneratorFlags.InitializationPending | MovementGeneratorFlags.Deactivated);
        AddFlag(MovementGeneratorFlags.Initialized);

        // Distracted creatures stand up if not standing
        if (!owner.IsStandState)
            owner.SetStandState(UnitStandStateType.Stand);

        MoveSplineInit init = new(owner);
        init.MoveTo(owner.Location, false);

        if (!owner.GetTransGUID().IsEmpty)
            init.DisableTransportPathTransformations();

        init.SetFacing(_orientation);
        init.Launch();
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

        if (diff > _timer)
        {
            AddFlag(MovementGeneratorFlags.InformEnabled);

            return false;
        }

        _timer -= diff;

        return true;
    }
}