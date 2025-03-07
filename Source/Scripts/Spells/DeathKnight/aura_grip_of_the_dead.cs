﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.DeathKnight;

[SpellScript(273980)]
public class aura_grip_of_the_dead : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectPeriodicHandler(OnTick, 0, AuraType.PeriodicDummy));
	}


	private void OnTick(AuraEffect UnnamedParameter)
	{
		var target = Target;

		if (target != null)
		{
			var caster = Caster;

			if (caster != null)
				caster.CastSpell(target, DeathKnightSpells.GRIP_OF_THE_DEAD_SLOW, true);
		}
	}
}