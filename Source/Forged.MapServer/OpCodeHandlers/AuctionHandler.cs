// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using Forged.MapServer.AuctionHouse;
using Forged.MapServer.Chat.Commands;
using Forged.MapServer.Chrono;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Items;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Globals;
using Forged.MapServer.Globals.Caching;
using Forged.MapServer.Networking;
using Forged.MapServer.Networking.Packets.AuctionHouse;
using Forged.MapServer.Server;
using Framework.Constants;
using Framework.Database;
using Framework.Util;
using Game.Common.Handlers;
using Microsoft.Extensions.Configuration;
using Serilog;

// ReSharper disable ArrangeTypeMemberModifiers
// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedMember.Local

namespace Forged.MapServer.OpCodeHandlers;

public class AuctionHandler : IWorldSessionHandler
{
    private readonly AuctionManager _auctionManager;
    private readonly CharacterDatabase _characterDatabase;
    private readonly IConfiguration _configuration;
    private readonly GameObjectManager _gameObjectManager;
    private readonly ObjectAccessor _objectAccessor;
    private readonly AuctionBucketKeyFactory _auctionBucketKeyFactory;
    private readonly ItemTemplateCache _itemTemplateCache;
    private readonly WorldSession _session;

    public AuctionHandler(WorldSession session, IConfiguration configuration, GameObjectManager gameObjectManager, AuctionManager auctionManager,
                          CharacterDatabase characterDatabase, ObjectAccessor objectAccessor, AuctionBucketKeyFactory auctionBucketKeyFactory,
                          ItemTemplateCache itemTemplateCache)
    {
        _session = session;
        _configuration = configuration;
        _gameObjectManager = gameObjectManager;
        _auctionManager = auctionManager;
        _characterDatabase = characterDatabase;
        _objectAccessor = objectAccessor;
        _auctionBucketKeyFactory = auctionBucketKeyFactory;
        _itemTemplateCache = itemTemplateCache;
    }

    public void SendAuctionClosedNotification(AuctionPosting auction, float mailDelay, bool sold)
    {
        AuctionClosedNotification packet = new();
        packet.Info.Initialize(auction);
        packet.ProceedsMailDelay = mailDelay;
        packet.Sold = sold;
        _session.SendPacket(packet);
    }

    public void SendAuctionCommandResult(uint auctionId, AuctionCommand command, AuctionResult errorCode, TimeSpan delayForNextAction, InventoryResult bagError = 0)
    {
        _session.SendPacket(new AuctionCommandResult
        {
            AuctionID = auctionId,
            Command = (int)command,
            ErrorCode = (int)errorCode,
            BagResult = (int)bagError,
            DesiredDelay = (uint)delayForNextAction.TotalSeconds
        });
    }

    public void SendAuctionHello(ObjectGuid guid, Creature unit)
    {
        if (_session.Player.Level < _configuration.GetDefaultValue("LevelReq:Auction", 1))
        {
            _session.SendNotification(_gameObjectManager.GetCypherString(CypherStrings.AuctionReq), _configuration.GetValue<int>("LevelReq:Auction"));

            return;
        }

        var ahEntry = _auctionManager.GetAuctionHouseEntry(unit.Faction);

        if (ahEntry == null)
            return;

        _session.SendPacket(new AuctionHelloResponse
        {
            Guid = guid,
            OpenForBusiness = true
        });
    }

    public void SendAuctionOwnerBidNotification(AuctionPosting auction)
    {
        AuctionOwnerBidNotification packet = new();
        packet.Info.Initialize(auction);
        packet.Bidder = auction.Bidder;
        packet.MinIncrement = auction.CalculateMinIncrement();
        _session.SendPacket(packet);
    }

    [WorldPacketHandler(ClientOpcodes.AuctionBrowseQuery)]
    private void HandleAuctionBrowseQuery(AuctionBrowseQuery browseQuery)
    {
        var throttle = _auctionManager.CheckThrottle(_session.Player, browseQuery.TaintedBy.HasValue);

        if (throttle.Throttled)
            return;

        var creature = _session.Player.GetNPCIfCanInteractWith(browseQuery.Auctioneer, NPCFlags.Auctioneer, NPCFlags2.None);

        if (creature == null)
        {
            Log.Logger.Error($"WORLD: HandleAuctionListItems - {browseQuery.Auctioneer} not found or you can't interact with him.");

            return;
        }

        // remove fake death
        if (_session.Player.HasUnitState(UnitState.Died))
            _session.Player.RemoveAurasByType(AuraType.FeignDeath);

        var auctionHouse = _auctionManager.GetAuctionsMap(creature.Faction);

        Log.Logger.Debug($"Auctionhouse search ({browseQuery.Auctioneer}), searchedname: {browseQuery.Name}, levelmin: {browseQuery.MinLevel}, levelmax: {browseQuery.MaxLevel}, filters: {browseQuery.Filters}");

        AuctionSearchClassFilters classFilters = null;

        AuctionListBucketsResult listBucketsResult = new();

        if (!browseQuery.ItemClassFilters.Empty())
        {
            classFilters = new AuctionSearchClassFilters();

            foreach (var classFilter in browseQuery.ItemClassFilters)
                if (!classFilter.SubClassFilters.Empty())
                {
                    foreach (var subClassFilter in classFilter.SubClassFilters)
                        if (classFilter.ItemClass < (int)ItemClass.Max)
                        {
                            classFilters.Classes[classFilter.ItemClass].SubclassMask |= (AuctionSearchClassFilters.FilterType)(1 << subClassFilter.ItemSubclass);

                            if (subClassFilter.ItemSubclass < ItemConst.MaxItemSubclassTotal)
                                classFilters.Classes[classFilter.ItemClass].InvTypes[subClassFilter.ItemSubclass] = subClassFilter.InvTypeMask;
                        }
                }
                else
                {
                    classFilters.Classes[classFilter.ItemClass].SubclassMask = AuctionSearchClassFilters.FilterType.SkipSubclass;
                }
        }

        auctionHouse.BuildListBuckets(listBucketsResult,
                                    _session.Player,
                                    browseQuery.Name,
                                    browseQuery.MinLevel,
                                    browseQuery.MaxLevel,
                                    browseQuery.Filters,
                                    classFilters,
                                    browseQuery.KnownPets,
                                    browseQuery.KnownPets.Length,
                                    (byte)browseQuery.MaxPetLevel,
                                    browseQuery.Offset,
                                    browseQuery.Sorts,
                                    browseQuery.Sorts.Count);

        listBucketsResult.BrowseMode = AuctionHouseBrowseMode.Search;
        listBucketsResult.DesiredDelay = (uint)throttle.DelayUntilNext.TotalSeconds;
        _session.SendPacket(listBucketsResult);
    }

