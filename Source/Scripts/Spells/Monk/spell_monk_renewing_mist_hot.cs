// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Framework.Models;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Monk;

[SpellScript(119611)]
public class spell_monk_renewing_mist_hot : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override bool Validate(SpellInfo UnnamedParameter)
	{
		return ValidateSpellInfo(MonkSpells.RENEWING_MIST_JUMP, MonkSpells.RENEWING_MIST);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectPeriodicHandler(HandlePeriodicHeal, 0, AuraType.PeriodicHeal));
		AuraEffects.Add(new AuraEffectCalcAmountHandler(CalcAmount, 0, AuraType.PeriodicHeal));
	}

	private void HandlePeriodicHeal(AuraEffect UnnamedParameter)
	{
		var caster = Caster;

		if (caster == null)
			return;

		if (Target.IsFullHealth)
			caster.CastSpell(Target, MonkSpells.RENEWING_MIST_JUMP, true);
	}

	private void CalcAmount(AuraEffect UnnamedParameter, BoxedValue<double> amount, BoxedValue<bool> canBeRecalculated)
	{
		var caster = Caster;
		var counteractAura = caster.GetAura(MonkSpells.COUNTERACT_MAGIC);

		if (counteractAura != null)
		{
			var appliedAuras = OwnerAsUnit.GetAppliedAurasQuery();

			foreach (var kvp in appliedAuras.IsPositive(false).GetResults())
			{
				var baseAura = kvp.Base;

				if ((baseAura.SpellInfo.GetSchoolMask() & SpellSchoolMask.Shadow) == 0)
					continue;

				if ((baseAura.SpellInfo.GetDispelMask() & (1 << (int)DispelType.Magic)) == 0)
					continue;

				if (baseAura.HasEffectType(AuraType.PeriodicDamage) || baseAura.HasEffectType(AuraType.PeriodicDamagePercent))
				{
					var effInfo = counteractAura.GetEffect(0);

					if (effInfo != null)
						amount.Value = MathFunctions.AddPct(amount.Value, effInfo.Amount);
				}
			}
		}
	}
}