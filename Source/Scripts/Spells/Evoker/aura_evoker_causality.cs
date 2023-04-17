// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.CAUSALITY)]
public class AuraEvokerCausality : AuraScript, IAuraCheckProc
{
    public bool CheckProc(ProcEventInfo info)
    {
        var id = info.SpellInfo.Id;

        return id.EqualsAny(EvokerSpells.BLUE_DISINTEGRATE, EvokerSpells.BLUE_DISINTEGRATE_2, EvokerSpells.ECHO, EvokerSpells.RED_PYRE) || (id == EvokerSpells.GREEN_EMERALD_BLOSSOM && Caster.TryGetAsPlayer(out var player) && player.HasSpell(EvokerSpells.IMPROVED_EMERALD_BLOSSOM));
    }
}