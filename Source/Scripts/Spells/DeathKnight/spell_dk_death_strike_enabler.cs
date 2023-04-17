// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using System.Linq;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;
using Framework.Models;

namespace Scripts.Spells.DeathKnight;

[Script] // 89832 - Death Strike Enabler - DEATH_STRIKE_ENABLER
internal class SpellDkDeathStrikeEnabler : AuraScript, IAuraCheckProc, IHasAuraEffects
{
    // Amount of seconds we calculate Damage over
    private double[] _damagePerSecond = new double[5];

    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public bool CheckProc(ProcEventInfo eventInfo)
    {
        return eventInfo.DamageInfo != null;
    }

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.PeriodicDummy, AuraScriptHookType.EffectProc));
        AuraEffects.Add(new AuraEffectCalcAmountHandler(HandleCalcAmount, 0, AuraType.PeriodicDummy));
        AuraEffects.Add(new AuraEffectUpdatePeriodicHandler(Update, 0, AuraType.PeriodicDummy));
    }

    private void Update(AuraEffect aurEff)
    {
        // Move backwards all datas by one from [23][0][0][0][0] -> [0][23][0][0][0]
        _damagePerSecond = Enumerable.Range(1, _damagePerSecond.Length).Select(i => _damagePerSecond[i % _damagePerSecond.Length]).ToArray();
        _damagePerSecond[0] = 0;
    }

    private void HandleCalcAmount(AuraEffect aurEff, BoxedValue<double> amount, BoxedValue<bool> canBeRecalculated)
    {
        canBeRecalculated.Value = true;
        amount.Value = Enumerable.Range(1, _damagePerSecond.Length).Sum();
    }

    private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        _damagePerSecond[0] += eventInfo.DamageInfo.Damage;
    }
}