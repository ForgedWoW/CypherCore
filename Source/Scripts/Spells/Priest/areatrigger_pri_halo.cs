// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAreaTrigger;
using Game.Spells;

namespace Scripts.Spells.Priest;

[Script] // 120517 - Halo
internal class areatrigger_pri_halo : AreaTriggerScript, IAreaTriggerOnUnitEnter
{
	public void OnUnitEnter(Unit unit)
	{
		var caster = At.GetCaster();

		if (caster != null)
		{
			if (caster.IsValidAttackTarget(unit))
				caster.CastSpell(unit, PriestSpells.HALO_DAMAGE, new CastSpellExtraArgs(TriggerCastFlags.IgnoreGCD | TriggerCastFlags.IgnoreCastInProgress));
			else if (caster.IsValidAssistTarget(unit))
				caster.CastSpell(unit, PriestSpells.HALO_HEAL, new CastSpellExtraArgs(TriggerCastFlags.IgnoreGCD | TriggerCastFlags.IgnoreCastInProgress));
		}
	}
}