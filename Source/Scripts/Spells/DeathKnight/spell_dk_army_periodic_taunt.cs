// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.DeathKnight;

/// 6.x, does this belong here or in spell_generic? apply this in creature_template_addon? sniffs say this is always cast army of the dead ghouls.
[SpellScript(43264)]
public class SpellDkArmyPeriodicTaunt : SpellScript, ISpellCheckCast
{
    public override bool Load()
    {
        return Caster.IsGuardian;
    }

    public SpellCastResult CheckCast()
    {
        var owner = Caster.OwnerUnit;

        if (owner != null)
            if (!owner.HasAura(DeathKnightSpells.GLYPH_OF_ARMY_OF_THE_DEAD))
                return SpellCastResult.SpellCastOk;

        return SpellCastResult.SpellUnavailable;
    }
}