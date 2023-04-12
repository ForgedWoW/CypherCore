// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Forged.MapServer.Cache;
using Forged.MapServer.Chat;
using Forged.MapServer.Chat.Commands;
using Forged.MapServer.Chrono;
using Forged.MapServer.DataStorage;
using Forged.MapServer.DataStorage.Structs.A;
using Forged.MapServer.Entities.Items;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Globals;
using Forged.MapServer.Mails;
using Forged.MapServer.Networking.Packets.AuctionHouse;
using Forged.MapServer.Networking.Packets.Item;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAuctionHouse;
using Forged.MapServer.World;
using Framework.Constants;
using Framework.Database;
using Framework.IO;
using Framework.Util;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Forged.MapServer.AuctionHouse;

public class AuctionHouseObject
{
    private readonly AuctionHouseRecord _auctionHouse;
    private readonly AuctionManager _auctionManager;
    private readonly BattlePetMgrData _battlePetMgr;
    private readonly SortedDictionary<AuctionsBucketKey, AuctionsBucketData> _buckets = new();
    private readonly CharacterCache _characterCache;
    private readonly CharacterDatabase _characterDatabase;
    private readonly CliDB _cliDB;

    // ordered for search by itemid only
    private readonly Dictionary<ObjectGuid, CommodityQuote> _commodityQuotes = new();

    private readonly IConfiguration _configuration;
    private readonly DB2Manager _db2Manager;
    private readonly SortedList<uint, AuctionPosting> _itemsByAuctionId = new();
    private readonly ObjectAccessor _objectAccessor;
    private readonly GameObjectManager _objectManager;

    // ordered for replicate
    private readonly MultiMap<ObjectGuid, uint> _playerBidderAuctions = new();

    private readonly MultiMap<ObjectGuid, uint> _playerOwnedAuctions = new();

    // Map of throttled players for GetAll, and throttle expiry time
    // Stored here, rather than player object to maintain persistence after logout
    private readonly Dictionary<ObjectGuid, PlayerReplicateThrottleData> _replicateThrottleMap = new();

    private readonly ScriptManager _scriptManager;
    private readonly WorldManager _worldManager;

    public AuctionHouseObject(uint auctionHouseId, CliDB cliDB, CharacterDatabase characterDatabase, AuctionManager auctionManager, IConfiguration configuration,
                              ScriptManager scriptManager, DB2Manager db2Manager, GameObjectManager objectManager, BattlePetMgrData battlePetMgr, CharacterCache characterCache,
                              WorldManager worldManager, ObjectAccessor objectAccessor)
    {
        _cliDB = cliDB;
        _characterDatabase = characterDatabase;
        _auctionManager = auctionManager;
        _configuration = configuration;
        _scriptManager = scriptManager;
        _db2Manager = db2Manager;
        _objectManager = objectManager;
        _battlePetMgr = battlePetMgr;
        _characterCache = characterCache;
        _worldManager = worldManager;
        _objectAccessor = objectAccessor;
        _auctionHouse = _cliDB.AuctionHouseStorage.LookupByKey(auctionHouseId);
    }

