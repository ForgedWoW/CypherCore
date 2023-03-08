﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Numerics;
using Framework.Constants;
using Game.Entities;

namespace Game.Networking.Packets;

class DismissCritter : ClientPacket
{
	public ObjectGuid CritterGUID;
	public DismissCritter(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		CritterGUID = _worldPacket.ReadPackedGuid();
	}
}

class RequestPetInfo : ClientPacket
{
	public RequestPetInfo(WorldPacket packet) : base(packet) { }

	public override void Read() { }
}

class PetAbandon : ClientPacket
{
	public ObjectGuid Pet;
	public PetAbandon(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Pet = _worldPacket.ReadPackedGuid();
	}
}

class PetStopAttack : ClientPacket
{
	public ObjectGuid PetGUID;
	public PetStopAttack(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		PetGUID = _worldPacket.ReadPackedGuid();
	}
}

class PetSpellAutocast : ClientPacket
{
	public ObjectGuid PetGUID;
	public uint SpellID;
	public bool AutocastEnabled;
	public PetSpellAutocast(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		PetGUID = _worldPacket.ReadPackedGuid();
		SpellID = _worldPacket.ReadUInt32();
		AutocastEnabled = _worldPacket.HasBit();
	}
}

public class PetSpells : ServerPacket
{
	public ObjectGuid PetGUID;
	public ushort CreatureFamily;
	public ushort Specialization;
	public uint TimeLimit;
	public ReactStates ReactState;
	public CommandStates CommandState;
	public byte Flag;

	public uint[] ActionButtons = new uint[10];

	public List<uint> Actions = new();
	public List<PetSpellCooldown> Cooldowns = new();
	public List<PetSpellHistory> SpellHistory = new();
	public PetSpells() : base(ServerOpcodes.PetSpellsMessage, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(PetGUID);
		_worldPacket.WriteUInt16(CreatureFamily);
		_worldPacket.WriteUInt16(Specialization);
		_worldPacket.WriteUInt32(TimeLimit);
		_worldPacket.WriteUInt16((ushort)((byte)CommandState | (Flag << 16)));
		_worldPacket.WriteUInt8((byte)ReactState);

		foreach (var actionButton in ActionButtons)
			_worldPacket.WriteUInt32(actionButton);

		_worldPacket.WriteInt32(Actions.Count);
		_worldPacket.WriteInt32(Cooldowns.Count);
		_worldPacket.WriteInt32(SpellHistory.Count);

		foreach (var action in Actions)
			_worldPacket.WriteUInt32(action);

		foreach (var cooldown in Cooldowns)
		{
			_worldPacket.WriteUInt32(cooldown.SpellID);
			_worldPacket.WriteUInt32(cooldown.Duration);
			_worldPacket.WriteUInt32(cooldown.CategoryDuration);
			_worldPacket.WriteFloat(cooldown.ModRate);
			_worldPacket.WriteUInt16(cooldown.Category);
		}

		foreach (var history in SpellHistory)
		{
			_worldPacket.WriteUInt32(history.CategoryID);
			_worldPacket.WriteUInt32(history.RecoveryTime);
			_worldPacket.WriteFloat(history.ChargeModRate);
			_worldPacket.WriteInt8(history.ConsumedCharges);
		}
	}
}

class PetStableList : ServerPacket
{
	public ObjectGuid StableMaster;
	public List<PetStableInfo> Pets = new();
	public PetStableList() : base(ServerOpcodes.PetStableList, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(StableMaster);

		_worldPacket.WriteInt32(Pets.Count);

		foreach (var pet in Pets)
		{
			_worldPacket.WriteUInt32(pet.PetSlot);
			_worldPacket.WriteUInt32(pet.PetNumber);
			_worldPacket.WriteUInt32(pet.CreatureID);
			_worldPacket.WriteUInt32(pet.DisplayID);
			_worldPacket.WriteUInt32(pet.ExperienceLevel);
			_worldPacket.WriteUInt8((byte)pet.PetFlags);
			_worldPacket.WriteBits(pet.PetName.GetByteCount(), 8);
			_worldPacket.WriteString(pet.PetName);
		}
	}
}

class PetStableResult : ServerPacket
{
	public StableResult Result;
	public PetStableResult() : base(ServerOpcodes.PetStableResult, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WriteUInt8((byte)Result);
	}
}

class PetLearnedSpells : ServerPacket
{
	public List<uint> Spells = new();
	public PetLearnedSpells() : base(ServerOpcodes.PetLearnedSpells, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WriteInt32(Spells.Count);

		foreach (var spell in Spells)
			_worldPacket.WriteUInt32(spell);
	}
}

class PetUnlearnedSpells : ServerPacket
{
	public List<uint> Spells = new();
	public PetUnlearnedSpells() : base(ServerOpcodes.PetUnlearnedSpells, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WriteInt32(Spells.Count);

		foreach (var spell in Spells)
			_worldPacket.WriteUInt32(spell);
	}
}

class PetNameInvalid : ServerPacket
{
	public PetRenameData RenameData;
	public PetNameInvalidReason Result;
	public PetNameInvalid() : base(ServerOpcodes.PetNameInvalid) { }

