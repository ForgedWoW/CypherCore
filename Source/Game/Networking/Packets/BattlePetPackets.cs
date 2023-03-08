// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;

namespace Game.Networking.Packets;

class BattlePetJournal : ServerPacket
{
	public ushort Trap;
	public bool HasJournalLock = false;
	public List<BattlePetSlot> Slots = new();
	public List<BattlePetStruct> Pets = new();
	public BattlePetJournal() : base(ServerOpcodes.BattlePetJournal) { }

	public override void Write()
	{
		_worldPacket.WriteUInt16(Trap);
		_worldPacket.WriteInt32(Slots.Count);
		_worldPacket.WriteInt32(Pets.Count);
		_worldPacket.WriteBit(HasJournalLock);
		_worldPacket.FlushBits();

		foreach (var slot in Slots)
			slot.Write(_worldPacket);

		foreach (var pet in Pets)
			pet.Write(_worldPacket);
	}
}

class BattlePetJournalLockAcquired : ServerPacket
{
	public BattlePetJournalLockAcquired() : base(ServerOpcodes.BattlePetJournalLockAcquired) { }

	public override void Write() { }
}

class BattlePetJournalLockDenied : ServerPacket
{
	public BattlePetJournalLockDenied() : base(ServerOpcodes.BattlePetJournalLockDenied) { }

	public override void Write() { }
}

class BattlePetRequestJournal : ClientPacket
{
	public BattlePetRequestJournal(WorldPacket packet) : base(packet) { }

	public override void Read() { }
}

class BattlePetRequestJournalLock : ClientPacket
{
	public BattlePetRequestJournalLock(WorldPacket packet) : base(packet) { }

	public override void Read() { }
}

class BattlePetUpdates : ServerPacket
{
	public List<BattlePetStruct> Pets = new();
	public bool PetAdded;
	public BattlePetUpdates() : base(ServerOpcodes.BattlePetUpdates) { }

	public override void Write()
	{
		_worldPacket.WriteInt32(Pets.Count);
		_worldPacket.WriteBit(PetAdded);
		_worldPacket.FlushBits();

		foreach (var pet in Pets)
			pet.Write(_worldPacket);
	}
}

class PetBattleSlotUpdates : ServerPacket
{
	public List<BattlePetSlot> Slots = new();
	public bool AutoSlotted;
	public bool NewSlot;
	public PetBattleSlotUpdates() : base(ServerOpcodes.PetBattleSlotUpdates) { }

	public override void Write()
	{
		_worldPacket.WriteInt32(Slots.Count);
		_worldPacket.WriteBit(NewSlot);
		_worldPacket.WriteBit(AutoSlotted);
		_worldPacket.FlushBits();

		foreach (var slot in Slots)
			slot.Write(_worldPacket);
	}
}

class BattlePetSetBattleSlot : ClientPacket
{
	public ObjectGuid PetGuid;
	public byte Slot;
	public BattlePetSetBattleSlot(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		PetGuid = _worldPacket.ReadPackedGuid();
		Slot = _worldPacket.ReadUInt8();
	}
}

class BattlePetModifyName : ClientPacket
{
	public ObjectGuid PetGuid;
	public string Name;
	public DeclinedName DeclinedNames;
	public BattlePetModifyName(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		PetGuid = _worldPacket.ReadPackedGuid();
		var nameLength = _worldPacket.ReadBits<uint>(7);

		if (_worldPacket.HasBit())
		{
			DeclinedNames = new DeclinedName();

			var declinedNameLengths = new byte[SharedConst.MaxDeclinedNameCases];

			for (byte i = 0; i < SharedConst.MaxDeclinedNameCases; ++i)
				declinedNameLengths[i] = _worldPacket.ReadBits<byte>(7);

			for (byte i = 0; i < SharedConst.MaxDeclinedNameCases; ++i)
				DeclinedNames.Name[i] = _worldPacket.ReadString(declinedNameLengths[i]);
		}

		Name = _worldPacket.ReadString(nameLength);
	}
}

