// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Scripting;

namespace Scripts.Spells.Mage;

[SpellScript(new uint[]
{
	157997, 157980
})]
public class spell_mage_nova_talent : SpellScript
{
	public void OnHit()
	{
		var caster = Caster;
		var target = HitUnit;
		var explTarget = ExplTargetUnit;

		if (target == null || caster == null || explTarget == null)
			return;

		var eff2 = SpellInfo.GetEffect(2).CalcValue();

		if (eff2 != 0)
		{
			var dmg = HitDamage;

			if (target == explTarget)
				dmg = MathFunctions.CalculatePct(dmg, eff2);

			HitDamage = dmg;
		}
	}
}