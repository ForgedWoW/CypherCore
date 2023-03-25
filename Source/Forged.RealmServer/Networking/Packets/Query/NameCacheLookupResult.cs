// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.RealmServer.Entities;

namespace Forged.RealmServer.Networking.Packets;

public struct NameCacheLookupResult
{
	public ObjectGuid Player;
	public byte Result; // 0 - full packet, != 0 - only guid
	public PlayerGuidLookupData Data;
	public NameCacheUnused920 Unused920;

	public void Write(WorldPacket data)
	{
		data.WriteUInt8(Result);
		data.WritePackedGuid(Player);
		data.WriteBit(Data != null);
		data.WriteBit(Unused920 != null);
		data.FlushBits();

		if (Data != null)
			Data.Write(data);

		if (Unused920 != null)
			Unused920.Write(data);
	}
}