    public void AddAuction(SQLTransaction trans, AuctionPosting auction)
    {
        var key = AuctionsBucketKey.ForItem(auction.Items[0]);

        if (!_buckets.TryGetValue(key, out var bucket))
        {
            // we don't have any item for this key yet, create new bucket
            bucket = new AuctionsBucketData
            {
                Key = key
            };

            var itemTemplate = auction.Items[0].Template;
            bucket.ItemClass = (byte)itemTemplate.Class;
            bucket.ItemSubClass = (byte)itemTemplate.SubClass;
            bucket.InventoryType = (byte)itemTemplate.InventoryType;
            bucket.RequiredLevel = (byte)auction.Items[0].GetRequiredLevel();

            bucket.SortLevel = itemTemplate.Class switch
            {
                ItemClass.Weapon          => (byte)key.ItemLevel,
                ItemClass.Armor           => (byte)key.ItemLevel,
                ItemClass.Container       => (byte)itemTemplate.ContainerSlots,
                ItemClass.Gem             => (byte)itemTemplate.BaseItemLevel,
                ItemClass.ItemEnhancement => (byte)itemTemplate.BaseItemLevel,
                ItemClass.Consumable      => Math.Max((byte)1, bucket.RequiredLevel),
                ItemClass.Miscellaneous   => 1,
                ItemClass.BattlePets      => 1,
                ItemClass.Recipe          => (byte)((ItemSubClassRecipe)itemTemplate.SubClass != ItemSubClassRecipe.Book ? itemTemplate.RequiredSkillRank : (uint)itemTemplate.BaseRequiredLevel),
                _                         => bucket.SortLevel
            };

            for (var locale = Locale.enUS; locale < Locale.Total; ++locale)
            {
                if (locale == Locale.None)
                    continue;

                bucket.FullName[(int)locale] = auction.Items[0].GetName(locale);
            }

            _buckets.Add(key, bucket);
        }

        // update cache fields
        var priceToDisplay = auction.BuyoutOrUnitPrice != 0 ? auction.BuyoutOrUnitPrice : auction.BidAmount;

        if (bucket.MinPrice == 0 || priceToDisplay < bucket.MinPrice)
            bucket.MinPrice = priceToDisplay;

        var itemModifiedAppearance = auction.Items[0].GetItemModifiedAppearance();

        if (itemModifiedAppearance != null)
        {
            var index = 0;

            for (var i = 0; i < bucket.ItemModifiedAppearanceId.Length; ++i)
                if (bucket.ItemModifiedAppearanceId[i].Id == itemModifiedAppearance.Id)
                {
                    index = i;

                    break;
                }

            bucket.ItemModifiedAppearanceId[index] = (itemModifiedAppearance.Id, bucket.ItemModifiedAppearanceId[index].Item2 + 1);
        }

        uint quality;

        if (auction.Items[0].GetModifier(ItemModifier.BattlePetSpeciesId) == 0)
        {
            quality = (byte)auction.Items[0].Quality;
        }
        else
        {
            quality = (auction.Items[0].GetModifier(ItemModifier.BattlePetBreedData) >> 24) & 0xFF;

            foreach (var item in auction.Items)
            {
                var battlePetLevel = (byte)item.GetModifier(ItemModifier.BattlePetLevel);

                if (bucket.MinBattlePetLevel == 0)
                    bucket.MinBattlePetLevel = battlePetLevel;
                else if (bucket.MinBattlePetLevel > battlePetLevel)
                    bucket.MinBattlePetLevel = battlePetLevel;

                bucket.MaxBattlePetLevel = Math.Max(bucket.MaxBattlePetLevel, battlePetLevel);
                bucket.SortLevel = bucket.MaxBattlePetLevel;
            }
        }

        bucket.QualityMask |= (AuctionHouseFilterMask)(1 << ((int)quality + 4));
        ++bucket.QualityCounts[quality];

        if (trans != null)
        {
            var stmt = _characterDatabase.GetPreparedStatement(CharStatements.INS_AUCTION);
            stmt.AddValue(0, auction.Id);
            stmt.AddValue(1, _auctionHouse.Id);
            stmt.AddValue(2, auction.Owner.Counter);
            stmt.AddValue(3, ObjectGuid.Empty.Counter);
            stmt.AddValue(4, auction.MinBid);
            stmt.AddValue(5, auction.BuyoutOrUnitPrice);
            stmt.AddValue(6, auction.Deposit);
            stmt.AddValue(7, auction.BidAmount);
            stmt.AddValue(8, Time.DateTimeToUnixTime(auction.StartTime));
            stmt.AddValue(9, Time.DateTimeToUnixTime(auction.EndTime));
            stmt.AddValue(10, (byte)auction.ServerFlags);
            trans.Append(stmt);

            foreach (var item in auction.Items)
            {
                stmt = _characterDatabase.GetPreparedStatement(CharStatements.INS_AUCTION_ITEMS);
                stmt.AddValue(0, auction.Id);
                stmt.AddValue(1, item.GUID.Counter);
                trans.Append(stmt);
            }
        }

        foreach (var item in auction.Items)
            _auctionManager.AddAItem(item);

        auction.Bucket = bucket;
        _playerOwnedAuctions.Add(auction.Owner, auction.Id);

        foreach (var bidder in auction.BidderHistory)
            _playerBidderAuctions.Add(bidder, auction.Id);

        _itemsByAuctionId[auction.Id] = auction;

        AuctionPosting.Sorter insertSorter = new(Locale.enUS,
                                                 new AuctionSortDef[]
                                                 {
                                                     new(AuctionHouseSortOrder.Price, false)
                                                 },
                                                 1);

        var auctionIndex = bucket.Auctions.BinarySearch(auction, insertSorter);

        if (auctionIndex < 0)
            auctionIndex = ~auctionIndex;

        bucket.Auctions.Insert(auctionIndex, auction);

        _scriptManager.ForEach<IAuctionHouseOnAuctionAdd>(p => p.OnAuctionAdd(this, auction));
    }

    public void BuildListAuctionItems(AuctionListItemsResult listItemsResult, Player player, AuctionsBucketKey bucketKey, uint offset, AuctionSortDef[] sorts, int sortCount)
    {
        listItemsResult.TotalCount = 0;
        if (_buckets.TryGetValue(bucketKey, out var bucket))
        {
            var sorter = new AuctionPosting.Sorter(player.Session.SessionDbcLocale, sorts, sortCount);
            var builder = new AuctionsResultBuilder<AuctionPosting>(offset, sorter, AuctionHouseResultLimits.Items);

            foreach (var auction in bucket.Auctions)
            {
                builder.AddItem(auction);

                foreach (var item in auction.Items)
                    listItemsResult.TotalCount += item.Count;
            }

            foreach (var resultAuction in builder.GetResultRange())
            {
                AuctionItem auctionItem = new();
                resultAuction.BuildAuctionItem(auctionItem, false, false, resultAuction.OwnerAccount != player.Session.AccountGUID, resultAuction.Bidder.IsEmpty);
                listItemsResult.Items.Add(auctionItem);
            }

            listItemsResult.HasMoreResults = builder.HasMoreResults();
        }
    }

    public void BuildListAuctionItems(AuctionListItemsResult listItemsResult, Player player, uint itemId, uint offset, AuctionSortDef[] sorts, int sortCount)
    {
        var sorter = new AuctionPosting.Sorter(player.Session.SessionDbcLocale, sorts, sortCount);
        var builder = new AuctionsResultBuilder<AuctionPosting>(offset, sorter, AuctionHouseResultLimits.Items);

        listItemsResult.TotalCount = 0;
        if (_buckets.TryGetValue(new AuctionsBucketKey(itemId, 0, 0, 0), out var bucketData))
            foreach (var auction in bucketData.Auctions)
            {
                builder.AddItem(auction);

                foreach (var item in auction.Items)
                    listItemsResult.TotalCount += item.Count;
            }

        foreach (var resultAuction in builder.GetResultRange())
        {
            AuctionItem auctionItem = new();

            resultAuction.BuildAuctionItem(auctionItem,
                                           false,
                                           true,
                                           resultAuction.OwnerAccount != player.Session.AccountGUID,
                                           resultAuction.Bidder.IsEmpty);

            listItemsResult.Items.Add(auctionItem);
        }

        listItemsResult.HasMoreResults = builder.HasMoreResults();
    }

    public void BuildListBiddedItems(AuctionListBiddedItemsResult listBidderItemsResult, Player player, uint offset, AuctionSortDef[] sorts, int sortCount)
    {
        // always full list
        List<AuctionPosting> auctions = new();

        foreach (var auctionId in _playerBidderAuctions.LookupByKey(player.GUID))
        {
            if (_itemsByAuctionId.TryGetValue(auctionId, out var auction))
                auctions.Add(auction);
        }

        AuctionPosting.Sorter sorter = new(player.Session.SessionDbcLocale, sorts, sortCount);
        auctions.Sort(sorter);

        foreach (var resultAuction in auctions)
        {
            AuctionItem auctionItem = new();
            resultAuction.BuildAuctionItem(auctionItem, true, true, true, false);
            listBidderItemsResult.Items.Add(auctionItem);
        }

        listBidderItemsResult.HasMoreResults = false;
    }

