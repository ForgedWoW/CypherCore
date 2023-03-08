// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Hunter;

[SpellScript(204147)]
public class spell_hun_windburst : SpellScript, ISpellAfterHit
{
	public void AfterHit()
	{
		var caster = Caster;

		if (caster != null)
		{
			var target = HitUnit;

			if (target == null)
				return;

			caster.CastSpell(new Position(target.Location.X, target.Location.Y, target.Location.Z), 204475, true);
		}
	}
}