// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;

namespace Game.Networking.Packets;

class BlackMarketOpen : ClientPacket
{
	public ObjectGuid Guid;
	public BlackMarketOpen(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Guid = _worldPacket.ReadPackedGuid();
	}
}

class BlackMarketRequestItems : ClientPacket
{
	public ObjectGuid Guid;
	public long LastUpdateID;
	public BlackMarketRequestItems(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Guid = _worldPacket.ReadPackedGuid();
		LastUpdateID = _worldPacket.ReadInt64();
	}
}

public class BlackMarketRequestItemsResult : ServerPacket
{
	public long LastUpdateID;
	public List<BlackMarketItem> Items = new();
	public BlackMarketRequestItemsResult() : base(ServerOpcodes.BlackMarketRequestItemsResult) { }

	public override void Write()
	{
		_worldPacket.WriteInt64(LastUpdateID);
		_worldPacket.WriteInt32(Items.Count);

		foreach (var item in Items)
			item.Write(_worldPacket);
	}
}

class BlackMarketBidOnItem : ClientPacket
{
	public ObjectGuid Guid;
	public uint MarketID;
	public ItemInstance Item = new();
	public ulong BidAmount;
	public BlackMarketBidOnItem(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Guid = _worldPacket.ReadPackedGuid();
		MarketID = _worldPacket.ReadUInt32();
		BidAmount = _worldPacket.ReadUInt64();
		Item.Read(_worldPacket);
	}
}

class BlackMarketBidOnItemResult : ServerPacket
{
	public uint MarketID;
	public ItemInstance Item;
	public BlackMarketError Result;
	public BlackMarketBidOnItemResult() : base(ServerOpcodes.BlackMarketBidOnItemResult) { }

	public override void Write()
	{
		_worldPacket.WriteUInt32(MarketID);
		_worldPacket.WriteUInt32((uint)Result);
		Item.Write(_worldPacket);
	}
}

class BlackMarketOutbid : ServerPacket
{
	public uint MarketID;
	public ItemInstance Item;
	public uint RandomPropertiesID;
	public BlackMarketOutbid() : base(ServerOpcodes.BlackMarketOutbid) { }

	public override void Write()
	{
		_worldPacket.WriteUInt32(MarketID);
		_worldPacket.WriteUInt32(RandomPropertiesID);
		Item.Write(_worldPacket);
	}
}

class BlackMarketWon : ServerPacket
{
	public uint MarketID;
	public ItemInstance Item;
	public int RandomPropertiesID;
	public BlackMarketWon() : base(ServerOpcodes.BlackMarketWon) { }

	public override void Write()
	{
		_worldPacket.WriteUInt32(MarketID);
		_worldPacket.WriteInt32(RandomPropertiesID);
		Item.Write(_worldPacket);
	}
}

public struct BlackMarketItem
{
	public void Read(WorldPacket data)
	{
		MarketID = data.ReadUInt32();
		SellerNPC = data.ReadUInt32();
		Item.Read(data);
		Quantity = data.ReadUInt32();
		MinBid = data.ReadUInt64();
		MinIncrement = data.ReadUInt64();
		CurrentBid = data.ReadUInt64();
		SecondsRemaining = data.ReadUInt32();
		NumBids = data.ReadUInt32();
		HighBid = data.HasBit();
	}

	public void Write(WorldPacket data)
	{
		data.WriteUInt32(MarketID);
		data.WriteUInt32(SellerNPC);
		data.WriteUInt32(Quantity);
		data.WriteUInt64(MinBid);
		data.WriteUInt64(MinIncrement);
		data.WriteUInt64(CurrentBid);
		data.WriteUInt32(SecondsRemaining);
		data.WriteUInt32(NumBids);
		Item.Write(data);
		data.WriteBit(HighBid);
		data.FlushBits();
	}

	public uint MarketID;
	public uint SellerNPC;
	public ItemInstance Item;
	public uint Quantity;
	public ulong MinBid;
	public ulong MinIncrement;
	public ulong CurrentBid;
	public uint SecondsRemaining;
	public uint NumBids;
	public bool HighBid;
}