    public void BuildListBuckets(AuctionListBucketsResult listBucketsResult, Player player, string name, byte minLevel, byte maxLevel, AuctionHouseFilterMask filters, AuctionSearchClassFilters classFilters,
                                 byte[] knownPetBits, int knownPetBitsCount, byte maxKnownPetLevel, uint offset, AuctionSortDef[] sorts, int sortCount)
    {
        List<uint> knownAppearanceIds = new();
        BitArray knownPetSpecies = new(knownPetBits);

        // prepare uncollected filter for more efficient searches
        if (filters.HasFlag(AuctionHouseFilterMask.UncollectedOnly))
            knownAppearanceIds = player.Session.CollectionMgr.GetAppearanceIds();

        //todo fix me
        //if (knownPetSpecies.Length < _cliDB.BattlePetSpeciesStorage.GetNumRows())
        //knownPetSpecies.resize(_cliDB.BattlePetSpeciesStorage.GetNumRows());
        var sorter = new AuctionsBucketData.Sorter(player.Session.SessionDbcLocale, sorts, sortCount);
        var builder = new AuctionsResultBuilder<AuctionsBucketData>(offset, sorter, AuctionHouseResultLimits.Browse);

        foreach (var bucket in _buckets)
        {
            var bucketData = bucket.Value;

            if (!name.IsEmpty())
            {
                if (filters.HasFlag(AuctionHouseFilterMask.ExactMatch))
                {
                    if (bucketData.FullName[(int)player.Session.SessionDbcLocale] != name)
                        continue;
                }
                else
                {
                    if (!bucketData.FullName[(int)player.Session.SessionDbcLocale].Contains(name))
                        continue;
                }
            }

            if (minLevel != 0 && bucketData.RequiredLevel < minLevel)
                continue;

            if (maxLevel != 0 && bucketData.RequiredLevel > maxLevel)
                continue;

            if (!filters.HasFlag(bucketData.QualityMask))
                continue;

            if (classFilters != null)
            {
                // if we dont want any class filters, Optional is not initialized
                // if we dont want this class included, SubclassMask is set to FILTER_SKIP_CLASS
                // if we want this class and did not specify and subclasses, its set to FILTER_SKIP_SUBCLASS
                // otherwise full restrictions apply
                if (classFilters.Classes[bucketData.ItemClass].SubclassMask == AuctionSearchClassFilters.FilterType.SkipClass)
                    continue;

                if (classFilters.Classes[bucketData.ItemClass].SubclassMask != AuctionSearchClassFilters.FilterType.SkipSubclass)
                {
                    if (!classFilters.Classes[bucketData.ItemClass].SubclassMask.HasAnyFlag((AuctionSearchClassFilters.FilterType)(1 << bucketData.ItemSubClass)))
                        continue;

                    if (!classFilters.Classes[bucketData.ItemClass].InvTypes[bucketData.ItemSubClass].HasAnyFlag(1u << bucketData.InventoryType))
                        continue;
                }
            }

            if (filters.HasFlag(AuctionHouseFilterMask.UncollectedOnly))
            {
                // appearances - by ItemAppearanceId, not ItemModifiedAppearanceId
                if (bucketData.InventoryType != (byte)InventoryType.NonEquip && bucketData.InventoryType != (byte)InventoryType.Bag)
                {
                    var hasAll = true;

                    foreach (var bucketAppearance in bucketData.ItemModifiedAppearanceId)
                    {
                        if (_cliDB.ItemModifiedAppearanceStorage.TryGetValue(bucketAppearance.Item1, out var itemModifiedAppearance))
                            if (!knownAppearanceIds.Contains((uint)itemModifiedAppearance.ItemAppearanceID))
                            {
                                hasAll = false;

                                break;
                            }
                    }

                    if (hasAll)
                        continue;
                }
                // caged pets
                else if (bucket.Key.BattlePetSpeciesId != 0)
                {
                    if (knownPetSpecies.Get(bucket.Key.BattlePetSpeciesId))
                        continue;
                }
                // toys
                else if (_db2Manager.IsToyItem(bucket.Key.ItemId))
                {
                    if (player.Session.CollectionMgr.HasToy(bucket.Key.ItemId))
                        continue;
                }
                // mounts
                // recipes
                // pet items
                else if (bucketData.ItemClass is (int)ItemClass.Consumable or (int)ItemClass.Recipe or (int)ItemClass.Miscellaneous)
                {
                    var itemTemplate = _objectManager.GetItemTemplate(bucket.Key.ItemId);

                    if (itemTemplate.Effects.Count >= 2 && itemTemplate.Effects[0].SpellID is 483 or 55884)
                    {
                        if (player.HasSpell((uint)itemTemplate.Effects[1].SpellID))
                            continue;

                        var battlePetSpecies = _battlePetMgr.GetBattlePetSpeciesBySpell((uint)itemTemplate.Effects[1].SpellID);

                        if (battlePetSpecies != null)
                            if (knownPetSpecies.Get((int)battlePetSpecies.Id))
                                continue;
                    }
                }
            }

            if (filters.HasFlag(AuctionHouseFilterMask.UsableOnly))
            {
                if (bucketData.RequiredLevel != 0 && player.Level < bucketData.RequiredLevel)
                    continue;

                if (player.CanUseItem(_objectManager.GetItemTemplate(bucket.Key.ItemId), true) != InventoryResult.Ok)
                    continue;

                // cannot learn caged pets whose level exceeds highest level of currently owned pet
                if (bucketData.MinBattlePetLevel != 0 && bucketData.MinBattlePetLevel > maxKnownPetLevel)
                    continue;
            }

            // TODO: this one needs to access loot history to know highest item level for every inventory type
            //if (filters.HasFlag(AuctionHouseFilterMask.UpgradesOnly))
            //{
            //}

            builder.AddItem(bucketData);
        }

        foreach (var resultBucket in builder.GetResultRange())
        {
            BucketInfo bucketInfo = new();
            resultBucket.BuildBucketInfo(bucketInfo, player);
            listBucketsResult.Buckets.Add(bucketInfo);
        }

        listBucketsResult.HasMoreResults = builder.HasMoreResults();
    }

