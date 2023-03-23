// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Game.Entities;
using Game.Common.Entities.Objects;
using Game.Common.Networking;
using Game.Common.Networking.Packets.BattlePet;

namespace Game.Common.Networking.Packets.BattlePet;

public struct BattlePetStruct
{
	public void Write(WorldPacket data)
	{
		data.WritePackedGuid(Guid);
		data.WriteUInt32(Species);
		data.WriteUInt32(CreatureID);
		data.WriteUInt32(DisplayID);
		data.WriteUInt16(Breed);
		data.WriteUInt16(Level);
		data.WriteUInt16(Exp);
		data.WriteUInt16(Flags);
		data.WriteUInt32(Power);
		data.WriteUInt32(Health);
		data.WriteUInt32(MaxHealth);
		data.WriteUInt32(Speed);
		data.WriteUInt8(Quality);
		data.WriteBits(Name.GetByteCount(), 7);
		data.WriteBit(OwnerInfo.HasValue); // HasOwnerInfo
		data.WriteBit(false);              // NoRename
		data.FlushBits();

		data.WriteString(Name);

		if (OwnerInfo.HasValue)
		{
			data.WritePackedGuid(OwnerInfo.Value.Guid);
			data.WriteUInt32(OwnerInfo.Value.PlayerVirtualRealm); // Virtual
			data.WriteUInt32(OwnerInfo.Value.PlayerNativeRealm);  // Native
		}
	}

	public struct BattlePetOwnerInfo
	{
		public ObjectGuid Guid;
		public uint PlayerVirtualRealm;
		public uint PlayerNativeRealm;
	}

	public ObjectGuid Guid;
	public uint Species;
	public uint CreatureID;
	public uint DisplayID;
	public ushort Breed;
	public ushort Level;
	public ushort Exp;
	public ushort Flags;
	public uint Power;
	public uint Health;
	public uint MaxHealth;
	public uint Speed;
	public byte Quality;
	public BattlePetOwnerInfo? OwnerInfo;
	public string Name;
}
