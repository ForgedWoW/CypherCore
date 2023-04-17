// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.BURNOUT)]
public class AuraEvokerBurnout : AuraScript, IAuraCheckProc, IAuraOnProc
{
    public bool CheckProc(ProcEventInfo info)
    {
        return info.SpellInfo.Id == EvokerSpells.RED_FIRE_BREATH_CHARGED;
    }

    public void OnProc(ProcEventInfo info)
    {
        Caster.AddAura(EvokerSpells.BURNOUT_AURA);
    }
}