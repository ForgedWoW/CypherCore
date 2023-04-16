// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Units;
using Framework.Constants;

namespace Forged.MapServer.Movement.Generators;

public class TimedFleeingMovementGenerator : FleeingMovementGenerator<Creature>
{
    private readonly TimeTracker _totalFleeTime;

    public TimedFleeingMovementGenerator(ObjectGuid fright, uint time) : base(fright)
    {
        _totalFleeTime = new TimeTracker(time);
    }

    public override void Finalize(Unit owner, bool active, bool movementInform)
    {
        AddFlag(MovementGeneratorFlags.Finalized);

        if (!active)
            return;

        owner.RemoveUnitFlag(UnitFlags.Fleeing);
        var victim = owner.Victim;

        if (victim != null)
            if (owner.IsAlive)
            {
                owner.AttackStop();
                owner.AsCreature.AI.AttackStart(victim);
            }

        if (!movementInform)
            return;

        var ownerCreature = owner.AsCreature;
        var ai = ownerCreature?.AI;

        ai?.MovementInform(MovementGeneratorType.TimedFleeing, 0);
    }

    public override MovementGeneratorType GetMovementGeneratorType()
    {
        return MovementGeneratorType.TimedFleeing;
    }

    public override bool Update(Unit owner, uint diff)
    {
        if (owner is not { IsAlive: true })
            return false;

        _totalFleeTime.Update(diff);

        return !_totalFleeTime.Passed && DoUpdate(owner.AsCreature, diff);
    }
}