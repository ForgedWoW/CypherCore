// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Framework.Constants;

namespace Scripts.Spells.DeathKnight;

[SpellScript(47568)]
public class SpellDkEmpowerRuneWeapon : SpellScript
{
    public void OnHit()
    {
        var caster = Caster;

        if (caster != null)
        {
            var player = caster.AsPlayer;

            if (player != null)
            {
                for (byte i = 0; i < PlayerConst.MaxRunes; ++i)
                    player.SetRuneCooldown(i, 0);

                player.ResyncRunes();
            }
        }
    }
}