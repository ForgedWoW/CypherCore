// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.DeathKnight;

[SpellScript(45524)]
public class spell_dk_chilblains : SpellScript, ISpellOnHit
{
	public void OnHit()
	{
		var player = Caster.ToPlayer();

		if (player != null)
		{
			var target = HitUnit;

			if (target != null)
				if (player.HasAura(DeathKnightSpells.CHILBLAINS))
					player.CastSpell(target, DeathKnightSpells.CHAINS_OF_ICE_ROOT, true);
		}

		if (Caster.HasAura(DeathKnightSpells.COLD_HEART_CHARGE))
		{
			var coldHeartCharge = Caster.GetAura(DeathKnightSpells.COLD_HEART_CHARGE);

			if (coldHeartCharge != null)
			{
				var stacks = coldHeartCharge.StackAmount;
				HitDamage = HitDamage * stacks;
				Caster.CastSpell(HitUnit, DeathKnightSpells.COLD_HEART_DAMAGE, true);
				coldHeartCharge.ModStackAmount(-stacks);
			}
		}
	}
}