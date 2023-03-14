// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Priest;

[Script] // 186263 - Shadow Mend
internal class spell_pri_shadow_mend : SpellScript, ISpellAfterHit
{
	public void AfterHit()
	{
		var target = HitUnit;

		if (target != null)
		{
			var caster = Caster;

			var periodicAmount = HitHeal / 20;
			var damageForAuraRemoveAmount = periodicAmount * 10;

			if (caster.HasAura(PriestSpells.ATONEMENT) &&
				!caster.HasAura(PriestSpells.TRINITY))
				caster.CastSpell(target, PriestSpells.ATONEMENT_TRIGGERED, new CastSpellExtraArgs(Spell));

			// Handle Masochism talent
			if (caster.HasAura(PriestSpells.MASOCHISM_TALENT) &&
				caster.GUID == target.GUID)
			{
				caster.CastSpell(caster, PriestSpells.MASOCHISM_PERIODIC_HEAL, new CastSpellExtraArgs(Spell).AddSpellMod(SpellValueMod.BasePoint0, periodicAmount));
			}
			else if (target.IsInCombat &&
					periodicAmount != 0)
			{
				CastSpellExtraArgs args = new(TriggerCastFlags.FullMask);
				args.SetTriggeringSpell(Spell);
				args.AddSpellMod(SpellValueMod.BasePoint0, periodicAmount);
				args.AddSpellMod(SpellValueMod.BasePoint1, damageForAuraRemoveAmount);
				caster.CastSpell(target, PriestSpells.SHADOW_MEND_PERIODIC_DUMMY, args);
			}
		}
	}
}