﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Paladin
{
    // Crusader Strike - 35395
    [SpellScript(35395)]
    public class spell_pal_crusader_strike : SpellScript, ISpellOnHit
    {
        public void OnHit()
        {
            Unit caster = GetCaster();

            if (caster.HasAura(PaladinSpells.CRUSADERS_MIGHT))
            {
                if (caster.GetSpellHistory().HasCooldown(PaladinSpells.HolyShock))
                {
                    caster.GetSpellHistory().ModifyCooldown(PaladinSpells.HolyShock, TimeSpan.FromMilliseconds(-1 * Time.InMilliseconds));
                }

                if (caster.GetSpellHistory().HasCooldown(PaladinSpells.LIGHT_OF_DAWN))
                {
                    caster.GetSpellHistory().ModifyCooldown(PaladinSpells.LIGHT_OF_DAWN, TimeSpan.FromMilliseconds(-1 * Time.InMilliseconds));
                }
            }
        }
    }
}
