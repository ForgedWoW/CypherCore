// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Framework.Constants;

namespace Scripts.Spells.Warlock;

// Immolate proc - 193541
[SpellScript(193541)]
public class SpellWarlImmolateAura : AuraScript, IAuraCheckProc
{
    public bool CheckProc(ProcEventInfo eventInfo)
    {
        if (eventInfo.SpellInfo != null && eventInfo.SpellInfo.Id == WarlockSpells.IMMOLATE_DOT)
        {
            var rollChance = SpellInfo.GetEffect(0).BasePoints;
            rollChance = Caster.ModifyPower(PowerType.SoulShards, 25);
            var crit = (eventInfo.HitMask & ProcFlagsHit.Critical) != 0;

            return crit ? RandomHelper.randChance(rollChance * 2) : RandomHelper.randChance(rollChance);
        }

        return false;
    }
}