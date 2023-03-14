// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Shaman;

[SpellScript(375982)]
public class spell_sha_primordial_wave : SpellScript, ISpellOnHit
{
	public void OnHit()
	{
		var player = Caster.AsPlayer;
		var victim = HitUnit;

		if (player == null || victim == null)
			return;

		if (player.IsFriendlyTo(victim))
		{
			player.CastSpell(victim, ShamanSpells.PrimordialWaveHealing, true);
		}
		else
		{
			player.CastSpell(victim, ShamanSpells.PrimordialWaveDamage, true);
			player.AddAura(ShamanSpells.FlameShock, victim);
		}

		player.AddAura(ShamanSpells.PrimordialWaveAura, player);
	}
}