﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;

namespace Scripts.Spells.Paladin;

// The fires of Justice - 203316
[SpellScript(203316)]
public class spell_pal_the_fire_of_justice : AuraScript, IAuraCheckProc
{
    public bool CheckProc(ProcEventInfo eventInfo)
    {
        return eventInfo.SpellInfo.Id == PaladinSpells.CRUSADER_STRIKE;
    }
}