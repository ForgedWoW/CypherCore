﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.ECHO_AURA)]
public class AuraEvokerEcho : AuraScript, IAuraOnProc
{
    public void OnProc(ProcEventInfo info)
    {
        if (info.SpellInfo.Id != EvokerSpells.ECHO && Caster.TryGetAura(EvokerSpells.ECHO, out var echoAura))
        {
            var healInfo = info.HealInfo;

            if (healInfo == null)
                return;

            HealInfo newHeal = new(healInfo.Healer,
                                   healInfo.Target,
                                   healInfo.Heal * (echoAura.SpellInfo.GetEffect(1).BasePoints * 0.01),
                                   healInfo.SpellInfo,
                                   healInfo.SchoolMask);

            Unit.DealHeal(healInfo);
            echoAura.Remove();
        }
    }
}