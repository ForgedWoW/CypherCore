// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Networking.Packets.Warden;
using Framework.IO;

namespace Forged.MapServer.Warden;

internal class WardenHashRequest
{
    public WardenOpcodes Command;
    public byte[] Seed = new byte[16];

    public static implicit operator byte[](WardenHashRequest request)
    {
        var buffer = new ByteBuffer();
        buffer.WriteUInt8((byte)request.Command);
        buffer.WriteBytes(request.Seed);

        return buffer.GetData();
    }
}