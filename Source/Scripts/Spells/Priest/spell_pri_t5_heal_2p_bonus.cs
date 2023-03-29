﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Priest;

[Script] // 37594 - Greater Heal Refund
internal class spell_pri_t5_heal_2p_bonus : AuraScript, IAuraCheckProc, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();


    public bool CheckProc(ProcEventInfo eventInfo)
    {
        var healInfo = eventInfo.HealInfo;

        if (healInfo != null)
        {
            var healTarget = healInfo.Target;

            if (healTarget)
                // @todo: fix me later if (healInfo.GetEffectiveHeal())
                if (healTarget.Health >= healTarget.MaxHealth)
                    return true;
        }

        return false;
    }

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.ProcTriggerSpell, AuraScriptHookType.EffectProc));
    }

    private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        PreventDefaultAction();
        Target.CastSpell(Target, PriestSpells.ITEM_EFFICIENCY, new CastSpellExtraArgs(aurEff));
    }
}