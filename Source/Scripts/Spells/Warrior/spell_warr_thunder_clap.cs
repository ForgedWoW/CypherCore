// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warrior;

// 6343 - Thunder Clap
[SpellScript(6343)]
public class SpellWarrThunderClap : SpellScript, ISpellOnHit
{
    public void OnHit()
    {
        var player = Caster.AsPlayer;

        if (player != null)
        {
            var target = HitUnit;

            if (target != null)
            {
                player.SpellFactory.CastSpell(target, WarriorSpells.WEAKENED_BLOWS, true);

                if (player.HasAura(WarriorSpells.THUNDERSTRUCK))
                    player.SpellFactory.CastSpell(target, WarriorSpells.THUNDERSTRUCK_STUN, true);
            }
        }
    }
}