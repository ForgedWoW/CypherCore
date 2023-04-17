// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;

namespace Scripts.Spells.Warrior;

//206313 Frenzy
[SpellScript(206313)]
public class SpellWarrFrenzy : AuraScript, IAuraCheckProc
{
    public bool CheckProc(ProcEventInfo procInfo)
    {
        return procInfo.SpellInfo.Id == WarriorSpells.FURIOUS_SLASH;
    }
}