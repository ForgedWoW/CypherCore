// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Rogue;

[Script]
internal class SpellRogPickpocket : SpellScript, ISpellCheckCast
{
    public SpellCastResult CheckCast()
    {
        if (!ExplTargetUnit ||
            !Caster.IsValidAttackTarget(ExplTargetUnit, SpellInfo))
            return SpellCastResult.BadTargets;

        return SpellCastResult.SpellCastOk;
    }
}