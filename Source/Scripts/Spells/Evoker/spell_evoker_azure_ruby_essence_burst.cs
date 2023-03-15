// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.RED_PYRE, EvokerSpells.BLUE_DISINTEGRATE, EvokerSpells.BLUE_DISINTEGRATE_2)]
public class spell_evoker_azure_ruby_essence_burst : SpellScript, ISpellOnCast
{
	public void OnCast()
	{
		if (Caster.TryGetAsPlayer(out var player) 
			&& (!player.TryGetAura(EvokerSpells.HOARDED_POWER, out var hpAura) || !RandomHelper.randChance(hpAura.SpellInfo.GetEffect(0).BasePoints)))
            player.RemoveAura(EvokerSpells.AZURE_RUBY_ESSENCE_BURST_AURA);
	}
}