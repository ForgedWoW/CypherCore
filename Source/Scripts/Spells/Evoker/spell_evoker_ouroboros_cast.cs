// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.ECHO)]
internal class spell_evoker_ouroboros_cast : SpellScript, ISpellAfterHit
{
    public void AfterHit()
    {
        if (Caster.TryGetAsPlayer(out var player) && player.HasSpell(EvokerSpells.OUROBOROS))
            player.RemoveAura(EvokerSpells.OUROBOROS_AURA);
    }
}