// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Framework.Constants;

[Flags]
public enum HeaderFlags : short
{
    None = 0x0,
    Sparse = 0x1,
    SecondaryKey = 0x2,
    Index = 0x4,
    Unknown1 = 0x8,
    BitPacked = 0x10,
}