// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.RED_LIVING_FLAME_DAMAGE, EvokerSpells.RED_LIVING_FLAME_HEAL)]
public class SpellEvokerRubyEssenceBurst : SpellScript, ISpellAfterHit
{
    public void AfterHit()
    {
        if (Caster.TryGetAsPlayer(out var player) && player.HasSpell(EvokerSpells.RUBY_ESSENCE_BURST) && (player.HasAura(EvokerSpells.RED_DRAGONRAGE) || RandomHelper.randChance(SpellManager.Instance.GetSpellInfo(EvokerSpells.RUBY_ESSENCE_BURST).GetEffect(0).BasePoints)))
            player.AddAura(EvokerSpells.AZURE_RUBY_ESSENCE_BURST_AURA);
    }
}