    public void BuildListBuckets(AuctionListBucketsResult listBucketsResult, Player player, AuctionBucketKey[] keys, int keysCount, AuctionSortDef[] sorts, int sortCount)
    {
        List<AuctionsBucketData> buckets = new();

        for (var i = 0; i < keysCount; ++i)
        {
            if (_buckets.TryGetValue(new AuctionsBucketKey(keys[i]), out var bucketData))
                buckets.Add(bucketData);
        }

        AuctionsBucketData.Sorter sorter = new(player.Session.SessionDbcLocale, sorts, sortCount);
        buckets.Sort(sorter);

        foreach (var resultBucket in buckets)
        {
            BucketInfo bucketInfo = new();
            resultBucket.BuildBucketInfo(bucketInfo, player);
            listBucketsResult.Buckets.Add(bucketInfo);
        }

        listBucketsResult.HasMoreResults = false;
    }

    public void BuildListOwnedItems(AuctionListOwnedItemsResult listOwnerItemsResult, Player player, uint offset, AuctionSortDef[] sorts, int sortCount)
    {
        // always full list
        List<AuctionPosting> auctions = new();

        foreach (var auctionId in _playerOwnedAuctions.LookupByKey(player.GUID))
        {
            if (_itemsByAuctionId.TryGetValue(auctionId, out var auction))
                auctions.Add(auction);
        }

        AuctionPosting.Sorter sorter = new(player.Session.SessionDbcLocale, sorts, sortCount);
        auctions.Sort(sorter);

        foreach (var resultAuction in auctions)
        {
            AuctionItem auctionItem = new();
            resultAuction.BuildAuctionItem(auctionItem, true, true, false, false);
            listOwnerItemsResult.Items.Add(auctionItem);
        }

        listOwnerItemsResult.HasMoreResults = false;
    }

    public void BuildReplicate(AuctionReplicateResponse replicateResponse, Player player, uint global, uint cursor, uint tombstone, uint count)
    {
        var curTime = GameTime.Now;

        if (!_replicateThrottleMap.TryGetValue(player.GUID, out var throttleData))
        {
            throttleData = new PlayerReplicateThrottleData
            {
                NextAllowedReplication = curTime + TimeSpan.FromSeconds(_configuration.GetDefaultValue("Auction.ReplicateItemsCooldown", 900)),
                Global = _auctionManager.GenerateReplicationId
            };
        }
        else
        {
            if (throttleData.Global != global || throttleData.Cursor != cursor || throttleData.Tombstone != tombstone)
                return;

            if (!throttleData.IsReplicationInProgress() && throttleData.NextAllowedReplication > curTime)
                return;
        }

        if (_itemsByAuctionId.Empty() || count == 0)
            return;

        var keyIndex = _itemsByAuctionId.IndexOfKey(cursor);

        foreach (var pair in _itemsByAuctionId.Skip(keyIndex))
        {
            AuctionItem auctionItem = new();
            pair.Value.BuildAuctionItem(auctionItem, false, true, true, pair.Value.Bidder.IsEmpty);
            replicateResponse.Items.Add(auctionItem);

            if (--count == 0)
                break;
        }

        replicateResponse.ChangeNumberGlobal = throttleData.Global;
        replicateResponse.ChangeNumberCursor = throttleData.Cursor = !replicateResponse.Items.Empty() ? replicateResponse.Items.Last().AuctionID : 0;
        replicateResponse.ChangeNumberTombstone = throttleData.Tombstone = count == 0 ? _itemsByAuctionId.First().Key : 0;
        _replicateThrottleMap[player.GUID] = throttleData;
    }

