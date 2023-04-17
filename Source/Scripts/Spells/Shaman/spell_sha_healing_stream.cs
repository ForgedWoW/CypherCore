// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Shaman;

[SpellScript(52042)]
public class SpellShaHealingStream : SpellScript, ISpellOnHit
{
    public void OnHit()
    {
        if (!Caster.OwnerUnit)
            return;

        var player = Caster.OwnerUnit.AsPlayer;

        if (player != null)
        {
            var target = HitUnit;

            if (target != null)
                // Glyph of Healing Stream Totem
                if (target.GUID != player.GUID && player.HasAura(ShamanSpells.GLYPH_OF_HEALING_STREAM_TOTEM))
                    player.SpellFactory.CastSpell(target, ShamanSpells.GLYPH_OF_HEALING_STREAM, true);
        }
    }
}