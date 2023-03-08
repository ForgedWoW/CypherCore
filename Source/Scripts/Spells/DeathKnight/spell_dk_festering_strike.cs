// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.DeathKnight;

[Script] // 85948 - Festering Strike
public class spell_dk_festering_strike : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(DeathKnightSpells.FESTERING_WOUND);
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleScriptEffect, 1, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleScriptEffect(int effIndex)
	{
		Caster.CastSpell(HitUnit, DeathKnightSpells.FESTERING_WOUND, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.AuraStack, EffectValue));
	}
}