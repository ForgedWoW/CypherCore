// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using Forged.MapServer.Cache;
using Forged.MapServer.Chrono;
using Forged.MapServer.DataStorage;
using Forged.MapServer.DataStorage.Structs.A;
using Forged.MapServer.Entities.Items;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Globals;
using Forged.MapServer.Server;
using Framework.Constants;
using Framework.Database;
using Serilog;

namespace Forged.MapServer.AuctionHouse;

public class AuctionManager
{
    private readonly CharacterDatabase _characterDatabase;
    private readonly CliDB _cliDB;
    private readonly GameObjectManager _objectManager;
    private readonly CharacterCache _characterCache;
    const int MIN_AUCTION_TIME = 12 * Time.Hour;
	readonly AuctionHouseObject _hordeAuctions;
	readonly AuctionHouseObject _allianceAuctions;
	readonly AuctionHouseObject _neutralAuctions;
	readonly AuctionHouseObject _goblinAuctions;
	readonly Dictionary<ObjectGuid, PlayerPendingAuctions> _pendingAuctionsByPlayer = new();
	readonly Dictionary<ObjectGuid, Item> _itemsByGuid = new();
	readonly Dictionary<ObjectGuid, PlayerThrottleObject> _playerThrottleObjects = new();

	uint _replicateIdGenerator;
	DateTime _playerThrottleObjectsCleanupTime;

	public uint GenerateReplicationId => ++_replicateIdGenerator;

	public AuctionManager(CharacterDatabase characterDatabase, CliDB cliDB, GameObjectManager objectManager, CharacterCache characterCache)
	{
        _characterDatabase = characterDatabase;
        _cliDB = cliDB;
        _objectManager = objectManager;
        _characterCache = characterCache;
        _hordeAuctions = new AuctionHouseObject(6);
		_allianceAuctions = new AuctionHouseObject(2);
		_neutralAuctions = new AuctionHouseObject(1);
		_goblinAuctions = new AuctionHouseObject(7);
		_replicateIdGenerator = 0;
		_playerThrottleObjectsCleanupTime = GameTime.Now() + TimeSpan.FromHours(1);
	}

	public AuctionHouseObject GetAuctionsMap(uint factionTemplateId)
	{
		if (GetDefaultValue("AllowTwoSide.Interaction.Auction", true))
			return _neutralAuctions;

		// teams have linked auction houses
		var uEntry = _cliDB.FactionTemplateStorage.LookupByKey(factionTemplateId);

		if (uEntry == null)
			return _neutralAuctions;
		else if (uEntry.FactionGroup.HasAnyFlag((byte)FactionMasks.Alliance))
			return _allianceAuctions;
		else if (uEntry.FactionGroup.HasAnyFlag((byte)FactionMasks.Horde))
			return _hordeAuctions;
		else
			return _neutralAuctions;
	}

	public AuctionHouseObject GetAuctionsById(uint auctionHouseId)
	{
		switch (auctionHouseId)
		{
			case 1:
				return _neutralAuctions;
			case 2:
				return _allianceAuctions;
			case 6:
				return _hordeAuctions;
			case 7:
				return _goblinAuctions;
			default:
				break;
		}

		return _neutralAuctions;
	}

	public Item GetAItem(ObjectGuid itemGuid)
	{
		return _itemsByGuid.LookupByKey(itemGuid);
	}

	public ulong GetCommodityAuctionDeposit(ItemTemplate item, TimeSpan time, uint quantity)
	{
		var sellPrice = item.SellPrice;

		return (ulong)((Math.Ceiling(Math.Floor(Math.Max(0.15 * quantity * sellPrice, 100.0)) / MoneyConstants.Silver) * MoneyConstants.Silver) * (time.Minutes / (MIN_AUCTION_TIME / Time.Minute)));
	}

