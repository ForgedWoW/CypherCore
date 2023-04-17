// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;

namespace Scripts.Spells.Warlock;

// Demonic Calling - 205145
[SpellScript(205145)]
public class SpellWarlDemonicCallingAuraScript : AuraScript, IAuraCheckProc
{
    public bool CheckProc(ProcEventInfo eventInfo)
    {
        var caster = Caster;

        if (caster == null)
            return false;

        if (eventInfo.SpellInfo != null &&
            (eventInfo.SpellInfo.Id == WarlockSpells.DEMONBOLT || eventInfo.SpellInfo.Id == WarlockSpells.SHADOW_BOLT) &&
            RandomHelper.randChance(20))
            caster.SpellFactory.CastSpell(caster, WarlockSpells.DEMONIC_CALLING_TRIGGER, true);

        return false;
    }
}