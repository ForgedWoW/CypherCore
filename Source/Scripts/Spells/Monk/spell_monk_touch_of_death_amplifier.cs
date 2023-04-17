// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Monk;

[SpellScript(271233)]
public class SpellMonkTouchOfDeathAmplifier : AuraScript, IHasAuraEffects, IAuraCheckProc
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();


    public bool CheckProc(ProcEventInfo eventInfo)
    {
        return eventInfo.DamageInfo != null && eventInfo.DamageInfo.Damage > 0;
    }

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
    }

    private void HandleProc(AuraEffect unnamedParameter, ProcEventInfo eventInfo)
    {
        var aurEff = Target.GetAuraEffect(MonkSpells.TOUCH_OF_DEATH, 0);

        if (aurEff != null)
        {
            var aurEffAmplifier = eventInfo.Actor.GetAuraEffect(MonkSpells.TOUCH_OF_DEATH_AMPLIFIER, 0);

            if (aurEffAmplifier != null)
            {
                var damage = aurEff.Amount + MathFunctions.CalculatePct(eventInfo.DamageInfo.Damage, aurEffAmplifier.Amount);
                aurEff.SetAmount(damage);
            }
        }
    }
}