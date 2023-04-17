// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Shaman;

[SpellScript(375982)]
public class SpellShaPrimordialWave : SpellScript, ISpellOnHit
{
    public void OnHit()
    {
        var player = Caster.AsPlayer;
        var victim = HitUnit;

        if (player == null || victim == null)
            return;

        if (player.IsFriendlyTo(victim))
        {
            player.SpellFactory.CastSpell(victim, ShamanSpells.PRIMORDIAL_WAVE_HEALING, true);
        }
        else
        {
            player.SpellFactory.CastSpell(victim, ShamanSpells.PRIMORDIAL_WAVE_DAMAGE, true);
            player.AddAura(ShamanSpells.FlameShock, victim);
        }

        player.AddAura(ShamanSpells.PRIMORDIAL_WAVE_AURA, player);
    }
}