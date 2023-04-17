// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.CHARGED_BLAST)]
public class AuraEvokerChargedBlast : AuraScript, IAuraCheckProc
{
    public bool CheckProc(ProcEventInfo info)
    {
        return info.SpellInfo.Labels.Contains(EvokerLabels.BLUE) && info.DamageInfo != null && info.DamageInfo.Damage > 0;
    }
}