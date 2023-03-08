﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;

namespace Game.Networking.Packets;

class PlayerAzeriteItemGains : ServerPacket
{
	public ObjectGuid ItemGUID;
	public ulong XP;
	public PlayerAzeriteItemGains() : base(ServerOpcodes.PlayerAzeriteItemGains) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(ItemGUID);
		_worldPacket.WriteUInt64(XP);
	}
}

class AzeriteEssenceUnlockMilestone : ClientPacket
{
	public int AzeriteItemMilestonePowerID;
	public AzeriteEssenceUnlockMilestone(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		AzeriteItemMilestonePowerID = _worldPacket.ReadInt32();
	}
}

class AzeriteEssenceActivateEssence : ClientPacket
{
	public uint AzeriteEssenceID;
	public byte Slot;
	public AzeriteEssenceActivateEssence(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		AzeriteEssenceID = _worldPacket.ReadUInt32();
		Slot = _worldPacket.ReadUInt8();
	}
}

class ActivateEssenceFailed : ServerPacket
{
	public AzeriteEssenceActivateResult Reason;
	public uint Arg;
	public uint AzeriteEssenceID;
	public byte? Slot;
	public ActivateEssenceFailed() : base(ServerOpcodes.ActivateEssenceFailed) { }

	public override void Write()
	{
		_worldPacket.WriteBits((int)Reason, 4);
		_worldPacket.WriteBit(Slot.HasValue);
		_worldPacket.WriteUInt32(Arg);
		_worldPacket.WriteUInt32(AzeriteEssenceID);

		if (Slot.HasValue)
			_worldPacket.WriteUInt8(Slot.Value);
	}
}

class AzeriteEmpoweredItemViewed : ClientPacket
{
	public ObjectGuid ItemGUID;
	public AzeriteEmpoweredItemViewed(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		ItemGUID = _worldPacket.ReadPackedGuid();
	}
}

class AzeriteEmpoweredItemSelectPower : ClientPacket
{
	public int Tier;
	public int AzeritePowerID;
	public byte ContainerSlot;
	public byte Slot;
	public AzeriteEmpoweredItemSelectPower(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Tier = _worldPacket.ReadInt32();
		AzeritePowerID = _worldPacket.ReadInt32();
		ContainerSlot = _worldPacket.ReadUInt8();
		Slot = _worldPacket.ReadUInt8();
	}
}

class PlayerAzeriteItemEquippedStatusChanged : ServerPacket
{
	public bool IsHeartEquipped;
	public PlayerAzeriteItemEquippedStatusChanged() : base(ServerOpcodes.PlayerAzeriteItemEquippedStatusChanged) { }

	public override void Write()
	{
		_worldPacket.WriteBit(IsHeartEquipped);
		_worldPacket.FlushBits();
	}
}