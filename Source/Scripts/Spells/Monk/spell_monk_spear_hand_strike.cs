// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Monk;

[SpellScript(116705)]
public class SpellMonkSpearHandStrike : SpellScript, ISpellOnHit
{
    public void OnHit()
    {
        var player = Caster.AsPlayer;

        if (player != null)
        {
            var target = HitUnit;

            if (target != null)
                if (target.IsInFront(player))
                {
                    player.SpellFactory.CastSpell(target, MonkSpells.SPEAR_HAND_STRIKE_SILENCE, true);
                    player.SpellHistory.AddCooldown(116705, 0, TimeSpan.FromSeconds(15));
                }
        }
    }
}