    [WorldPacketHandler(ClientOpcodes.AuctionCancelCommoditiesPurchase)]
    private void HandleAuctionCancelCommoditiesPurchase(AuctionCancelCommoditiesPurchase cancelCommoditiesPurchase)
    {
        var throttle = _auctionManager.CheckThrottle(_session.Player, cancelCommoditiesPurchase.TaintedBy.HasValue, AuctionCommand.PlaceBid);

        if (throttle.Throttled)
            return;

        var creature = _session.Player.GetNPCIfCanInteractWith(cancelCommoditiesPurchase.Auctioneer, NPCFlags.Auctioneer, NPCFlags2.None);

        if (creature == null)
        {
            Log.Logger.Error($"WORLD: HandleAuctionListItems - {cancelCommoditiesPurchase.Auctioneer} not found or you can't interact with him.");

            return;
        }

        // remove fake death
        if (_session.Player.HasUnitState(UnitState.Died))
            _session.Player.RemoveAurasByType(AuraType.FeignDeath);

        var auctionHouse = _auctionManager.GetAuctionsMap(creature.Faction);
        auctionHouse.CancelCommodityQuote(_session.Player.GUID);
    }

    [WorldPacketHandler(ClientOpcodes.AuctionConfirmCommoditiesPurchase)]
    private void HandleAuctionConfirmCommoditiesPurchase(AuctionConfirmCommoditiesPurchase confirmCommoditiesPurchase)
    {
        var throttle = _auctionManager.CheckThrottle(_session.Player, confirmCommoditiesPurchase.TaintedBy.HasValue, AuctionCommand.PlaceBid);

        if (throttle.Throttled)
            return;

        var creature = _session.Player.GetNPCIfCanInteractWith(confirmCommoditiesPurchase.Auctioneer, NPCFlags.Auctioneer, NPCFlags2.None);

        if (creature == null)
        {
            Log.Logger.Error($"WORLD: HandleAuctionListItems - {confirmCommoditiesPurchase.Auctioneer} not found or you can't interact with him.");

            return;
        }

        // remove fake death
        if (_session.Player.HasUnitState(UnitState.Died))
            _session.Player.RemoveAurasByType(AuraType.FeignDeath);

        var auctionHouse = _auctionManager.GetAuctionsMap(creature.Faction);

        SQLTransaction trans = new();

        if (auctionHouse.BuyCommodity(trans, _session.Player, (uint)confirmCommoditiesPurchase.ItemID, confirmCommoditiesPurchase.Quantity, throttle.DelayUntilNext))
        {
            var buyerGuid = _session.Player.GUID;

            _session.AddTransactionCallback(_characterDatabase.AsyncCommitTransaction(trans))
                .AfterComplete(success =>
                {
                    if (_session.Player == null || _session.Player.GUID != buyerGuid)
                        return;

                    if (success)
                    {
                        _session.Player.UpdateCriteria(CriteriaType.AuctionsWon, 1);
                        SendAuctionCommandResult(0, AuctionCommand.PlaceBid, AuctionResult.Ok, throttle.DelayUntilNext);
                    }
                    else
                    {
                        SendAuctionCommandResult(0, AuctionCommand.PlaceBid, AuctionResult.CommodityPurchaseFailed, throttle.DelayUntilNext);
                    }
                });
        }
    }

    [WorldPacketHandler(ClientOpcodes.AuctionGetCommodityQuote)]
    private void HandleAuctionGetCommodityQuote(AuctionGetCommodityQuote getCommodityQuote)
    {
        var throttle = _auctionManager.CheckThrottle(_session.Player, getCommodityQuote.TaintedBy.HasValue, AuctionCommand.PlaceBid);

        if (throttle.Throttled)
            return;

        var creature = _session.Player.GetNPCIfCanInteractWith(getCommodityQuote.Auctioneer, NPCFlags.Auctioneer, NPCFlags2.None);

        if (creature == null)
        {
            Log.Logger.Error($"WORLD: HandleAuctionStartCommoditiesPurchase - {getCommodityQuote.Auctioneer} not found or you can't interact with him.");

            return;
        }

        // remove fake death
        if (_session.Player.HasUnitState(UnitState.Died))
            _session.Player.RemoveAurasByType(AuraType.FeignDeath);

        var auctionHouse = _auctionManager.GetAuctionsMap(creature.Faction);

        AuctionGetCommodityQuoteResult commodityQuoteResult = new();

        var quote = auctionHouse.CreateCommodityQuote(_session.Player, (uint)getCommodityQuote.ItemID, getCommodityQuote.Quantity);

        if (quote != null)
        {
            commodityQuoteResult.TotalPrice = quote.TotalPrice;
            commodityQuoteResult.Quantity = quote.Quantity;
            commodityQuoteResult.QuoteDuration = (int)(quote.ValidTo - GameTime.Now).TotalMilliseconds;
        }

        commodityQuoteResult.ItemID = getCommodityQuote.ItemID;
        commodityQuoteResult.DesiredDelay = (uint)throttle.DelayUntilNext.TotalSeconds;

        _session.SendPacket(commodityQuoteResult);
    }

    [WorldPacketHandler(ClientOpcodes.AuctionHelloRequest)]
    private void HandleAuctionHello(AuctionHelloRequest hello)
    {
        var unit = _session.Player.GetNPCIfCanInteractWith(hello.Guid, NPCFlags.Auctioneer, NPCFlags2.None);

        if (unit == null)
        {
            Log.Logger.Debug($"WORLD: HandleAuctionHelloOpcode - {hello.Guid} not found or you can't interact with him.");

            return;
        }

        // remove fake death
        if (_session.Player.HasUnitState(UnitState.Died))
            _session.Player.RemoveAurasByType(AuraType.FeignDeath);

        SendAuctionHello(hello.Guid, unit);
    }

