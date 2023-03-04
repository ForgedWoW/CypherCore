// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.LIVING_FLAME)] // 361469 - Living Flame (Red)
class spell_evo_living_flame : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(EvokerSpells.LIVING_FLAME_DAMAGE, EvokerSpells.LIVING_FLAME_HEAL, EvokerSpells.ENERGIZING_FLAME);
	}

	void HandleHitTarget(int effIndex)
	{
		var caster  = GetCaster();
		var hitUnit = GetHitUnit();

		if (caster.IsFriendlyTo(hitUnit))
			caster.CastSpell(hitUnit, EvokerSpells.LIVING_FLAME_HEAL, true);
		else
			caster.CastSpell(hitUnit, EvokerSpells.LIVING_FLAME_DAMAGE, true);
	}

	void HandleLaunchTarget(int effIndex)
	{
		var caster = GetCaster();

		if (caster.IsFriendlyTo(GetHitUnit()))
			return;

		var auraEffect = caster.GetAuraEffect(EvokerSpells.ENERGIZING_FLAME, 0);

		if (auraEffect != null)
		{
			var manaCost = GetSpell().GetPowerTypeCostAmount(PowerType.Mana).GetValueOrDefault(0);

			if (manaCost != 0)
				GetCaster().ModifyPower(PowerType.Mana, MathFunctions.CalculatePct(manaCost, auraEffect.GetAmount()));
		}
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleHitTarget, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
		SpellEffects.Add(new EffectHandler(HandleLaunchTarget, 0, SpellEffectName.Dummy, SpellScriptHookType.LaunchTarget));
	}
}