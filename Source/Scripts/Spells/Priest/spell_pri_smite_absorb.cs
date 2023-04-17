// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;

namespace Scripts.Spells.Priest;

[SpellScript(208771)]
public class SpellPriSmiteAbsorb : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectAbsorbHandler(HandleAbsorb, 0));
    }

    private double HandleAbsorb(AuraEffect unnamedParameter, DamageInfo dmgInfo, double absorbAmount)
    {
        var caster = Caster;
        var attacker = dmgInfo.Attacker;

        if (caster == null || attacker == null)
            return absorbAmount;

        if (!attacker.HasAura(PriestSpells.SMITE_AURA, caster.GUID))
        {
            absorbAmount = 0;
        }
        else
        {
            var aur = attacker.GetAura(PriestSpells.SMITE_AURA, caster.GUID);

            if (aur != null)
            {
                var aurEff = aur.GetEffect(0);

                if (aurEff != null)
                {
                    var absorb = Math.Max(0, aurEff.Amount - (int)dmgInfo.Damage);

                    if (absorb <= 0)
                    {
                        absorbAmount = (uint)aurEff.Amount;
                        aur.Remove();
                    }
                    else
                    {
                        aurEff.SetAmount(absorb);
                        aur.SetNeedClientUpdateForTargets();
                    }
                }
            }
        }

        return absorbAmount;
    }
}