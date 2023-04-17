// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.DataStorage;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;
using Framework.Models;

namespace Scripts.Spells.Mage;

internal class SpellMageFlameOn : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();


    public override void Register()
    {
        AuraEffects.Add(new AuraEffectCalcAmountHandler(CalculateAmount, 1, AuraType.ChargeRecoveryMultiplier));
    }

    private void CalculateAmount(AuraEffect aurEff, BoxedValue<double> amount, BoxedValue<bool> canBeRecalculated)
    {
        canBeRecalculated.Value = false;
        amount.Value = -MathFunctions.GetPctOf(GetEffectInfo(2).CalcValue() * Time.IN_MILLISECONDS, CliDB.SpellCategoryStorage.LookupByKey(Global.SpellMgr.GetSpellInfo(MageSpells.FireBlast, Difficulty.None).ChargeCategoryId).ChargeRecoveryTime);
    }
}