// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.BlackMarket;
using Forged.MapServer.Entities.Items;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Networking;
using Forged.MapServer.Networking.Packets.BlackMarket;
using Forged.MapServer.Networking.Packets.Item;
using Forged.MapServer.Networking.Packets.NPC;
using Forged.MapServer.Server;
using Framework.Constants;
using Framework.Database;
using Game.Common.Handlers;
using Serilog;

// ReSharper disable UnusedMember.Local

namespace Forged.MapServer.OpCodeHandlers;

public class BlackMarketHandlers : IWorldSessionHandler
{
    private readonly BlackMarketManager _blackMarketManager;
    private readonly CharacterDatabase _characterDatabase;
    private readonly WorldSession _session;

    public BlackMarketHandlers(WorldSession session, BlackMarketManager blackMarketManager, CharacterDatabase characterDatabase)
    {
        _session = session;
        _blackMarketManager = blackMarketManager;
        _characterDatabase = characterDatabase;
    }

    public void SendBlackMarketOutbidNotification(BlackMarketTemplate templ)
    {
        _session.SendPacket(new BlackMarketOutbid()
        {
            MarketID = templ.MarketID,
            Item = templ.Item,
            RandomPropertiesID = 0
        });
    }

    public void SendBlackMarketWonNotification(BlackMarketEntry entry, Item item)
    {
        _session.SendPacket(new BlackMarketWon()
        {
            MarketID = entry.MarketId,
            Item = new ItemInstance(item)
        });
    }

    [WorldPacketHandler(ClientOpcodes.BlackMarketBidOnItem)]
    private void HandleBlackMarketBidOnItem(BlackMarketBidOnItem blackMarketBidOnItem)
    {
        if (!_blackMarketManager.IsEnabled)
            return;

        var unit = _session.Player.GetNPCIfCanInteractWith(blackMarketBidOnItem.Guid, NPCFlags.BlackMarket, NPCFlags2.None);

        if (unit == null)
        {
            Log.Logger.Debug("WORLD: HandleBlackMarketBidOnItem - {0} not found or you can't interact with him.", blackMarketBidOnItem.Guid.ToString());

            return;
        }

        var entry = _blackMarketManager.GetAuctionByID(blackMarketBidOnItem.MarketID);

        if (entry == null)
        {
            Log.Logger.Debug("WORLD: HandleBlackMarketBidOnItem - {0} (name: {1}) tried to bid on a nonexistent auction (MarketId: {2}).", _session.Player.GUID.ToString(), _session.Player.GetName(), blackMarketBidOnItem.MarketID);
            SendBlackMarketBidOnItemResult(BlackMarketError.ItemNotFound, blackMarketBidOnItem.MarketID, blackMarketBidOnItem.Item);

            return;
        }

        if (entry.Bidder == _session.Player.GUID.Counter)
        {
            Log.Logger.Debug("WORLD: HandleBlackMarketBidOnItem - {0} (name: {1}) tried to place a bid on an item he already bid on. (MarketId: {2}).", _session.Player.GUID.ToString(), _session.Player.GetName(), blackMarketBidOnItem.MarketID);
            SendBlackMarketBidOnItemResult(BlackMarketError.AlreadyBid, blackMarketBidOnItem.MarketID, blackMarketBidOnItem.Item);

            return;
        }

        if (!entry.ValidateBid(blackMarketBidOnItem.BidAmount))
        {
            Log.Logger.Debug("WORLD: HandleBlackMarketBidOnItem - {0} (name: {1}) tried to place an invalid bid. Amount: {2} (MarketId: {3}).", _session.Player.GUID.ToString(), _session.Player.GetName(), blackMarketBidOnItem.BidAmount, blackMarketBidOnItem.MarketID);
            SendBlackMarketBidOnItemResult(BlackMarketError.HigherBid, blackMarketBidOnItem.MarketID, blackMarketBidOnItem.Item);

            return;
        }

        if (!_session.Player.HasEnoughMoney(blackMarketBidOnItem.BidAmount))
        {
            Log.Logger.Debug("WORLD: HandleBlackMarketBidOnItem - {0} (name: {1}) does not have enough money to place bid. (MarketId: {2}).", _session.Player.GUID.ToString(), _session.Player.GetName(), blackMarketBidOnItem.MarketID);
            SendBlackMarketBidOnItemResult(BlackMarketError.NotEnoughMoney, blackMarketBidOnItem.MarketID, blackMarketBidOnItem.Item);

            return;
        }

        if (entry.GetSecondsRemaining() <= 0)
        {
            Log.Logger.Debug("WORLD: HandleBlackMarketBidOnItem - {0} (name: {1}) tried to bid on a completed auction. (MarketId: {2}).", _session.Player.GUID.ToString(), _session.Player.GetName(), blackMarketBidOnItem.MarketID);
            SendBlackMarketBidOnItemResult(BlackMarketError.DatabaseError, blackMarketBidOnItem.MarketID, blackMarketBidOnItem.Item);

            return;
        }

        SQLTransaction trans = new();

        _blackMarketManager.SendAuctionOutbidMail(entry, trans);
        entry.PlaceBid(blackMarketBidOnItem.BidAmount, _session.Player, trans);

        _characterDatabase.CommitTransaction(trans);

        SendBlackMarketBidOnItemResult(BlackMarketError.Ok, blackMarketBidOnItem.MarketID, blackMarketBidOnItem.Item);
    }

    [WorldPacketHandler(ClientOpcodes.BlackMarketOpen)]
    private void HandleBlackMarketOpen(BlackMarketOpen blackMarketOpen)
    {
        var unit = _session.Player.GetNPCIfCanInteractWith(blackMarketOpen.Guid, NPCFlags.BlackMarket, NPCFlags2.BlackMarketView);

        if (unit == null)
        {
            Log.Logger.Debug("WORLD: HandleBlackMarketHello - {0} not found or you can't interact with him.", blackMarketOpen.Guid.ToString());

            return;
        }

        // remove fake death
        if (_session.Player.HasUnitState(UnitState.Died))
            _session.Player.RemoveAurasByType(AuraType.FeignDeath);

        SendBlackMarketOpenResult(blackMarketOpen.Guid);
    }

    [WorldPacketHandler(ClientOpcodes.BlackMarketRequestItems)]
    private void HandleBlackMarketRequestItems(BlackMarketRequestItems blackMarketRequestItems)
    {
        if (!_blackMarketManager.IsEnabled)
            return;

        var unit = _session.Player.GetNPCIfCanInteractWith(blackMarketRequestItems.Guid, NPCFlags.BlackMarket, NPCFlags2.BlackMarketView);

        if (unit == null)
        {
            Log.Logger.Debug("WORLD: HandleBlackMarketRequestItems - {0} not found or you can't interact with him.", blackMarketRequestItems.Guid.ToString());

            return;
        }

        BlackMarketRequestItemsResult result = new();
        _blackMarketManager.BuildItemsResponse(result, _session.Player);
        _session.SendPacket(result);
    }

    private void SendBlackMarketBidOnItemResult(BlackMarketError result, uint marketId, ItemInstance item)
    {
        _session.SendPacket(new BlackMarketBidOnItemResult()
        {
            MarketID = marketId,
            Item = item,
            Result = result
        });
    }

    private void SendBlackMarketOpenResult(ObjectGuid guid)
    {
        _session.SendPacket(new NPCInteractionOpenResult()
        {
            Npc = guid,
            InteractionType = PlayerInteractionType.BlackMarketAuctioneer,
            Success = _blackMarketManager.IsEnabled
        });
    }
}