// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Game.Scripting;

namespace Scripts.Spells.Druid;

[SpellScript(274837)]
public class spell_feral_frenzy : SpellScript
{
	private byte _strikes;

	public void OnHit()
	{
		var caster = Caster;
		var target = HitUnit;

		if (caster == null || target == null)
			return;

		_strikes = 0;

		var strikeDamage = 100 / 20 + caster.UnitData.AttackPower;

		caster.Events.AddRepeatEventAtOffset(() =>
											{
												if (caster.GetDistance2d(target) <= 5.0f)
												{
													_strikes++;

													if (_strikes < 5)
													{
														return TimeSpan.FromMilliseconds(200);
													}
													else if (_strikes == 5)
													{
														caster.CastSpell(target, DruidSpells.FERAL_FRENZY_BLEED, true);
														var bleedDamage = 100 / 10 + caster.UnitData.AttackPower;
													}
												}

												return default;
											},
											TimeSpan.FromMilliseconds(50));
	}
}