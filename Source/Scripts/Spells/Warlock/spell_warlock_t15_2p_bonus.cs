// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warlock;

// Called by Dark Soul - 77801 ( Generic ), 113858 ( Instability ), 113860 ( Misery ), 113861 ( Knowledge )
[Script]
public class SpellWarlockT152PBonus : SpellScript, ISpellAfterCast
{
    public void AfterCast()
    {
        var caster = Caster;

        if (caster != null)
            if (caster.HasAura(WarlockSpells.T15_2_P_BONUS)) // Check if caster has bonus aura
                caster.AddAura(WarlockSpells.T15_2_P_BONUS_TRIGGERED, caster);
    }
}