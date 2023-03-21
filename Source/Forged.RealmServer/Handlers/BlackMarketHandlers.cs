// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Framework.Database;
using Forged.RealmServer.BlackMarket;
using Forged.RealmServer.Entities;
using Forged.RealmServer.Networking;
using Forged.RealmServer.Networking.Packets;

namespace Forged.RealmServer;

public partial class WorldSession
{
	public void SendBlackMarketWonNotification(BlackMarketEntry entry, Item item)
	{
		BlackMarketWon packet = new();

		packet.MarketID = entry.MarketId;
		packet.Item = new ItemInstance(item);

		SendPacket(packet);
	}

	public void SendBlackMarketOutbidNotification(BlackMarketTemplate templ)
	{
		BlackMarketOutbid packet = new();

		packet.MarketID = templ.MarketID;
		packet.Item = templ.Item;
		packet.RandomPropertiesID = 0;

		SendPacket(packet);
	}

	[WorldPacketHandler(ClientOpcodes.BlackMarketBidOnItem)]
	void HandleBlackMarketBidOnItem(BlackMarketBidOnItem blackMarketBidOnItem)
	{
		if (!Global.BlackMarketMgr.IsEnabled)
			return;

		var player = Player;
		var unit = player.GetNPCIfCanInteractWith(blackMarketBidOnItem.Guid, NPCFlags.BlackMarket, NPCFlags2.None);

		if (!unit)
		{
			Log.outDebug(LogFilter.Network, "WORLD: HandleBlackMarketBidOnItem - {0} not found or you can't interact with him.", blackMarketBidOnItem.Guid.ToString());

			return;
		}

		var entry = Global.BlackMarketMgr.GetAuctionByID(blackMarketBidOnItem.MarketID);

		if (entry == null)
		{
			Log.outDebug(LogFilter.Network, "WORLD: HandleBlackMarketBidOnItem - {0} (name: {1}) tried to bid on a nonexistent auction (MarketId: {2}).", player.GUID.ToString(), player.GetName(), blackMarketBidOnItem.MarketID);
			SendBlackMarketBidOnItemResult(BlackMarketError.ItemNotFound, blackMarketBidOnItem.MarketID, blackMarketBidOnItem.Item);

			return;
		}

		if (entry.Bidder == player.GUID.Counter)
		{
			Log.outDebug(LogFilter.Network, "WORLD: HandleBlackMarketBidOnItem - {0} (name: {1}) tried to place a bid on an item he already bid on. (MarketId: {2}).", player.GUID.ToString(), player.GetName(), blackMarketBidOnItem.MarketID);
			SendBlackMarketBidOnItemResult(BlackMarketError.AlreadyBid, blackMarketBidOnItem.MarketID, blackMarketBidOnItem.Item);

			return;
		}

		if (!entry.ValidateBid(blackMarketBidOnItem.BidAmount))
		{
			Log.outDebug(LogFilter.Network, "WORLD: HandleBlackMarketBidOnItem - {0} (name: {1}) tried to place an invalid bid. Amount: {2} (MarketId: {3}).", player.GUID.ToString(), player.GetName(), blackMarketBidOnItem.BidAmount, blackMarketBidOnItem.MarketID);
			SendBlackMarketBidOnItemResult(BlackMarketError.HigherBid, blackMarketBidOnItem.MarketID, blackMarketBidOnItem.Item);

			return;
		}

		if (!player.HasEnoughMoney(blackMarketBidOnItem.BidAmount))
		{
			Log.outDebug(LogFilter.Network, "WORLD: HandleBlackMarketBidOnItem - {0} (name: {1}) does not have enough money to place bid. (MarketId: {2}).", player.GUID.ToString(), player.GetName(), blackMarketBidOnItem.MarketID);
			SendBlackMarketBidOnItemResult(BlackMarketError.NotEnoughMoney, blackMarketBidOnItem.MarketID, blackMarketBidOnItem.Item);

			return;
		}

		if (entry.GetSecondsRemaining() <= 0)
		{
			Log.outDebug(LogFilter.Network, "WORLD: HandleBlackMarketBidOnItem - {0} (name: {1}) tried to bid on a completed auction. (MarketId: {2}).", player.GUID.ToString(), player.GetName(), blackMarketBidOnItem.MarketID);
			SendBlackMarketBidOnItemResult(BlackMarketError.DatabaseError, blackMarketBidOnItem.MarketID, blackMarketBidOnItem.Item);

			return;
		}

		SQLTransaction trans = new();

		Global.BlackMarketMgr.SendAuctionOutbidMail(entry, trans);
		entry.PlaceBid(blackMarketBidOnItem.BidAmount, player, trans);

		DB.Characters.CommitTransaction(trans);

		SendBlackMarketBidOnItemResult(BlackMarketError.Ok, blackMarketBidOnItem.MarketID, blackMarketBidOnItem.Item);
	}

	void SendBlackMarketBidOnItemResult(BlackMarketError result, uint marketId, ItemInstance item)
	{
		BlackMarketBidOnItemResult packet = new();

		packet.MarketID = marketId;
		packet.Item = item;
		packet.Result = result;

		SendPacket(packet);
	}
}