	public ulong GetItemAuctionDeposit(Player player, Item item, TimeSpan time)
	{
		var sellPrice = item.GetSellPrice(player);

		return (ulong)((Math.Ceiling(Math.Floor(Math.Max(sellPrice * 0.15, 100.0)) / MoneyConstants.Silver) * MoneyConstants.Silver) * (time.Minutes / (MIN_AUCTION_TIME / Time.Minute)));
	}

	public string BuildItemAuctionMailSubject(AuctionMailType type, AuctionPosting auction)
	{
		return BuildAuctionMailSubject(auction.Items[0].Entry,
										type,
										auction.Id,
										auction.TotalItemCount,
										auction.Items[0].GetModifier(ItemModifier.BattlePetSpeciesId),
										auction.Items[0].GetContext(),
										auction.Items[0].GetBonusListIDs());
	}

	public string BuildCommodityAuctionMailSubject(AuctionMailType type, uint itemId, uint itemCount)
	{
		return BuildAuctionMailSubject(itemId, type, 0, itemCount, 0, ItemContext.None, null);
	}

	public string BuildAuctionMailSubject(uint itemId, AuctionMailType type, uint auctionId, uint itemCount, uint battlePetSpeciesId, ItemContext context, List<uint> bonusListIds)
	{
		var str = $"{itemId}:0:{(uint)type}:{auctionId}:{itemCount}:{battlePetSpeciesId}:0:0:0:0:{(uint)context}:{bonusListIds.Count}";

		foreach (var bonusListId in bonusListIds)
			str += ':' + bonusListId;

		return str;
	}

	public string BuildAuctionWonMailBody(ObjectGuid guid, ulong bid, ulong buyout)
	{
		return $"{guid}:{bid}:{buyout}:0";
	}

	public string BuildAuctionSoldMailBody(ObjectGuid guid, ulong bid, ulong buyout, uint deposit, ulong consignment)
	{
		return $"{guid}:{bid}:{buyout}:{deposit}:{consignment}:0";
	}

	public string BuildAuctionInvoiceMailBody(ObjectGuid guid, ulong bid, ulong buyout, uint deposit, ulong consignment, uint moneyDelay, uint eta)
	{
		return $"{guid}:{bid}:{buyout}:{deposit}:{consignment}:{moneyDelay}:{eta}:0";
	}

