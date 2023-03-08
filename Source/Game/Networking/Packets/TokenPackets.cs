// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;

namespace Game.Networking.Packets;

class CommerceTokenGetLog : ClientPacket
{
	public uint UnkInt;
	public CommerceTokenGetLog(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		UnkInt = _worldPacket.ReadUInt32();
	}
}

class CommerceTokenGetLogResponse : ServerPacket
{
	public uint UnkInt; // send CMSG_UPDATE_WOW_TOKEN_AUCTIONABLE_LIST
	public TokenResult Result;
	readonly List<AuctionableTokenInfo> AuctionableTokenAuctionableList = new();
	public CommerceTokenGetLogResponse() : base(ServerOpcodes.CommerceTokenGetLogResponse, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WriteUInt32(UnkInt);
		_worldPacket.WriteUInt32((uint)Result);
		_worldPacket.WriteInt32(AuctionableTokenAuctionableList.Count);

		foreach (var auctionableTokenAuctionable in AuctionableTokenAuctionableList)
		{
			_worldPacket.WriteUInt64(auctionableTokenAuctionable.UnkInt1);
			_worldPacket.WriteInt64(auctionableTokenAuctionable.UnkInt2);
			_worldPacket.WriteUInt64(auctionableTokenAuctionable.BuyoutPrice);
			_worldPacket.WriteUInt32(auctionableTokenAuctionable.Owner);
			_worldPacket.WriteUInt32(auctionableTokenAuctionable.DurationLeft);
		}
	}

	struct AuctionableTokenInfo
	{
		public ulong UnkInt1;
		public long UnkInt2;
		public uint Owner;
		public ulong BuyoutPrice;
		public uint DurationLeft;
	}
}

class CommerceTokenGetMarketPrice : ClientPacket
{
	public uint UnkInt;
	public CommerceTokenGetMarketPrice(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		UnkInt = _worldPacket.ReadUInt32();
	}
}

class CommerceTokenGetMarketPriceResponse : ServerPacket
{
	public ulong CurrentMarketPrice;
	public uint UnkInt; // send CMSG_REQUEST_WOW_TOKEN_MARKET_PRICE
	public TokenResult Result;
	public uint AuctionDuration; // preset auction duration enum
	public CommerceTokenGetMarketPriceResponse() : base(ServerOpcodes.CommerceTokenGetMarketPriceResponse) { }

	public override void Write()
	{
		_worldPacket.WriteUInt64(CurrentMarketPrice);
		_worldPacket.WriteUInt32(UnkInt);
		_worldPacket.WriteUInt32((uint)Result);
		_worldPacket.WriteUInt32(AuctionDuration);
	}
}