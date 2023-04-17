// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.RED_PYRE, EvokerSpells.BLUE_DISINTEGRATE, EvokerSpells.BLUE_DISINTEGRATE_2)]
public class SpellEvokerAzureRubyEssenceBurst : SpellScript, ISpellOnCast
{
    public void OnCast()
    {
        if (Caster.TryGetAsPlayer(out var player) && (!player.TryGetAura(EvokerSpells.HOARDED_POWER, out var hpAura) || !RandomHelper.randChance(hpAura.SpellInfo.GetEffect(0).BasePoints)))
            player.RemoveAura(EvokerSpells.AZURE_RUBY_ESSENCE_BURST_AURA);
    }
}