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

namespace Scripts.Spells.Druid;

[Script] // 40442 - Druid Tier 6 Trinket
internal class SpellDruItemT6Trinket : AuraScript, IHasAuraEffects
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

        // Starfire
        if (spellInfo.SpellFamilyFlags[0].HasAnyFlag(0x00000004u))
        {
            spellId = DruidSpellIds.BlessingOfRemulos;
            chance = 25;
        }
        // Rejuvenation
        else if (spellInfo.SpellFamilyFlags[0].HasAnyFlag(0x00000010u))
        {
            spellId = DruidSpellIds.BlessingOfElune;
            chance = 25;
        }
        // Mangle (Bear) and Mangle (Cat)
        else if (spellInfo.SpellFamilyFlags[1].HasAnyFlag(0x00000440u))
        {
            spellId = DruidSpellIds.BlessingOfCenarius;
            chance = 40;
        }
        else
        {
            return;
        }

        if (RandomHelper.randChance(chance))
            eventInfo.Actor.SpellFactory.CastSpell((Unit)null, spellId, new CastSpellExtraArgs(aurEff));
    }
}