    public bool BuyCommodity(SQLTransaction trans, Player player, uint itemId, uint quantity, TimeSpan delayForNextAction)
    {
        var itemTemplate = _objectManager.GetItemTemplate(itemId);

        if (itemTemplate == null)
            return false;

        if (!_buckets.TryGetValue(AuctionsBucketKey.ForCommodity(itemTemplate), out var bucketItr))
        {
            player.Session.SendAuctionCommandResult(0, AuctionCommand.PlaceBid, AuctionResult.CommodityPurchaseFailed, delayForNextAction);

            return false;
        }

        if (!_commodityQuotes.TryGetValue(player.GUID, out var quote))
        {
            player.Session.SendAuctionCommandResult(0, AuctionCommand.PlaceBid, AuctionResult.CommodityPurchaseFailed, delayForNextAction);

            return false;
        }

        ulong totalPrice = 0;
        var remainingQuantity = quantity;
        List<AuctionPosting> auctions = new();

        for (var i = 0; i < bucketItr.Auctions.Count;)
        {
            var auction = bucketItr.Auctions[i++];
            auctions.Add(auction);

            foreach (var auctionItem in auction.Items)
            {
                if (auctionItem.Count >= remainingQuantity)
                {
                    totalPrice += auction.BuyoutOrUnitPrice * remainingQuantity;
                    remainingQuantity = 0;
                    i = bucketItr.Auctions.Count;

                    break;
                }

                totalPrice += auction.BuyoutOrUnitPrice * auctionItem.Count;
                remainingQuantity -= auctionItem.Count;
            }
        }

        // not enough items on auction house
        if (remainingQuantity != 0)
        {
            player.Session.SendAuctionCommandResult(0, AuctionCommand.PlaceBid, AuctionResult.CommodityPurchaseFailed, delayForNextAction);

            return false;
        }

        // something was bought between creating quote and finalizing transaction
        // but we allow lower price if new items were posted at lower price
        if (totalPrice > quote.TotalPrice)
        {
            player.Session.SendAuctionCommandResult(0, AuctionCommand.PlaceBid, AuctionResult.CommodityPurchaseFailed, delayForNextAction);

            return false;
        }

        if (!player.HasEnoughMoney(totalPrice))
        {
            player.Session.SendAuctionCommandResult(0, AuctionCommand.PlaceBid, AuctionResult.CommodityPurchaseFailed, delayForNextAction);

            return false;
        }

        ObjectGuid uniqueSeller = new();

        // prepare items
        List<MailedItemsBatch> items = new();
        items.Add(new MailedItemsBatch());

        remainingQuantity = quantity;
        List<int> removedItemsFromAuction = new();

        for (var i = 0; i < bucketItr.Auctions.Count;)
        {
            var auction = bucketItr.Auctions[i++];

            if (uniqueSeller == ObjectGuid.Empty)
                uniqueSeller = auction.Owner;
            else if (uniqueSeller != auction.Owner)
                uniqueSeller = ObjectGuid.Empty;

            uint boughtFromAuction = 0;
            var removedItems = 0;

            foreach (var auctionItem in auction.Items)
            {
                var itemsBatch = items.Last();

                if (itemsBatch.IsFull())
                {
                    items.Add(new MailedItemsBatch());
                    itemsBatch = items.Last();
                }

                if (auctionItem.Count >= remainingQuantity)
                {
                    var clonedItem = auctionItem.CloneItem(remainingQuantity, player);

                    if (!clonedItem)
                    {
                        player.Session.SendAuctionCommandResult(0, AuctionCommand.PlaceBid, AuctionResult.CommodityPurchaseFailed, delayForNextAction);

                        return false;
                    }

                    auctionItem.SetCount(auctionItem.Count - remainingQuantity);
                    auctionItem.FSetState(ItemUpdateState.Changed);
                    auctionItem.SaveToDB(trans);
                    itemsBatch.AddItem(clonedItem, auction.BuyoutOrUnitPrice);
                    boughtFromAuction += remainingQuantity;
                    remainingQuantity = 0;
                    i = bucketItr.Auctions.Count;

                    break;
                }

                itemsBatch.AddItem(auctionItem, auction.BuyoutOrUnitPrice);
                boughtFromAuction += auctionItem.Count;
                remainingQuantity -= auctionItem.Count;
                ++removedItems;
            }

            removedItemsFromAuction.Add(removedItems);

            if (player.Session.HasPermission(RBACPermissions.LogGmTrade))
            {
                var bidderAccId = player.Session.AccountId;

                if (!_characterCache.GetCharacterNameByGuid(auction.Owner, out var ownerName))
                    ownerName = _objectManager.GetCypherString(CypherStrings.Unknown);

                Log.Logger.ForContext<GMCommands>().Information(
                               $"GM {player.GetName()} (Account: {bidderAccId}) bought commodity in auction: {items[0].Items[0].GetName(_worldManager.DefaultDbcLocale)} (Entry: {items[0].Items[0].Entry} " +
                               $"Count: {boughtFromAuction}) and pay money: {auction.BuyoutOrUnitPrice * boughtFromAuction}. Original owner {ownerName} (Account: {_characterCache.GetCharacterAccountIdByGuid(auction.Owner)})");
            }

            var auctionHouseCut = CalculateAuctionHouseCut(auction.BuyoutOrUnitPrice * boughtFromAuction);
            var depositPart = _auctionManager.GetCommodityAuctionDeposit(items[0].Items[0].Template, (auction.EndTime - auction.StartTime), boughtFromAuction);
            var profit = auction.BuyoutOrUnitPrice * boughtFromAuction + depositPart - auctionHouseCut;

            var owner = _objectAccessor.FindConnectedPlayer(auction.Owner);

            if (owner != null)
            {
                owner.UpdateCriteria(CriteriaType.MoneyEarnedFromAuctions, profit);
                owner.UpdateCriteria(CriteriaType.HighestAuctionSale, profit);
                owner.Session.SendAuctionClosedNotification(auction, _configuration.GetDefaultValue("MailDeliveryDelay", Time.FHOUR), true);
            }

            new MailDraft(_auctionManager.BuildCommodityAuctionMailSubject(AuctionMailType.Sold, itemId, boughtFromAuction),
                          _auctionManager.BuildAuctionSoldMailBody(player.GUID, auction.BuyoutOrUnitPrice * boughtFromAuction, boughtFromAuction, (uint)depositPart, auctionHouseCut))
                .AddMoney(profit)
                .SendMailTo(trans, new MailReceiver(_objectAccessor.FindConnectedPlayer(auction.Owner), auction.Owner), new MailSender(this), MailCheckMask.Copied, _configuration.GetDefaultValue("MailDeliveryDelay", Time.UHOUR));
        }

        player.ModifyMoney(-(long)totalPrice);
        player.SaveGoldToDB(trans);

        foreach (var batch in items)
        {
            MailDraft mail = new(_auctionManager.BuildCommodityAuctionMailSubject(AuctionMailType.Won, itemId, batch.Quantity),
                                 _auctionManager.BuildAuctionWonMailBody(uniqueSeller.Value, batch.TotalPrice, batch.Quantity));

            for (var i = 0; i < batch.ItemsCount; ++i)
            {
                var stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_AUCTION_ITEMS_BY_ITEM);
                stmt.AddValue(0, batch.Items[i].GUID.Counter);
                trans.Append(stmt);

                batch.Items[i].SetOwnerGUID(player.GUID);
                batch.Items[i].SaveToDB(trans);
                mail.AddItem(batch.Items[i]);
            }

            mail.SendMailTo(trans, player, new MailSender(this), MailCheckMask.Copied);
        }

        AuctionWonNotification packet = new();
        packet.Info.Initialize(auctions[0], items[0].Items[0]);
        player.SendPacket(packet);

