// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Shaman;

// 40463 - Shaman Tier 6 Trinket
[SpellScript(40463)]
internal class SpellShaItemT6Trinket : AuraScript, IHasAuraEffects
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

        // Lesser Healing Wave
        if (spellInfo.SpellFamilyFlags[0].HasAnyFlag(0x00000080u))
        {
            spellId = ShamanSpells.ENERGY_SURGE;
            chance = 10;
        }
        // Lightning Bolt
        else if (spellInfo.SpellFamilyFlags[0].HasAnyFlag(0x00000001u))
        {
            spellId = ShamanSpells.ENERGY_SURGE;
            chance = 15;
        }
        // Stormstrike
        else if (spellInfo.SpellFamilyFlags[1].HasAnyFlag(0x00000010u))
        {
            spellId = ShamanSpells.POWER_SURGE;
            chance = 50;
        }
        else
        {
            return;
        }

        if (RandomHelper.randChance(chance))
            eventInfo.Actor.SpellFactory.CastSpell((Unit)null, spellId, true);
    }
}