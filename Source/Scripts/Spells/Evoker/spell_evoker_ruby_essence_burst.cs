// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.LIVING_FLAME_DAMAGE, EvokerSpells.LIVING_FLAME_HEAL)]
public class spell_evoker_ruby_essence_burst : SpellScript, ISpellAfterHit
{
	public void AfterHit()
    {
        if (Caster.TryGetAsPlayer(out var player)
            && player.HasSpell(EvokerSpells.RUBY_ESSENCE_BURST)
            && (player.HasAura(EvokerSpells.DRAGONRAGE) 
            || RandomHelper.randChance(SpellManager.Instance.GetSpellInfo(EvokerSpells.RUBY_ESSENCE_BURST).GetEffect(0).BasePoints)))
            player.AddAura(EvokerSpells.AZURE_RUBY_ESSENCE_BURST_AURA);
	}
}