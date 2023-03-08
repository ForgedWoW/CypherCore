// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Framework.Models;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.DeathKnight;

[SpellScript(206977)]
public class spell_dk_blood_mirror : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectCalcAmountHandler(CalcAmount, 1, AuraType.SchoolAbsorb));
		AuraEffects.Add(new AuraEffectAbsorbHandler(HandleAbsorb, 1));
	}

	private void CalcAmount(AuraEffect UnnamedParameter, BoxedValue<double> amount, BoxedValue<bool> UnnamedParameter2)
	{
		amount.Value = -1;
	}


	private double HandleAbsorb(AuraEffect aurEff, DamageInfo dmgInfo, double absorbAmount)
	{
		absorbAmount = dmgInfo.GetDamage() * ((uint)aurEff.BaseAmount / 100);
		var caster = Caster;
		var target = Target;

		if (caster != null && target != null)
			caster.CastSpell(target, DeathKnightSpells.BLOOD_MIRROR_DAMAGE, (int)absorbAmount, true);

		return absorbAmount;
	}
}