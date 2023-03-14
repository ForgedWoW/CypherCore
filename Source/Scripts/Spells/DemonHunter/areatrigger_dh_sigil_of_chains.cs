// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Scripting;
using Game.Scripting.Interfaces.IAreaTrigger;
using Game.Spells;

namespace Scripts.Spells.DemonHunter;

[Script] // 202138 - Sigil of Chains
internal class areatrigger_dh_sigil_of_chains : AreaTriggerScript, IAreaTriggerOnRemove
{
	public void OnRemove()
	{
		var caster = At.GetCaster();

		if (caster != null)
		{
			caster.CastSpell(At.Location, DemonHunterSpells.SigilOfChainsVisual, new CastSpellExtraArgs());
			caster.CastSpell(At.Location, DemonHunterSpells.SigilOfChainsTargetSelect, new CastSpellExtraArgs());
		}
	}
}