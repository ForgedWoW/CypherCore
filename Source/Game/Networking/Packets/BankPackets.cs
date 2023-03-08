// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;

namespace Game.Networking.Packets;

public class AutoBankItem : ClientPacket
{
	public InvUpdate Inv;
	public byte Bag;
	public byte Slot;

	public AutoBankItem(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Inv = new InvUpdate(_worldPacket);
		Bag = _worldPacket.ReadUInt8();
		Slot = _worldPacket.ReadUInt8();
	}
}

public class AutoStoreBankItem : ClientPacket
{
	public InvUpdate Inv;
	public byte Bag;
	public byte Slot;

	public AutoStoreBankItem(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Inv = new InvUpdate(_worldPacket);
		Bag = _worldPacket.ReadUInt8();
		Slot = _worldPacket.ReadUInt8();
	}
}

public class BuyBankSlot : ClientPacket
{
	public ObjectGuid Guid;
	public BuyBankSlot(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Guid = _worldPacket.ReadPackedGuid();
	}
}

class AutoBankReagent : ClientPacket
{
	public InvUpdate Inv;
	public byte Slot;
	public byte PackSlot;
	public AutoBankReagent(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Inv = new InvUpdate(_worldPacket);
		PackSlot = _worldPacket.ReadUInt8();
		Slot = _worldPacket.ReadUInt8();
	}
}

class AutoStoreBankReagent : ClientPacket
{
	public InvUpdate Inv;
	public byte Slot;
	public byte PackSlot;
	public AutoStoreBankReagent(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Inv = new InvUpdate(_worldPacket);
		Slot = _worldPacket.ReadUInt8();
		PackSlot = _worldPacket.ReadUInt8();
	}
}

// CMSG_BUY_REAGENT_BANK
// CMSG_REAGENT_BANK_DEPOSIT
class ReagentBank : ClientPacket
{
	public ObjectGuid Banker;
	public ReagentBank(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Banker = _worldPacket.ReadPackedGuid();
	}
}