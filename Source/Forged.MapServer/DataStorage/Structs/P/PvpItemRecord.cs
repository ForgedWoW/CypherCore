// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.DataStorage.Structs.P;

public sealed record PvpItemRecord
{
    public uint Id;
    public uint ItemID;
    public byte ItemLevelDelta;
}