// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Networking.Packets.Warden;
using Framework.IO;

namespace Forged.MapServer.Warden;

internal class WardenModuleUse
{
    public WardenOpcodes Command;
    public byte[] ModuleId = new byte[16];
    public byte[] ModuleKey = new byte[16];
    public uint Size;

    public static implicit operator byte[](WardenModuleUse use)
    {
        var buffer = new ByteBuffer();
        buffer.WriteUInt8((byte)use.Command);
        buffer.WriteBytes(use.ModuleId, 16);
        buffer.WriteBytes(use.ModuleKey, 16);
        buffer.WriteUInt32(use.Size);

        return buffer.GetData();
    }
}