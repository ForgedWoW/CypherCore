// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;
using Game.Common.Entities.Creatures;
using Game.Common.Entities.Units;

namespace Forged.RealmServer.AI;

public class PossessedAI : CreatureAI
{
	public PossessedAI(Creature creature) : base(creature)
	{
		creature.ReactState = ReactStates.Passive;
	}

	public override void AttackStart(Unit target)
	{
		Me.Attack(target, true);
	}

	public override void UpdateAI(uint diff)
	{
		if (Me.Victim != null)
		{
			if (!Me.IsValidAttackTarget(Me.Victim))
				Me.AttackStop();
			else
				DoMeleeAttackIfReady();
		}
	}

	public override void JustDied(Unit unit)
	{
		// We died while possessed, disable our loot
		Me.RemoveDynamicFlag(UnitDynFlags.Lootable);
	}

	public override void MoveInLineOfSight(Unit who) { }

	public override void JustEnteredCombat(Unit who)
	{
		EngagementStart(who);
	}

	public override void JustExitedCombat()
	{
		EngagementOver();
	}

	public override void JustStartedThreateningMe(Unit who) { }

	public override void EnterEvadeMode(EvadeReason why) { }
}