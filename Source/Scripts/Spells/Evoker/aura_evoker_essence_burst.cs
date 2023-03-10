// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.ESSENCE_BURST)]
public class aura_evoker_essence_burst : AuraScript, IAuraCheckProc, IAuraOnProc
{
    public bool CheckProc(ProcEventInfo info)
    {
        if (info.ProcSpell == null || info.ProcSpell.SpellInfo == null)
            return false;

        return info.ProcSpell.SpellInfo.Id.EqualsAny(EvokerSpells.LIVING_FLAME_DAMAGE, EvokerSpells.LIVING_FLAME_HEAL);
    }

    public void OnProc(ProcEventInfo info)
    {
        if (TryGetCasterAsPlayer(out var player))
            player.AddAura(EvokerSpells.ESSENCE_BURST_AURA);
    }
}