	public override void Write()
	{
		_worldPacket.WriteUInt8((byte)Result);
		_worldPacket.WritePackedGuid(RenameData.PetGUID);
		_worldPacket.WriteInt32(RenameData.PetNumber);

		_worldPacket.WriteUInt8((byte)RenameData.NewName.GetByteCount());

		_worldPacket.WriteBit(RenameData.HasDeclinedNames);

		if (RenameData.HasDeclinedNames)
		{
			for (var i = 0; i < SharedConst.MaxDeclinedNameCases; i++)
				_worldPacket.WriteBits(RenameData.DeclinedNames.Name[i].GetByteCount(), 7);

			for (var i = 0; i < SharedConst.MaxDeclinedNameCases; i++)
				_worldPacket.WriteString(RenameData.DeclinedNames.Name[i]);
		}

		_worldPacket.WriteString(RenameData.NewName);
	}
}

class PetRename : ClientPacket
{
	public PetRenameData RenameData;
	public PetRename(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		RenameData.PetGUID = _worldPacket.ReadPackedGuid();
		RenameData.PetNumber = _worldPacket.ReadInt32();

		var nameLen = _worldPacket.ReadBits<uint>(8);

		RenameData.HasDeclinedNames = _worldPacket.HasBit();

		if (RenameData.HasDeclinedNames)
		{
			RenameData.DeclinedNames = new DeclinedName();
			var count = new uint[SharedConst.MaxDeclinedNameCases];

			for (var i = 0; i < SharedConst.MaxDeclinedNameCases; i++)
				count[i] = _worldPacket.ReadBits<uint>(7);

			for (var i = 0; i < SharedConst.MaxDeclinedNameCases; i++)
				RenameData.DeclinedNames.Name[i] = _worldPacket.ReadString(count[i]);
		}

		RenameData.NewName = _worldPacket.ReadString(nameLen);
	}
}

class PetAction : ClientPacket
{
	public ObjectGuid PetGUID;
	public uint Action;
	public ObjectGuid TargetGUID;
	public Vector3 ActionPosition;
	public PetAction(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		PetGUID = _worldPacket.ReadPackedGuid();

		Action = _worldPacket.ReadUInt32();
		TargetGUID = _worldPacket.ReadPackedGuid();

		ActionPosition = _worldPacket.ReadVector3();
	}
}

class PetSetAction : ClientPacket
{
	public ObjectGuid PetGUID;
	public uint Index;
	public uint Action;
	public PetSetAction(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		PetGUID = _worldPacket.ReadPackedGuid();

		Index = _worldPacket.ReadUInt32();
		Action = _worldPacket.ReadUInt32();
	}
}

class CancelModSpeedNoControlAuras : ClientPacket
{
	public ObjectGuid TargetGUID;

	public CancelModSpeedNoControlAuras(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		TargetGUID = _worldPacket.ReadPackedGuid();
	}
}

class PetCancelAura : ClientPacket
{
	public ObjectGuid PetGUID;
	public uint SpellID;
	public PetCancelAura(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		PetGUID = _worldPacket.ReadPackedGuid();
		SpellID = _worldPacket.ReadUInt32();
	}
}

class SetPetSpecialization : ServerPacket
{
	public ushort SpecID;
	public SetPetSpecialization() : base(ServerOpcodes.SetPetSpecialization) { }

	public override void Write()
	{
		_worldPacket.WriteUInt16(SpecID);
	}
}

class PetActionFeedbackPacket : ServerPacket
{
	public uint SpellID;
	public PetActionFeedback Response;
	public PetActionFeedbackPacket() : base(ServerOpcodes.PetStableResult) { }

	public override void Write()
	{
		_worldPacket.WriteUInt32(SpellID);
		_worldPacket.WriteUInt8((byte)Response);
	}
}

class PetActionSound : ServerPacket
{
	public ObjectGuid UnitGUID;
	public PetTalk Action;
	public PetActionSound() : base(ServerOpcodes.PetStableResult) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(UnitGUID);
		_worldPacket.WriteUInt32((uint)Action);
	}
}

class PetTameFailure : ServerPacket
{
	public byte Result;
	public PetTameFailure() : base(ServerOpcodes.PetTameFailure) { }

	public override void Write()
	{
		_worldPacket.WriteUInt8(Result);
	}
}

//Structs
public class PetSpellCooldown
{
	public uint SpellID;
	public uint Duration;
	public uint CategoryDuration;
	public float ModRate = 1.0f;
	public ushort Category;
}

public class PetSpellHistory
{
	public uint CategoryID;
	public uint RecoveryTime;
	public float ChargeModRate = 1.0f;
	public sbyte ConsumedCharges;
}

struct PetStableInfo
{
	public uint PetSlot;
	public uint PetNumber;
	public uint CreatureID;
	public uint DisplayID;
	public uint ExperienceLevel;
	public PetStableinfo PetFlags;
	public string PetName;
}

struct PetRenameData
{
	public ObjectGuid PetGUID;
	public int PetNumber;
	public string NewName;
	public bool HasDeclinedNames;
	public DeclinedName DeclinedNames;
}