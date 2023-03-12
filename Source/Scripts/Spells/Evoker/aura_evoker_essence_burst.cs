// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.ESSENCE_BURST)]
public class aura_evoker_essence_burst : AuraScript, IAuraOnProc
{
    public void OnProc(ProcEventInfo info)
    {
        if (TryGetCasterAsPlayer(out var player))
            player.AddAura(EvokerSpells.ESSENCE_BURST_AURA);
    }
}