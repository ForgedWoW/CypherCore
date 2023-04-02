﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
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
        _worldPacket.WritePackedGuid(Player);
        _worldPacket.WriteInt64(Time);
        _worldPacket.WriteUInt32(Size);
        _worldPacket.WriteBits(DataType, 4);

        if (CompressedData == null)
        {
            _worldPacket.WriteUInt32(0);
        }
        else
        {
            var bytes = CompressedData.GetData();
            _worldPacket.WriteInt32(bytes.Length);
            _worldPacket.WriteBytes(bytes);
        }
    }
}