// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.EMERALD_BLOSSOM, 
				EvokerSpells.DISINTEGRATE,
				EvokerSpells.DISINTEGRATE_2,
				EvokerSpells.ECHO,
				EvokerSpells.DREAM_PROJECTION)]
public class spell_evoker_essence_burst : SpellScript, ISpellOnCast
{
	public void OnCast()
	{
		Caster.RemoveAura(EvokerSpells.ESSENCE_BURST);
	}
}