// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Units;
using Framework.Constants;

namespace Forged.MapServer.Movement.Generators;

public class AssistanceDistractMovementGenerator : DistractMovementGenerator
{
    public AssistanceDistractMovementGenerator(uint timer, float orientation) : base(timer, orientation)
    {
        Priority = MovementGeneratorPriority.Normal;
    }

    public override void Finalize(Unit owner, bool active, bool movementInform)
    {
        owner.ClearUnitState(UnitState.Distracted);
        owner.AsCreature.ReactState = ReactStates.Aggressive;
    }

    public override MovementGeneratorType GetMovementGeneratorType()
    {
        return MovementGeneratorType.AssistanceDistract;
    }
}