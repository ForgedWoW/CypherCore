// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Game.Movement;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.DEEP_BREATH)]
public class spell_evoker_deep_breath : SpellScript, ISpellOnCast, ISpellCheckCast
{
	public SpellCastResult CheckCast()
	{
		var dest = ExplTargetDest;

		if (dest != null)
		{
			if (Caster.HasUnitMovementFlag(MovementFlag.Root))
				return SpellCastResult.Rooted;

			if (Caster.Map.Instanceable)
			{
				var range = SpellInfo.GetMaxRange(true, Caster) * 1.5f;

				PathGenerator generatedPath = new(Caster);
				generatedPath.SetPathLengthLimit(range);

				var result = generatedPath.CalculatePath(dest, false);

				if (generatedPath.GetPathType().HasAnyFlag(PathType.Short))
					return SpellCastResult.OutOfRange;
				else if (!result ||
						generatedPath.GetPathType().HasAnyFlag(PathType.NoPath))
					return SpellCastResult.NoPath;
			}
			else if (dest.Z > Caster.Location.Z + 4.0f)
			{
				return SpellCastResult.NoPath;
			}

			return SpellCastResult.SpellCastOk;
		}

		return SpellCastResult.NoValidTargets;
	}

	public void OnCast()
	{
		Caster.CastSpell(Spell.Targets.DstPos, EvokerSpells.DEEP_BREATH_EFFECT, true);
	}
}