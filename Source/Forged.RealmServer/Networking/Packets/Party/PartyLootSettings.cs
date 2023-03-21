// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.RealmServer.Entities;

namespace Forged.RealmServer.Networking.Packets;

struct PartyLootSettings
{
	public void Write(WorldPacket data)
	{
		data.WriteUInt8(Method);
		data.WritePackedGuid(LootMaster);
		data.WriteUInt8(Threshold);
	}

	public byte Method;
	public ObjectGuid LootMaster;
	public byte Threshold;
}