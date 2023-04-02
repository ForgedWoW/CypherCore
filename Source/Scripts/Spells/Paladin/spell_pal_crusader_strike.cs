// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Paladin;

// Crusader Strike - 35395
[SpellScript(35395)]
public class spell_pal_crusader_strike : SpellScript, ISpellOnHit
{
    public void OnHit()
    {
        var caster = Caster;

        if (caster.HasAura(PaladinSpells.CRUSADERS_MIGHT))
        {
            if (caster.SpellHistory.HasCooldown(PaladinSpells.HolyShock))
                caster.SpellHistory.ModifyCooldown(PaladinSpells.HolyShock, TimeSpan.FromMilliseconds(-1 * Time.IN_MILLISECONDS));

            if (caster.SpellHistory.HasCooldown(PaladinSpells.LIGHT_OF_DAWN))
                caster.SpellHistory.ModifyCooldown(PaladinSpells.LIGHT_OF_DAWN, TimeSpan.FromMilliseconds(-1 * Time.IN_MILLISECONDS));
        }
    }
}