// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Units;
using Framework.Constants;

namespace Forged.MapServer.AI.CoreAI;

public class CritterAI : PassiveAI
{
    public CritterAI(Creature c) : base(c)
    {
        Me.ReactState = ReactStates.Passive;
    }

    public override void JustEngagedWith(Unit who)
    {
        if (!Me.HasUnitState(UnitState.Fleeing))
            Me.SetControlled(true, UnitState.Fleeing);
    }

    public override void MovementInform(MovementGeneratorType type, uint id)
    {
        if (type == MovementGeneratorType.TimedFleeing)
            EnterEvadeMode(EvadeReason.Other);
    }

    public override void EnterEvadeMode(EvadeReason why)
    {
        if (Me.HasUnitState(UnitState.Fleeing))
            Me.SetControlled(false, UnitState.Fleeing);

        base.EnterEvadeMode(why);
    }
}