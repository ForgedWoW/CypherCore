﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.DeathKnight;

[Script] // 147157 Glyph of the Skeleton (Unholy)
internal class spell_dk_pet_skeleton_transform : SpellScript, ISpellCheckCast
{
	public SpellCastResult CheckCast()
	{
		var owner = Caster.OwnerUnit;

		if (owner)
			if (owner.HasAura(DeathKnightSpells.GlyphOfTheSkeleton))
				return SpellCastResult.SpellCastOk;

		return SpellCastResult.SpellUnavailable;
	}
}