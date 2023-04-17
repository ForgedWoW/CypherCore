// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.AZURE_STRIKE)]
public class SpellEvokerAzureEssenceBurst : SpellScript, ISpellAfterHit
{
    public void AfterHit()
    {
        if (Caster.TryGetAsPlayer(out var player) && player.HasSpell(EvokerSpells.AZURE_ESSENCE_BURST) && (player.TryGetAura(EvokerSpells.RED_DRAGONRAGE, out var drAura) || RandomHelper.randChance(SpellManager.Instance.GetSpellInfo(EvokerSpells.AZURE_ESSENCE_BURST).GetEffect(0).BasePoints)))
            player.AddAura(EvokerSpells.AZURE_RUBY_ESSENCE_BURST_AURA);
    }
}