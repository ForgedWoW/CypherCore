﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;

namespace Scripts.Spells.Paladin;

// 216860 - Judgement of the Pure
[SpellScript(216860)]
public class spell_pal_judgement_of_the_pure : AuraScript, IAuraCheckProc
{
    public bool CheckProc(ProcEventInfo eventInfo)
    {
        var spellinfo = eventInfo.SpellInfo;

        return spellinfo != null && eventInfo.SpellInfo.Id == PaladinSpells.JUDGMENT;
    }
}