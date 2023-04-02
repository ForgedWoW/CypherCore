// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.DataStorage;
using Framework.Constants;
using Framework.IO;

namespace Forged.MapServer.Networking.Packets.Hotfix;

public class DBReply : ServerPacket
{
    public ByteBuffer Data = new();
    public uint RecordID;
    public HotfixRecord.Status Status = HotfixRecord.Status.Invalid;
    public uint TableHash;
    public uint Timestamp;
    public DBReply() : base(ServerOpcodes.DbReply) { }

    public override void Write()
    {
        _worldPacket.WriteUInt32(TableHash);
        _worldPacket.WriteUInt32(RecordID);
        _worldPacket.WriteUInt32(Timestamp);
        _worldPacket.WriteBits((byte)Status, 3);
        _worldPacket.WriteUInt32(Data.GetSize());
        _worldPacket.WriteBytes(Data.GetData());
    }
}