    [WorldPacketHandler(ClientOpcodes.AuctionListBiddedItems)]
    private void HandleAuctionListBiddedItems(AuctionListBiddedItems listBiddedItems)
    {
        var throttle = _auctionManager.CheckThrottle(_session.Player, listBiddedItems.TaintedBy.HasValue);

        if (throttle.Throttled)
            return;

        var creature = _session.Player.GetNPCIfCanInteractWith(listBiddedItems.Auctioneer, NPCFlags.Auctioneer, NPCFlags2.None);

        if (creature == null)
        {
            Log.Logger.Debug($"WORLD: HandleAuctionListBidderItems - {listBiddedItems.Auctioneer} not found or you can't interact with him.");

            return;
        }

        // remove fake death
        if (_session.Player.HasUnitState(UnitState.Died))
            _session.Player.RemoveAurasByType(AuraType.FeignDeath);

        var auctionHouse = _auctionManager.GetAuctionsMap(creature.Faction);

        AuctionListBiddedItemsResult result = new();

        auctionHouse.BuildListBiddedItems(result, _session.Player, listBiddedItems.Offset, listBiddedItems.Sorts, listBiddedItems.Sorts.Count);
        result.DesiredDelay = (uint)throttle.DelayUntilNext.TotalSeconds;
        _session.SendPacket(result);
    }

    [WorldPacketHandler(ClientOpcodes.AuctionListBucketsByBucketKeys)]
    private void HandleAuctionListBucketsByBucketKeys(AuctionListBucketsByBucketKeys listBucketsByBucketKeys)
    {
        var throttle = _auctionManager.CheckThrottle(_session.Player, listBucketsByBucketKeys.TaintedBy.HasValue);

        if (throttle.Throttled)
            return;

        var creature = _session.Player.GetNPCIfCanInteractWith(listBucketsByBucketKeys.Auctioneer, NPCFlags.Auctioneer, NPCFlags2.None);

        if (creature == null)
        {
            Log.Logger.Debug($"WORLD: HandleAuctionListItems - {listBucketsByBucketKeys.Auctioneer} not found or you can't interact with him.");

            return;
        }

        // remove fake death
        if (_session.Player.HasUnitState(UnitState.Died))
            _session.Player.RemoveAurasByType(AuraType.FeignDeath);

        var auctionHouse = _auctionManager.GetAuctionsMap(creature.Faction);

        AuctionListBucketsResult listBucketsResult = new();

        auctionHouse.BuildListBuckets(listBucketsResult,
                                    _session.Player,
                                    listBucketsByBucketKeys.BucketKeys,
                                    listBucketsByBucketKeys.BucketKeys.Count,
                                    listBucketsByBucketKeys.Sorts,
                                    listBucketsByBucketKeys.Sorts.Count);

        listBucketsResult.BrowseMode = AuctionHouseBrowseMode.SpecificKeys;
        listBucketsResult.DesiredDelay = (uint)throttle.DelayUntilNext.TotalSeconds;
        _session.SendPacket(listBucketsResult);
    }

    [WorldPacketHandler(ClientOpcodes.AuctionListItemsByBucketKey)]
    private void HandleAuctionListItemsByBucketKey(AuctionListItemsByBucketKey listItemsByBucketKey)
    {
        var throttle = _auctionManager.CheckThrottle(_session.Player, listItemsByBucketKey.TaintedBy.HasValue);

        if (throttle.Throttled)
            return;

        var creature = _session.Player.GetNPCIfCanInteractWith(listItemsByBucketKey.Auctioneer, NPCFlags.Auctioneer, NPCFlags2.None);

        if (creature == null)
        {
            Log.Logger.Debug($"WORLD: HandleAuctionListItemsByBucketKey - {listItemsByBucketKey.Auctioneer} not found or you can't interact with him.");

            return;
        }

        // remove fake death
        if (_session.Player.HasUnitState(UnitState.Died))
            _session.Player.RemoveAurasByType(AuraType.FeignDeath);

        var auctionHouse = _auctionManager.GetAuctionsMap(creature.Faction);

        AuctionListItemsResult listItemsResult = new()
        {
            DesiredDelay = (uint)throttle.DelayUntilNext.TotalSeconds,
            BucketKey = listItemsByBucketKey.BucketKey
        };

        var itemTemplate = _itemTemplateCache.GetItemTemplate(listItemsByBucketKey.BucketKey.ItemID);
        listItemsResult.ListType = itemTemplate is { MaxStackSize: > 1 } ? AuctionHouseListType.Commodities : AuctionHouseListType.Items;

        auctionHouse.BuildListAuctionItems(listItemsResult,
                                            _session.Player,
                                            new AuctionsBucketKey(listItemsByBucketKey.BucketKey),
                                            listItemsByBucketKey.Offset,
                                            listItemsByBucketKey.Sorts,
                                            listItemsByBucketKey.Sorts.Count);

        _session.SendPacket(listItemsResult);
    }

    [WorldPacketHandler(ClientOpcodes.AuctionListItemsByItemId)]
    private void HandleAuctionListItemsByItemID(AuctionListItemsByItemID listItemsByItemID)
    {
        var throttle = _auctionManager.CheckThrottle(_session.Player, listItemsByItemID.TaintedBy.HasValue);

        if (throttle.Throttled)
            return;

        var creature = _session.Player.GetNPCIfCanInteractWith(listItemsByItemID.Auctioneer, NPCFlags.Auctioneer, NPCFlags2.None);

        if (creature == null)
        {
            Log.Logger.Debug($"WORLD: HandleAuctionListItemsByItemID - {listItemsByItemID.Auctioneer} not found or you can't interact with him.");

            return;
        }

        // remove fake death
        if (_session.Player.HasUnitState(UnitState.Died))
            _session.Player.RemoveAurasByType(AuraType.FeignDeath);

        var auctionHouse = _auctionManager.GetAuctionsMap(creature.Faction);

        AuctionListItemsResult listItemsResult = new()
        {
            DesiredDelay = (uint)throttle.DelayUntilNext.TotalSeconds,
            BucketKey =
            {
                ItemID = listItemsByItemID.ItemID
            }
        };

        var itemTemplate = _itemTemplateCache.GetItemTemplate(listItemsByItemID.ItemID);
        listItemsResult.ListType = itemTemplate is { MaxStackSize: > 1 } ? AuctionHouseListType.Commodities : AuctionHouseListType.Items;

        auctionHouse.BuildListAuctionItems(listItemsResult,
                                            _session.Player,
                                            listItemsByItemID.ItemID,
                                            listItemsByItemID.Offset,
                                            listItemsByItemID.Sorts,
                                            listItemsByItemID.Sorts.Count);

        _session.SendPacket(listItemsResult);
    }

