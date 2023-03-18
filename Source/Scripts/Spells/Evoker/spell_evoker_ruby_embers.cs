// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.RED_LIVING_FLAME_DAMAGE, EvokerSpells.RED_LIVING_FLAME_HEAL)]
class spell_evoker_ruby_embers : SpellScript, ISpellAfterCast
{
    public void AfterCast()
    {
        if (!Caster.TryGetAsPlayer(out var player) || !player.HasSpell(EvokerSpells.RUBY_EMBERS))
            ExplTargetUnit.RemoveAura(Spell.SpellInfo.Id);
    }
}