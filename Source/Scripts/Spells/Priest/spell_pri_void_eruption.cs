﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Priest;

[SpellScript(228260)]
public class spell_pri_void_eruption : SpellScript, IHasSpellEffects, ISpellOnCast, ISpellOnTakePower
{
	public List<ISpellEffect> SpellEffects { get; } = new();


	public void OnCast()
	{
		var caster = Caster;

		if (caster == null)
			return;

		caster.CastSpell(caster, PriestSpells.VOIDFORM_BUFFS, true);

		if (!caster.HasAura(PriestSpells.SHADOWFORM_STANCE))
			caster.CastSpell(caster, PriestSpells.SHADOWFORM_STANCE, true);
	}

	public override void Register()
	{
		SpellEffects.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 0, Targets.UnitDestAreaEnemy));
		SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
	}

	public void TakePower(SpellPowerCost powerCost)
	{
		powerCost.Amount = 0;
	}

	private void FilterTargets(List<WorldObject> targets)
	{
		var caster = Caster;

		if (caster == null)
			return;

		targets.RemoveIf((WorldObject target) =>
		{
			var targ = target.AsUnit;

			if (targ == null)
				return true;

			return !(targ.HasAura(PriestSpells.SHADOW_WORD_PAIN, caster.GUID) || targ.HasAura(PriestSpells.VAMPIRIC_TOUCH, caster.GUID));
		});
	}

	private void HandleDummy(int effIndex)
	{
		var caster = Caster;
		var target = HitUnit;

		if (caster == null || target == null)
			return;

		var spellid = RandomHelper.RandShort() % 2; //there are two animations which should be random
		caster.CastSpell(target, PriestSpells.VOID_ERUPTION_DAMAGE + spellid, true);
	}
}