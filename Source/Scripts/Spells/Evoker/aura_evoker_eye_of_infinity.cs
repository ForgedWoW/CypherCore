// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.EYE_OF_INFINITY)]
public class AuraEvokerEyeOfInfinity : AuraScript, IAuraCheckProc
{
    public bool CheckProc(ProcEventInfo info)
    {
        return info.SpellInfo.Id == EvokerSpells.ETERNITY_SURGE_CHARGED &&
               info.DamageInfo != null &&
               info.DamageInfo.IsCritical;
    }
}