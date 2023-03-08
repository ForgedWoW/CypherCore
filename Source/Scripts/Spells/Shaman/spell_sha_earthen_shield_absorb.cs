// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Framework.Models;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Shaman;

//201633 - Earthen Shield
[SpellScript(201633)]
public class spell_sha_earthen_shield_absorb : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectCalcAmountHandler(CalcAbsorb, 0, AuraType.SchoolAbsorb));
		AuraEffects.Add(new AuraEffectAbsorbHandler(HandleAbsorb, 0));
	}

	private void CalcAbsorb(AuraEffect UnnamedParameter, BoxedValue<double> amount, BoxedValue<bool> canBeRecalculated)
	{
		if (!Caster)
			return;

		amount.Value = Caster.GetHealth();
	}

	private double HandleAbsorb(AuraEffect UnnamedParameter, DamageInfo dmgInfo, double absorbAmount)
	{
		var caster = Caster;

		if (caster == null || !caster.IsTotem())
			return absorbAmount;

		var owner = caster.GetOwner();

		if (owner == null)
			return absorbAmount;

		if (dmgInfo.GetDamage() - owner.GetTotalSpellPowerValue(SpellSchoolMask.All, true) > 0)
			absorbAmount = owner.GetTotalSpellPowerValue(SpellSchoolMask.All, true);
		else
			absorbAmount = dmgInfo.GetDamage();

		//201657 - The damager
		caster.CastSpell(caster, 201657, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.BasePoint0, (int)absorbAmount));

		return absorbAmount;
	}
}