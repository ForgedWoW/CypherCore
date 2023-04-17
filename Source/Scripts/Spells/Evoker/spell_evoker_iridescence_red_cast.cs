// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.RED_DRAGONRAGE,
             EvokerSpells.RED_FIRE_BREATH,
             EvokerSpells.RED_FIRE_BREATH_2,
             EvokerSpells.RED_FIRE_STORM,
             EvokerSpells.RED_LIVING_FLAME,
             EvokerSpells.RED_PYRE)]
public class SpellEvokerIridescenceRedCast : SpellScript, ISpellAfterCast
{
    public void AfterCast()
    {
        if (Caster.TryGetAura(EvokerSpells.IRIDESCENCE_BLUE, out var aura))
            aura.ModStackAmount(-1);
    }
}