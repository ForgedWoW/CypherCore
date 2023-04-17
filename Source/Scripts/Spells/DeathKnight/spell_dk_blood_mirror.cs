// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;
using Framework.Models;

namespace Scripts.Spells.DeathKnight;

[SpellScript(206977)]
public class SpellDkBloodMirror : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectCalcAmountHandler(CalcAmount, 1, AuraType.SchoolAbsorb));
        AuraEffects.Add(new AuraEffectAbsorbHandler(HandleAbsorb, 1));
    }

    private void CalcAmount(AuraEffect unnamedParameter, BoxedValue<double> amount, BoxedValue<bool> unnamedParameter2)
    {
        amount.Value = -1;
    }


    private double HandleAbsorb(AuraEffect aurEff, DamageInfo dmgInfo, double absorbAmount)
    {
        absorbAmount = dmgInfo.Damage * ((uint)aurEff.BaseAmount / 100);
        var caster = Caster;
        var target = Target;

        if (caster != null && target != null)
            caster.SpellFactory.CastSpell(target, DeathKnightSpells.BLOOD_MIRROR_DAMAGE, (int)absorbAmount, true);

        return absorbAmount;
    }
}