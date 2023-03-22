// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Networking.Packets;

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