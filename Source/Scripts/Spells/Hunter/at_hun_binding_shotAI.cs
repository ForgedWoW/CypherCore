// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAreaTrigger;

namespace Scripts.Spells.Hunter;

[Script]
public class at_hun_binding_shotAI : AreaTriggerScript, IAreaTriggerOnUnitEnter, IAreaTriggerOnUnitExit
{
	public enum UsedSpells
	{
		BINDING_SHOT_AURA = 117405,
		BINDING_SHOT_STUN = 117526,
		BINDING_SHOT_IMMUNE = 117553,
		BINDING_SHOT_VISUAL_1 = 118306,
		HUNDER_BINDING_SHOT_VISUAL_2 = 117614
	}

	public void OnUnitEnter(Unit unit)
	{
		var caster = At.GetCaster();

		if (caster == null)
			return;

		if (unit == null)
			return;

		if (!caster.IsFriendlyTo(unit))
			unit.CastSpell(unit, UsedSpells.BINDING_SHOT_AURA, true);
	}

	public void OnUnitExit(Unit unit)
	{
		if (unit == null || !At.GetCaster())
			return;

		var pos = At.Location;

		// Need to check range also, since when the trigger is removed, this get called as well.
		if (unit.HasAura(UsedSpells.BINDING_SHOT_AURA) && unit.Location.GetExactDist(pos) >= 5.0f)
		{
			unit.RemoveAura(UsedSpells.BINDING_SHOT_AURA);
			At.GetCaster().CastSpell(unit, UsedSpells.BINDING_SHOT_STUN, true);
		}
	}
}