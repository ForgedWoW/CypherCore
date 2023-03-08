// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.AZURE_STRIKE)]
public class spell_evoker_azure_essence_burst : SpellScript, ISpellAfterHit
{
	public void AfterHit()
	{
		if (Caster.TryGetAsPlayer(out var player) && player.HasSpell(EvokerSpells.AZURE_ESSENCE_BURST))
			player.AddAura(EvokerSpells.AZURE_RUBY_ESSENCE_BURST_AURA);
	}
}