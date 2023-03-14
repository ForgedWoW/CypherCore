// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAreaTrigger;

namespace Scripts.Spells.DeathKnight;

[Script]
public class at_dk_decomposing_aura : AreaTriggerScript, IAreaTriggerOnUnitExit
{
	public void OnUnitExit(Unit unit)
	{
		unit.RemoveAurasDueToSpell(DeathKnightSpells.DECOMPOSING_AURA_DAMAGE, At.CasterGuid);
	}
}