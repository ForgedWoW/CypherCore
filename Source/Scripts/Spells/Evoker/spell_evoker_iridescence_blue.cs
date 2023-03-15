// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.BLUE_ETERNITY_SURGE, EvokerSpells.BLUE_ETERNITY_SURGE_2)]
public class spell_evoker_iridescence_blue : SpellScript, ISpellAfterCast
{
    public void AfterCast()
    {
        if (Caster.TryGetAsPlayer(out var player) && player.HasSpell(EvokerSpells.IRIDESCENCE))
            player.AddAura(EvokerSpells.IRIDESCENCE_BLUE);
    }
}