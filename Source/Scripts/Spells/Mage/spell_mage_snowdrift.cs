﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Mage;

[SpellScript(389794)]
public class spell_mage_snowdrift : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectPeriodicHandler(OnTick, 0, AuraType.PeriodicDamage));
	}

	private void OnTick(AuraEffect aurEff)
	{
		var target = Target;
		var caster = Caster;

		if (target == null || caster == null)
			return;

		// Slow enemies by 70%
		target.ApplySpellImmune(0, SpellImmunity.State, AuraType.ModDecreaseSpeed, true);
		target.ApplySpellImmune(0, SpellImmunity.State, AuraType.ModSpeedSlowAll, true);
		target.ApplySpellImmune(0, SpellImmunity.State, AuraType.ModRoot, true);

		// Deal (20% of Spell power) Frost damage every 1 sec
		var damage = caster.SpellDamageBonusDone(target, aurEff.SpellInfo, 0, DamageEffectType.DOT, aurEff.GetSpellEffectInfo(), StackAmount) * aurEff.Amount;
		damage = target.SpellDamageBonusTaken(caster, aurEff.SpellInfo, (uint)damage, DamageEffectType.DOT);
		Unit.DealDamage(target, target, (uint)damage, null, DamageEffectType.DOT, SpellSchoolMask.Frost, aurEff.SpellInfo, false);

		// Check if target has been caught in Snowdrift for 3 sec consecutively
		if (aurEff.GetTickNumber() >= 3)
		{
			// Apply Frozen in Ice and stun for 4 sec
			target.CastSpell(target, MageSpells.FROZEN_IN_ICE, true);
			target.RemoveAura(MageSpells.SNOWDRIFT);
		}
	}
}