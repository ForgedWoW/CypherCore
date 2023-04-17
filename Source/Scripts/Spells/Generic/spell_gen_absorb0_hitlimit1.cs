// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Generic;

[Script]
internal class SpellGenAbsorb0Hitlimit1 : AuraScript, IHasAuraEffects
{
    private double _limit;
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override bool Load()
    {
        // Max Absorb stored in 1 dummy effect
        _limit = SpellInfo.GetEffect(1).CalcValue();

        return true;
    }

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectAbsorbHandler(Absorb, 0, false, AuraScriptHookType.EffectAbsorb));
    }

    private double Absorb(AuraEffect aurEff, DamageInfo dmgInfo, double absorbAmount)
    {
        return Math.Min(_limit, absorbAmount);
    }
}