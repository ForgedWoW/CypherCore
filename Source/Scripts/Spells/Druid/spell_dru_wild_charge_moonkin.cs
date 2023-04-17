// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Druid;

[SpellScript(102383)]
public class SpellDruWildChargeMoonkin : SpellScript, ISpellCheckCast
{
    public SpellCastResult CheckCast()
    {
        if (Caster)
        {
            if (!Caster.IsInCombat)
                return SpellCastResult.DontReport;
        }
        else
        {
            return SpellCastResult.DontReport;
        }

        return SpellCastResult.SpellCastOk;
    }
}