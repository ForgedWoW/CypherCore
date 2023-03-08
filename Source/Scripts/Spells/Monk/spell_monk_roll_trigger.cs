// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Framework.Models;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Monk;

[SpellScript(107427)]
public class spell_monk_roll_trigger : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectCalcAmountHandler(CalcSpeed, 0, AuraType.ModSpeedNoControl));
		AuraEffects.Add(new AuraEffectCalcAmountHandler(CalcSpeed2, 2, AuraType.ModMinimumSpeed));
		AuraEffects.Add(new AuraEffectApplyHandler(SendAmount, 4, AuraType.UseNormalMovementSpeed, AuraEffectHandleModes.Real));
	}

	private void CalcSpeed(AuraEffect UnnamedParameter, BoxedValue<double> amount, BoxedValue<bool> canBeRecalculated)
	{
		var caster = Caster;

		if (caster == null)
			return;

		if (caster.HasAura(MonkSpells.ENHANCED_ROLL))
			amount.Value = 277;
	}

	private void CalcSpeed2(AuraEffect UnnamedParameter, BoxedValue<double> amount, BoxedValue<bool> canBeRecalculated)
	{
		var caster = Caster;

		if (caster == null)
			return;

		if (!caster.HasAura(MonkSpells.ENHANCED_ROLL))
			return;

		amount.Value = 377;
	}

	private void SendAmount(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
	{
		var caster = Caster;

		if (caster == null)
			return;

		if (!caster.HasAura(MonkSpells.ENHANCED_ROLL))
			return;

		var aur = Aura;

		if (aur == null)
			return;

		aur.SetMaxDuration(600);
		aur.SetDuration(600);

		var aurApp = Aura.GetApplicationOfTarget(caster.GUID);

		if (aurApp != null)
			aurApp.ClientUpdate();
	}
}