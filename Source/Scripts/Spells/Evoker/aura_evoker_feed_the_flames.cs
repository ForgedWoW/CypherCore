// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.AZURE_RUBY_ESSENCE_BURST_AURA)]
public class AuraEvokerFeedTheFlames : AuraScript, IAuraOnRemove
{
    public void AuraRemoved(AuraRemoveMode removeMode)
    {
        if (removeMode == AuraRemoveMode.Default && TryGetCasterAsPlayer(out var player) && player.IsAlive && player.HasSpell(EvokerSpells.FEED_THE_FLAMES))
        {
            var cdr = TimeSpan.FromSeconds(SpellManager.Instance.GetSpellInfo(EvokerSpells.FEED_THE_FLAMES).GetEffect(0).BasePoints);
            player.SpellHistory.ModifyCooldown(EvokerSpells.RED_FIRE_BREATH, -cdr);
            player.SpellHistory.ModifyCooldown(EvokerSpells.RED_FIRE_BREATH_2, -cdr);
        }
    }
}