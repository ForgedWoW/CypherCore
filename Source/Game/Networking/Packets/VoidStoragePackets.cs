// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;

namespace Game.Networking.Packets;

class VoidTransferResult : ServerPacket
{
	public VoidTransferError Result;

	public VoidTransferResult(VoidTransferError result) : base(ServerOpcodes.VoidTransferResult, ConnectionType.Instance)
	{
		Result = result;
	}

	public override void Write()
	{
		_worldPacket.WriteInt32((int)Result);
	}
}

class UnlockVoidStorage : ClientPacket
{
	public ObjectGuid Npc;
	public UnlockVoidStorage(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Npc = _worldPacket.ReadPackedGuid();
	}
}

class QueryVoidStorage : ClientPacket
{
	public ObjectGuid Npc;
	public QueryVoidStorage(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Npc = _worldPacket.ReadPackedGuid();
	}
}

class VoidStorageFailed : ServerPacket
{
	public byte Reason = 0;
	public VoidStorageFailed() : base(ServerOpcodes.VoidStorageFailed, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WriteUInt8(Reason);
	}
}

class VoidStorageContents : ServerPacket
{
	public List<VoidItem> Items = new();
	public VoidStorageContents() : base(ServerOpcodes.VoidStorageContents, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WriteBits(Items.Count, 8);
		_worldPacket.FlushBits();

		foreach (var voidItem in Items)
			voidItem.Write(_worldPacket);
	}
}

class VoidStorageTransfer : ClientPacket
{
	public ObjectGuid[] Withdrawals = new ObjectGuid[(int)SharedConst.VoidStorageMaxWithdraw];
	public ObjectGuid[] Deposits = new ObjectGuid[(int)SharedConst.VoidStorageMaxDeposit];
	public ObjectGuid Npc;
	public VoidStorageTransfer(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Npc = _worldPacket.ReadPackedGuid();
		var DepositCount = _worldPacket.ReadInt32();
		var WithdrawalCount = _worldPacket.ReadInt32();

		for (uint i = 0; i < DepositCount; ++i)
			Deposits[i] = _worldPacket.ReadPackedGuid();

		for (uint i = 0; i < WithdrawalCount; ++i)
			Withdrawals[i] = _worldPacket.ReadPackedGuid();
	}
}

class VoidStorageTransferChanges : ServerPacket
{
	public List<ObjectGuid> RemovedItems = new();
	public List<VoidItem> AddedItems = new();
	public VoidStorageTransferChanges() : base(ServerOpcodes.VoidStorageTransferChanges, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WriteBits(AddedItems.Count, 4);
		_worldPacket.WriteBits(RemovedItems.Count, 4);
		_worldPacket.FlushBits();

		foreach (var addedItem in AddedItems)
			addedItem.Write(_worldPacket);

		foreach (var removedItem in RemovedItems)
			_worldPacket.WritePackedGuid(removedItem);
	}
}

class SwapVoidItem : ClientPacket
{
	public ObjectGuid Npc;
	public ObjectGuid VoidItemGuid;
	public uint DstSlot;
	public SwapVoidItem(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Npc = _worldPacket.ReadPackedGuid();
		VoidItemGuid = _worldPacket.ReadPackedGuid();
		DstSlot = _worldPacket.ReadUInt32();
	}
}

class VoidItemSwapResponse : ServerPacket
{
	public ObjectGuid VoidItemA;
	public ObjectGuid VoidItemB;
	public uint VoidItemSlotA;
	public uint VoidItemSlotB;
	public VoidItemSwapResponse() : base(ServerOpcodes.VoidItemSwapResponse, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(VoidItemA);
		_worldPacket.WriteUInt32(VoidItemSlotA);
		_worldPacket.WritePackedGuid(VoidItemB);
		_worldPacket.WriteUInt32(VoidItemSlotB);
	}
}

struct VoidItem
{
	public void Write(WorldPacket data)
	{
		data.WritePackedGuid(Guid);
		data.WritePackedGuid(Creator);
		data.WriteUInt32(Slot);
		Item.Write(data);
	}

	public ObjectGuid Guid;
	public ObjectGuid Creator;
	public uint Slot;
	public ItemInstance Item;
}