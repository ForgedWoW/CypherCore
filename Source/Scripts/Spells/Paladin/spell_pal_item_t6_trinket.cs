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

[SpellScript(40470)] // 40470 - Paladin Tier 6 Trinket
internal class SpellPalItemT6Trinket : AuraScript, IHasAuraEffects
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
            spellId = PaladinSpells.ENDURING_LIGHT;
            chance = 15;
        }
        // Judgements
        else if (spellInfo.SpellFamilyFlags[0].HasAnyFlag(0x00800000u))
        {
            spellId = PaladinSpells.ENDURING_JUDGEMENT;
            chance = 50;
        }
        else
        {
            return;
        }

        if (RandomHelper.randChance(chance))
            eventInfo.Actor.SpellFactory.CastSpell(eventInfo.ProcTarget, spellId, new CastSpellExtraArgs(aurEff));
    }
}