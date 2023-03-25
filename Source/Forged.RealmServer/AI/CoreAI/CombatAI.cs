// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Forged.RealmServer.Entities;
using Forged.RealmServer.Entities.Creatures;
using Forged.RealmServer.Entities.Units;

namespace Forged.RealmServer.AI;

public class CombatAI : CreatureAI
{
	protected List<uint> _spells = new();

	public CombatAI(Creature c) : base(c) { }

	public override void InitializeAI()
	{
		for (var i = 0; i < SharedConst.MaxCreatureSpells; ++i)
			if (Me.Spells[i] != 0 && Global.SpellMgr.HasSpellInfo(Me.Spells[i], Me.Map.DifficultyID))
				_spells.Add(Me.Spells[i]);

		base.InitializeAI();
	}

	public override void Reset()
	{
		Events.Reset();
	}

	public override void JustDied(Unit killer)
	{
		foreach (var id in _spells)
		{
			var info = GetAISpellInfo(id, Me.Map.DifficultyID);

			if (info != null && info.Condition == AICondition.Die)
				Me.CastSpell(killer, id, true);
		}
	}

	public override void JustEngagedWith(Unit victim)
	{
		foreach (var id in _spells)
		{
			var info = GetAISpellInfo(id, Me.Map.DifficultyID);

			if (info != null)
			{
				if (info.Condition == AICondition.Aggro)
					Me.CastSpell(victim, id, false);
				else if (info.Condition == AICondition.Combat)
					Events.ScheduleEvent(id, info.Cooldown, info.Cooldown * 2);
			}
		}
	}

	public override void UpdateAI(uint diff)
	{
		if (!UpdateVictim())
			return;

		Events.Update(diff);

		if (Me.HasUnitState(UnitState.Casting))
			return;

		var spellId = Events.ExecuteEvent();

		if (spellId != 0)
		{
			DoCast(spellId);
			var info = GetAISpellInfo(spellId, Me.Map.DifficultyID);

			if (info != null)
				Events.ScheduleEvent(spellId, info.Cooldown, info.Cooldown * 2);
		}
		else
		{
			DoMeleeAttackIfReady();
		}
	}

	public override void SpellInterrupted(uint spellId, uint unTimeMs)
	{
		Events.RescheduleEvent(spellId, TimeSpan.FromMilliseconds(unTimeMs));
	}
}