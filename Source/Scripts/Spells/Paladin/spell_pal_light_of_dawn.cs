// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Paladin;

//Light of Dawn - 85222
[SpellScript(85222)]
public class spell_pal_light_of_dawn : SpellScript, ISpellOnHit, ISpellAfterCast
{
	public void AfterCast()
	{
		var caster = Caster;

		if (caster == null)
			return;

		if (caster.HasAura(PaladinSpells.AWAKENING))
			if (RandomHelper.randChance(15))
			{
				caster.CastSpell(null, PaladinSpells.AvengingWrath, true);

				var avengingWrath = caster.GetAura(PaladinSpells.AvengingWrath);

				if (avengingWrath != null)
					avengingWrath.SetDuration(10000, true);
			}
	}

	public void OnHit()
	{
		var caster = Caster;

		if (caster != null)
		{
			caster.CastSpell(caster, PaladinSpells.LIGHT_OF_DAWN_TRIGGER, true);

			if (caster.HasAura(PaladinSpells.DIVINE_PURPOSE_HOLY_AURA_2))
				caster.RemoveAura(PaladinSpells.DIVINE_PURPOSE_HOLY_AURA_2);
		}
	}
}