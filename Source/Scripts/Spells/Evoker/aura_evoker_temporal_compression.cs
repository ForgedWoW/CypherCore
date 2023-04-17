// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.TEMPORAL_COMPRESSION)]
public class AuraEvokerTemporalCompression : AuraScript, IAuraCheckProc, IAuraOnProc
{
    public bool CheckProc(ProcEventInfo info)
    {
        return info.SpellInfo.Labels.Contains(EvokerLabels.BRONZE);
    }

    public void OnProc(ProcEventInfo info)
    {
        Caster.AddAura(EvokerSpells.TEMPORAL_COMPRESSION_AURA);
    }
}