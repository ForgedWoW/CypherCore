﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Paladin;

[SpellScript(223817)] // 223817 - Divine Purpose
internal class spell_pal_divine_purpose : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();


    public override void Register()
    {
        AuraEffects.Add(new AuraCheckEffectProcHandler(CheckProc, 0, AuraType.Dummy));
        AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
    }

    private bool CheckProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        var procSpell = eventInfo.ProcSpell;

        if (!procSpell)
            return false;

        if (!procSpell.HasPowerTypeCost(PowerType.HolyPower))
            return false;

        return RandomHelper.randChance(aurEff.Amount);
    }

    private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        eventInfo.Actor
                 .CastSpell(eventInfo.Actor,
                            PaladinSpells.DivinePurposeTriggerred,
                            new CastSpellExtraArgs(TriggerCastFlags.IgnoreCastInProgress).SetTriggeringSpell(eventInfo.ProcSpell));
    }
}