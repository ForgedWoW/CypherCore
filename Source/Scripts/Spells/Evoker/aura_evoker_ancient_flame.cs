// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.ANCIENT_FLAME)]
public class AuraEvokerAncientFlame : AuraScript, IAuraOnProc
{
    public void OnProc(ProcEventInfo info)
    {
        if (Caster == Target)
            Caster.AddAura(EvokerSpells.ANCIENT_FLAME_AURA);
    }
}