// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Framework.Constants;

namespace Scripts.Spells.DeathKnight;

[SpellScript(194909)]
public class SpellDkFrozenPulse : AuraScript, IAuraCheckProc
{
    public bool CheckProc(ProcEventInfo unnamedParameter)
    {
        var caster = Caster;

        if (caster == null)
            return false;

        if (caster.GetPower(PowerType.Runes) > SpellInfo.GetEffect(1).BasePoints)
            return false;

        return true;
    }
}