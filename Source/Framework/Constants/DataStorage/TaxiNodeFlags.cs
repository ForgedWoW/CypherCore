// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Framework.Constants;

[Flags]
public enum TaxiNodeFlags : ushort
{
    Alliance = 0x1,
    Horde = 0x2,
    UseFavoriteMount = 0x10
}