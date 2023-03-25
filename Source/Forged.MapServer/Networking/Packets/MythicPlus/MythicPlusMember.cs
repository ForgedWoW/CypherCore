// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;

namespace Game.Networking.Packets;

public struct MythicPlusMember
{
	public ObjectGuid BnetAccountGUID;
	public ulong GuildClubMemberID;
	public ObjectGuid GUID;
	public ObjectGuid GuildGUID;
	public uint NativeRealmAddress;
	public uint VirtualRealmAddress;
	public int ChrSpecializationID;
	public short RaceID;
	public int ItemLevel;
	public int CovenantID;
	public int SoulbindID;

	public void Write(WorldPacket data)
	{
		data.WritePackedGuid(BnetAccountGUID);
		data.WriteUInt64(GuildClubMemberID);
		data.WritePackedGuid(GUID);
		data.WritePackedGuid(GuildGUID);
		data.WriteUInt32(NativeRealmAddress);
		data.WriteUInt32(VirtualRealmAddress);
		data.WriteInt32(ChrSpecializationID);
		data.WriteInt16(RaceID);
		data.WriteInt32(ItemLevel);
		data.WriteInt32(CovenantID);
		data.WriteInt32(SoulbindID);
	}
}