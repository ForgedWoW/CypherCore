// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Networking;
using Game.Common.Networking.Packets.Who;

namespace Game.Common.Networking.Packets.Who;

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
