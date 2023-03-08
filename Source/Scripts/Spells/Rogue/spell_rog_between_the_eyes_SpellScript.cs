// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Rogue;

[SpellScript(199804)]
public class spell_rog_between_the_eyes_SpellScript : SpellScript, ISpellAfterHit, ISpellOnTakePower
{
	private int _cp = 0;

	public void AfterHit()
	{
		var target = HitUnit;

		if (target != null)
		{
			var aura = target.GetAura(TrueBearingIDs.BETWEEN_THE_EYES, Caster.GUID);

			if (aura != null)
				aura.SetDuration(_cp * Time.InMilliseconds);
		}
	}

	public void TakePower(SpellPowerCost powerCost)
	{
		if (powerCost.Power == PowerType.ComboPoints)
			_cp = powerCost.Amount;
	}
}