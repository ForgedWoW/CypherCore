// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Priest;

[SpellScript(2050)]
public class SpellPriHolyWordSerenity : SpellScript, ISpellOnCast
{
    public void OnCast()
    {
        var player = Caster.AsPlayer;

        if (player != null)
            player.SpellHistory.ModifyCooldown(PriestSpells.HOLY_WORLD_SALVATION, TimeSpan.FromSeconds(-30000));
    }
}