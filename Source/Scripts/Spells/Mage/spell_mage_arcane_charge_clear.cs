// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Mage;

[Script] // 195302 - Arcane Charge
internal class spell_mage_arcane_charge_clear : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();


	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(RemoveArcaneCharge, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
	}

	private void RemoveArcaneCharge(int effIndex)
	{
		HitUnit.RemoveAura(MageSpells.ArcaneCharge);
	}
}