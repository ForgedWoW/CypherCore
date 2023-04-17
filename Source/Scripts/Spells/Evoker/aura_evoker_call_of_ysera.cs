// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.CALL_OF_YSERA_AURA)]
public class AuraEvokerCallOfYsera : AuraScript, IAuraCheckProc
{
    public bool CheckProc(ProcEventInfo info)
    {
        return info.SpellInfo.Id.EqualsAny(EvokerSpells.GREEN_DREAM_BREATH_CHARGED, EvokerSpells.RED_LIVING_FLAME_HEAL);
    }
}