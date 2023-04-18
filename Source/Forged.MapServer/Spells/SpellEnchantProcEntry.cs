// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Spells;

public class SpellEnchantProcEntry
{
    public EnchantProcAttributes AttributesMask { get; set; }
    public float Chance { get; set; } // if nonzero - overwrite SpellItemEnchantment value
    public uint HitMask { get; set; }

    public float ProcsPerMinute { get; set; } // if nonzero - chance to proc is equal to value * aura caster's weapon speed / 60
    // if nonzero - bitmask for matching proc condition based on hit result, see enum ProcFlagsHit
    // bitmask, see EnchantProcAttributes
}