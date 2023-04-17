// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Monk;

[SpellScript(121253)]
public class SpellMonkKegSmash : SpellScript, ISpellOnHit
{
    public void OnHit()
    {
        var caster = Caster;

        if (caster != null)
        {
            var player = caster.AsPlayer;

            if (player != null)
            {
                var target = HitUnit;

                if (target != null)
                {
                    player.SpellFactory.CastSpell(target, MonkSpells.KEG_SMASH_VISUAL, true);
                    player.SpellFactory.CastSpell(target, MonkSpells.WEAKENED_BLOWS, true);
                    player.SpellFactory.CastSpell(player, MonkSpells.KEG_SMASH_ENERGIZE, true);

                    // Prevent to receive 2 CHI more than once time per cast
                    player. // Prevent to receive 2 CHI more than once time per cast
                        SpellHistory.AddCooldown(MonkSpells.KEG_SMASH_ENERGIZE, 0, TimeSpan.FromSeconds(1));

                    player.SpellFactory.CastSpell(target, MonkSpells.DIZZYING_HAZE, true);
                }
            }
        }
    }
}