    [WorldPacketHandler(ClientOpcodes.AuctionListOwnedItems)]
    private void HandleAuctionListOwnedItems(AuctionListOwnedItems listOwnedItems)
    {
        var throttle = _auctionManager.CheckThrottle(_session.Player, listOwnedItems.TaintedBy.HasValue);

        if (throttle.Throttled)
            return;

        var creature = _session.Player.GetNPCIfCanInteractWith(listOwnedItems.Auctioneer, NPCFlags.Auctioneer, NPCFlags2.None);

        if (creature == null)
        {
            Log.Logger.Debug($"WORLD: HandleAuctionListOwnerItems - {listOwnedItems.Auctioneer} not found or you can't interact with him.");

            return;
        }

        // remove fake death
        if (_session.Player.HasUnitState(UnitState.Died))
            _session.Player.RemoveAurasByType(AuraType.FeignDeath);

        var auctionHouse = _auctionManager.GetAuctionsMap(creature.Faction);

        AuctionListOwnedItemsResult result = new();

        auctionHouse.BuildListOwnedItems(result, _session.Player, listOwnedItems.Offset, listOwnedItems.Sorts, listOwnedItems.Sorts.Count);
        result.DesiredDelay = (uint)throttle.DelayUntilNext.TotalSeconds;
        _session.SendPacket(result);
    }

    [WorldPacketHandler(ClientOpcodes.AuctionPlaceBid)]
    private void HandleAuctionPlaceBid(AuctionPlaceBid placeBid)
    {
        var throttle = _auctionManager.CheckThrottle(_session.Player, placeBid.TaintedBy.HasValue, AuctionCommand.PlaceBid);

        if (throttle.Throttled)
            return;

        var creature = _session.Player.GetNPCIfCanInteractWith(placeBid.Auctioneer, NPCFlags.Auctioneer, NPCFlags2.None);

        if (creature == null)
        {
            Log.Logger.Debug($"WORLD: HandleAuctionPlaceBid - {placeBid.Auctioneer} not found or you can't interact with him.");

            return;
        }

        // auction house does not deal with copper
        if ((placeBid.BidAmount % MoneyConstants.Silver) != 0)
        {
            SendAuctionCommandResult(placeBid.AuctionID, AuctionCommand.PlaceBid, AuctionResult.BidIncrement, throttle.DelayUntilNext);

            return;
        }

        // remove fake death
        if (_session.Player.HasUnitState(UnitState.Died))
            _session.Player.RemoveAurasByType(AuraType.FeignDeath);

        var auctionHouse = _auctionManager.GetAuctionsMap(creature.Faction);

        var auction = auctionHouse.GetAuction(placeBid.AuctionID);

        if (auction == null || auction.IsCommodity)
        {
            SendAuctionCommandResult(placeBid.AuctionID, AuctionCommand.PlaceBid, AuctionResult.ItemNotFound, throttle.DelayUntilNext);

            return;
        }

        // check auction owner - cannot buy own auctions
        if (auction.Owner == _session.Player.GUID || auction.OwnerAccount == _session.AccountGUID)
        {
            SendAuctionCommandResult(placeBid.AuctionID, AuctionCommand.PlaceBid, AuctionResult.BidOwn, throttle.DelayUntilNext);

            return;
        }

        var canBid = auction.MinBid != 0;
        var canBuyout = auction.BuyoutOrUnitPrice != 0;

        // buyout attempt with wrong amount
        if (!canBid && placeBid.BidAmount != auction.BuyoutOrUnitPrice)
        {
            SendAuctionCommandResult(placeBid.AuctionID, AuctionCommand.PlaceBid, AuctionResult.BidIncrement, throttle.DelayUntilNext);

            return;
        }

        var minBid = auction.BidAmount != 0 ? auction.BidAmount + auction.CalculateMinIncrement() : auction.MinBid;

        if (canBid && placeBid.BidAmount < minBid)
        {
            SendAuctionCommandResult(placeBid.AuctionID, AuctionCommand.PlaceBid, AuctionResult.HigherBid, throttle.DelayUntilNext);

            return;
        }

        SQLTransaction trans = new();
        var priceToPay = placeBid.BidAmount;

        if (!auction.Bidder.IsEmpty)
        {
            // return money to previous bidder
            if (auction.Bidder != _session.Player.GUID)
                auctionHouse.SendAuctionOutbid(auction, _session.Player.GUID, placeBid.BidAmount, trans);
            else
                priceToPay = placeBid.BidAmount - auction.BidAmount;
        }

        // check money
        if (!_session.Player.HasEnoughMoney(priceToPay))
        {
            SendAuctionCommandResult(placeBid.AuctionID, AuctionCommand.PlaceBid, AuctionResult.NotEnoughMoney, throttle.DelayUntilNext);

            return;
        }

        _session.Player.ModifyMoney(-(long)priceToPay);
        auction.Bidder = _session.Player.GUID;
        auction.BidAmount = placeBid.BidAmount;

        if (_session.HasPermission(RBACPermissions.LogGmTrade))
            auction.ServerFlags |= AuctionPostingServerFlag.GmLogBuyer;
        else
            auction.ServerFlags &= ~AuctionPostingServerFlag.GmLogBuyer;

        if (canBuyout && placeBid.BidAmount == auction.BuyoutOrUnitPrice)
        {
            // buyout
            auctionHouse.SendAuctionWon(auction, _session.Player, trans);
            auctionHouse.SendAuctionSold(auction, null, trans);

            auctionHouse.RemoveAuction(trans, auction);
        }
        else
        {
            // place bid
            var stmt = _characterDatabase.GetPreparedStatement(CharStatements.UPD_AUCTION_BID);
            stmt.AddValue(0, auction.Bidder.Counter);
            stmt.AddValue(1, auction.BidAmount);
            stmt.AddValue(2, (byte)auction.ServerFlags);
            stmt.AddValue(3, auction.Id);
            trans.Append(stmt);

            auction.BidderHistory.Add(_session.Player.GUID);

            if (auction.BidderHistory.Contains(_session.Player.GUID))
            {
                stmt = _characterDatabase.GetPreparedStatement(CharStatements.INS_AUCTION_BIDDER);
                stmt.AddValue(0, auction.Id);
                stmt.AddValue(1, _session.Player.GUID.Counter);
                trans.Append(stmt);
            }

            // Not sure if we must send this now.
            var owner = _objectAccessor.FindConnectedPlayer(auction.Owner);

            if (owner != null && owner.Session.PacketRouter.TryGetOpCodeHandler(out AuctionHandler auctionHandler))
                auctionHandler.SendAuctionOwnerBidNotification(auction);
        }

        _session.Player.SaveInventoryAndGoldToDB(trans);

        _session.AddTransactionCallback(_characterDatabase.AsyncCommitTransaction(trans))
            .AfterComplete(success =>
            {
                if (_session.Player == null)
                    return;

                if (success)
                {
                    _session.Player.UpdateCriteria(CriteriaType.HighestAuctionBid, placeBid.BidAmount);
                    SendAuctionCommandResult(placeBid.AuctionID, AuctionCommand.PlaceBid, AuctionResult.Ok, throttle.DelayUntilNext);
                }
                else
                {
                    SendAuctionCommandResult(placeBid.AuctionID, AuctionCommand.PlaceBid, AuctionResult.DatabaseError, throttle.DelayUntilNext);
                }
            });
    }

