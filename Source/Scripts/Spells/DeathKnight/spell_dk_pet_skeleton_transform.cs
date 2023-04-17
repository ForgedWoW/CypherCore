// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.DeathKnight;

[Script] // 147157 Glyph of the Skeleton (Unholy)
internal class SpellDkPetSkeletonTransform : SpellScript, ISpellCheckCast
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