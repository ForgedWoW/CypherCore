// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Druid;

// Teleport : Moonglade - 18960
[SpellScript(18960)]
public class SpellDruTeleportMoonglade : SpellScript, ISpellAfterCast
{
    public void AfterCast()
    {
        var player = Caster.AsPlayer;

        if (player != null)
            player.TeleportTo(1, 7964.063f, -2491.099f, 487.83f, player.Location.Orientation);
    }
}