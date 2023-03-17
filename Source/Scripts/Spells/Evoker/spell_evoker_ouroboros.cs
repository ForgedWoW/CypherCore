// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.GREEN_DREAM_BREATH_CHARGED, EvokerSpells.GREEN_EMERALD_BLOSSOM_HEAL)]
internal class spell_evoker_ouroboros : SpellScript, ISpellAfterHit
{
    public void AfterHit()
    {
        if (Caster.TryGetAsPlayer(out var player) && player.HasSpell(EvokerSpells.OUROBOROS))
            player.AddAura(EvokerSpells.OUROBOROS_AURA);
    }
}