    [WorldPacketHandler(ClientOpcodes.AuctionRemoveItem)]
    private void HandleAuctionRemoveItem(AuctionRemoveItem removeItem)
    {
        var throttle = _auctionManager.CheckThrottle(_session.Player, removeItem.TaintedBy.HasValue, AuctionCommand.Cancel);

        if (throttle.Throttled)
            return;

        var creature = _session.Player.GetNPCIfCanInteractWith(removeItem.Auctioneer, NPCFlags.Auctioneer, NPCFlags2.None);

        if (creature == null)
        {
            Log.Logger.Error($"WORLD: HandleAuctionRemoveItem - {removeItem.Auctioneer} not found or you can't interact with him.");

            return;
        }

        // remove fake death
        if (_session.Player.HasUnitState(UnitState.Died))
            _session.Player.RemoveAurasByType(AuraType.FeignDeath);

        var auctionHouse = _auctionManager.GetAuctionsMap(creature.Faction);

        var auction = auctionHouse.GetAuction(removeItem.AuctionID);

        SQLTransaction trans = new();

        if (auction != null && auction.Owner == _session.Player.GUID)
        {
            if (auction.Bidder.IsEmpty) // If we have a bidder, we have to send him the money he paid
            {
                var cancelCost = MathFunctions.CalculatePct(auction.BidAmount, 5u);

                if (!_session.Player.HasEnoughMoney(cancelCost)) //player doesn't have enough money
                {
                    SendAuctionCommandResult(0, AuctionCommand.Cancel, AuctionResult.NotEnoughMoney, throttle.DelayUntilNext);

                    return;
                }

                auctionHouse.SendAuctionCancelledToBidder(auction, trans);
                _session.Player.ModifyMoney(-(long)cancelCost);
            }

            auctionHouse.SendAuctionRemoved(auction, _session.Player, trans);
        }
        else
        {
            SendAuctionCommandResult(0, AuctionCommand.Cancel, AuctionResult.DatabaseError, throttle.DelayUntilNext);
            //this code isn't possible ... maybe there should be assert
            Log.Logger.Error($"CHEATER: {_session.Player.GUID} tried to cancel auction (id: {removeItem.AuctionID}) of another player or auction is null");

            return;
        }

        // client bug - instead of removing auction in the UI, it only substracts 1 from visible count
        var auctionIdForClient = auction.IsCommodity ? 0 : auction.Id;

        // Now remove the auction
        _session.Player.SaveInventoryAndGoldToDB(trans);
        auctionHouse.RemoveAuction(trans, auction);

        _session.AddTransactionCallback(_characterDatabase.AsyncCommitTransaction(trans))
            .AfterComplete(success =>
            {
                if (_session.Player == null)
                    return;

                if (success)
                    SendAuctionCommandResult(auctionIdForClient, AuctionCommand.Cancel, AuctionResult.Ok, throttle.DelayUntilNext); //inform player, that auction is removed
                else
                    SendAuctionCommandResult(0, AuctionCommand.Cancel, AuctionResult.DatabaseError, throttle.DelayUntilNext);
            });
    }

    [WorldPacketHandler(ClientOpcodes.AuctionRequestFavoriteList)]
    private void HandleAuctionRequestFavoriteList(AuctionRequestFavoriteList requestFavoriteList)
    {
        if (requestFavoriteList == null) return;

        var stmt = _characterDatabase.GetPreparedStatement(CharStatements.SEL_CHARACTER_FAVORITE_AUCTIONS);
        stmt.AddValue(0, _session.Player.GUID.Counter);

        _session.QueryProcessor.AddCallback(_characterDatabase.AsyncQuery(stmt))
                    .WithCallback(favoriteAuctionResult =>
                    {
                        AuctionFavoriteList favoriteItems = new();

                        if (!favoriteAuctionResult.IsEmpty())
                            do
                            {
                                favoriteItems.Items.Add(new AuctionFavoriteInfo()
                                {
                                    Order = favoriteAuctionResult.Read<uint>(0),
                                    ItemID = favoriteAuctionResult.Read<uint>(1),
                                    ItemLevel = favoriteAuctionResult.Read<uint>(2),
                                    BattlePetSpeciesID = favoriteAuctionResult.Read<uint>(3),
                                    SuffixItemNameDescriptionID = favoriteAuctionResult.Read<uint>(4)
                                });
                            } while (favoriteAuctionResult.NextRow());

                        _session.SendPacket(favoriteItems);
                    });
    }