	public void LoadAuctions()
	{
		var oldMSTime = Time.MSTime;

		// need to clear in case we are reloading
		_itemsByGuid.Clear();

		var result = _characterDatabase.Query(_characterDatabase.GetPreparedStatement(CharStatements.SEL_AUCTION_ITEMS));

		if (result.IsEmpty())
		{
			Log.Logger.Information("Loaded 0 auctions. DB table `auctionhouse` is empty.");

			return;
		}

		// data needs to be at first place for Item.LoadFromDB
		uint count = 0;
		MultiMap<uint, Item> itemsByAuction = new();
		MultiMap<uint, ObjectGuid> biddersByAuction = new();

		do
		{
			var itemGuid = result.Read<ulong>(0);
			var itemEntry = result.Read<uint>(1);

			var proto = _objectManager.GetItemTemplate(itemEntry);

			if (proto == null)
			{
				Log.Logger.Error($"AuctionHouseMgr.LoadAuctionItems: Unknown item (GUID: {itemGuid} item entry: #{itemEntry}) in auction, skipped.");

				continue;
			}

			var item = Item.NewItemOrBag(proto);

			if (!item.LoadFromDB(itemGuid, ObjectGuid.Create(HighGuid.Player, result.Read<ulong>(51)), result.GetFields(), itemEntry))
			{
				item.Dispose();

				continue;
			}

			var auctionId = result.Read<uint>(52);
			itemsByAuction.Add(auctionId, item);

			++count;
		} while (result.NextRow());

		Log.Logger.Information($"Loaded {count} auction items in {Time.GetMSTimeDiffToNow(oldMSTime)} ms");

		oldMSTime = Time.MSTime;
		count = 0;

		result = _characterDatabase.Query(_characterDatabase.GetPreparedStatement(CharStatements.SEL_AUCTION_BIDDERS));

		if (!result.IsEmpty())
			do
			{
				biddersByAuction.Add(result.Read<uint>(0), ObjectGuid.Create(HighGuid.Player, result.Read<ulong>(1)));
			} while (result.NextRow());

		Log.Logger.Information($"Loaded {count} auction bidders in {Time.GetMSTimeDiffToNow(oldMSTime)} ms");

		oldMSTime = Time.MSTime;
		count = 0;

		result = _characterDatabase.Query(_characterDatabase.GetPreparedStatement(CharStatements.SEL_AUCTIONS));

		if (!result.IsEmpty())
		{
			SQLTransaction trans = new();

			do
			{
				AuctionPosting auction = new()
				{
					Id = result.Read<uint>(0)
				};

				var auctionHouseId = result.Read<uint>(1);

				var auctionHouse = GetAuctionsById(auctionHouseId);

				if (auctionHouse == null)
				{
					Log.Logger.Error($"Auction {auction.Id} has wrong auctionHouseId {auctionHouseId}");
					var stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_AUCTION);
					stmt.AddValue(0, auction.Id);
					trans.Append(stmt);

					continue;
				}

				if (!itemsByAuction.ContainsKey(auction.Id))
				{
					Log.Logger.Error($"Auction {auction.Id} has no items");
					var stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_AUCTION);
					stmt.AddValue(0, auction.Id);
					trans.Append(stmt);

					continue;
				}

				auction.Items = itemsByAuction[auction.Id];
				auction.Owner = ObjectGuid.Create(HighGuid.Player, result.Read<ulong>(2));
				auction.OwnerAccount = ObjectGuid.Create(HighGuid.WowAccount, _characterCache.GetCharacterAccountIdByGuid(auction.Owner));
				var bidder = result.Read<ulong>(3);

				if (bidder != 0)
					auction.Bidder = ObjectGuid.Create(HighGuid.Player, bidder);

				auction.MinBid = result.Read<ulong>(4);
				auction.BuyoutOrUnitPrice = result.Read<ulong>(5);
				auction.Deposit = result.Read<ulong>(6);
				auction.BidAmount = result.Read<ulong>(7);
				auction.StartTime = Time.UnixTimeToDateTime(result.Read<long>(8));
				auction.EndTime = Time.UnixTimeToDateTime(result.Read<long>(9));
				auction.ServerFlags = (AuctionPostingServerFlag)result.Read<byte>(10);

				if (biddersByAuction.ContainsKey(auction.Id))
					auction.BidderHistory = biddersByAuction[auction.Id];

				auctionHouse.AddAuction(null, auction);

				++count;
			} while (result.NextRow());

