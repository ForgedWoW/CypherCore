// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Networking.Packets.Warden;
using Framework.IO;

namespace Forged.MapServer.Warden;

internal class WardenModuleTransfer
{
    public WardenOpcodes Command { get; set; }
    public byte[] Data { get; set; } = new byte[500];
    public ushort DataSize { get; set; }

    public static implicit operator byte[](WardenModuleTransfer transfer)
    {
        var buffer = new ByteBuffer();
        buffer.WriteUInt8((byte)transfer.Command);
        buffer.WriteUInt16(transfer.DataSize);
        buffer.WriteBytes(transfer.Data, 500);

        return buffer.GetData();
    }
}