// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Generic;

[Script]
internal class SpellGenAllowCastFromItemOnly : SpellScript, ISpellCheckCast
{
    public SpellCastResult CheckCast()
    {
        if (!CastItem)
            return SpellCastResult.CantDoThatRightNow;

        return SpellCastResult.SpellCastOk;
    }
}