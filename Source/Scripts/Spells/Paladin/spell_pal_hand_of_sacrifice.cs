// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Paladin;

[SpellScript(6940)] // 6940 - Hand of Sacrifice
internal class spell_pal_hand_of_sacrifice : AuraScript, IHasAuraEffects
{
	private int remainingAmount;
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override bool Load()
	{
		var caster = Caster;

		if (caster)
		{
			remainingAmount = (int)caster.GetMaxHealth();

			return true;
		}

		return false;
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectSplitHandler(Split, 0));
	}

	private double Split(AuraEffect aurEff, DamageInfo dmgInfo, double splitAmount)
	{
		remainingAmount -= (int)splitAmount;

		if (remainingAmount <= 0)
			Target.RemoveAura(PaladinSpells.HandOfSacrifice);

		return splitAmount;
	}
}