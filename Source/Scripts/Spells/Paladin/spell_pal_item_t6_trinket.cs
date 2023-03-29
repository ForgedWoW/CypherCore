﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Paladin;

[SpellScript(40470)] // 40470 - Paladin Tier 6 Trinket
internal class spell_pal_item_t6_trinket : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();


    public override void Register()
    {
        AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
    }

    private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        PreventDefaultAction();
        var spellInfo = eventInfo.SpellInfo;

        if (spellInfo == null)
            return;

        uint spellId;
        int chance;

        // Holy Light & Flash of Light
        if (spellInfo.SpellFamilyFlags[0].HasAnyFlag(0xC0000000))
        {
            spellId = PaladinSpells.EnduringLight;
            chance = 15;
        }
        // Judgements
        else if (spellInfo.SpellFamilyFlags[0].HasAnyFlag(0x00800000u))
        {
            spellId = PaladinSpells.EnduringJudgement;
            chance = 50;
        }
        else
        {
            return;
        }

        if (RandomHelper.randChance(chance))
            eventInfo.Actor.CastSpell(eventInfo.ProcTarget, spellId, new CastSpellExtraArgs(aurEff));
    }
}