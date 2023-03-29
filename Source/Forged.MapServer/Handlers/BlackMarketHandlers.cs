// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.BlackMarket;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Items;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Networking;
using Forged.MapServer.Networking.Packets.BlackMarket;
using Forged.MapServer.Networking.Packets.Item;
using Forged.MapServer.Networking.Packets.NPC;
using Framework.Constants;
using Framework.Database;
using Game.Common.Handlers;
using Serilog;

namespace Forged.MapServer.Handlers;

public class BlackMarketHandlers : IWorldSessionHandler
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

    [WorldPacketHandler(ClientOpcodes.BlackMarketOpen)]
    private void HandleBlackMarketOpen(BlackMarketOpen blackMarketOpen)
    {
        var unit = Player.GetNPCIfCanInteractWith(blackMarketOpen.Guid, NPCFlags.BlackMarket, NPCFlags2.BlackMarketView);

        if (!unit)
        {
            Log.Logger.Debug("WORLD: HandleBlackMarketHello - {0} not found or you can't interact with him.", blackMarketOpen.Guid.ToString());

            return;
        }

        // remove fake death
        if (Player.HasUnitState(UnitState.Died))
            Player.RemoveAurasByType(AuraType.FeignDeath);

        SendBlackMarketOpenResult(blackMarketOpen.Guid, unit);
    }

    private void SendBlackMarketOpenResult(ObjectGuid guid, Creature auctioneer)
    {
        NPCInteractionOpenResult npcInteraction = new();
        npcInteraction.Npc = guid;
        npcInteraction.InteractionType = PlayerInteractionType.BlackMarketAuctioneer;
        npcInteraction.Success = Global.BlackMarketMgr.IsEnabled;
        SendPacket(npcInteraction);
    }

    [WorldPacketHandler(ClientOpcodes.BlackMarketRequestItems)]
    private void HandleBlackMarketRequestItems(BlackMarketRequestItems blackMarketRequestItems)
    {
        if (!Global.BlackMarketMgr.IsEnabled)
            return;

        var unit = Player.GetNPCIfCanInteractWith(blackMarketRequestItems.Guid, NPCFlags.BlackMarket, NPCFlags2.BlackMarketView);

        if (!unit)
        {
            Log.Logger.Debug("WORLD: HandleBlackMarketRequestItems - {0} not found or you can't interact with him.", blackMarketRequestItems.Guid.ToString());

            return;
        }

        BlackMarketRequestItemsResult result = new();
        Global.BlackMarketMgr.BuildItemsResponse(result, Player);
        SendPacket(result);
    }

    [WorldPacketHandler(ClientOpcodes.BlackMarketBidOnItem)]
    private void HandleBlackMarketBidOnItem(BlackMarketBidOnItem blackMarketBidOnItem)
    {
        if (!Global.BlackMarketMgr.IsEnabled)
            return;

        var player = Player;
        var unit = player.GetNPCIfCanInteractWith(blackMarketBidOnItem.Guid, NPCFlags.BlackMarket, NPCFlags2.None);

        if (!unit)
        {
            Log.Logger.Debug("WORLD: HandleBlackMarketBidOnItem - {0} not found or you can't interact with him.", blackMarketBidOnItem.Guid.ToString());

            return;
        }

        var entry = Global.BlackMarketMgr.GetAuctionByID(blackMarketBidOnItem.MarketID);

        if (entry == null)
        {
            Log.Logger.Debug("WORLD: HandleBlackMarketBidOnItem - {0} (name: {1}) tried to bid on a nonexistent auction (MarketId: {2}).", player.GUID.ToString(), player.GetName(), blackMarketBidOnItem.MarketID);
            SendBlackMarketBidOnItemResult(BlackMarketError.ItemNotFound, blackMarketBidOnItem.MarketID, blackMarketBidOnItem.Item);

            return;
        }

        if (entry.Bidder == player.GUID.Counter)
        {
            Log.Logger.Debug("WORLD: HandleBlackMarketBidOnItem - {0} (name: {1}) tried to place a bid on an item he already bid on. (MarketId: {2}).", player.GUID.ToString(), player.GetName(), blackMarketBidOnItem.MarketID);
            SendBlackMarketBidOnItemResult(BlackMarketError.AlreadyBid, blackMarketBidOnItem.MarketID, blackMarketBidOnItem.Item);

            return;
        }

        if (!entry.ValidateBid(blackMarketBidOnItem.BidAmount))
        {
            Log.Logger.Debug("WORLD: HandleBlackMarketBidOnItem - {0} (name: {1}) tried to place an invalid bid. Amount: {2} (MarketId: {3}).", player.GUID.ToString(), player.GetName(), blackMarketBidOnItem.BidAmount, blackMarketBidOnItem.MarketID);
            SendBlackMarketBidOnItemResult(BlackMarketError.HigherBid, blackMarketBidOnItem.MarketID, blackMarketBidOnItem.Item);

            return;
        }

        if (!player.HasEnoughMoney(blackMarketBidOnItem.BidAmount))
        {
            Log.Logger.Debug("WORLD: HandleBlackMarketBidOnItem - {0} (name: {1}) does not have enough money to place bid. (MarketId: {2}).", player.GUID.ToString(), player.GetName(), blackMarketBidOnItem.MarketID);
            SendBlackMarketBidOnItemResult(BlackMarketError.NotEnoughMoney, blackMarketBidOnItem.MarketID, blackMarketBidOnItem.Item);

            return;
        }

        if (entry.GetSecondsRemaining() <= 0)
        {
            Log.Logger.Debug("WORLD: HandleBlackMarketBidOnItem - {0} (name: {1}) tried to bid on a completed auction. (MarketId: {2}).", player.GUID.ToString(), player.GetName(), blackMarketBidOnItem.MarketID);
            SendBlackMarketBidOnItemResult(BlackMarketError.DatabaseError, blackMarketBidOnItem.MarketID, blackMarketBidOnItem.Item);

            return;
        }

        SQLTransaction trans = new();

        Global.BlackMarketMgr.SendAuctionOutbidMail(entry, trans);
        entry.PlaceBid(blackMarketBidOnItem.BidAmount, player, trans);

        DB.Characters.CommitTransaction(trans);

        SendBlackMarketBidOnItemResult(BlackMarketError.Ok, blackMarketBidOnItem.MarketID, blackMarketBidOnItem.Item);
    }

    private void SendBlackMarketBidOnItemResult(BlackMarketError result, uint marketId, ItemInstance item)
    {
        BlackMarketBidOnItemResult packet = new();

        packet.MarketID = marketId;
        packet.Item = item;
        packet.Result = result;

        SendPacket(packet);
    }
}