			_characterDatabase.CommitTransaction(trans);
		}

		Log.Logger.Information($"Loaded {count} auctions in {Time.GetMSTimeDiffToNow(oldMSTime)} ms");
	}

	public void AddAItem(Item item)
	{
		if (item == null || _itemsByGuid.ContainsKey(item.GUID)) return;

		_itemsByGuid[item.GUID] = item;
	}

	public bool RemoveAItem(ObjectGuid guid, bool deleteItem = false, SQLTransaction trans = null)
	{
		var item = _itemsByGuid.LookupByKey(guid);

		if (item == null)
			return false;

		if (deleteItem)
		{
			item.FSetState(ItemUpdateState.Removed);
			item.SaveToDB(trans);
		}

		_itemsByGuid.Remove(guid);

		return true;
	}

	public bool PendingAuctionAdd(Player player, uint auctionHouseId, uint auctionId, ulong deposit)
	{
		var pendingAuction = _pendingAuctionsByPlayer.GetOrAdd(player.GUID, () => new PlayerPendingAuctions());
		// Get deposit so far
		ulong totalDeposit = 0;

		foreach (var thisAuction in pendingAuction.Auctions)
			totalDeposit += thisAuction.Deposit;

		// Add this deposit
		totalDeposit += deposit;

		if (!player.HasEnoughMoney(totalDeposit))
			return false;

		pendingAuction.Auctions.Add(new PendingAuctionInfo(auctionId, auctionHouseId, deposit));

		return true;
	}

	public int PendingAuctionCount(Player player)
	{
		var itr = _pendingAuctionsByPlayer.LookupByKey(player.GUID);

		if (itr != null)
			return itr.Auctions.Count;

		return 0;
	}

	public void PendingAuctionProcess(Player player)
	{
		var playerPendingAuctions = _pendingAuctionsByPlayer.LookupByKey(player.GUID);

		if (playerPendingAuctions == null)
			return;

		ulong totaldeposit = 0;
		var auctionIndex = 0;

		for (; auctionIndex < playerPendingAuctions.Auctions.Count; ++auctionIndex)
		{
			var pendingAuction = playerPendingAuctions.Auctions[auctionIndex];

			if (!player.HasEnoughMoney(totaldeposit + pendingAuction.Deposit))
				break;

			totaldeposit += pendingAuction.Deposit;
		}

		// expire auctions we cannot afford
		if (auctionIndex < playerPendingAuctions.Auctions.Count)
		{
			SQLTransaction trans = new();

			do
			{
				var pendingAuction = playerPendingAuctions.Auctions[auctionIndex];
				var auction = GetAuctionsById(pendingAuction.AuctionHouseId).GetAuction(pendingAuction.AuctionId);

				if (auction != null)
					auction.EndTime = GameTime.GetSystemTime();

				var stmt = _characterDatabase.GetPreparedStatement(CharStatements.UPD_AUCTION_EXPIRATION);
				stmt.AddValue(0, (uint)GameTime.GetGameTime());
				stmt.AddValue(1, pendingAuction.AuctionId);
				trans.Append(stmt);
				++auctionIndex;
			} while (auctionIndex < playerPendingAuctions.Auctions.Count);

			_characterDatabase.CommitTransaction(trans);
		}

		_pendingAuctionsByPlayer.Remove(player.GUID);
		player.ModifyMoney(-(long)totaldeposit);
	}

	public void UpdatePendingAuctions()
	{
		foreach (var pair in _pendingAuctionsByPlayer)
		{
			var playerGUID = pair.Key;
			var player = Global.ObjAccessor.FindConnectedPlayer(playerGUID);

			if (player != null)
			{
				// Check if there were auctions since last update process if not
				if (PendingAuctionCount(player) == pair.Value.LastAuctionsSize)
					PendingAuctionProcess(player);
				else
					_pendingAuctionsByPlayer[playerGUID].LastAuctionsSize = PendingAuctionCount(player);
			}
			else
			{
				// Expire any auctions that we couldn't get a deposit for
				Log.Logger.Warning($"Player {playerGUID} was offline, unable to retrieve deposit!");

				SQLTransaction trans = new();

				foreach (var pendingAuction in pair.Value.Auctions)
				{
					var auction = GetAuctionsById(pendingAuction.AuctionHouseId).GetAuction(pendingAuction.AuctionId);

					if (auction != null)
						auction.EndTime = GameTime.GetSystemTime();

					var stmt = _characterDatabase.GetPreparedStatement(CharStatements.UPD_AUCTION_EXPIRATION);
					stmt.AddValue(0, (uint)GameTime.GetGameTime());
					stmt.AddValue(1, pendingAuction.AuctionId);
					trans.Append(stmt);
				}

				_characterDatabase.CommitTransaction(trans);
				_pendingAuctionsByPlayer.Remove(playerGUID);
			}
		}
	}

	public void Update()
	{
		_hordeAuctions.Update();
		_allianceAuctions.Update();
		_neutralAuctions.Update();
		_goblinAuctions.Update();

		var now = GameTime.Now();

		if (now >= _playerThrottleObjectsCleanupTime)
		{
			foreach (var pair in _playerThrottleObjects.ToList())
				if (pair.Value.PeriodEnd < now)
					_playerThrottleObjects.Remove(pair.Key);

			_playerThrottleObjectsCleanupTime = now + TimeSpan.FromHours(1);
		}
	}

	public AuctionThrottleResult CheckThrottle(Player player, bool addonTainted, AuctionCommand command = AuctionCommand.SellItem)
	{
		var now = GameTime.Now();

		var throttleObject = _playerThrottleObjects.GetOrAdd(player.GUID, () => new PlayerThrottleObject());

		if (now > throttleObject.PeriodEnd)
		{
			throttleObject.PeriodEnd = now + TimeSpan.FromMinutes(1);
			throttleObject.QueriesRemaining = 100;
		}

		if (throttleObject.QueriesRemaining == 0)
		{
			player.Session.SendAuctionCommandResult(0, command, AuctionResult.AuctionHouseBusy, throttleObject.PeriodEnd - now);

			return new AuctionThrottleResult(TimeSpan.Zero, true);
		}

		if ((--throttleObject.QueriesRemaining) == 0)
			return new AuctionThrottleResult(throttleObject.PeriodEnd - now, false);
		else
			return new AuctionThrottleResult(TimeSpan.FromMilliseconds(GetDefaultValue(addonTainted ? "Auction.TaintedSearchDelay" : "Auction.SearchDelay")), false);

    }

	public AuctionHouseRecord GetAuctionHouseEntry(uint factionTemplateId)
	{
		uint houseId = 0;

		return GetAuctionHouseEntry(factionTemplateId, ref houseId);
	}

	public AuctionHouseRecord GetAuctionHouseEntry(uint factionTemplateId, ref uint houseId)
	{
		uint houseid = 1; // Auction House

		if (!GetDefaultValue("AllowTwoSide.Interaction.Auction", true))
			// FIXME: found way for proper auctionhouse selection by another way
			// AuctionHouse.dbc have faction field with _player_ factions associated with auction house races.
			// but no easy way convert creature faction to player race faction for specific city
			switch (factionTemplateId)
			{
				case 120:
					houseid = 7;

					break; // booty bay, Blackwater Auction House
				case 474:
					houseid = 7;

					break; // gadgetzan, Blackwater Auction House
				case 855:
					houseid = 7;

					break; // everlook, Blackwater Auction House
				default:   // default
				{
					var u_entry = CliDB.FactionTemplateStorage.LookupByKey(factionTemplateId);

					if (u_entry == null)
						houseid = 1; // Auction House
					else if ((u_entry.FactionGroup & (int)FactionMasks.Alliance) != 0)
						houseid = 2; // Alliance Auction House
					else if ((u_entry.FactionGroup & (int)FactionMasks.Horde) != 0)
						houseid = 6; // Horde Auction House
					else
						houseid = 1; // Auction House

					break;
				}
			}

		houseId = houseid;

		return _cliDB.AuctionHouseStorage.LookupByKey(houseid);
	}

	class PendingAuctionInfo
	{
		public readonly uint AuctionId;
		public readonly uint AuctionHouseId;
		public readonly ulong Deposit;

		public PendingAuctionInfo(uint auctionId, uint auctionHouseId, ulong deposit)
		{
			AuctionId = auctionId;
			AuctionHouseId = auctionHouseId;
			Deposit = deposit;
		}
	}

	class PlayerPendingAuctions
	{
		public readonly List<PendingAuctionInfo> Auctions = new();
		public int LastAuctionsSize;
	}

	class PlayerThrottleObject
	{
		public DateTime PeriodEnd;
		public byte QueriesRemaining = 100;
	}
}