// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Forged.MapServer.Spells;
using Framework.Constants;

namespace Scripts.Spells.Rogue;

[SpellScript(24532)]
internal class SpellRogBurstOfEnergy : SpellScript, ISpellEnergizedBySpell
{
    public void EnergizeBySpell(Unit target, SpellInfo spellInfo, ref double amount, PowerType powerType)
    {
        // Instantly increases your energy by ${60-4*$max(0,$min(15,$PL-60))}.
        amount -= 4 * Math.Max(0, Math.Min(15, target.Level - 60));
    }
}