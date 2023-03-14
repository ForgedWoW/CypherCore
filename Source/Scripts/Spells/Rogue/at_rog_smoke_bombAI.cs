// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.AI;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAreaTrigger;

namespace Scripts.Spells.Rogue;

[Script]
public class at_rog_smoke_bombAI : AreaTriggerScript, IAreaTriggerOnUnitEnter, IAreaTriggerOnUnitExit
{
	public void OnUnitEnter(Unit unit)
	{
		var caster = At.GetCaster();

		if (caster == null || unit == null)
			return;

		if (!caster.AsPlayer)
			return;

		if (caster.IsValidAssistTarget(unit))
			caster.CastSpell(unit, RogueSpells.SMOKE_BOMB_AURA, true);
	}

	public void OnUnitExit(Unit unit)
	{
		var caster = At.GetCaster();

		if (caster == null || unit == null)
			return;

		if (!caster.AsPlayer)
			return;

		if (unit.HasAura(RogueSpells.SMOKE_BOMB_AURA))
			unit.RemoveAura(RogueSpells.SMOKE_BOMB_AURA);
	}
}