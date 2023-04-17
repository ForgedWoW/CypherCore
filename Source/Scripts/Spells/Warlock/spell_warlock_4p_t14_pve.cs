// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warlock;

// Raging Soul handled via Dark Soul: Instability (113858) \ Knowledge (113861) \ Misery (113860)
[Script]
public class SpellWarlock4PT14Pve : SpellScript, ISpellAfterCast
{
    public void AfterCast()
    {
        var caster = Caster;

        if (caster != null)
            if (caster.HasAura(WarlockSpells.T14_BONUS))
                caster.SpellFactory.CastSpell(caster, WarlockSpells.RAGING_SOUL, true);
    }
}