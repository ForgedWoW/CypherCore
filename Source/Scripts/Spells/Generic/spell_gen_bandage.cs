// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Generic;

[Script]
internal class spell_gen_bandage : SpellScript, ISpellCheckCast, ISpellAfterHit
{
	public void AfterHit()
	{
		var target = HitUnit;

		if (target)
			Caster.CastSpell(target, GenericSpellIds.RecentlyBandaged, true);
	}


	public SpellCastResult CheckCast()
	{
		var target = ExplTargetUnit;

		if (target)
			if (target.HasAura(GenericSpellIds.RecentlyBandaged))
				return SpellCastResult.TargetAurastate;

		return SpellCastResult.SpellCastOk;
	}
}