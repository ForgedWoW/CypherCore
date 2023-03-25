// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Units;
using Framework.Constants;

namespace Forged.MapServer.AI.CoreAI;

public class PassiveAI : CreatureAI
{
	public PassiveAI(Creature creature) : base(creature)
	{
		creature.ReactState = ReactStates.Passive;
	}

	public override void UpdateAI(uint diff)
	{
		if (Me.IsEngaged && !Me.IsInCombat)
			EnterEvadeMode(EvadeReason.NoHostiles);
	}

	public override void AttackStart(Unit victim) { }

	public override void MoveInLineOfSight(Unit who) { }
}