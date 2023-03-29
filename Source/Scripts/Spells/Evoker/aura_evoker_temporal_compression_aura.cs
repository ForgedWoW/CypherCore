// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.TEMPORAL_COMPRESSION_AURA)]
public class aura_evoker_temporal_compression_aura : AuraScript, IAuraCheckProc, IAuraOnProc
{
    public bool CheckProc(ProcEventInfo info)
    {
        return info.SpellInfo.Id.EqualsAny(EvokerSpells.GREEN_DREAM_BREATH_CHARGED,
                                           EvokerSpells.ETERNITY_SURGE_CHARGED,
                                           EvokerSpells.RED_FIRE_BREATH_CHARGED,
                                           EvokerSpells.SPIRITBLOOM_CHARGED);
    }

    public void OnProc(ProcEventInfo info)
    {
        if (Caster.TryGetAsPlayer(out var player) && player.TryGetAura(EvokerSpells.TEMPORAL_COMPRESSION_AURA, out var tcAura))
        {
            // Spark of Insight
            if (player.HasSpell(EvokerSpells.SPARK_OF_INSIGHT) && tcAura.StackAmount == tcAura.SpellInfo.StackAmount)
                player.AddAura(EvokerSpells.ESSENCE_BURST_AURA);

            player.RemoveAura(tcAura);
        }
    }
}