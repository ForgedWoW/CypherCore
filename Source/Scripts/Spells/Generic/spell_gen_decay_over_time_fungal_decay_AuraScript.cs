// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Generic;

[Script] // 32065 - Fungal Decay
internal class SpellGenDecayOverTimeFungalDecayAuraScript : AuraScript, IAuraCheckProc, IAuraOnProc, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public bool CheckProc(ProcEventInfo eventInfo)
    {
        return eventInfo.SpellInfo == SpellInfo;
    }

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectApplyHandler(ModDuration, 0, AuraType.ModDecreaseSpeed, AuraEffectHandleModes.RealOrReapplyMask, AuraScriptHookType.EffectApply));
    }

    public void OnProc(ProcEventInfo info)
    {
        PreventDefaultAction();
        ModStackAmount(-1);
    }

    private void ModDuration(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        // only on actual reapply, not on stack decay
        if (Duration == MaxDuration)
        {
            MaxDuration = Misc.AURA_DURATION;
            SetDuration(Misc.AURA_DURATION);
        }
    }
}