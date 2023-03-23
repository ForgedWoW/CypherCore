// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Hunter;

[SpellScript(63900)]
public class spell_hun_pet_thunderstomp : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDamage, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
	}


	private void HandleDamage(int effIndex)
	{
		var caster = Caster;
		var owner = Caster.OwnerUnit;
		var target = HitUnit;

		if (owner == null || target == null)
			return;

		double dmg = 1.5f * (owner.UnitData.RangedAttackPower * 0.250f);

		dmg = caster.SpellDamageBonusDone(target, SpellInfo, dmg, DamageEffectType.Direct, GetEffectInfo(0), 1, Spell);
		dmg = target.SpellDamageBonusTaken(caster, SpellInfo, dmg, DamageEffectType.Direct);

		HitDamage = dmg;
	}
}