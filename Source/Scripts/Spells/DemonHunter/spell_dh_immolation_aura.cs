// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.DemonHunter;

[SpellScript(258920)]
public class spell_dh_immolation_aura : SpellScript, ISpellOnCast
{
	public void OnCast()
	{
		var caster = Caster;

		if (caster.HasAura(DemonHunterSpells.CLEANSED_BY_FLAME))
			caster.CastSpell(caster, DemonHunterSpells.CLEANSED_BY_FLAME_DISPEL, true);

		/*
			if (RandomHelper.randChance(40) && caster->HasAura(FALLOUT))
			    caster->CastSpell(caster, SHATTERED_SOULS_MISSILE, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.BasePoint0, (int)LESSER_SOUL_SHARD));
			*/
	}
}