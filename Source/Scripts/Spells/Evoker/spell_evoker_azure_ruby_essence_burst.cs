// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.PYRE, EvokerSpells.AZURE_RUBY_ESSENCE_BURST_AURA)]
public class spell_evoker_azure_ruby_essence_burst : SpellScript, ISpellOnCast
{
	public void OnCast()
	{
		Caster.RemoveAura(EvokerSpells.AZURE_RUBY_ESSENCE_BURST_AURA);
	}
}