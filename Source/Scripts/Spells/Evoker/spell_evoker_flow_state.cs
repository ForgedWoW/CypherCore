// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.DREAM_BREATH,
			EvokerSpells.DREAM_BREATH_2,
			EvokerSpells.ETERNITY_SURGE,
			EvokerSpells.ETERNITY_SURGE_2,
			EvokerSpells.FIRE_BREATH,
			EvokerSpells.FIRE_BREATH,
			EvokerSpells.SPIRITBLOOM,
			EvokerSpells.SPIRITBLOOM_2)]
public class spell_evoker_flow_state : SpellScript, ISpellAfterCast
{
	public void AfterCast()
	{
		if (Caster.TryGetAsPlayer(out var player) && player.HasSpell(EvokerSpells.FLOW_STATE))
			player.AddAura(EvokerSpells.FLOW_STATE_AURA);
	}
}