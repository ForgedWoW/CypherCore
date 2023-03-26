// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Units;
using Framework.Constants;

namespace Forged.MapServer.AI.ScriptedAI;

public class WorldBossAI : ScriptedAI
{
    private readonly SummonList _summons;

	public WorldBossAI(Creature creature) : base(creature)
	{
		_summons = new SummonList(creature);
	}

	public override void JustSummoned(Creature summon)
	{
		_summons.Summon(summon);
		var target = SelectTarget(SelectTargetMethod.Random, 0, 0.0f, true);

		if (target)
			summon.AI.AttackStart(target);
	}

	public override void SummonedCreatureDespawn(Creature summon)
	{
		_summons.Despawn(summon);
	}

	public override void UpdateAI(uint diff)
	{
		if (!UpdateVictim())
			return;

		Events.Update(diff);

		if (Me.HasUnitState(UnitState.Casting))
			return;

		Events.ExecuteEvents(eventId =>
		{
			ExecuteEvent(eventId);

			if (Me.HasUnitState(UnitState.Casting))
				return;
		});

		DoMeleeAttackIfReady();
	}

	// Hook used to execute events scheduled into EventMap without the need
	// to override UpdateAI
	// note: You must re-schedule the event within this method if the event
	// is supposed to run more than once
	public virtual void ExecuteEvent(uint eventId) { }

	public override void Reset()
	{
		_Reset();
	}

	public override void JustEngagedWith(Unit who)
	{
		_JustEngagedWith();
	}

	public override void JustDied(Unit killer)
	{
		_JustDied();
	}

    private void _Reset()
	{
		if (!Me.IsAlive)
			return;

		Events.Reset();
		_summons.DespawnAll();
	}

    private void _JustDied()
	{
		Events.Reset();
		_summons.DespawnAll();
	}

    private void _JustEngagedWith()
	{
		var target = SelectTarget(SelectTargetMethod.Random, 0, 0.0f, true);

		if (target)
			AttackStart(target);
	}
}