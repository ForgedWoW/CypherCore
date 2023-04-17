// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Shaman;

// 215864 Rainfall
[SpellScript(215864)]
public class AuraShaRainfallAuraScript : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectPeriodicHandler(HandleHealPeriodic, 1, AuraType.PeriodicDummy));
        AuraEffects.Add(new AuraEffectApplyHandler(HandleAfterRemove, 1, AuraType.PeriodicDummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
        AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 2, AuraType.Dummy, AuraScriptHookType.EffectProc));
    }

    private void HandleHealPeriodic(AuraEffect unnamedParameter)
    {
        var caster = Caster;

        if (caster != null)
        {
            var rainfallTrigger = caster.GetSummonedCreatureByEntry(ShamanNpcs.NPC_RAINFALL);

            if (rainfallTrigger != null)
                caster.SpellFactory.CastSpell(rainfallTrigger.Location, ShamanSpells.RAINFALL_HEAL, true);
        }
    }

    private void HandleAfterRemove(AuraEffect unnamedParameter, AuraEffectHandleModes unnamedParameter2)
    {
        var caster = Caster;

        if (caster != null)
        {
            var rainfallTrigger = caster.GetSummonedCreatureByEntry(ShamanNpcs.NPC_RAINFALL);

            if (rainfallTrigger != null)
                rainfallTrigger.DespawnOrUnsummon(TimeSpan.FromMilliseconds(100));
        }
    }

    private void HandleProc(AuraEffect aurEff, ProcEventInfo unnamedParameter)
    {
        SetDuration(Duration + GetEffect(2).BaseAmount * Time.IN_MILLISECONDS, (GetEffect(3).BaseAmount * Time.IN_MILLISECONDS) > 0);
    }
}