// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Framework.IO;
using Game.DataStorage;

namespace Game.Networking.Packets;

class DBQueryBulk : ClientPacket
{
	public uint TableHash;
	public List<DBQueryRecord> Queries = new();
	public DBQueryBulk(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		TableHash = _worldPacket.ReadUInt32();

		var count = _worldPacket.ReadBits<uint>(13);

		for (uint i = 0; i < count; ++i)
			Queries.Add(new DBQueryRecord(_worldPacket.ReadUInt32()));
	}

	public struct DBQueryRecord
	{
		public DBQueryRecord(uint recordId)
		{
			RecordID = recordId;
		}

		public uint RecordID;
	}
}

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

class AvailableHotfixes : ServerPacket
{
	public uint VirtualRealmAddress;
	public MultiMap<int, HotfixRecord> Hotfixes;

	public AvailableHotfixes(uint virtualRealmAddress, MultiMap<int, HotfixRecord> hotfixes) : base(ServerOpcodes.AvailableHotfixes)
	{
		VirtualRealmAddress = virtualRealmAddress;
		Hotfixes = hotfixes;
	}

	public override void Write()
	{
		_worldPacket.WriteUInt32(VirtualRealmAddress);
		_worldPacket.WriteInt32(Hotfixes.Keys.Count);

		foreach (var key in Hotfixes.Keys)
			Hotfixes[key][0].ID.Write(_worldPacket);
	}
}

class HotfixRequest : ClientPacket
{
	public uint ClientBuild;
	public uint DataBuild;
	public List<int> Hotfixes = new();
	public HotfixRequest(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		ClientBuild = _worldPacket.ReadUInt32();
		DataBuild = _worldPacket.ReadUInt32();

		var hotfixCount = _worldPacket.ReadUInt32();

		for (var i = 0; i < hotfixCount; ++i)
			Hotfixes.Add(_worldPacket.ReadInt32());
	}
}

class HotfixConnect : ServerPacket
{
	public List<HotfixData> Hotfixes = new();
	public ByteBuffer HotfixContent = new();
	public HotfixConnect() : base(ServerOpcodes.HotfixConnect) { }

	public override void Write()
	{
		_worldPacket.WriteInt32(Hotfixes.Count);

		foreach (var hotfix in Hotfixes)
			hotfix.Write(_worldPacket);

		_worldPacket.WriteUInt32(HotfixContent.GetSize());
		_worldPacket.WriteBytes(HotfixContent);
	}

	public class HotfixData
	{
		public HotfixRecord Record = new();
		public uint Size;

		public void Write(WorldPacket data)
		{
			Record.Write(data);
			data.WriteUInt32(Size);
			data.WriteBits((byte)Record.HotfixStatus, 3);
			data.FlushBits();
		}
	}
}