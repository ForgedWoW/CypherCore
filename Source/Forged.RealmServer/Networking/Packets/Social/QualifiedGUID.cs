// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.RealmServer.Entities;

namespace Forged.RealmServer.Networking.Packets;

public struct QualifiedGUID
{
	public void Read(WorldPacket data)
	{
		VirtualRealmAddress = data.ReadUInt32();
		Guid = data.ReadPackedGuid();
	}

	public ObjectGuid Guid;
	public uint VirtualRealmAddress;
}