    [WorldPacketHandler(ClientOpcodes.AuctionSellCommodity)]
    private void HandleAuctionSellCommodity(AuctionSellCommodity sellCommodity)
    {
        var throttle = _auctionManager.CheckThrottle(_session.Player, sellCommodity.TaintedBy.HasValue);

        if (throttle.Throttled)
            return;

        if (sellCommodity.UnitPrice is 0 or > PlayerConst.MaxMoneyAmount)
        {
            Log.Logger.Error($"WORLD: HandleAuctionSellItem - Player {_session.Player.GetName()} ({_session.Player.GUID}) attempted to sell item with invalid price.");
            SendAuctionCommandResult(0, AuctionCommand.SellItem, AuctionResult.DatabaseError, throttle.DelayUntilNext);

            return;
        }

        // auction house does not deal with copper
        if ((sellCommodity.UnitPrice % MoneyConstants.Silver) != 0)
        {
            SendAuctionCommandResult(0, AuctionCommand.SellItem, AuctionResult.DatabaseError, throttle.DelayUntilNext);

            return;
        }

        var creature = _session.Player.GetNPCIfCanInteractWith(sellCommodity.Auctioneer, NPCFlags.Auctioneer, NPCFlags2.None);

        if (creature == null)
        {
            Log.Logger.Error($"WORLD: HandleAuctionListItems - {sellCommodity.Auctioneer} not found or you can't interact with him.");

            return;
        }

        uint houseId = 0;
        var auctionHouseEntry = _auctionManager.GetAuctionHouseEntry(creature.Faction, ref houseId);

        if (auctionHouseEntry == null)
        {
            Log.Logger.Error($"WORLD: HandleAuctionSellItem - Unit ({sellCommodity.Auctioneer}) has wrong faction.");

            return;
        }

        switch (sellCommodity.RunTime)
        {
            case 1 * SharedConst.MinAuctionTime / Time.MINUTE:
            case 2 * SharedConst.MinAuctionTime / Time.MINUTE:
            case 4 * SharedConst.MinAuctionTime / Time.MINUTE:
                break;

            default:
                SendAuctionCommandResult(0, AuctionCommand.SellItem, AuctionResult.AuctionHouseBusy, throttle.DelayUntilNext);

                return;
        }

        if (_session.Player.HasUnitState(UnitState.Died))
            _session.Player.RemoveAurasByType(AuraType.FeignDeath);

        // find all items for sale
        ulong totalCount = 0;
        Dictionary<ObjectGuid, (Item Item, ulong UseCount)> items2 = new();

        foreach (var itemForSale in sellCommodity.Items)
        {
            var item = _session.Player.GetItemByGuid(itemForSale.Guid);

            if (item == null)
            {
                SendAuctionCommandResult(0, AuctionCommand.SellItem, AuctionResult.ItemNotFound, throttle.DelayUntilNext);

                return;
            }

            if (item.Template.MaxStackSize == 1)
            {
                // not commodity, must use different packet
                SendAuctionCommandResult(0, AuctionCommand.SellItem, AuctionResult.ItemNotFound, throttle.DelayUntilNext);

                return;
            }

            // verify that all items belong to the same bucket
            if (!items2.Empty() && _auctionBucketKeyFactory.ForItem(item) != _auctionBucketKeyFactory.ForItem(items2.FirstOrDefault().Value.Item1))
            {
                SendAuctionCommandResult(0, AuctionCommand.SellItem, AuctionResult.ItemNotFound, throttle.DelayUntilNext);

                return;
            }

            if (_auctionManager.GetAItem(item.GUID) != null ||
                !item.CanBeTraded() ||
                item.IsNotEmptyBag ||
                item.Template.HasFlag(ItemFlags.Conjured) ||
                item.ItemData.Expiration != 0 ||
                item.Count < itemForSale.UseCount)
            {
                SendAuctionCommandResult(0, AuctionCommand.SellItem, AuctionResult.DatabaseError, throttle.DelayUntilNext);

                return;
            }

            var soldItem = items2.LookupByKey(item.GUID);
            soldItem.Item = item;
            soldItem.UseCount += itemForSale.UseCount;
            items2[item.GUID] = soldItem;

            if (item.Count < soldItem.UseCount)
            {
                // check that we have enough of this item to sell
                SendAuctionCommandResult(0, AuctionCommand.SellItem, AuctionResult.ItemNotFound, throttle.DelayUntilNext);

                return;
            }

            totalCount += itemForSale.UseCount;
        }

        if (totalCount == 0)
        {
            SendAuctionCommandResult(0, AuctionCommand.SellItem, AuctionResult.DatabaseError, throttle.DelayUntilNext);

            return;
        }

        var auctionTime = TimeSpan.FromSeconds((long)TimeSpan.FromMinutes(sellCommodity.RunTime).TotalSeconds * _configuration.GetDefaultValue("Rate:Auction:Time", 1f));
        var auctionHouse = _auctionManager.GetAuctionsMap(creature.Faction);

        var deposit = _auctionManager.GetCommodityAuctionDeposit(items2.FirstOrDefault().Value.Item.Template, TimeSpan.FromMinutes(sellCommodity.RunTime), (uint)totalCount);

        if (!_session.Player.HasEnoughMoney(deposit))
        {
            SendAuctionCommandResult(0, AuctionCommand.SellItem, AuctionResult.NotEnoughMoney, throttle.DelayUntilNext);

            return;
        }

        var auctionId = _gameObjectManager.IDGeneratorCache.GenerateAuctionID();
        AuctionPosting auction = new(_auctionBucketKeyFactory)
        {
            Id = auctionId,
            Owner = _session.Player.GUID,
            OwnerAccount = _session.AccountGUID,
            BuyoutOrUnitPrice = sellCommodity.UnitPrice,
            Deposit = deposit,
            StartTime = GameTime.SystemTime
        };

        auction.EndTime = auction.StartTime + auctionTime;

        // keep track of what was cloned to undo/modify counts later
        Dictionary<Item, Item> clones = new();

        foreach (var pair in items2)
        {
            if (pair.Value.Item1.Count == pair.Value.Item2)
                continue;

            var itemForSale = pair.Value.Item1.CloneItem((uint)pair.Value.Item2, _session.Player);

            if (itemForSale == null)
            {
                Log.Logger.Error($"CMSG_AUCTION_SELL_COMMODITY: Could not create clone of item {pair.Value.Item1.Entry}");
                SendAuctionCommandResult(0, AuctionCommand.SellItem, AuctionResult.DatabaseError, throttle.DelayUntilNext);

                return;
            }

            clones.Add(pair.Value.Item1, itemForSale);
        }

        if (!_auctionManager.PendingAuctionAdd(_session.Player, auctionHouse.GetAuctionHouseId(), auction.Id, auction.Deposit))
        {
            SendAuctionCommandResult(0, AuctionCommand.SellItem, AuctionResult.NotEnoughMoney, throttle.DelayUntilNext);

            return;
        }

        /*TC_LOG_INFO("network", "CMSG_AUCTION_SELL_COMMODITY: %s %s is selling item %s %s to auctioneer %s with count " UI64FMTD " with with unit price " UI64FMTD " and with time %u (in sec) in auctionhouse %u",
			_session.Player.GetGUID().ToString(), _session.Player.GetName(), items2.begin().second.first.GetNameForLocaleIdx(sWorld.GetDefaultDbcLocale()),
			([&items2]()
	{
			std.stringstream ss;
			auto itr = items2.begin();
			ss << (itr++).first.ToString();
			for (; itr != items2.end(); ++itr)
				ss << ',' << itr.first.ToString();
			return ss.str();
		} ()),
	creature.GetGUID().ToString(), totalCount, sellCommodity.UnitPrice, uint32(auctionTime.count()), auctionHouse.GetAuctionHouseId());*/

        if (_session.HasPermission(RBACPermissions.LogGmTrade))
        {
            var logItem = items2.First().Value.Item1;
            Log.Logger.ForContext<GMCommands>().Information($"GM {_session.PlayerName} (Account: {_session.AccountId}) create auction: {logItem.GetName()} (Entry: {logItem.Entry} Count: {totalCount})");
        }

        SQLTransaction trans = new();

        foreach (var pair in items2)
        {
            var itemForSale = pair.Value.Item1;
            var cloneItr = clones.LookupByKey(pair.Value.Item1);

            if (cloneItr != null)
            {
                var original = itemForSale;
                original.SetCount(original.Count - (uint)pair.Value.Item2);
                original.SetState(ItemUpdateState.Changed, _session.Player);
                _session.Player.ItemRemovedQuestCheck(original.Entry, (uint)pair.Value.Item2);
                original.SaveToDB(trans);

                itemForSale = cloneItr;
            }
            else
            {
                _session.Player.MoveItemFromInventory(itemForSale.BagSlot, itemForSale.Slot, true);
                itemForSale.DeleteFromInventoryDB(trans);
            }

            itemForSale.SaveToDB(trans);
            auction.Items.Add(itemForSale);
        }

        auctionHouse.AddAuction(trans, auction);
        _session.Player.SaveInventoryAndGoldToDB(trans);

        var auctionPlayerGuid = _session.Player.GUID;

        _session.AddTransactionCallback(_characterDatabase.AsyncCommitTransaction(trans))
            .AfterComplete(success =>
            {
                if (_session.Player == null || _session.Player.GUID != auctionPlayerGuid)
                    return;

                if (success)
                {
                    _session.Player.UpdateCriteria(CriteriaType.ItemsPostedAtAuction, 1);
                    SendAuctionCommandResult(auctionId, AuctionCommand.SellItem, AuctionResult.Ok, throttle.DelayUntilNext);
                }
                else
                {
                    SendAuctionCommandResult(0, AuctionCommand.SellItem, AuctionResult.DatabaseError, throttle.DelayUntilNext);
                }
            });
    }

