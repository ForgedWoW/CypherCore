// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;

namespace Game.Networking.Packets;

class PartyMemberStats
{
	public ushort Level;
	public GroupMemberOnlineStatus Status;

	public int CurrentHealth;
	public int MaxHealth;

	public byte PowerType;
	public ushort CurrentPower;
	public ushort MaxPower;

	public ushort ZoneID;
	public short PositionX;
	public short PositionY;
	public short PositionZ;

	public int VehicleSeat;

	public PartyMemberPhaseStates Phases = new();
	public List<PartyMemberAuraStates> Auras = new();
	public PartyMemberPetStats PetStats;

	public ushort PowerDisplayID;
	public ushort SpecID;
	public ushort WmoGroupID;
	public uint WmoDoodadPlacementID;
	public sbyte[] PartyType = new sbyte[2];
	public CTROptions ChromieTime;
	public DungeonScoreSummary DungeonScore = new();

	public void Write(WorldPacket data)
	{
		for (byte i = 0; i < 2; i++)
			data.WriteInt8(PartyType[i]);

		data.WriteInt16((short)Status);
		data.WriteUInt8(PowerType);
		data.WriteInt16((short)PowerDisplayID);
		data.WriteInt32(CurrentHealth);
		data.WriteInt32(MaxHealth);
		data.WriteUInt16(CurrentPower);
		data.WriteUInt16(MaxPower);
		data.WriteUInt16(Level);
		data.WriteUInt16(SpecID);
		data.WriteUInt16(ZoneID);
		data.WriteUInt16(WmoGroupID);
		data.WriteUInt32(WmoDoodadPlacementID);
		data.WriteInt16(PositionX);
		data.WriteInt16(PositionY);
		data.WriteInt16(PositionZ);
		data.WriteInt32(VehicleSeat);
		data.WriteInt32(Auras.Count);

		Phases.Write(data);
		ChromieTime.Write(data);

		foreach (var aura in Auras)
			aura.Write(data);

		data.WriteBit(PetStats != null);
		data.FlushBits();

		DungeonScore.Write(data);

		if (PetStats != null)
			PetStats.Write(data);
	}
}