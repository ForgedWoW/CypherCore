// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Paladin;

// 248033 - Awakening
[SpellScript(248033)]
internal class SpellPalAwakening : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();


    public override void Register()
    {
        AuraEffects.Add(new AuraCheckEffectProcHandler(CheckProc, 0, AuraType.Dummy));
        AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
    }

    private bool CheckProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        return RandomHelper.randChance(aurEff.Amount);
    }

    private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        var extraDuration = TimeSpan.Zero;
        var durationEffect = GetEffect(1);

        if (durationEffect != null)
            extraDuration = TimeSpan.FromSeconds(durationEffect.Amount);

        var avengingWrath = Target.GetAura(PaladinSpells.AVENGING_WRATH);

        if (avengingWrath != null)
        {
            avengingWrath.SetDuration((int)(avengingWrath.Duration + extraDuration.TotalMilliseconds));
            avengingWrath.SetMaxDuration((int)(avengingWrath.MaxDuration + extraDuration.TotalMilliseconds));
        }
        else
            Target
                .SpellFactory.CastSpell(Target,
                                        PaladinSpells.AVENGING_WRATH,
                                        new CastSpellExtraArgs(TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.IgnoreSpellAndCategoryCD)
                                            .SetTriggeringSpell(eventInfo.ProcSpell)
                                            .AddSpellMod(SpellValueMod.Duration, (int)extraDuration.TotalMilliseconds));
    }
}