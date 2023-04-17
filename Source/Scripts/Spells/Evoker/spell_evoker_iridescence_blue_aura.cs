// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.BLUE_DISINTEGRATE,
             EvokerSpells.BLUE_DISINTEGRATE_2,
             EvokerSpells.ETERNITY_SURGE_CHARGED,
             EvokerSpells.BLUE_SHATTERING_STAR)]
public class SpellEvokerIridescenceBlueAura : SpellScript, ISpellCalculateMultiplier
{
    public double CalcMultiplier(double multiplier)
    {
        if (Caster.TryGetAura(EvokerSpells.IRIDESCENCE_BLUE, out var aura))
            multiplier *= 1 + (aura.SpellInfo.GetEffect(0).BasePoints * 0.01);

        return multiplier;
    }
}