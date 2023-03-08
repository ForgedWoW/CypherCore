// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;

namespace Game.Networking.Packets;

public class AcceptTrade : ClientPacket
{
	public uint StateIndex;
	public AcceptTrade(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		StateIndex = _worldPacket.ReadUInt32();
	}
}

public class BeginTrade : ClientPacket
{
	public BeginTrade(WorldPacket packet) : base(packet) { }

	public override void Read() { }
}

public class BusyTrade : ClientPacket
{
	public BusyTrade(WorldPacket packet) : base(packet) { }

	public override void Read() { }
}

public class CancelTrade : ClientPacket
{
	public CancelTrade(WorldPacket packet) : base(packet) { }

	public override void Read() { }
}

public class ClearTradeItem : ClientPacket
{
	public byte TradeSlot;
	public ClearTradeItem(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		TradeSlot = _worldPacket.ReadUInt8();
	}
}

public class IgnoreTrade : ClientPacket
{
	public IgnoreTrade(WorldPacket packet) : base(packet) { }

	public override void Read() { }
}

public class InitiateTrade : ClientPacket
{
	public ObjectGuid Guid;
	public InitiateTrade(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Guid = _worldPacket.ReadPackedGuid();
	}
}

public class SetTradeCurrency : ClientPacket
{
	public uint Type;
	public uint Quantity;
	public SetTradeCurrency(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Type = _worldPacket.ReadUInt32();
		Quantity = _worldPacket.ReadUInt32();
	}
}

public class SetTradeGold : ClientPacket
{
	public ulong Coinage;
	public SetTradeGold(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Coinage = _worldPacket.ReadUInt64();
	}
}

public class SetTradeItem : ClientPacket
{
	public byte TradeSlot;
	public byte PackSlot;
	public byte ItemSlotInPack;
	public SetTradeItem(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		TradeSlot = _worldPacket.ReadUInt8();
		PackSlot = _worldPacket.ReadUInt8();
		ItemSlotInPack = _worldPacket.ReadUInt8();
	}
}

public class UnacceptTrade : ClientPacket
{
	public UnacceptTrade(WorldPacket packet) : base(packet) { }

	public override void Read() { }
}

public class TradeStatusPkt : ServerPacket
{
	public TradeStatus Status = TradeStatus.Initiated;
	public byte TradeSlot;
	public ObjectGuid PartnerAccount;
	public ObjectGuid Partner;
	public int CurrencyType;
	public int CurrencyQuantity;
	public bool FailureForYou;
	public InventoryResult BagResult;
	public uint ItemID;
	public uint Id;
	public bool PartnerIsSameBnetAccount;
	public TradeStatusPkt() : base(ServerOpcodes.TradeStatus, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WriteBit(PartnerIsSameBnetAccount);
		_worldPacket.WriteBits(Status, 5);

		switch (Status)
		{
			case TradeStatus.Failed:
				_worldPacket.WriteBit(FailureForYou);
				_worldPacket.WriteInt32((int)BagResult);
				_worldPacket.WriteUInt32(ItemID);

				break;
			case TradeStatus.Initiated:
				_worldPacket.WriteUInt32(Id);

				break;
			case TradeStatus.Proposed:
				_worldPacket.WritePackedGuid(Partner);
				_worldPacket.WritePackedGuid(PartnerAccount);

				break;
			case TradeStatus.WrongRealm:
			case TradeStatus.NotOnTaplist:
				_worldPacket.WriteUInt8(TradeSlot);

				break;
			case TradeStatus.NotEnoughCurrency:
			case TradeStatus.CurrencyNotTradable:
				_worldPacket.WriteInt32(CurrencyType);
				_worldPacket.WriteInt32(CurrencyQuantity);

				break;
			default:
				_worldPacket.FlushBits();

				break;
		}
	}
}

public class TradeUpdated : ServerPacket
{
	public ulong Gold;
	public uint CurrentStateIndex;
	public byte WhichPlayer;
	public uint ClientStateIndex;
	public List<TradeItem> Items = new();
	public int CurrencyType;
	public uint Id;
	public int ProposedEnchantment;
	public int CurrencyQuantity;
	public TradeUpdated() : base(ServerOpcodes.TradeUpdated, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WriteUInt8(WhichPlayer);
		_worldPacket.WriteUInt32(Id);
		_worldPacket.WriteUInt32(ClientStateIndex);
		_worldPacket.WriteUInt32(CurrentStateIndex);
		_worldPacket.WriteUInt64(Gold);
		_worldPacket.WriteInt32(CurrencyType);
		_worldPacket.WriteInt32(CurrencyQuantity);
		_worldPacket.WriteInt32(ProposedEnchantment);
		_worldPacket.WriteInt32(Items.Count);

		Items.ForEach(item => item.Write(_worldPacket));
	}

	public class UnwrappedTradeItem
	{
		public ItemInstance Item;
		public int EnchantID;
		public int OnUseEnchantmentID;
		public ObjectGuid Creator;
		public int Charges;
		public bool Lock;
		public uint MaxDurability;
		public uint Durability;
		public List<ItemGemData> Gems = new();

		public void Write(WorldPacket data)
		{
			data.WriteInt32(EnchantID);
			data.WriteInt32(OnUseEnchantmentID);
			data.WritePackedGuid(Creator);
			data.WriteInt32(Charges);
			data.WriteUInt32(MaxDurability);
			data.WriteUInt32(Durability);
			data.WriteBits(Gems.Count, 2);
			data.WriteBit(Lock);
			data.FlushBits();

			foreach (var gem in Gems)
				gem.Write(data);
		}
	}

	public class TradeItem
	{
		public byte Slot;
		public ItemInstance Item = new();
		public int StackCount;
		public ObjectGuid GiftCreator;
		public UnwrappedTradeItem Unwrapped;

		public void Write(WorldPacket data)
		{
			data.WriteUInt8(Slot);
			data.WriteInt32(StackCount);
			data.WritePackedGuid(GiftCreator);
			Item.Write(data);
			data.WriteBit(Unwrapped != null);
			data.FlushBits();

			if (Unwrapped != null)
				Unwrapped.Write(data);
		}
	}
}