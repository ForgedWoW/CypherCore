// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Units;
using Framework.Constants;

namespace Forged.MapServer.Movement.Generators;

public class IdleMovementGenerator : MovementGenerator
{
    public IdleMovementGenerator()
    {
        Mode = MovementGeneratorMode.Default;
        Priority = MovementGeneratorPriority.Normal;
        Flags = MovementGeneratorFlags.Initialized;
        BaseUnitState = 0;
    }

    public override void Deactivate(Unit owner) { }

    public override void Finalize(Unit owner, bool active, bool movementInform)
    {
        AddFlag(MovementGeneratorFlags.Finalized);
    }

    public override MovementGeneratorType GetMovementGeneratorType()
    {
        return MovementGeneratorType.Idle;
    }

    public override void Initialize(Unit owner)
    {
        owner.StopMoving();
    }

    public override void Reset(Unit owner)
    {
        owner.StopMoving();
    }

    public override bool Update(Unit owner, uint diff)
    {
        return true;
    }
}