class QueryBattlePetName : ClientPacket
{
	public ObjectGuid BattlePetID;
	public ObjectGuid UnitGUID;
	public QueryBattlePetName(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		BattlePetID = _worldPacket.ReadPackedGuid();
		UnitGUID = _worldPacket.ReadPackedGuid();
	}
}

class QueryBattlePetNameResponse : ServerPacket
{
	public ObjectGuid BattlePetID;
	public uint CreatureID;
	public long Timestamp;
	public bool Allow;

	public bool HasDeclined;
	public DeclinedName DeclinedNames;
	public string Name;
	public QueryBattlePetNameResponse() : base(ServerOpcodes.QueryBattlePetNameResponse, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(BattlePetID);
		_worldPacket.WriteUInt32(CreatureID);
		_worldPacket.WriteInt64(Timestamp);

		_worldPacket.WriteBit(Allow);

		if (Allow)
		{
			_worldPacket.WriteBits(Name.GetByteCount(), 8);
			_worldPacket.WriteBit(HasDeclined);

			for (byte i = 0; i < SharedConst.MaxDeclinedNameCases; ++i)
				_worldPacket.WriteBits(DeclinedNames.Name[i].GetByteCount(), 7);

			for (byte i = 0; i < SharedConst.MaxDeclinedNameCases; ++i)
				_worldPacket.WriteString(DeclinedNames.Name[i]);

			_worldPacket.WriteString(Name);
		}

		_worldPacket.FlushBits();
	}
}

class BattlePetDeletePet : ClientPacket
{
	public ObjectGuid PetGuid;
	public BattlePetDeletePet(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		PetGuid = _worldPacket.ReadPackedGuid();
	}
}

class BattlePetSetFlags : ClientPacket
{
	public ObjectGuid PetGuid;
	public uint Flags;
	public FlagsControlType ControlType;
	public BattlePetSetFlags(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		PetGuid = _worldPacket.ReadPackedGuid();
		Flags = _worldPacket.ReadUInt32();
		ControlType = (FlagsControlType)_worldPacket.ReadBits<byte>(2);
	}
}

class BattlePetClearFanfare : ClientPacket
{
	public ObjectGuid PetGuid;
	public BattlePetClearFanfare(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		PetGuid = _worldPacket.ReadPackedGuid();
	}
}

class CageBattlePet : ClientPacket
{
	public ObjectGuid PetGuid;
	public CageBattlePet(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		PetGuid = _worldPacket.ReadPackedGuid();
	}
}

class BattlePetDeleted : ServerPacket
{
	public ObjectGuid PetGuid;
	public BattlePetDeleted() : base(ServerOpcodes.BattlePetDeleted) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(PetGuid);
	}
}

class BattlePetErrorPacket : ServerPacket
{
	public BattlePetError Result;
	public uint CreatureID;
	public BattlePetErrorPacket() : base(ServerOpcodes.BattlePetError) { }

	public override void Write()
	{
		_worldPacket.WriteBits(Result, 4);
		_worldPacket.WriteUInt32(CreatureID);
	}
}

class BattlePetSummon : ClientPacket
{
	public ObjectGuid PetGuid;
	public BattlePetSummon(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		PetGuid = _worldPacket.ReadPackedGuid();
	}
}

class BattlePetUpdateNotify : ClientPacket
{
	public ObjectGuid PetGuid;

	public BattlePetUpdateNotify(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		PetGuid = _worldPacket.ReadPackedGuid();
	}
}

//Structs
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

public class BattlePetSlot
{
	public BattlePetStruct Pet;
	public uint CollarID;
	public byte Index;
	public bool Locked = true;

	public void Write(WorldPacket data)
	{
		data.WritePackedGuid(Pet.Guid.IsEmpty ? ObjectGuid.Create(HighGuid.BattlePet, 0) : Pet.Guid);
		data.WriteUInt32(CollarID);
		data.WriteUInt8(Index);
		data.WriteBit(Locked);
		data.FlushBits();
	}
}