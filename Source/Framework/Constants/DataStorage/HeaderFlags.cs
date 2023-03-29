// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Framework.Constants;

[Flags]
public enum HeaderFlags : short
{
    None = 0x0,
    OffsetMap = 0x1,
    SecondIndex = 0x2,
    IndexMap = 0x4,
    Unknown = 0x8,
    Compressed = 0x10,
}