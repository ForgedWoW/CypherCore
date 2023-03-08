// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Priest;

[SpellScript(14769)]
public class spell_pri_improved_power_word_shield : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectCalcSpellModHandler(HandleEffectCalcSpellMod, 0, AuraType.Dummy));
	}

	private void HandleEffectCalcSpellMod(AuraEffect aurEff, SpellModifier spellMod)
	{
		if (spellMod == null)
		{
			var mod = new SpellModifierByClassMask(Aura);
			spellMod.Op = (SpellModOp)aurEff.MiscValue;
			spellMod.Type = SpellModType.Pct;
			spellMod.SpellId = Id;
			mod.Mask = aurEff.GetSpellEffectInfo().SpellClassMask;
		}

		((SpellModifierByClassMask)spellMod).Value = aurEff.Amount;
	}
}