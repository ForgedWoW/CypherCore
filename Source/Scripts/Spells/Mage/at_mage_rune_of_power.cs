// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAreaTrigger;

namespace Scripts.Spells.Mage;

[Script]
public class at_mage_rune_of_power : AreaTriggerScript, IAreaTriggerOnCreate, IAreaTriggerOnUnitEnter, IAreaTriggerOnUnitExit
{
	public void OnCreate()
	{
		//at->SetSpellXSpellVisualId(25943);
	}

	public void OnUnitEnter(Unit unit)
	{
		var caster = At.GetCaster();

		if (caster != null)
			if (unit.GUID == caster.GUID)
				caster.CastSpell(unit, UsingSpells.RUNE_OF_POWER_AURA, true);
	}

	public void OnUnitExit(Unit unit)
	{
		if (unit.HasAura(UsingSpells.RUNE_OF_POWER_AURA))
			unit.RemoveAura(UsingSpells.RUNE_OF_POWER_AURA);
	}

	public struct UsingSpells
	{
		public const uint RUNE_OF_POWER_AURA = 116014;
	}
}