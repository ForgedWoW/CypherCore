// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Monk;

[SpellScript(132464)]
public class SpellMonkChiWaveHealingBolt : SpellScript, ISpellOnHit
{
    public void OnHit()
    {
        if (!OriginalCaster)
            return;

        var player = OriginalCaster.AsPlayer;

        if (player != null)
        {
            var target = HitUnit;

            if (target != null)
                player.SpellFactory.CastSpell(target, MonkSpells.CHI_WAVE_HEAL, true);
        }
    }
}