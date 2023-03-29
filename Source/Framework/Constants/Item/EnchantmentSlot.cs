// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum EnchantmentSlot
{
    Perm = 0,
    Temp = 1,
    Sock1 = 2,
    Sock2 = 3,
    Sock3 = 4,
    Bonus = 5,
    Prismatic = 6, // added at apply special permanent enchantment
    Use = 7,

    MaxInspected = 8,

    Prop0 = 8,  // used with RandomSuffix
    Prop1 = 9,  // used with RandomSuffix
    Prop2 = 10, // used with RandomSuffix and RandomProperty
    Prop3 = 11, // used with RandomProperty
    Prop4 = 12, // used with RandomProperty
    Max = 13
}