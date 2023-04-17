// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.RED_LIVING_FLAME_DAMAGE, EvokerSpells.RED_LIVING_FLAME_HEAL)]
class SpellEvokerRubyEmbers : SpellScript, ISpellAfterCast
{
    public void AfterCast()
    {
        if (!Caster.TryGetAsPlayer(out var player) || !player.HasSpell(EvokerSpells.RUBY_EMBERS))
            ExplTargetUnit.RemoveAura(Spell.SpellInfo.Id);
    }
}