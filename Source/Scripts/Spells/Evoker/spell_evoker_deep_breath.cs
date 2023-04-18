// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.Movement.Generators;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.BLACK_DEEP_BREATH)]
public class SpellEvokerDeepBreath : SpellScript, ISpellOnCast, ISpellCheckCast
{
    public SpellCastResult CheckCast()
    {
        var dest = ExplTargetDest;

        if (dest != null)
        {
            if (Caster.MovementInfo.HasMovementFlag(MovementFlag.Root))
                return SpellCastResult.Rooted;

            if (Caster.Location.Map.Instanceable)
            {
                var range = SpellInfo.GetMaxRange(true, Caster) * 1.5f;

                PathGenerator generatedPath = new(Caster);
                generatedPath.SetPathLengthLimit(range);

                var result = generatedPath.CalculatePath(dest, false);

                if (generatedPath.PathType.HasAnyFlag(PathType.Short))
                    return SpellCastResult.OutOfRange;
                else if (!result ||
                         generatedPath.PathType.HasAnyFlag(PathType.NoPath))
                    return SpellCastResult.NoPath;
            }
            else if (dest.Z > Caster.Location.Z + 4.0f)
                return SpellCastResult.NoPath;

            return SpellCastResult.SpellCastOk;
        }

        return SpellCastResult.NoValidTargets;
    }

    public void OnCast()
    {
        Caster.SpellFactory.CastSpell(Spell.Targets.DstPos, EvokerSpells.BLACK_DEEP_BREATH_EFFECT, true);
    }
}