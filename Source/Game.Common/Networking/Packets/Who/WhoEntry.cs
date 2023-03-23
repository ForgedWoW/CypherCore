// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Game.Entities;
using Game.Common.Entities.Objects;
using Game.Common.Networking;
using Game.Common.Networking.Packets.Query;

namespace Game.Common.Networking.Packets.Who;

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
