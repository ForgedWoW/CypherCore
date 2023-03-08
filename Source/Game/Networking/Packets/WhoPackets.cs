// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;

namespace Game.Networking.Packets;

public class WhoIsRequest : ClientPacket
{
	public string CharName;
	public WhoIsRequest(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		CharName = _worldPacket.ReadString(_worldPacket.ReadBits<uint>(6));
	}
}

public class WhoIsResponse : ServerPacket
{
	public string AccountName;
	public WhoIsResponse() : base(ServerOpcodes.WhoIs) { }

	public override void Write()
	{
		_worldPacket.WriteBits(AccountName.GetByteCount(), 11);
		_worldPacket.WriteString(AccountName);
	}
}

public class WhoRequestPkt : ClientPacket
{
	public WhoRequest Request = new();
	public uint RequestID;
	public List<int> Areas = new();
	public WhoRequestPkt(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		var areasCount = _worldPacket.ReadBits<uint>(4);

		Request.Read(_worldPacket);
		RequestID = _worldPacket.ReadUInt32();

		for (var i = 0; i < areasCount; ++i)
			Areas.Add(_worldPacket.ReadInt32());
	}
}

public class WhoResponsePkt : ServerPacket
{
	public uint RequestID;
	public List<WhoEntry> Response = new();
	public WhoResponsePkt() : base(ServerOpcodes.Who) { }

	public override void Write()
	{
		_worldPacket.WriteUInt32(RequestID);
		_worldPacket.WriteBits(Response.Count, 6);
		_worldPacket.FlushBits();

		Response.ForEach(p => p.Write(_worldPacket));
	}
}

public struct WhoRequestServerInfo
{
	public void Read(WorldPacket data)
	{
		FactionGroup = data.ReadInt32();
		Locale = data.ReadInt32();
		RequesterVirtualRealmAddress = data.ReadUInt32();
	}

	public int FactionGroup;
	public int Locale;
	public uint RequesterVirtualRealmAddress;
}

public class WhoRequest
{
	public int MinLevel;
	public int MaxLevel;
	public string Name;
	public string VirtualRealmName;
	public string Guild;
	public string GuildVirtualRealmName;
	public long RaceFilter;
	public int ClassFilter = -1;
	public List<string> Words = new();
	public bool ShowEnemies;
	public bool ShowArenaPlayers;
	public bool ExactName;
	public WhoRequestServerInfo? ServerInfo;

	public void Read(WorldPacket data)
	{
		MinLevel = data.ReadInt32();
		MaxLevel = data.ReadInt32();
		RaceFilter = data.ReadInt64();
		ClassFilter = data.ReadInt32();

		var nameLength = data.ReadBits<uint>(6);
		var virtualRealmNameLength = data.ReadBits<uint>(9);
		var guildNameLength = data.ReadBits<uint>(7);
		var guildVirtualRealmNameLength = data.ReadBits<uint>(9);
		var wordsCount = data.ReadBits<uint>(3);

		ShowEnemies = data.HasBit();
		ShowArenaPlayers = data.HasBit();
		ExactName = data.HasBit();

		if (data.HasBit())
			ServerInfo = new WhoRequestServerInfo();

		data.ResetBitPos();

		for (var i = 0; i < wordsCount; ++i)
		{
			Words.Add(data.ReadString(data.ReadBits<uint>(7)));
			data.ResetBitPos();
		}

		Name = data.ReadString(nameLength);
		VirtualRealmName = data.ReadString(virtualRealmNameLength);
		Guild = data.ReadString(guildNameLength);
		GuildVirtualRealmName = data.ReadString(guildVirtualRealmNameLength);

		if (ServerInfo.HasValue)
			ServerInfo.Value.Read(data);
	}
}

public class WhoEntry
{
	public PlayerGuidLookupData PlayerData = new();
	public ObjectGuid GuildGUID;
	public uint GuildVirtualRealmAddress;
	public string GuildName = "";
	public int AreaID;
	public bool IsGM;

	public void Write(WorldPacket data)
	{
		PlayerData.Write(data);

		data.WritePackedGuid(GuildGUID);
		data.WriteUInt32(GuildVirtualRealmAddress);
		data.WriteInt32(AreaID);

		data.WriteBits(GuildName.GetByteCount(), 7);
		data.WriteBit(IsGM);
		data.WriteString(GuildName);

		data.FlushBits();
	}
}