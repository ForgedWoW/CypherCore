// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.RealmServer.Entities;

namespace Forged.RealmServer.Networking.Packets;

public class RideTicket
{
	public ObjectGuid RequesterGuid;
	public uint Id;
	public RideType Type;
	public long Time;
	public bool Unknown925;

	public void Read(WorldPacket data)
	{
		RequesterGuid = data.ReadPackedGuid();
		Id = data.ReadUInt32();
		Type = (RideType)data.ReadUInt32();
		Time = data.ReadInt64();
		Unknown925 = data.HasBit();
		data.ResetBitPos();
	}

	public void Write(WorldPacket data)
	{
		data.WritePackedGuid(RequesterGuid);
		data.WriteUInt32(Id);
		data.WriteUInt32((uint)Type);
		data.WriteInt64(Time);
		data.WriteBit(Unknown925);
		data.FlushBits();
	}
}