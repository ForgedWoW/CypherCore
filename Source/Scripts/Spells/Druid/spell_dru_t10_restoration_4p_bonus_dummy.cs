﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Druid;

[Script] // 70664 - Druid T10 Restoration 4P Bonus (Rejuvenation)
internal class spell_dru_t10_restoration_4p_bonus_dummy : AuraScript, IAuraCheckProc, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();


    public bool CheckProc(ProcEventInfo eventInfo)
    {
        var spellInfo = eventInfo.SpellInfo;

        if (spellInfo == null ||
            spellInfo.Id == DruidSpellIds.RejuvenationT10Proc)
            return false;

        var healInfo = eventInfo.HealInfo;

        if (healInfo == null ||
            healInfo.Heal == 0)
            return false;

        var caster = eventInfo.Actor.AsPlayer;

        if (!caster)
            return false;

        return caster.Group || caster != eventInfo.ProcTarget;
    }

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
    }

    private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        PreventDefaultAction();

        var amount = (int)eventInfo.HealInfo.Heal;
        CastSpellExtraArgs args = new(aurEff);
        args.AddSpellMod(SpellValueMod.BasePoint0, (int)eventInfo.HealInfo.Heal);
        eventInfo.Actor.CastSpell((Unit)null, DruidSpellIds.RejuvenationT10Proc, args);
    }
}