// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Shaman;

[SpellScript(55448)]
public class SpellShaGlyphOfLakestrider : SpellScript, ISpellOnHit
{
    public void OnHit()
    {
        var player = Caster.AsPlayer;

        if (player != null)
            if (player.HasAura(ShamanSpells.GLYPH_OF_LAKESTRIDER))
                player.SpellFactory.CastSpell(player, ShamanSpells.WATER_WALKING, true);
    }
}