// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;

namespace Scripts.Spells.DeathKnight;

/// Blood Shield - 77535
[SpellScript(77535)]
public class SpellDkBloodShield : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectAbsorbHandler(AfterAbsorb, 0));
    }

    private double AfterAbsorb(AuraEffect pAurEff, DamageInfo unnamedParameter, double pAbsorbAmount)
    {
        var lTarget = Target;

        if (lTarget != null)
        {
            /// While Vampiric Blood is active, your Blood Shield cannot be reduced below 3% of your maximum health.
            var lAurEff = lTarget.GetAuraEffect(ESpells.T17_BLOOD4_P, 0);

            if (lAurEff != null)
            {
                var lFutureAbsorb = Convert.ToInt32(pAurEff.Amount - pAbsorbAmount);
                var lMinimaAbsorb = Convert.ToInt32(lTarget.CountPctFromMaxHealth(lAurEff.Amount));

                /// We need to add some absorb amount to correct the absorb amount after that, and set it to 3% of max health
                if (lFutureAbsorb < lMinimaAbsorb)
                {
                    var lAddedAbsorb = lMinimaAbsorb - lFutureAbsorb;
                    pAurEff.ChangeAmount(pAurEff.Amount + lAddedAbsorb);
                }
            }
        }

        return pAbsorbAmount;
    }

    private struct ESpells
    {
        public const uint T17_BLOOD4_P = 165571;
    }
}