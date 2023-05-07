// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.DataStorage.Structs.B;

public sealed record BannedAddonsRecord
{
    public byte Flags;
    public uint Id;
    public string Name;
    public string Version;
}