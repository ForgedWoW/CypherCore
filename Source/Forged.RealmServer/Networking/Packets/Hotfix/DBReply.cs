// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Framework.IO;
using Forged.RealmServer.DataStorage;

namespace Forged.RealmServer.Networking.Packets;

public class DBReply : ServerPacket
{
	public uint TableHash;
	public uint Timestamp;
	public uint RecordID;
	public HotfixRecord.Status Status = HotfixRecord.Status.Invalid;

	public ByteBuffer Data = new();
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