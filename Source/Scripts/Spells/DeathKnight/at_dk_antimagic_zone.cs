// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAreaTrigger;

namespace Scripts.Spells.DeathKnight;

[Script]
public class at_dk_antimagic_zone : AreaTriggerScript, IAreaTriggerOnUnitEnter, IAreaTriggerOnUnitExit
{
	public void OnUnitEnter(Unit unit)
	{
		//TODO: Improve unit targets
		if (unit.IsPlayer && !unit.IsHostileTo(At.GetCaster()))
			if (!unit.HasAura(DeathKnightSpells.ANTIMAGIC_ZONE_DAMAGE_TAKEN))
				unit.AddAura(DeathKnightSpells.ANTIMAGIC_ZONE_DAMAGE_TAKEN, unit);
	}

	public void OnUnitExit(Unit unit)
	{
		if (unit.HasAura(DeathKnightSpells.ANTIMAGIC_ZONE_DAMAGE_TAKEN))
			unit.RemoveAura(DeathKnightSpells.ANTIMAGIC_ZONE_DAMAGE_TAKEN);
	}
}