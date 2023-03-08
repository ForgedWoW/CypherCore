// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Warrior;

// 85288
[SpellScript(85288)]
public class spell_warr_raging_blow : SpellScript, ISpellOnHit
{
	private byte _targetHit;

	public void OnHit()
	{
		var player = Caster.ToPlayer();

		if (player != null)
			player.CastSpell(player, WarriorSpells.ALLOW_RAGING_BLOW, true);

		if (Caster.HasAura(WarriorSpells.BATTLE_TRANCE))
		{
			var target = Caster.ToPlayer().GetSelectedUnit();
			var targetGUID = target.GetGUID();
			_targetHit++;

			if (_targetHit == 4)
			{
				//targetGUID.Clear();
				_targetHit = 0;
				Caster.CastSpell(null, WarriorSpells.BATTLE_TRANCE_BUFF, true);
				var battleTrance = Caster.GetAura(WarriorSpells.BATTLE_TRANCE_BUFF).GetEffect(0);

				//if (battleTrance != null)
				//	battleTrance.Amount;
			}
		}

		if (RandomHelper.randChance(20))
			Caster.GetSpellHistory().ResetCooldown(85288, true);

		var whirlWind = Caster.GetAura(WarriorSpells.WHIRLWIND_PASSIVE);

		if (whirlWind != null)
			whirlWind.ModStackAmount(-1, AuraRemoveMode.Default, false);
	}
}