        for (var i = 0; i < auctions.Count; ++i)
            if (removedItemsFromAuction[i] == auctions[i].Items.Count)
            {
                RemoveAuction(trans, auctions[i]); // bought all items
            }
            else if (removedItemsFromAuction[i] != 0)
            {
                var lastRemovedItemIndex = removedItemsFromAuction[i];

                for (var c = 0; c != removedItemsFromAuction[i]; ++c)
                    _auctionManager.RemoveAItem(auctions[i].Items[c].GUID);

                auctions[i].Items.RemoveRange(0, lastRemovedItemIndex);
            }

        return true;
    }

    public ulong CalculateAuctionHouseCut(ulong bidAmount)
    {
        return (ulong)Math.Max((long)(MathFunctions.CalculatePct(bidAmount, _auctionHouse.ConsignmentRate) * _configuration.GetDefaultValue("Rate.Auction.Cut", 1.0f)), 0);
    }

    public void CancelCommodityQuote(ObjectGuid guid)
    {
        _commodityQuotes.Remove(guid);
    }

    public CommodityQuote CreateCommodityQuote(Player player, uint itemId, uint quantity)
    {
        var itemTemplate = _objectManager.GetItemTemplate(itemId);

        if (itemTemplate == null)
            return null;

        if (!_buckets.TryGetValue(AuctionsBucketKey.ForCommodity(itemTemplate), out var bucketData))
            return null;

        ulong totalPrice = 0;
        var remainingQuantity = quantity;

        foreach (var auction in bucketData.Auctions)
        {
            foreach (var auctionItem in auction.Items)
            {
                if (auctionItem.Count >= remainingQuantity)
                {
                    totalPrice += auction.BuyoutOrUnitPrice * remainingQuantity;
                    remainingQuantity = 0;

                    break;
                }

                totalPrice += auction.BuyoutOrUnitPrice * auctionItem.Count;
                remainingQuantity -= auctionItem.Count;
            }
        }

        // not enough items on auction house
        if (remainingQuantity != 0)
            return null;

        if (!player.HasEnoughMoney(totalPrice))
            return null;

        var quote = _commodityQuotes[player.GUID];
        quote.TotalPrice = totalPrice;
        quote.Quantity = quantity;
        quote.ValidTo = GameTime.Now + TimeSpan.FromSeconds(30);

        return quote;
    }

    public AuctionPosting GetAuction(uint auctionId)
    {
        return _itemsByAuctionId.LookupByKey(auctionId);
    }

    public uint GetAuctionHouseId()
    {
        return _auctionHouse.Id;
    }

    public void RemoveAuction(SQLTransaction trans, AuctionPosting auction, AuctionPosting auctionPosting = null)
    {
        var bucket = auction.Bucket;

        bucket.Auctions.RemoveAll(auct => auct.Id == auction.Id);

        if (!bucket.Auctions.Empty())
        {
            // update cache fields
            var priceToDisplay = auction.BuyoutOrUnitPrice != 0 ? auction.BuyoutOrUnitPrice : auction.BidAmount;

            if (bucket.MinPrice == priceToDisplay)
            {
                bucket.MinPrice = ulong.MaxValue;

                foreach (var remainingAuction in bucket.Auctions)
                    bucket.MinPrice = Math.Min(bucket.MinPrice, remainingAuction.BuyoutOrUnitPrice != 0 ? remainingAuction.BuyoutOrUnitPrice : remainingAuction.BidAmount);
            }

            var itemModifiedAppearance = auction.Items[0].GetItemModifiedAppearance();

            if (itemModifiedAppearance != null)
            {
                var index = -1;

                for (var i = 0; i < bucket.ItemModifiedAppearanceId.Length; ++i)
                    if (bucket.ItemModifiedAppearanceId[i].Item1 == itemModifiedAppearance.Id)
                    {
                        index = i;

                        break;
                    }

                if (index != -1)
                    if (--bucket.ItemModifiedAppearanceId[index].Count == 0)
                        bucket.ItemModifiedAppearanceId[index].Id = 0;
            }

            uint quality;

            if (auction.Items[0].GetModifier(ItemModifier.BattlePetSpeciesId) == 0)
            {
                quality = (uint)auction.Items[0].Quality;
            }
            else
            {
                quality = (auction.Items[0].GetModifier(ItemModifier.BattlePetBreedData) >> 24) & 0xFF;
                bucket.MinBattlePetLevel = 0;
                bucket.MaxBattlePetLevel = 0;

                foreach (var remainingAuction in bucket.Auctions)
                {
                    foreach (var item in remainingAuction.Items)
                    {
                        var battlePetLevel = (byte)item.GetModifier(ItemModifier.BattlePetLevel);

                        if (bucket.MinBattlePetLevel == 0)
                            bucket.MinBattlePetLevel = battlePetLevel;
                        else if (bucket.MinBattlePetLevel > battlePetLevel)
                            bucket.MinBattlePetLevel = battlePetLevel;

                        bucket.MaxBattlePetLevel = Math.Max(bucket.MaxBattlePetLevel, battlePetLevel);
                    }
                }
            }

            if (--bucket.QualityCounts[quality] == 0)
                bucket.QualityMask &= (AuctionHouseFilterMask)(~(1 << ((int)quality + 4)));
        }
        else
        {
            _buckets.Remove(bucket.Key);
        }

        var stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_AUCTION);
        stmt.AddValue(0, auction.Id);
        trans.Append(stmt);

        foreach (var item in auction.Items)
            _auctionManager.RemoveAItem(item.GUID);

        _scriptManager.ForEach<IAuctionHouseOnAcutionRemove>(p => p.OnAuctionRemove(this, auction));

        _playerOwnedAuctions.Remove(auction.Owner, auction.Id);

        foreach (var bidder in auction.BidderHistory)
            _playerBidderAuctions.Remove(bidder, auction.Id);

        _itemsByAuctionId.Remove(auction.Id);
    }

    //this function sends mail, when auction is cancelled to old bidder
    public void SendAuctionCancelledToBidder(AuctionPosting auction, SQLTransaction trans)
    {
        var bidder = _objectAccessor.FindConnectedPlayer(auction.Bidder);

        // bidder exist
        if ((bidder || _characterCache.HasCharacterCacheEntry(auction.Bidder))) // && !sAuctionBotConfig.IsBotChar(auction.Bidder))
            new MailDraft(_auctionManager.BuildItemAuctionMailSubject(AuctionMailType.Removed, auction), "")
                .AddMoney(auction.BidAmount)
                .SendMailTo(trans, new MailReceiver(bidder, auction.Bidder), new MailSender(this), MailCheckMask.Copied);
    }

    public void SendAuctionExpired(AuctionPosting auction, SQLTransaction trans)
    {
        var owner = _objectAccessor.FindConnectedPlayer(auction.Owner);

        // owner exist
        if ((owner || _characterCache.HasCharacterCacheEntry(auction.Owner))) // && !sAuctionBotConfig.IsBotChar(auction.Owner))
        {
            if (owner)
                owner.Session.SendAuctionClosedNotification(auction, 0.0f, false);

            var itemIndex = 0;

            while (itemIndex < auction.Items.Count)
            {
                MailDraft mail = new(_auctionManager.BuildItemAuctionMailSubject(AuctionMailType.Expired, auction), "");

                for (var i = 0; i < SharedConst.MaxMailItems && itemIndex < auction.Items.Count; ++i, ++itemIndex)
                    mail.AddItem(auction.Items[itemIndex]);

                mail.SendMailTo(trans, new MailReceiver(owner, auction.Owner), new MailSender(this), MailCheckMask.Copied);
            }
        }
        else
        {
            // owner doesn't exist, delete the item
            foreach (var item in auction.Items)
                _auctionManager.RemoveAItem(item.GUID, true, trans);
        }
    }

    public void SendAuctionInvoice(AuctionPosting auction, Player owner, SQLTransaction trans)
    {
        if (!owner)
            owner = _objectAccessor.FindConnectedPlayer(auction.Owner);

        // owner exist (online or offline)
        if ((owner || _characterCache.HasCharacterCacheEntry(auction.Owner))) // && !sAuctionBotConfig.IsBotChar(auction.Owner))
        {
            ByteBuffer tempBuffer = new();
            tempBuffer.WritePackedTime(GameTime.CurrentTime + _configuration.GetDefaultValue("MailDeliveryDelay", Time.HOUR));
            var eta = tempBuffer.ReadUInt32();

            new MailDraft(_auctionManager.BuildItemAuctionMailSubject(AuctionMailType.Invoice, auction),
                          _auctionManager.BuildAuctionInvoiceMailBody(auction.Bidder,
                                                                             auction.BidAmount,
                                                                             auction.BuyoutOrUnitPrice,
                                                                             (uint)auction.Deposit,
                                                                             CalculateAuctionHouseCut(auction.BidAmount),
                                                                             _configuration.GetDefaultValue("MailDeliveryDelay", Time.UHOUR),
                                                                             eta))
                .SendMailTo(trans, new MailReceiver(owner, auction.Owner), new MailSender(this), MailCheckMask.Copied);
        }
    }

    // this function notified old bidder that his bid is no longer highest
    public void SendAuctionOutbid(AuctionPosting auction, ObjectGuid newBidder, ulong newBidAmount, SQLTransaction trans)
    {
        var oldBidder = _objectAccessor.FindConnectedPlayer(auction.Bidder);

        // old bidder exist
        if ((oldBidder || _characterCache.HasCharacterCacheEntry(auction.Bidder))) // && !sAuctionBotConfig.IsBotChar(auction.Bidder))
        {
            if (oldBidder)
            {
                AuctionOutbidNotification packet = new()
                {
                    BidAmount = newBidAmount,
                    MinIncrement = AuctionPosting.CalculateMinIncrement(newBidAmount)
                };

                packet.Info.AuctionID = auction.Id;
                packet.Info.Bidder = newBidder;
                packet.Info.Item = new ItemInstance(auction.Items[0]);
                oldBidder.SendPacket(packet);
            }

            new MailDraft(_auctionManager.BuildItemAuctionMailSubject(AuctionMailType.Outbid, auction), "")
                .AddMoney(auction.BidAmount)
                .SendMailTo(trans, new MailReceiver(oldBidder, auction.Bidder), new MailSender(this), MailCheckMask.Copied);
        }
    }

    public void SendAuctionRemoved(AuctionPosting auction, Player owner, SQLTransaction trans)
    {
        var itemIndex = 0;

        while (itemIndex < auction.Items.Count)
        {
            MailDraft draft = new(_auctionManager.BuildItemAuctionMailSubject(AuctionMailType.Cancelled, auction), "");

            for (var i = 0; i < SharedConst.MaxMailItems && itemIndex < auction.Items.Count; ++i, ++itemIndex)
                draft.AddItem(auction.Items[itemIndex]);

            draft.SendMailTo(trans, owner, new MailSender(this), MailCheckMask.Copied);
        }
    }

    //call this method to send mail to auction owner, when auction is successful, it does not clear ram
    public void SendAuctionSold(AuctionPosting auction, Player owner, SQLTransaction trans)
    {
        if (!owner)
            owner = _objectAccessor.FindConnectedPlayer(auction.Owner);

        // owner exist
        if ((owner || _characterCache.HasCharacterCacheEntry(auction.Owner))) // && !sAuctionBotConfig.IsBotChar(auction.Owner))
        {
            var auctionHouseCut = CalculateAuctionHouseCut(auction.BidAmount);
            var profit = auction.BidAmount + auction.Deposit - auctionHouseCut;

            //FIXME: what do if owner offline
            if (owner)
            {
                owner.UpdateCriteria(CriteriaType.MoneyEarnedFromAuctions, profit);
                owner.UpdateCriteria(CriteriaType.HighestAuctionSale, auction.BidAmount);

                //send auction owner notification, bidder must be current!
                owner. //send auction owner notification, bidder must be current!
                    Session.SendAuctionClosedNotification(auction, _configuration.GetDefaultValue("MailDeliveryDelay", Time.FHOUR), true);
            }

            new MailDraft(_auctionManager.BuildItemAuctionMailSubject(AuctionMailType.Sold, auction),
                          _auctionManager.BuildAuctionSoldMailBody(auction.Bidder, auction.BidAmount, auction.BuyoutOrUnitPrice, (uint)auction.Deposit, auctionHouseCut))
                .AddMoney(profit)
                .SendMailTo(trans, new MailReceiver(owner, auction.Owner), new MailSender(this), MailCheckMask.Copied, _configuration.GetDefaultValue("MailDeliveryDelay", Time.UHOUR));
        }
    }

    public void SendAuctionWon(AuctionPosting auction, Player bidder, SQLTransaction trans)
    {
        uint bidderAccId;

        if (!bidder)
            bidder = _objectAccessor.FindConnectedPlayer(auction.Bidder); // try lookup bidder when called from .Update

        // data for gm.log
        var bidderName = "";
        var logGmTrade = auction.ServerFlags.HasFlag(AuctionPostingServerFlag.GmLogBuyer);

        if (bidder)
        {
            bidderAccId = bidder.Session.AccountId;
            bidderName = bidder.GetName();
        }
        else
        {
            bidderAccId = _characterCache.GetCharacterAccountIdByGuid(auction.Bidder);

            if (logGmTrade && !_characterCache.GetCharacterNameByGuid(auction.Bidder, out bidderName))
                bidderName = _objectManager.GetCypherString(CypherStrings.Unknown);
        }

        if (logGmTrade)
        {
            if (!_characterCache.GetCharacterNameByGuid(auction.Owner, out var ownerName))
                ownerName = _objectManager.GetCypherString(CypherStrings.Unknown);

            var ownerAccId = _characterCache.GetCharacterAccountIdByGuid(auction.Owner);

            Log.Logger.ForContext<GMCommands>().Information($"GM {bidderName} (Account: {bidderAccId}) won item in auction: {auction.Items[0].GetName(_worldManager.DefaultDbcLocale)} (Entry: {auction.Items[0].Entry}" +
                           $" Count: {auction.TotalItemCount}) and pay money: {auction.BidAmount}. Original owner {ownerName} (Account: {ownerAccId})");
        }

        // receiver exist
        if ((bidder != null || bidderAccId != 0)) // && !sAuctionBotConfig.IsBotChar(auction.Bidder))
        {
            MailDraft mail = new(_auctionManager.BuildItemAuctionMailSubject(AuctionMailType.Won, auction),
                                 _auctionManager.BuildAuctionWonMailBody(auction.Owner, auction.BidAmount, auction.BuyoutOrUnitPrice));

            // set owner to bidder (to prevent delete item with sender char deleting)
            // owner in `data` will set at mail receive and item extracting
            foreach (var item in auction.Items)
            {
                var stmt = _characterDatabase.GetPreparedStatement(CharStatements.UPD_ITEM_OWNER);
                stmt.AddValue(0, auction.Bidder.Counter);
                stmt.AddValue(1, item.GUID.Counter);
                trans.Append(stmt);

                mail.AddItem(item);
            }

            if (bidder != null)
            {
                AuctionWonNotification packet = new();
                packet.Info.Initialize(auction, auction.Items[0]);
                bidder.SendPacket(packet);

                // FIXME: for offline player need also
                bidder.UpdateCriteria(CriteriaType.AuctionsWon, 1);
            }

            mail.SendMailTo(trans, new MailReceiver(bidder, auction.Bidder), new MailSender(this), MailCheckMask.Copied);
        }
        else
        {
            // bidder doesn't exist, delete the item
            foreach (var item in auction.Items)
                _auctionManager.RemoveAItem(item.GUID, true, trans);
        }
    }

    public void Update()
    {
        var curTime = GameTime.SystemTime;
        var curTimeSteady = GameTime.Now;
        //- Handle expired auctions

        // Clear expired throttled players
        foreach (var key in _replicateThrottleMap.Keys.ToList())
            if (_replicateThrottleMap[key].NextAllowedReplication <= curTimeSteady)
                _replicateThrottleMap.Remove(key);

        foreach (var key in _commodityQuotes.Keys.ToList())
            if (_commodityQuotes[key].ValidTo < curTimeSteady)
                _commodityQuotes.Remove(key);

        if (_itemsByAuctionId.Empty())
            return;

        SQLTransaction trans = new();

        foreach (var auction in _itemsByAuctionId.Values.ToList())
        {
            //- filter auctions expired on next update
            if (auction.EndTime > curTime.AddMinutes(1))
                continue;

            //- Either cancel the auction if there was no bidder
            if (auction.Bidder.IsEmpty)
            {
                SendAuctionExpired(auction, trans);
                _scriptManager.ForEach<IAuctionHouseOnAuctionExpire>(p => p.OnAuctionExpire(this, auction));
            }
            //- Or perform the transaction
            else
            {
                //we should send an "item sold" message if the seller is online
                //we send the item to the winner
                //we send the money to the seller
                SendAuctionWon(auction, null, trans);
                SendAuctionSold(auction, null, trans);
                _scriptManager.ForEach<IAuctionHouseOnAuctionSuccessful>(p => p.OnAuctionSuccessful(this, auction));
            }

            //- In any case clear the auction
            RemoveAuction(trans, auction);
        }

        // Run DB changes
        _characterDatabase.CommitTransaction(trans);
    }

    private class MailedItemsBatch
    {
        public readonly Item[] Items = new Item[SharedConst.MaxMailItems];
        public int ItemsCount;
        public uint Quantity;
        public ulong TotalPrice;

        public void AddItem(Item item, ulong unitPrice)
        {
            Items[ItemsCount++] = item;
            Quantity += item.Count;
            TotalPrice += unitPrice * item.Count;
        }

        public bool IsFull()
        {
            return ItemsCount >= Items.Length;
        }
    }

    private class PlayerReplicateThrottleData
    {
        public uint Cursor;
        public uint Global;
        public DateTime NextAllowedReplication = DateTime.MinValue;
        public uint Tombstone;

        public bool IsReplicationInProgress()
        {
            return Cursor != Tombstone && Global != 0;
        }
    }
}