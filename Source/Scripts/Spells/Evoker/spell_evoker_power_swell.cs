﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Evoker;

// all empower spells
[SpellScript(EvokerSpells.GREEN_DREAM_BREATH,
             EvokerSpells.GREEN_DREAM_BREATH_2,
             EvokerSpells.BLUE_ETERNITY_SURGE,
             EvokerSpells.BLUE_ETERNITY_SURGE_2,
             EvokerSpells.RED_FIRE_BREATH,
             EvokerSpells.RED_FIRE_BREATH_2,
             EvokerSpells.GREEN_SPIRITBLOOM,
             EvokerSpells.GREEN_SPIRITBLOOM_2)]
public class spell_evoker_power_swell : SpellScript, ISpellAfterCast
{
    public void AfterCast()
    {
        if (Caster.TryGetAsPlayer(out var player) && player.HasSpell(EvokerSpells.POWER_SWELL))
            player.AddAura(EvokerSpells.POWER_SWELL_AURA);
    }
}