// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Forged.MapServer.Entities.Creatures;

public class CreatureBaseStats
{
    public uint AttackPower { get; set; }
    public uint BaseMana { get; set; }
    public uint RangedAttackPower { get; set; }

    // Helpers
    public uint GenerateMana(CreatureTemplate info)
    {
        // Mana can be 0.
        if (BaseMana == 0)
            return 0;

        return (uint)Math.Ceiling(BaseMana * info.ModMana * info.ModManaExtra);
    }
}