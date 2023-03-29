// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Priest;

[Script] // 15286 - Vampiric Embrace
internal class spell_pri_vampiric_embrace : AuraScript, IAuraCheckProc, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();


    public bool CheckProc(ProcEventInfo eventInfo)
    {
        // Not proc from Mind Sear
        return !eventInfo.DamageInfo.SpellInfo.SpellFamilyFlags[1].HasAnyFlag(0x80000u);
    }

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectProcHandler(HandleEffectProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
    }

    private void HandleEffectProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        PreventDefaultAction();
        var damageInfo = eventInfo.DamageInfo;

        if (damageInfo == null ||
            damageInfo.Damage == 0)
            return;

        var selfHeal = (int)MathFunctions.CalculatePct(damageInfo.Damage, aurEff.Amount);
        var teamHeal = selfHeal / 2;

        CastSpellExtraArgs args = new(aurEff);
        args.AddSpellMod(SpellValueMod.BasePoint0, teamHeal);
        args.AddSpellMod(SpellValueMod.BasePoint1, selfHeal);
        Target.CastSpell((Unit)null, PriestSpells.VAMPIRIC_EMBRACE_HEAL, args);
    }
}