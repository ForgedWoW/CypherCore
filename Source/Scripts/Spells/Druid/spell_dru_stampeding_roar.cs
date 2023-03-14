// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Druid;

[Script] // 106898 - Stampeding Roar
internal class spell_dru_stampeding_roar : SpellScript, ISpellBeforeCast
{
	public void BeforeCast()
	{
		// Change into cat form
		if (Caster.ShapeshiftForm != ShapeShiftForm.BearForm)
			Caster.CastSpell(Caster, DruidSpellIds.BearForm, true);
	}
}