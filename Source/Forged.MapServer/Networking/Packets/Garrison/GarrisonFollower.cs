// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.DataStorage.Structs.G;

namespace Forged.MapServer.Networking.Packets.Garrison;

public class GarrisonFollower
{
	public ulong DbID;
	public uint GarrFollowerID;
	public uint Quality;
	public uint FollowerLevel;
	public uint ItemLevelWeapon;
	public uint ItemLevelArmor;
	public uint Xp;
	public uint Durability;
	public uint CurrentBuildingID;
	public uint CurrentMissionID;
	public List<GarrAbilityRecord> AbilityID = new();
	public uint ZoneSupportSpellID;
	public uint FollowerStatus;
	public int Health;
	public long HealingTimestamp;
	public sbyte BoardIndex;
	public string CustomName = "";

	public void Write(WorldPacket data)
	{
		data.WriteUInt64(DbID);
		data.WriteUInt32(GarrFollowerID);
		data.WriteUInt32(Quality);
		data.WriteUInt32(FollowerLevel);
		data.WriteUInt32(ItemLevelWeapon);
		data.WriteUInt32(ItemLevelArmor);
		data.WriteUInt32(Xp);
		data.WriteUInt32(Durability);
		data.WriteUInt32(CurrentBuildingID);
		data.WriteUInt32(CurrentMissionID);
		data.WriteInt32(AbilityID.Count);
		data.WriteUInt32(ZoneSupportSpellID);
		data.WriteUInt32(FollowerStatus);
		data.WriteInt32(Health);
		data.WriteInt8(BoardIndex);
		data.WriteInt64(HealingTimestamp);

		AbilityID.ForEach(ability => data.WriteUInt32(ability.Id));

		data.WriteBits(CustomName.GetByteCount(), 7);
		data.FlushBits();
		data.WriteString(CustomName);
	}
}