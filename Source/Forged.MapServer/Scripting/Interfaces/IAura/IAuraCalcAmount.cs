// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;
using Framework.Models;

namespace Forged.MapServer.Scripting.Interfaces.IAura;

public interface IAuraCalcAmount : IAuraEffectHandler
{
    void HandleCalcAmount(AuraEffect aurEff, ref double amount, ref bool canBeRecalculated);
}

public class AuraEffectCalcAmountHandler : AuraEffectHandler, IAuraCalcAmount
{
    private readonly Action<AuraEffect, BoxedValue<double>, BoxedValue<bool>> _fn;

    public AuraEffectCalcAmountHandler(Action<AuraEffect, BoxedValue<double>, BoxedValue<bool>> fn, int effectIndex, AuraType auraType) : base(effectIndex, auraType, AuraScriptHookType.EffectCalcAmount)
    {
        _fn = fn;
    }

    public void HandleCalcAmount(AuraEffect aurEff, ref double amount, ref bool canBeRecalculated)
    {
        var canbeCalc = new BoxedValue<bool>(canBeRecalculated);
        var boxedValue = new BoxedValue<double>(amount);

        _fn(aurEff, boxedValue, canbeCalc);

        amount = boxedValue.Value;
        canBeRecalculated = canbeCalc.Value;
    }
}