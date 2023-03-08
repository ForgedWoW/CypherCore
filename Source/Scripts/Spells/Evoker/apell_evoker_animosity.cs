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
public class apell_evoker_animosity : SpellScript, ISpellAfterHit
{
	public void AfterHit()
	{
		if (Caster.TryGetAsPlayer(out var player) && player.HasSpell(EvokerSpells.ANIMOSITY) && player.TryGetAura(EvokerSpells.DRAGONRAGE, out var aura))
			aura.ModDuration(GetEffectInfo(0).BasePoints);
	}
}