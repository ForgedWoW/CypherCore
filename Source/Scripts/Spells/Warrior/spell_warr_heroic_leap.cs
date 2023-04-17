// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Movement.Generators;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Forged.MapServer.Spells;
using Framework.Constants;

namespace Scripts.Spells.Warrior;

[Script] // 6544 Heroic leap
internal class SpellWarrHeroicLeap : SpellScript, ISpellCheckCast, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();


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

    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHit));
    }

    private void HandleDummy(int effIndex)
    {
        var dest = HitDest;

        if (dest != null)
            Caster.SpellFactory.CastSpell(dest, WarriorSpells.HEROIC_LEAP_JUMP, new CastSpellExtraArgs(true));
    }
}