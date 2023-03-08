// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Rogue;

[SpellScript(14172)]
public class spell_rog_serrated_blades_SpellScript : SpellScript, ISpellOnHit
{
	public void OnHit()
	{
		var blade = Caster.GetAuraEffectOfRankedSpell(RogueSpells.SERRATED_BLADES_R1, 0);

		if (blade != null)
		{
			var combo = Caster.ToPlayer().GetPower(PowerType.ComboPoints);

			if (RandomHelper.randChance(blade.Amount * combo))
			{
				var dot = HitUnit.GetAura(RogueSpells.RUPTURE, Caster.GetGUID());

				if (dot != null)
					dot.RefreshDuration();
			}
		}
	}
}