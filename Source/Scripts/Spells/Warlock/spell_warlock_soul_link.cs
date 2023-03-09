// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Warlock;

// 108446 - Soul Link
[SpellScript(108446)]
public class spell_warlock_soul_link : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectSplitHandler(HandleSplit, 0));
	}

	private double HandleSplit(AuraEffect UnnamedParameter, DamageInfo UnnamedParameter2, double splitAmount)
	{
		var pet = OwnerAsUnit;

		if (pet == null)
			return splitAmount;

		var owner = pet.OwnerUnit;

		if (owner == null)
			return splitAmount;

		if (owner.HasAura(WarlockSpells.SOUL_SKIN) && owner.HealthBelowPct(35))
			splitAmount *= 2;

		return splitAmount;
	}
}