﻿using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Items;

[Script] // 13280 Gnomish Death Ray
internal class spell_item_gnomish_death_ray : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(ItemSpellIds.GnomishDeathRaySelf, ItemSpellIds.GnomishDeathRayTarget);
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleDummy(uint effIndex)
	{
		Unit caster = GetCaster();
		Unit target = GetHitUnit();

		if (target)
		{
			if (RandomHelper.URand(0, 99) < 15)
				caster.CastSpell(caster, ItemSpellIds.GnomishDeathRaySelf, true); // failure
			else
				caster.CastSpell(target, ItemSpellIds.GnomishDeathRayTarget, true);
		}
	}
}