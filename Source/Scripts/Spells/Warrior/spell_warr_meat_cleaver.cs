// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;

namespace Scripts.Spells.Warrior;
// Improved Whirlwind - 12950

public class SpellWarrMeatCleaver : AuraScript, IAuraCheckProc
{
    public bool CheckProc(ProcEventInfo unnamedParameter)
    {
        return false;
    }
}