    [WorldPacketHandler(ClientOpcodes.AuctionSellItem)]
    private void HandleAuctionSellItem(AuctionSellItem sellItem)
    {
        var throttle = _auctionManager.CheckThrottle(_session.Player, sellItem.TaintedBy.HasValue);

        if (throttle.Throttled)
            return;

        if (sellItem.Items.Count != 1 || sellItem.Items[0].UseCount != 1)
        {
            SendAuctionCommandResult(0, AuctionCommand.SellItem, AuctionResult.ItemNotFound, throttle.DelayUntilNext);

            return;
        }

        if (sellItem.MinBid == 0 && sellItem.BuyoutPrice == 0)
        {
            SendAuctionCommandResult(0, AuctionCommand.SellItem, AuctionResult.NotEnoughMoney, throttle.DelayUntilNext);

            return;
        }

        if (sellItem.MinBid > PlayerConst.MaxMoneyAmount || sellItem.BuyoutPrice > PlayerConst.MaxMoneyAmount)
        {
            Log.Logger.Error($"WORLD: HandleAuctionSellItem - Player {_session.Player.GetName()} ({_session.Player.GUID}) attempted to sell item with higher price than max gold amount.");
            SendAuctionCommandResult(0, AuctionCommand.SellItem, AuctionResult.Inventory, throttle.DelayUntilNext, InventoryResult.TooMuchGold);

            return;
        }

        // auction house does not deal with copper
        if ((sellItem.MinBid % MoneyConstants.Silver) != 0 || (sellItem.BuyoutPrice % MoneyConstants.Silver) != 0)
        {
            SendAuctionCommandResult(0, AuctionCommand.SellItem, AuctionResult.DatabaseError, throttle.DelayUntilNext);

            return;
        }

        var creature = _session.Player.GetNPCIfCanInteractWith(sellItem.Auctioneer, NPCFlags.Auctioneer, NPCFlags2.None);

        if (creature == null)
        {
            Log.Logger.Error($"WORLD: HandleAuctionSellItem - Unit ({sellItem.Auctioneer.ToString()}) not found or you can't interact with him.");

            return;
        }

        uint houseId = 0;
        var auctionHouseEntry = _auctionManager.GetAuctionHouseEntry(creature.Faction, ref houseId);

        if (auctionHouseEntry == null)
        {
            Log.Logger.Error($"WORLD: HandleAuctionSellItem - Unit ({sellItem.Auctioneer.ToString()}) has wrong faction.");

            return;
        }

        switch (sellItem.RunTime)
        {
            case 1 * SharedConst.MinAuctionTime / Time.MINUTE:
            case 2 * SharedConst.MinAuctionTime / Time.MINUTE:
            case 4 * SharedConst.MinAuctionTime / Time.MINUTE:
                break;

            default:
                SendAuctionCommandResult(0, AuctionCommand.SellItem, AuctionResult.AuctionHouseBusy, throttle.DelayUntilNext);

                return;
        }

        if (_session.Player.HasUnitState(UnitState.Died))
            _session.Player.RemoveAurasByType(AuraType.FeignDeath);

        var item = _session.Player.GetItemByGuid(sellItem.Items[0].Guid);

        if (item == null)
        {
            SendAuctionCommandResult(0, AuctionCommand.SellItem, AuctionResult.ItemNotFound, throttle.DelayUntilNext);

            return;
        }

        if (item.Template.MaxStackSize > 1)
        {
            // commodity, must use different packet
            SendAuctionCommandResult(0, AuctionCommand.SellItem, AuctionResult.ItemNotFound, throttle.DelayUntilNext);

            return;
        }

        if (_auctionManager.GetAItem(item.GUID) != null ||
            !item.CanBeTraded() ||
            item.IsNotEmptyBag ||
            item.Template.HasFlag(ItemFlags.Conjured) ||
            item.ItemData.Expiration != 0 ||
            item.Count != 1)
        {
            SendAuctionCommandResult(0, AuctionCommand.SellItem, AuctionResult.DatabaseError, throttle.DelayUntilNext);

            return;
        }

        var auctionTime = TimeSpan.FromSeconds((long)(TimeSpan.FromMinutes(sellItem.RunTime).TotalSeconds * _configuration.GetDefaultValue("Rate:Auction:Time", 1f)));
        var auctionHouse = _auctionManager.GetAuctionsMap(creature.Faction);

        var deposit = _auctionManager.GetItemAuctionDeposit(_session.Player, item, TimeSpan.FromMinutes(sellItem.RunTime));

        if (!_session.Player.HasEnoughMoney(deposit))
        {
            SendAuctionCommandResult(0, AuctionCommand.SellItem, AuctionResult.NotEnoughMoney, throttle.DelayUntilNext);

            return;
        }

        var auctionId = _gameObjectManager.IDGeneratorCache.GenerateAuctionID();

        AuctionPosting auction = new(_auctionBucketKeyFactory)
        {
            Id = auctionId,
            Owner = _session.Player.GUID,
            OwnerAccount = _session.AccountGUID,
            MinBid = sellItem.MinBid,
            BuyoutOrUnitPrice = sellItem.BuyoutPrice,
            Deposit = deposit,
            BidAmount = sellItem.MinBid,
            StartTime = GameTime.SystemTime
        };

        auction.EndTime = auction.StartTime + auctionTime;

        if (_session.HasPermission(RBACPermissions.LogGmTrade))
            Log.Logger.ForContext<GMCommands>().Information($"GM {_session.PlayerName} (Account: {_session.AccountId}) create auction: {item.Template.GetName()} (Entry: {item.Entry} Count: {item.Count})");

        auction.Items.Add(item);

        Log.Logger.Information($"CMSG_AuctionAction.SellItem: {_session.Player.GUID} {_session.Player.GetName()} is selling item {item.GUID} {item.Template.GetName()} " +
                    $"to auctioneer {creature.GUID} with count {item.Count} with initial bid {sellItem.MinBid} with buyout {sellItem.BuyoutPrice} and with time {auctionTime.TotalSeconds} " +
                    $"(in sec) in auctionhouse {auctionHouse.GetAuctionHouseId()}");

        // Add to pending auctions, or fail with insufficient funds error
        if (!_auctionManager.PendingAuctionAdd(_session.Player, auctionHouse.GetAuctionHouseId(), auctionId, auction.Deposit))
        {
            SendAuctionCommandResult(0, AuctionCommand.SellItem, AuctionResult.NotEnoughMoney, throttle.DelayUntilNext);

            return;
        }

        _session.Player.MoveItemFromInventory(item.BagSlot, item.Slot, true);

        SQLTransaction trans = new();
        item.DeleteFromInventoryDB(trans);
        item.SaveToDB(trans);

        auctionHouse.AddAuction(trans, auction);
        _session.Player.SaveInventoryAndGoldToDB(trans);

        var auctionPlayerGuid = _session.Player.GUID;

        _session.AddTransactionCallback(_characterDatabase.AsyncCommitTransaction(trans))
                 .AfterComplete(success =>
                 {
                     if (_session.Player == null || _session.Player.GUID != auctionPlayerGuid)
                         return;

                     if (success)
                     {
                         _session.Player.UpdateCriteria(CriteriaType.ItemsPostedAtAuction, 1);
                         SendAuctionCommandResult(auctionId, AuctionCommand.SellItem, AuctionResult.Ok, throttle.DelayUntilNext);
                     }
                     else
                     {
                         SendAuctionCommandResult(0, AuctionCommand.SellItem, AuctionResult.DatabaseError, throttle.DelayUntilNext);
                     }
                 });
    }

