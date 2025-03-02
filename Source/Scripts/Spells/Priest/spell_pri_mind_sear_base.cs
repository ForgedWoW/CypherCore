﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Priest;

[SpellScript(48045)]
public class spell_pri_mind_sear_base : SpellScript, ISpellCheckCast
{
	public SpellCastResult CheckCast()
	{
		var explTarget = ExplTargetUnit;

		if (explTarget != null)
			if (explTarget == Caster)
				return SpellCastResult.BadTargets;

		return SpellCastResult.SpellCastOk;
	}
}