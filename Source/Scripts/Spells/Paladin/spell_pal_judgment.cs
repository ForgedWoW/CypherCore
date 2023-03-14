// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Paladin;

[SpellScript(new uint[]
{
	20271, 275779, 275773
})] // 20271/275779/275773 - Judgement (Retribution/Protection/Holy)
internal class spell_pal_judgment : SpellScript, ISpellOnHit
{
	public void OnHit()
	{
		var caster = Caster;

		if (caster.HasSpell(PaladinSpells.JudgmentProtRetR3))
			caster.CastSpell(caster, PaladinSpells.JudgmentGainHolyPower, Spell);

		if (caster.HasSpell(PaladinSpells.JudgmentHolyR3))
			caster.CastSpell(HitUnit, PaladinSpells.JudgmentHolyR3Debuff, Spell);
	}
}