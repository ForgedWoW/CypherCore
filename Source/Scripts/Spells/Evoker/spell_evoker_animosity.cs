// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Evoker;

// all empower spells
[SpellScript(EvokerSpells.GREEN_DREAM_BREATH,
			EvokerSpells.GREEN_DREAM_BREATH_2,
			EvokerSpells.BLUE_ETERNITY_SURGE,
			EvokerSpells.BLUE_ETERNITY_SURGE_2,
			EvokerSpells.RED_FIRE_BREATH,
			EvokerSpells.RED_FIRE_BREATH_2,
			EvokerSpells.GREEN_SPIRITBLOOM,
			EvokerSpells.GREEN_SPIRITBLOOM_2)]
public class spell_evoker_animosity : SpellScript, ISpellAfterHit
{
	public void AfterHit()
	{
		if (Caster.TryGetAsPlayer(out var player) && player.HasSpell(EvokerSpells.ANIMOSITY) && player.TryGetAura(EvokerSpells.RED_DRAGONRAGE, out var aura))
			aura.ModDuration(GetEffectInfo(0).BasePoints);
	}
}