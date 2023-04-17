﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.GREEN_EMERALD_COMMUNION)]
internal class SpellEvokerRushOfVitality : SpellScript, ISpellOnCast
{
    public void OnCast()
    {
        if (Caster.TryGetAsPlayer(out var player) && player.HasSpell(EvokerSpells.RUSH_OF_VITALITY))
            player.AddAura(EvokerSpells.RUSH_OF_VITALITY_AURA);
    }
}