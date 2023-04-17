// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Monk;

[SpellScript(191837)]
public class SpellMonkEssenceFont : SpellScript, ISpellOnCast
{
    public void OnCast()
    {
        var caster = Caster;

        if (caster != null)
        {
            caster.AddAura(MonkSpells.ESSENCE_FONT_PERIODIC_HEAL, null);
            var uLi = new List<Unit>();
            byte targetLimit = 6;
            uLi.RandomResize(targetLimit);
            caster.GetFriendlyUnitListInRange(uLi, 30.0f, false);

            foreach (var targets in uLi)
                caster.AddAura(MonkSpells.ESSENCE_FONT_PERIODIC_HEAL, targets);
        }
    }
}