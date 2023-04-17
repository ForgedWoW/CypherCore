// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;
using Framework.IO;

namespace Forged.MapServer.Networking.Packets.ClientConfig;

public class UpdateAccountData : ServerPacket
{
    public ByteBuffer CompressedData;
    public AccountDataTypes DataType = 0;
    public ObjectGuid Player;
    public uint Size;

    public long Time; // UnixTime

    // decompressed size
    public UpdateAccountData() : base(ServerOpcodes.UpdateAccountData) { }

    public override void Write()
    {
        WorldPacket.WritePackedGuid(Player);
        WorldPacket.WriteInt64(Time);
        WorldPacket.WriteUInt32(Size);
        WorldPacket.WriteBits(DataType, 4);

        if (CompressedData == null)
            WorldPacket.WriteUInt32(0);
        else
        {
            var bytes = CompressedData.GetData();
            WorldPacket.WriteInt32(bytes.Length);
            WorldPacket.WriteBytes(bytes);
        }
    }
}