    [WorldPacketHandler(ClientOpcodes.AuctionSetFavoriteItem)]
    private void HandleAuctionSetFavoriteItem(AuctionSetFavoriteItem setFavoriteItem)
    {
        var throttle = _auctionManager.CheckThrottle(_session.Player, false);

        if (throttle.Throttled)
            return;

        SQLTransaction trans = new();

        var stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_CHARACTER_FAVORITE_AUCTION);
        stmt.AddValue(0, _session.Player.GUID.Counter);
        stmt.AddValue(1, setFavoriteItem.Item.Order);
        trans.Append(stmt);

        if (!setFavoriteItem.IsNotFavorite)
        {
            stmt = _characterDatabase.GetPreparedStatement(CharStatements.INS_CHARACTER_FAVORITE_AUCTION);
            stmt.AddValue(0, _session.Player.GUID.Counter);
            stmt.AddValue(1, setFavoriteItem.Item.Order);
            stmt.AddValue(2, setFavoriteItem.Item.ItemID);
            stmt.AddValue(3, setFavoriteItem.Item.ItemLevel);
            stmt.AddValue(4, setFavoriteItem.Item.BattlePetSpeciesID);
            stmt.AddValue(5, setFavoriteItem.Item.SuffixItemNameDescriptionID);
            trans.Append(stmt);
        }

        _characterDatabase.CommitTransaction(trans);
    }

    [WorldPacketHandler(ClientOpcodes.AuctionReplicateItems)]
    private void HandleReplicateItems(AuctionReplicateItems replicateItems)
    {
        var creature = _session.Player.GetNPCIfCanInteractWith(replicateItems.Auctioneer, NPCFlags.Auctioneer, NPCFlags2.None);

        if (creature == null)
        {
            Log.Logger.Error($"WORLD: HandleReplicateItems - {replicateItems.Auctioneer} not found or you can't interact with him.");

            return;
        }

        // remove fake death
        if (_session.Player.HasUnitState(UnitState.Died))
            _session.Player.RemoveAurasByType(AuraType.FeignDeath);

        var auctionHouse = _auctionManager.GetAuctionsMap(creature.Faction);

        AuctionReplicateResponse response = new();

        auctionHouse.BuildReplicate(response, _session.Player, replicateItems.ChangeNumberGlobal, replicateItems.ChangeNumberCursor, replicateItems.ChangeNumberTombstone, replicateItems.Count);

        response.DesiredDelay = _configuration.GetDefaultValue("Auction:SearchDelay", 300u) * 5;
        response.Result = 0;

        _session.SendPacket(response);
    }
}