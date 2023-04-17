// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.GREEN_EMERALD_BLOSSOM,
             EvokerSpells.BLUE_DISINTEGRATE,
             EvokerSpells.BLUE_DISINTEGRATE_2,
             EvokerSpells.ECHO,
             EvokerSpells.DREAM_PROJECTION)]
public class SpellEvokerEssenceBurst : SpellScript, ISpellOnCast
{
    public void OnCast()
    {
        Caster.RemoveAura(EvokerSpells.ESSENCE_BURST);
    }
}