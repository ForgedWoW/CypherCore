// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Units;
using Framework.Constants;
using Framework.Util;
using Microsoft.Extensions.Configuration;

namespace Forged.MapServer.Movement.Generators;

public class AssistanceMovementGenerator : PointMovementGenerator
{
    private readonly IConfiguration _configuration;

    public AssistanceMovementGenerator(uint id, float x, float y, float z, IConfiguration configuration) : base(id, x, y, z, true)
    {
        _configuration = configuration;
    }

    public override void Finalize(Unit owner, bool active, bool movementInform)
    {
        AddFlag(MovementGeneratorFlags.Finalized);

        if (active)
            owner.ClearUnitState(UnitState.RoamingMove);

        if (!movementInform || !HasFlag(MovementGeneratorFlags.InformEnabled))
            return;

        var ownerCreature = owner.AsCreature;
        ownerCreature.SetNoCallAssistance(false);
        ownerCreature.CallAssistance();

        if (ownerCreature.IsAlive)
            ownerCreature.MotionMaster.MoveSeekAssistanceDistract(_configuration.GetDefaultValue("CreatureFamilyAssistanceDelay", 1500u));
    }

    public override MovementGeneratorType GetMovementGeneratorType()
    {
        return MovementGeneratorType.Assistance;
    }
}