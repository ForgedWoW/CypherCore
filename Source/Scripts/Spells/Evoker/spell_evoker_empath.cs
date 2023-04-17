// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Players;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.GREEN_SPIRITBLOOM, EvokerSpells.GREEN_SPIRITBLOOM_2)]
internal class SpellEvokerEmpath : SpellScript, ISpellAfterCast
{
    public void AfterCast()
    {
        if (TryGetCaster(out Player player) && player.HasSpell(EvokerSpells.EMPATH))
            player.AddAura(EvokerSpells.EMPATH_AURA);
    }
}