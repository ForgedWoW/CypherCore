// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;

namespace Forged.RealmServer.AI;

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