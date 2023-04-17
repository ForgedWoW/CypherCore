// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.DeathKnight;

[SpellScript(194311)]
public class SpellDkFesteringWoundDamage : SpellScript, ISpellOnHit
{
    public void OnHit()
    {
        if (Caster.HasAura(DeathKnightSpells.PESTILENT_PUSTULES) && RandomHelper.randChance(10))
            Caster.SpellFactory.CastSpell(null, DeathKnightSpells.RUNIC_CORRUPTION_MOD_RUNES, true);
    }
}