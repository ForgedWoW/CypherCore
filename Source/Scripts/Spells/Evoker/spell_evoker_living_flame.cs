﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.RED_LIVING_FLAME)] // 361469 - Living Flame (Red)
class spell_evoker_living_flame : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();


	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleHitTarget, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
		SpellEffects.Add(new EffectHandler(HandleLaunchTarget, 0, SpellEffectName.Dummy, SpellScriptHookType.LaunchTarget));
	}

	void HandleHitTarget(int effIndex)
	{
		var caster = Caster;
		var hitUnit = HitUnit;

		if (caster.IsFriendlyTo(hitUnit))
        {
            CastSpellExtraArgs args = new(TriggerCastFlags.TriggeredAllowProc);
            caster.CastSpell(hitUnit, EvokerSpells.RED_LIVING_FLAME_HEAL, args);
		}
		else
		{
			double damage = 0;

			if (caster.TryGetAura(EvokerSpells.SCARLET_ADAPTATION_AURA, out var aura))
			{
				damage = aura.GetEffect(0).Amount;
				caster.RemoveAura(aura);
			}

			CastSpellExtraArgs args = new(TriggerCastFlags.TriggeredAllowProc);
			args.SpellValueOverrides[SpellValueMod.BasePoint0] = damage;

			caster.CastSpell(hitUnit, EvokerSpells.RED_LIVING_FLAME_DAMAGE, args);
		}
	}

	void HandleLaunchTarget(int effIndex)
	{
		var caster = Caster;

		if (caster.IsFriendlyTo(HitUnit))
			return;

		var auraEffect = caster.GetAuraEffect(EvokerSpells.ENERGIZING_FLAME, 0);

		if (auraEffect != null)
		{
			var manaCost = Spell.GetPowerTypeCostAmount(PowerType.Mana).GetValueOrDefault(0);

			if (manaCost != 0)
				Caster.ModifyPower(PowerType.Mana, MathFunctions.CalculatePct(manaCost, auraEffect.Amount));
		}
	}
}