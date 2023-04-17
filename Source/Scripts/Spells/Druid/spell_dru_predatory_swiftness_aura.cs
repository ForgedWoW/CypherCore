// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Druid;

[SpellScript(69369)]
public class SpellDruPredatorySwiftnessAura : SpellScript, ISpellAfterHit
{
    public void AfterHit()
    {
        var player = Caster.AsPlayer;

        if (player != null)
            if (player.HasAura(PredatorySwiftnessSpells.PredatorySwiftnessAura))
                player.RemoveAura(PredatorySwiftnessSpells.PredatorySwiftnessAura);
    }
}