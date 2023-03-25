// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.Party;

struct PartyLFGInfo
{
	public void Write(WorldPacket data)
	{
		data.WriteUInt8(MyFlags);
		data.WriteUInt32(Slot);
		data.WriteUInt32(MyRandomSlot);
		data.WriteUInt8(MyPartialClear);
		data.WriteFloat(MyGearDiff);
		data.WriteUInt8(MyStrangerCount);
		data.WriteUInt8(MyKickVoteCount);
		data.WriteUInt8(BootCount);
		data.WriteBit(Aborted);
		data.WriteBit(MyFirstReward);
		data.FlushBits();
	}

	public byte MyFlags;
	public uint Slot;
	public byte BootCount;
	public uint MyRandomSlot;
	public bool Aborted;
	public byte MyPartialClear;
	public float MyGearDiff;
	public byte MyStrangerCount;
	public byte MyKickVoteCount;
	public bool MyFirstReward;
}