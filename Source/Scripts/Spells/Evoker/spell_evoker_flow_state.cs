// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Evoker;

// all empower spells
[SpellScript(EvokerSpells.GREEN_DREAM_BREATH,
             EvokerSpells.GREEN_DREAM_BREATH_2,
             EvokerSpells.BLUE_ETERNITY_SURGE,
             EvokerSpells.BLUE_ETERNITY_SURGE_2,
             EvokerSpells.RED_FIRE_BREATH,
             EvokerSpells.RED_FIRE_BREATH,
             EvokerSpells.GREEN_SPIRITBLOOM,
             EvokerSpells.GREEN_SPIRITBLOOM_2)]
public class SpellEvokerFlowState : SpellScript, ISpellAfterCast
{
    public void AfterCast()
    {
        if (Caster.TryGetAsPlayer(out var player) && player.HasSpell(EvokerSpells.FLOW_STATE))
            player.AddAura(EvokerSpells.FLOW_STATE_AURA);
    }
}