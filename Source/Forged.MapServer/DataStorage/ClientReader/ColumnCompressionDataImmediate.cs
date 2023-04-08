// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.DataStorage.ClientReader;

public struct ColumnCompressionDataImmediate
{
    public int BitOffset;
    public int BitWidth;
    public int Flags; // 0x1 signed
}