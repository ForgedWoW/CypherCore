// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Rogue;

[Script]
internal class spell_rog_pickpocket : SpellScript, ISpellCheckCast
{
	public SpellCastResult CheckCast()
	{
		if (!ExplTargetUnit ||
			!Caster.IsValidAttackTarget(ExplTargetUnit, SpellInfo))
			return SpellCastResult.BadTargets;

		return SpellCastResult.SpellCastOk;
	}
}