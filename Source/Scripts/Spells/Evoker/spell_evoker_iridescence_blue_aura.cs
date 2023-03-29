// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.BLUE_DISINTEGRATE,
             EvokerSpells.BLUE_DISINTEGRATE_2,
             EvokerSpells.ETERNITY_SURGE_CHARGED,
             EvokerSpells.BLUE_SHATTERING_STAR)]
public class spell_evoker_iridescence_blue_aura : SpellScript, ISpellCalculateMultiplier
{
    public double CalcMultiplier(double multiplier)
    {
        if (Caster.TryGetAura(EvokerSpells.IRIDESCENCE_BLUE, out var aura))
            multiplier *= 1 + (aura.SpellInfo.GetEffect(0).BasePoints * 0.01);

        return multiplier;
    }
}