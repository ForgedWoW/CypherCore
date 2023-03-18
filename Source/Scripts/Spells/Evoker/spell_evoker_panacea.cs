// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.GREEN_EMERALD_BLOSSOM)]
internal class spell_evoker_panacea : SpellScript, ISpellOnHit
{
    public void OnHit()
    {
        if (TryGetCaster(out Player player) && player.HasSpell(EvokerSpells.PANACEA))
            player.CastSpell(player, EvokerSpells.PANACEA_HEAL, true);
    }
}