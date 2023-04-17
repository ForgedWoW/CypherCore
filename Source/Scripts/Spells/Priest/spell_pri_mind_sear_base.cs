// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Priest;

[SpellScript(48045)]
public class SpellPriMindSearBase : SpellScript, ISpellCheckCast
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