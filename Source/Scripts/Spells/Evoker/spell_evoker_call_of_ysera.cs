// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.VERDANT_EMBRACE_HEAL)]
public class spell_evoker_call_of_ysera : SpellScript, ISpellAfterCast
{
	public void AfterCast()
	{
		if (Caster.TryGetAsPlayer(out var player))
			player.AddAura(EvokerSpells.CALL_OF_YSERA_AURA);
	}
}