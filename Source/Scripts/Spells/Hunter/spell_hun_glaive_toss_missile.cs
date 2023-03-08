// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Hunter;

[SpellScript(new uint[]
{
	120755, 120756
})]
public class spell_hun_glaive_toss_missile : SpellScript, ISpellOnHit, ISpellAfterCast
{
	public void AfterCast()
	{
		if (SpellInfo.Id == HunterSpells.GLAIVE_TOSS_RIGHT)
		{
			var plr = Caster.AsPlayer;

			if (plr != null)
			{
				plr.CastSpell(plr, HunterSpells.GLAIVE_TOSS_DAMAGE_AND_SNARE_RIGHT, true);
			}
			else if (OriginalCaster)
			{
				var caster = OriginalCaster.AsPlayer;

				if (caster != null)
					caster.CastSpell(caster, HunterSpells.GLAIVE_TOSS_DAMAGE_AND_SNARE_RIGHT, true);
			}
		}
		else
		{
			var plr = Caster.AsPlayer;

			if (plr != null)
			{
				plr.CastSpell(plr, HunterSpells.GLAIVE_TOSS_DAMAGE_AND_SNARE_LEFT, true);
			}
			else if (OriginalCaster)
			{
				var caster = OriginalCaster.AsPlayer;

				if (caster != null)
					caster.CastSpell(caster, HunterSpells.GLAIVE_TOSS_DAMAGE_AND_SNARE_LEFT, true);
			}
		}

		var target = ExplTargetUnit;

		if (target != null)
			if (Caster == OriginalCaster)
				Caster.AddAura(HunterSpells.GLAIVE_TOSS_AURA, target);
	}

	public void OnHit()
	{
		if (SpellInfo.Id == HunterSpells.GLAIVE_TOSS_RIGHT)
		{
			var caster = Caster;

			if (caster != null)
			{
				var target = HitUnit;

				if (target != null)
					if (caster == OriginalCaster)
						target.CastSpell(caster, HunterSpells.GLAIVE_TOSS_LEFT, new CastSpellExtraArgs(TriggerCastFlags.FullMask).SetOriginalCaster(caster.GUID));
			}
		}
		else
		{
			var caster = Caster;

			if (caster != null)
			{
				var target = HitUnit;

				if (target != null)
					if (caster == OriginalCaster)
						target.CastSpell(caster, HunterSpells.GLAIVE_TOSS_RIGHT, new CastSpellExtraArgs(TriggerCastFlags.FullMask).SetOriginalCaster(caster.GUID));
			}
		}
	}
}