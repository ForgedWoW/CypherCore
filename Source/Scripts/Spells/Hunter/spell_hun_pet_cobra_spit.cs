// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Hunter;

[SpellScript(206685)]
public class spell_hun_pet_cobra_spit : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDamage, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
	}


	private void HandleDamage(int effIndex)
	{
		var caster = Caster;

		if (caster == null)
			return;

		var owner = caster.GetOwner();

		if (owner == null)
			return;

		var target = ExplTargetUnit;

		if (target == null)
			return;

		// (1 + AP * 0,2)
		double dmg = 1 + owner.UnitData.RangedAttackPower * 0.2f;

		dmg = caster.SpellDamageBonusDone(target, SpellInfo, dmg, DamageEffectType.Direct, GetEffectInfo(0), 1, Spell);
		dmg = target.SpellDamageBonusTaken(caster, SpellInfo, dmg, DamageEffectType.Direct);

		HitDamage = dmg;
	}
}