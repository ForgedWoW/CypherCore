// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Warden;

public class ClientWardenModule
{
    public byte[] CompressedData { get; set; }
    public uint CompressedSize { get; set; }
    public byte[] Id { get; set; } = new byte[16];
    public byte[] Key { get; set; } = new byte[16];
}