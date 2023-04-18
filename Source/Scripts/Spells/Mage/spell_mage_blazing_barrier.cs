// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;
using Framework.Models;

namespace Scripts.Spells.Mage;

[Script] // 235313 - Blazing Barrier
internal class SpellMageBlazingBarrier : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();


    public override void Register()
    {
        AuraEffects.Add(new AuraEffectCalcAmountHandler(CalculateAmount, 0, AuraType.SchoolAbsorb));
        AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 1, AuraType.ProcTriggerSpell, AuraScriptHookType.EffectProc));
    }

    private void CalculateAmount(AuraEffect aurEff, BoxedValue<double> amount, BoxedValue<bool> canBeRecalculated)
    {
        canBeRecalculated.Value = false;
        var caster = Caster;

        if (caster)
            amount.Value = (caster.SpellBaseHealingBonusDone(SpellInfo.SchoolMask) * 7.0f);
    }

    private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        PreventDefaultAction();
        var caster = eventInfo.DamageInfo.Victim;
        var target = eventInfo.DamageInfo.Attacker;

        if (caster && target)
            caster.SpellFactory.CastSpell(target, MageSpells.BlazingBarrierTrigger, true);
    }
}