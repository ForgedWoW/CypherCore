// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Shaman;

[SpellScript(52042)]
public class spell_sha_healing_stream : SpellScript, ISpellOnHit
{
    public void OnHit()
    {
        if (!Caster.OwnerUnit)
            return;

        var _player = Caster.OwnerUnit.AsPlayer;

        if (_player != null)
        {
            var target = HitUnit;

            if (target != null)
                // Glyph of Healing Stream Totem
                if (target.GUID != _player.GUID && _player.HasAura(ShamanSpells.GLYPH_OF_HEALING_STREAM_TOTEM))
                    _player.CastSpell(target, ShamanSpells.GLYPH_OF_HEALING_STREAM, true);
        }
    }
}