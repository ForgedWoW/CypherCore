// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.RED_FIRE_BREATH_CHARGED,
             EvokerSpells.RED_FIRE_STORM_DAMAGE,
             EvokerSpells.RED_LIVING_FLAME_DAMAGE,
             EvokerSpells.RED_PYRE_DAMAGE)]
public class SpellEvokerIridescenceRedAura : SpellScript, ISpellCalculateMultiplier
{
    public double CalcMultiplier(double multiplier)
    {
        if (Caster.TryGetAura(EvokerSpells.IRIDESCENCE_RED, out var aura))
            multiplier *= 1 + (aura.SpellInfo.GetEffect(0).BasePoints * 0.01);

        return multiplier;
    }
}