// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Hunter;

[SpellScript(19574)]
public class spell_hun_bestial_wrath : SpellScript, ISpellOnCast
{
	public void OnCast()
	{
		var caster = Caster;

		if (caster != null)
		{
			var player = caster.AsPlayer;

			if (player != null)
			{
				var pet = player.CurrentPet;

				if (pet != null)
					pet.AddAura(19574, pet);
			}
		}
	}
}