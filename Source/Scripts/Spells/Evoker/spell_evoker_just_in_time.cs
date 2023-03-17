// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using System;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.GREEN_EMERALD_BLOSSOM,
				EvokerSpells.BLUE_DISINTEGRATE,
				EvokerSpells.BLUE_DISINTEGRATE_2,
				EvokerSpells.ECHO,
				EvokerSpells.DREAM_PROJECTION)]
public class spell_evoker_just_in_time : SpellScript, ISpellOnCast
{
	public void OnCast()
	{
		if (Caster.TryGetAura(EvokerSpells.JUST_IN_TIME, out var aura))
			Caster.SpellHistory.ModifyCooldown(EvokerSpells.BRONZE_TIME_DILATION, TimeSpan.FromSeconds(-aura.SpellInfo.GetEffect(0).BasePoints));
	}
}