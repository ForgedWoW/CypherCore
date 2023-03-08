// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Framework.Models;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.DemonHunter;

[SpellScript(209258)]
public class spell_dh_last_resort : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();


	public override bool Validate(SpellInfo UnnamedParameter)
	{
		if (Global.SpellMgr.GetSpellInfo(DemonHunterSpells.LAST_RESORT_DEBUFF, Difficulty.None) != null)
			return false;

		return true;
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectCalcAmountHandler(CalcAmount, 0, AuraType.SchoolAbsorb));
		AuraEffects.Add(new AuraEffectAbsorbHandler(HandleAbsorb, 0));
	}

	private void CalcAmount(AuraEffect UnnamedParameter, BoxedValue<double> amount, BoxedValue<bool> canBeRecalculated)
	{
		amount.Value = -1;
	}

	private double HandleAbsorb(AuraEffect UnnamedParameter, DamageInfo dmgInfo, double absorbAmount)
	{
		var target = Target;

		if (target == null)
			return absorbAmount;

		if (dmgInfo.GetDamage() < target.GetHealth())
			return absorbAmount;

		if (target.HasAura(DemonHunterSpells.LAST_RESORT_DEBUFF))
			return absorbAmount;

		var healthPct = SpellInfo.GetEffect(1).IsEffect() ? SpellInfo.GetEffect(1).BasePoints : 0;
		target.SetHealth(1);
		var healInfo = new HealInfo(target, target, target.CountPctFromMaxHealth(healthPct), SpellInfo, (SpellSchoolMask)SpellInfo.SchoolMask);
		target.HealBySpell(healInfo);
		// We use AddAura instead of CastSpell, since if the spell is on cooldown, it will not be casted
		target.AddAura(DemonHunterSpells.METAMORPHOSIS_VENGEANCE, target);
		target.CastSpell(target, DemonHunterSpells.LAST_RESORT_DEBUFF, true);

		return dmgInfo.GetDamage();
	}
}