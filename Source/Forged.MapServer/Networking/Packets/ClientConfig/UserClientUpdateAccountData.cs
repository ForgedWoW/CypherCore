// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;
using Framework.IO;

namespace Forged.MapServer.Networking.Packets.ClientConfig;

public class UserClientUpdateAccountData : ClientPacket
{
    public ByteBuffer CompressedData;
    public AccountDataTypes DataType = 0;
    public ObjectGuid PlayerGuid;
    public uint Size;
    public long Time; // UnixTime
                      // decompressed size
    public UserClientUpdateAccountData(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        PlayerGuid = WorldPacket.ReadPackedGuid();
        Time = WorldPacket.ReadInt64();
        Size = WorldPacket.ReadUInt32();
        DataType = (AccountDataTypes)WorldPacket.ReadBits<uint>(4);

        var compressedSize = WorldPacket.ReadUInt32();

        if (compressedSize != 0)
            CompressedData = new ByteBuffer(WorldPacket.ReadBytes(compressedSize));
    }
}