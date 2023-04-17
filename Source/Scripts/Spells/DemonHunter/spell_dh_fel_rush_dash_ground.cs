// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;
using Framework.Models;

namespace Scripts.Spells.DemonHunter;

[SpellScript(197922)]
public class SpellDhFelRushDashGround : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectCalcAmountHandler(CalcSpeed, 1, AuraType.ModSpeedNoControl));
        AuraEffects.Add(new AuraEffectCalcAmountHandler(CalcSpeed, 3, AuraType.ModMinimumSpeed));
        AuraEffects.Add(new AuraEffectApplyHandler(AfterRemove, 6, AuraType.ModMinimumSpeedRate, AuraEffectHandleModes.SendForClientMask, AuraScriptHookType.EffectAfterRemove));
    }

    private void AfterRemove(AuraEffect unnamedParameter, AuraEffectHandleModes unnamedParameter2)
    {
        var caster = Caster;

        if (caster != null)
            caster.SetDisableGravity(false);
    }

    private void CalcSpeed(AuraEffect unnamedParameter, BoxedValue<double> amount, BoxedValue<bool> canBeRecalculated)
    {
        amount.Value = 1250;
        RefreshDuration();
    }
}