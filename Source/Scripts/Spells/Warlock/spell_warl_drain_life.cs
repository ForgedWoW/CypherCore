// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warlock;

[SpellScript(WarlockSpells.DRAIN_LIFE_ENEMY_AURA)]
internal class spell_warl_drain_life : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects => new List<IAuraEffectHandler>()
	{
		new AuraEffectPeriodicHandler((periodicEff) =>
		{
            if (Caster.TryGetAura(WarlockSpells.DESPERATE_PACT, out var pact) &&
				pact.TryGetEffect(0, out var eff) &&
				pact.TryGetEffect(1, out var effDmg) &&
				periodicEff.TryGetEstimatedAmount(out var amount) &&
				Caster.HealthBelowPct(eff.Amount))
                periodicEff.SetAmount(MathFunctions.ApplyPct(amount, effDmg.Amount)); // its 50/100, needs to be changed to .5/1.0
		}, 0, AuraType.PeriodicLeech)
	};

    public double CalcMultiplier(double multiplier)
	{
		

		return multiplier;
	}
}