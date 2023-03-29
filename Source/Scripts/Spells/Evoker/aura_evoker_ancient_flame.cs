// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.ANCIENT_FLAME)]
public class aura_evoker_ancient_flame : AuraScript, IAuraOnProc
{
    public void OnProc(ProcEventInfo info)
    {
        if (Caster == Target)
            Caster.AddAura(EvokerSpells.ANCIENT_FLAME_AURA);
    }
}