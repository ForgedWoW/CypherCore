// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;

namespace Scripts.Spells.Warlock;

[SpellScript(17962)]
public class SpellWarlConflagrateAura : SpellScript
{
    public void OnHit()
    {
        var player = Caster.AsPlayer;

        if (player != null)
        {
            var target = HitUnit;

            if (target != null)
            {
                if (!target.HasAura(WarlockSpells.IMMOLATE) && !player.HasAura(WarlockSpells.GLYPH_OF_CONFLAGRATE))
                    if (target.GetAura(WarlockSpells.CONFLAGRATE) != null)
                        target.RemoveAura(WarlockSpells.CONFLAGRATE);

                if (!target.HasAura(WarlockSpells.IMMOLATE_FIRE_AND_BRIMSTONE))
                    target.RemoveAura(WarlockSpells.CONFLAGRATE_FIRE_AND_BRIMSTONE);
            }
        }
    }
}