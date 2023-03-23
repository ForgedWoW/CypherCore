// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.DemonHunter;

[SpellScript(210047)]
public class spell_dh_consume_soul_missile : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleHit, 1, SpellEffectName.TriggerMissile, SpellScriptHookType.EffectHit));
	}

	private void HandleHit(int effIndex)
	{
		PreventHitDefaultEffect(effIndex);
		var caster = Caster;

		if (caster == null)
			return;

		var spellToCast = SpellValue.EffectBasePoints[0];
		caster.CastSpell(caster, (uint)spellToCast, true);
	}
}