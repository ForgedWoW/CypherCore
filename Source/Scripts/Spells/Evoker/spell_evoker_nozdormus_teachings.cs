// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.BRONZE_TEMPORAL_ANOMALY)]
public class spell_evoker_nozdormus_teachings : SpellScript, ISpellAfterCast
{
    public void AfterCast()
    {
        if (Caster.TryGetAsPlayer(out var player) && player.TryGetAura(EvokerSpells.NOZDORMUS_TEACHINGS, out var aura))
        {
            var cdr = TimeSpan.FromSeconds(-aura.SpellInfo.GetEffect(0).BasePoints);
            // reduce cooldown all empower
            Caster.SpellHistory.ModifyCooldown(EvokerSpells.GREEN_DREAM_BREATH, cdr);
            Caster.SpellHistory.ModifyCooldown(EvokerSpells.GREEN_DREAM_BREATH_2, cdr);
            Caster.SpellHistory.ModifyCooldown(EvokerSpells.BLUE_ETERNITY_SURGE, cdr);
            Caster.SpellHistory.ModifyCooldown(EvokerSpells.BLUE_ETERNITY_SURGE_2, cdr);
            Caster.SpellHistory.ModifyCooldown(EvokerSpells.RED_FIRE_BREATH, cdr);
            Caster.SpellHistory.ModifyCooldown(EvokerSpells.RED_FIRE_BREATH_2, cdr);
            Caster.SpellHistory.ModifyCooldown(EvokerSpells.GREEN_DREAM_BREATH, cdr);
            Caster.SpellHistory.ModifyCooldown(EvokerSpells.GREEN_SPIRITBLOOM_2, cdr);
        }
    }
}