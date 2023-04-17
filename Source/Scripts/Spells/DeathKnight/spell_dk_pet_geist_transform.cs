// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.DeathKnight;

[Script] // 121916 - Glyph of the Geist (Unholy)
internal class SpellDkPetGeistTransform : SpellScript, ISpellCheckCast
{
    public override bool Load()
    {
        return Caster.IsPet;
    }

    public SpellCastResult CheckCast()
    {
        var owner = Caster.OwnerUnit;

        if (owner)
            if (owner.HasAura(DeathKnightSpells.GlyphOfTheGeist))
                return SpellCastResult.SpellCastOk;

        return SpellCastResult.SpellUnavailable;
    }
}