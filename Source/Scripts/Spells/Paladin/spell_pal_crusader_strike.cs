// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Paladin;

// Crusader Strike - 35395
[SpellScript(35395)]
public class SpellPalCrusaderStrike : SpellScript, ISpellOnHit
{
    public void OnHit()
    {
        var caster = Caster;

        if (caster.HasAura(PaladinSpells.CRUSADERS_MIGHT))
        {
            if (caster.SpellHistory.HasCooldown(PaladinSpells.HOLY_SHOCK))
                caster.SpellHistory.ModifyCooldown(PaladinSpells.HOLY_SHOCK, TimeSpan.FromMilliseconds(-1 * Time.IN_MILLISECONDS));

            if (caster.SpellHistory.HasCooldown(PaladinSpells.LIGHT_OF_DAWN))
                caster.SpellHistory.ModifyCooldown(PaladinSpells.LIGHT_OF_DAWN, TimeSpan.FromMilliseconds(-1 * Time.IN_MILLISECONDS));
        }
    }
}