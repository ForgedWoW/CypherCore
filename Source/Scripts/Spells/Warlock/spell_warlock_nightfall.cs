// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;

namespace Scripts.Spells.Warlock;

// 108558 - Nightfall
[SpellScript(108558)]
public class SpellWarlockNightfall : AuraScript, IAuraOnProc, IAuraCheckProc
{
    public bool CheckProc(ProcEventInfo eventInfo)
    {
        return (eventInfo).SpellInfo.Id == WarlockSpells.CORRUPTION_DOT;
    }

    public void OnProc(ProcEventInfo unnamedParameter)
    {
        Caster.SpellFactory.CastSpell(Caster, WarlockSpells.NIGHTFALL_BUFF, true);
    }
}