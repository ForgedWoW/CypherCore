// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Warlock;

// Burning Rush - 111400
[SpellScript(111400)]
public class aura_warl_burning_rush : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectPeriodicHandler(OnTick, 1, AuraType.PeriodicDamagePercent));
	}

	private void OnTick(AuraEffect UnnamedParameter)
	{
		if (Caster)
		{
			// This way if the current tick takes you below 4%, next tick won't execute
			var basepoints = Caster.CountPctFromMaxHealth(4);

			if (Caster.Health <= basepoints || Caster.Health - basepoints <= basepoints)
				Aura.SetDuration(0);
		}
	}
}