// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.RealmServer.Entities;

namespace Forged.RealmServer.Networking.Packets;

public struct CriteriaProgressPkt
{
	public void Write(WorldPacket data)
	{
		data.WriteUInt32(Id);
		data.WriteUInt64(Quantity);
		data.WritePackedGuid(Player);
		data.WritePackedTime(Date);
		data.WriteInt64(TimeFromStart);
		data.WriteInt64(TimeFromCreate);
		data.WriteBits(Flags, 4);
		data.WriteBit(RafAcceptanceID.HasValue);
		data.FlushBits();

		if (RafAcceptanceID.HasValue)
			data.WriteUInt64(RafAcceptanceID.Value);
	}

	public uint Id;
	public ulong Quantity;
	public ObjectGuid Player;
	public uint Flags;
	public long Date;
	public long TimeFromStart;
	public long TimeFromCreate;
	public ulong? RafAcceptanceID;
}