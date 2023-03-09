// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.AI;
using Game.Entities;
using Game.Scripting;

namespace Scripts.Spells.DeathKnight;

[Script]
public class at_dk_death_and_decay : AreaTriggerAI
{
	public at_dk_death_and_decay(AreaTrigger areatrigger) : base(areatrigger) { }

	public override void OnUnitEnter(Unit unit)
	{
		var caster = At.GetCaster();

		if (caster != null)
			if (unit.GUID == caster.GUID)
				if (!caster.HasAura(DeathKnightSpells.DEATH_AND_DECAY_CLEAVE))
					caster.CastSpell(unit, DeathKnightSpells.DEATH_AND_DECAY_CLEAVE, true);
	}

	public override void OnUnitExit(Unit unit)
	{
		if (At.GetCaster().HasAura(DeathKnightSpells.DEATH_AND_DECAY_CLEAVE))
			unit.RemoveAura(DeathKnightSpells.DEATH_AND_DECAY_CLEAVE, Game.Spells.AuraRemoveMode.Cancel);
	}
}