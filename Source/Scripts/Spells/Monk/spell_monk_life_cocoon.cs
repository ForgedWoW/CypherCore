﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Framework.Models;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Monk;

[SpellScript(116849)]
public class spell_monk_life_cocoon : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectCalcAmountHandler(CalcAbsorb, 0, AuraType.SchoolAbsorb));
    }

    private void CalcAbsorb(AuraEffect UnnamedParameter, BoxedValue<double> amount, BoxedValue<bool> canBeRecalculated)
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