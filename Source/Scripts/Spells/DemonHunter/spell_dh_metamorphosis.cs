// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.DemonHunter;

[SpellScript(191427)]
public class spell_dh_metamorphosis : SpellScript, ISpellBeforeCast
{
	public void BeforeCast()
	{
		var caster = Caster;

		if (caster == null)
			return;

		var player = caster.AsPlayer;

		if (player == null)
			return;

		var dest = ExplTargetDest;

		if (dest != null)
			player.CastSpell(new Position(dest.X, dest.Y, dest.Z), DemonHunterSpells.METAMORPHOSIS_JUMP, true);

		if (player.HasAura(DemonHunterSpells.DEMON_REBORN)) // Remove CD of Eye Beam, Chaos Nova and Blur
		{
			player.SpellHistory.ResetCooldown(DemonHunterSpells.CHAOS_NOVA, true);
			player.SpellHistory.ResetCooldown(DemonHunterSpells.BLUR, true);
			player.SpellHistory.AddCooldown(DemonHunterSpells.BLUR_BUFF, 0, TimeSpan.FromMinutes(1));
			player.SpellHistory.ResetCooldown(DemonHunterSpells.BLUR_BUFF, true);
			player.SpellHistory.ResetCooldown(DemonHunterSpells.EYE_BEAM, true);
		}
	}
}