﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Generic;

[Script] // 24379 - Restoration
internal class spell_gen_restoration : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectPeriodicHandler(PeriodicTick, 0, AuraType.PeriodicTriggerSpell));
	}

	private void PeriodicTick(AuraEffect aurEff)
	{
		PreventDefaultAction();

		var target = Target;

		if (target == null)
			return;

		var heal = (uint)target.CountPctFromMaxHealth(10);
		HealInfo healInfo = new(target, target, heal, SpellInfo, SpellInfo.GetSchoolMask());
		target.HealBySpell(healInfo);

		/// @todo: should proc other Auras?
		var mana = target.GetMaxPower(PowerType.Mana);

		if (mana != 0)
		{
			mana /= 10;
			target.EnergizeBySpell(target, SpellInfo, mana, PowerType.Mana);
		}
	}
}