// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Spells;

public class SpellEnchantProcEntry
{
    public EnchantProcAttributes AttributesMask;
    public float Chance;                         // if nonzero - overwrite SpellItemEnchantment value
    public uint HitMask;
    public float ProcsPerMinute;                 // if nonzero - chance to proc is equal to value * aura caster's weapon speed / 60
                                                 // if nonzero - bitmask for matching proc condition based on hit result, see enum ProcFlagsHit
                                                 // bitmask, see EnchantProcAttributes
}