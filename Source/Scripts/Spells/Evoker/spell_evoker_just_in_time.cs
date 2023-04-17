// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.GREEN_EMERALD_BLOSSOM,
             EvokerSpells.BLUE_DISINTEGRATE,
             EvokerSpells.BLUE_DISINTEGRATE_2,
             EvokerSpells.ECHO,
             EvokerSpells.DREAM_PROJECTION)]
public class SpellEvokerJustInTime : SpellScript, ISpellOnCast
{
    public void OnCast()
    {
        if (Caster.TryGetAura(EvokerSpells.JUST_IN_TIME, out var aura))
            Caster.SpellHistory.ModifyCooldown(EvokerSpells.BRONZE_TIME_DILATION, TimeSpan.FromSeconds(-aura.SpellInfo.GetEffect(0).BasePoints));
    }
}