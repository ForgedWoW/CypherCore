// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;
using Framework.Models;

namespace Scripts.Spells.Monk;

[SpellScript(116849)]
public class SpellMonkLifeCocoon : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectCalcAmountHandler(CalcAbsorb, 0, AuraType.SchoolAbsorb));
    }

    private void CalcAbsorb(AuraEffect unnamedParameter, BoxedValue<double> amount, BoxedValue<bool> canBeRecalculated)
    {
        if (!Caster)
            return;

        var caster = Caster;

        //Formula:  [(((Spell power * 11) + 0)) * (1 + Versatility)]
        //Simplified to : [(Spellpower * 11)]
        //Versatility will be taken into account at a later date.
        amount.Value += caster.SpellBaseDamageBonusDone(SpellInfo.GetSchoolMask()) * 11;
        canBeRecalculated.Value = false;
    }
}