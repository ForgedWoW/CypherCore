// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Units;
using Framework.Constants;

namespace Forged.MapServer.AI.CoreAI;

public class CasterAI : CombatAI
{
	float _attackDistance;

	public CasterAI(Creature creature) : base(creature)
	{
		_attackDistance = SharedConst.MeleeRange;
	}

	public override void InitializeAI()
	{
		base.InitializeAI();

		_attackDistance = 30.0f;

		foreach (var id in _spells)
		{
			var info = GetAISpellInfo(id, Me.Map.DifficultyID);

			if (info != null && info.Condition == AICondition.Combat && _attackDistance > info.MaxRange)
				_attackDistance = info.MaxRange;
		}

		if (_attackDistance == 30.0f)
			_attackDistance = SharedConst.MeleeRange;
	}

	public override void AttackStart(Unit victim)
	{
		AttackStartCaster(victim, _attackDistance);
	}

	public override void JustEngagedWith(Unit victim)
	{
		if (_spells.Empty())
			return;

		var spell = (int)(RandomHelper.Rand32() % _spells.Count);
		uint count = 0;

		foreach (var id in _spells)
		{
			var info = GetAISpellInfo(id, Me.Map.DifficultyID);

			if (info != null)
			{
				if (info.Condition == AICondition.Aggro)
				{
					Me.CastSpell(victim, id, false);
				}
				else if (info.Condition == AICondition.Combat)
				{
					var cooldown = info.RealCooldown;

					if (count == spell)
					{
						DoCast(_spells[spell]);
						cooldown += TimeSpan.FromMilliseconds(Me.GetCurrentSpellCastTime(id));
					}

					Events.ScheduleEvent(id, cooldown);
				}
			}
		}
	}

	public override void UpdateAI(uint diff)
	{
		if (!UpdateVictim())
			return;

		Events.Update(diff);

		if (Me.Victim != null)
			if (Me.Victim.HasBreakableByDamageCrowdControlAura(Me))
			{
				Me.InterruptNonMeleeSpells(false);

				return;
			}

		if (Me.HasUnitState(UnitState.Casting))
			return;

		var spellId = Events.ExecuteEvent();

		if (spellId != 0)
		{
			DoCast(spellId);
			var casttime = (uint)Me.GetCurrentSpellCastTime(spellId);
			var info = GetAISpellInfo(spellId, Me.Map.DifficultyID);

			if (info != null)
				Events.ScheduleEvent(spellId, TimeSpan.FromMilliseconds(casttime != 0 ? casttime : 500) + info.RealCooldown);
		}
	}
}