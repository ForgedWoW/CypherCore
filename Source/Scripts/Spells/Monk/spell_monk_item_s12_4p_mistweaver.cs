// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Monk;

[SpellScript(116680)]
public class SpellMonkItemS124PMistweaver : SpellScript, ISpellOnHit
{
    public void OnHit()
    {
        var player = Caster.AsPlayer;

        if (player != null)
            if (player.HasAura(MonkSpells.ITEM_4_S12_MISTWEAVER))
                player.SpellFactory.CastSpell(player, MonkSpells.ZEN_FOCUS, true);
    }
}