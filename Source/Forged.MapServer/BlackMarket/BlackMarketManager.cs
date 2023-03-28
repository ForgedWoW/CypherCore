// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Accounts;
using Forged.MapServer.Cache;
using Forged.MapServer.Chat.Commands;
using Forged.MapServer.Chrono;
using Forged.MapServer.Entities.Items;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Globals;
using Forged.MapServer.Mails;
using Forged.MapServer.Networking.Packets.BlackMarket;
using Forged.MapServer.World;
using Framework.Constants;
using Framework.Database;
using Framework.Util;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Forged.MapServer.BlackMarket;

public class BlackMarketManager
{
    private readonly IConfiguration _configuration;
    private readonly WorldDatabase _worldDatabase;
    private readonly CharacterDatabase _characterDatabase;
    private readonly ObjectAccessor _objectAccessor;
    private readonly WorldManager _worldManager;
    private readonly GameObjectManager _objectManager;
    private readonly CharacterCache _characterCache;
    private readonly AccountManager _accountManager;
    private readonly Dictionary<uint, BlackMarketEntry> _auctions = new();
    private readonly Dictionary<uint, BlackMarketTemplate> _templates = new();
    private long _lastUpdate;


	public bool IsEnabled => _configuration.GetDefaultValue("BlackMarket.Enabled", true);
	public long LastUpdate => _lastUpdate;

    public BlackMarketManager(IConfiguration configuration, WorldDatabase worldDatabase, CharacterDatabase characterDatabase,
                              ObjectAccessor objectAccessor, WorldManager worldManager, GameObjectManager objectManager,
                              CharacterCache characterCache, AccountManager accountManager)
    {
        _configuration = configuration;
        _worldDatabase = worldDatabase;
        _characterDatabase = characterDatabase;
        _objectAccessor = objectAccessor;
        _worldManager = worldManager;
        _objectManager = objectManager;
        _characterCache = characterCache;
        _accountManager = accountManager;
    }

	public void LoadTemplates()
	{
		var oldMSTime = Time.MSTime;

		// Clear in case we are reloading
		_templates.Clear();

		var result = _worldDatabase.Query("SELECT marketId, sellerNpc, itemEntry, quantity, minBid, duration, chance, bonusListIDs FROM blackmarket_template");

		if (result.IsEmpty())
		{
			Log.Logger.Information("Loaded 0 black market templates. DB table `blackmarket_template` is empty.");

			return;
		}

		do
		{
			BlackMarketTemplate templ = new();

			if (!templ.LoadFromDB(result.GetFields())) // Add checks
				continue;

			AddTemplate(templ);
		} while (result.NextRow());

		Log.Logger.Information("Loaded {0} black market templates in {1} ms.", _templates.Count, Time.GetMSTimeDiffToNow(oldMSTime));
	}

	public void LoadAuctions()
	{
		var oldMSTime = Time.MSTime;

		// Clear in case we are reloading
		_auctions.Clear();

		var stmt = _characterDatabase.GetPreparedStatement(CharStatements.SEL_BLACKMARKET_AUCTIONS);
		var result = _characterDatabase.Query(stmt);

		if (result.IsEmpty())
		{
			Log.Logger.Information("Loaded 0 black market auctions. DB table `blackmarket_auctions` is empty.");

			return;
		}

		_lastUpdate = GameTime.GetGameTime(); //Set update time before loading

		SQLTransaction trans = new();

		do
		{
			BlackMarketEntry auction = new();

			if (!auction.LoadFromDB(result.GetFields()))
			{
				auction.DeleteFromDB(trans);

				continue;
			}

			if (auction.IsCompleted())
			{
				auction.DeleteFromDB(trans);

				continue;
			}

			AddAuction(auction);
		} while (result.NextRow());

		_characterDatabase.CommitTransaction(trans);

		Log.Logger.Information("Loaded {0} black market auctions in {1} ms.", _auctions.Count, Time.GetMSTimeDiffToNow(oldMSTime));
	}

	public void Update(bool updateTime = false)
	{
		SQLTransaction trans = new();
		var now = GameTime.GetGameTime();

		foreach (var entry in _auctions.Values)
		{
			if (entry.IsCompleted() && entry.Bidder != 0)
				SendAuctionWonMail(entry, trans);

			if (updateTime)
				entry.Update(now);
		}

		if (updateTime)
			_lastUpdate = now;

		_characterDatabase.CommitTransaction(trans);
	}

	public void RefreshAuctions()
	{
		SQLTransaction trans = new();

		// Delete completed auctions
		foreach (var pair in _auctions)
		{
			if (!pair.Value.IsCompleted())
				continue;

			pair.Value.DeleteFromDB(trans);
			_auctions.Remove(pair.Key);
		}

		_characterDatabase.CommitTransaction(trans);
		trans = new SQLTransaction();

		List<BlackMarketTemplate> templates = new();

		foreach (var pair in _templates)
		{
			if (GetAuctionByID(pair.Value.MarketID) != null)
				continue;

			if (!RandomHelper.randChance(pair.Value.Chance))
				continue;

			templates.Add(pair.Value);
		}

		templates.RandomResize(_configuration.GetDefaultValue("BlackMarket.MaxAuctions", 12));

		foreach (var templat in templates)
		{
			BlackMarketEntry entry = new();
			entry.Initialize(templat.MarketID, (uint)templat.Duration);
			entry.SaveToDB(trans);
			AddAuction(entry);
		}

		_characterDatabase.CommitTransaction(trans);

		Update(true);
	}


	public void BuildItemsResponse(BlackMarketRequestItemsResult packet, Player player)
	{
		packet.LastUpdateID = (int)_lastUpdate;

		foreach (var pair in _auctions)
		{
			var templ = pair.Value.GetTemplate();

			BlackMarketItem item = new()
			{
				MarketID = pair.Value.MarketId,
				SellerNPC = templ.SellerNPC,
				Item = templ.Item,
				Quantity = templ.Quantity
			};

			// No bids yet
			if (pair.Value.NumBids == 0)
			{
				item.MinBid = templ.MinBid;
				item.MinIncrement = 1;
			}
			else
			{
				item.MinIncrement = pair.Value.MinIncrement; // 5% increment minimum
				item.MinBid = pair.Value.CurrentBid + item.MinIncrement;
			}

			item.CurrentBid = pair.Value.CurrentBid;
			item.SecondsRemaining = pair.Value.GetSecondsRemaining();
			item.HighBid = (pair.Value.Bidder == player.GUID.Counter);
			item.NumBids = pair.Value.NumBids;

			packet.Items.Add(item);
		}
	}

	public void AddAuction(BlackMarketEntry auction)
	{
		_auctions[auction.MarketId] = auction;
	}

	public void AddTemplate(BlackMarketTemplate templ)
	{
		_templates[templ.MarketID] = templ;
	}

	public void SendAuctionWonMail(BlackMarketEntry entry, SQLTransaction trans)
	{
		// Mail already sent
		if (entry.MailSent)
			return;

		uint bidderAccId;
		var bidderGuid = ObjectGuid.Create(HighGuid.Player, entry.Bidder);
		var bidder = _objectAccessor.FindConnectedPlayer(bidderGuid);
		// data for gm.log
		var bidderName = "";
		bool logGmTrade;

		if (bidder)
		{
			bidderAccId = bidder.Session.AccountId;
			bidderName = bidder.GetName();
			logGmTrade = bidder.Session.HasPermission(RBACPermissions.LogGmTrade);
		}
		else
		{
			bidderAccId = _characterCache.GetCharacterAccountIdByGuid(bidderGuid);

			if (bidderAccId == 0) // Account exists
				return;

			logGmTrade = _accountManager.HasPermission(bidderAccId, RBACPermissions.LogGmTrade, _worldManager.RealmId.Index);

			if (logGmTrade && !_characterCache.GetCharacterNameByGuid(bidderGuid, out bidderName))
				bidderName = _objectManager.GetCypherString(CypherStrings.Unknown);
		}

		// Create item
		var templ = entry.GetTemplate();
		var item = Item.CreateItem(templ.Item.ItemID, templ.Quantity, ItemContext.BlackMarket);

		if (!item)
			return;

		if (templ.Item.ItemBonus != null)
			foreach (var bonusList in templ.Item.ItemBonus.BonusListIDs)
				item.AddBonuses(bonusList);

		item.SetOwnerGUID(bidderGuid);

		item.SaveToDB(trans);

		// Log trade
		if (logGmTrade)
            Log.Logger.ForContext<GMCommands>().Information(
                                            "GM {0} (Account: {1}) won item in blackmarket auction: {2} (Entry: {3} Count: {4}) and payed gold : {5}.",
                                            bidderName,
                                            bidderAccId,
                                            item.Template.GetName(),
                                            item.Entry,
                                            item.Count,
                                            entry.CurrentBid / MoneyConstants.Gold);

		if (bidder)
			bidder.Session.SendBlackMarketWonNotification(entry, item);

		new MailDraft(entry.BuildAuctionMailSubject(BMAHMailAuctionAnswers.Won), entry.BuildAuctionMailBody())
			.AddItem(item)
			.SendMailTo(trans, new MailReceiver(bidder, entry.Bidder), new MailSender(entry), MailCheckMask.Copied);

		entry.SetMailSent();
	}

	public void SendAuctionOutbidMail(BlackMarketEntry entry, SQLTransaction trans)
	{
		var oldBidderGuid = ObjectGuid.Create(HighGuid.Player, entry.Bidder);
		var oldBidder = _objectAccessor.FindConnectedPlayer(oldBidderGuid);

		uint oldBidderAccId = 0;

		if (!oldBidder)
			oldBidderAccId = _characterCache.GetCharacterAccountIdByGuid(oldBidderGuid);

		// old bidder exist
		if (!oldBidder && oldBidderAccId == 0)
			return;

		if (oldBidder)
			oldBidder.Session.SendBlackMarketOutbidNotification(entry.GetTemplate());

		new MailDraft(entry.BuildAuctionMailSubject(BMAHMailAuctionAnswers.Outbid), entry.BuildAuctionMailBody())
			.AddMoney(entry.CurrentBid)
			.SendMailTo(trans, new MailReceiver(oldBidder, entry.Bidder), new MailSender(entry), MailCheckMask.Copied);
	}

	public BlackMarketEntry GetAuctionByID(uint marketId)
	{
		return _auctions.LookupByKey(marketId);
	}

	public BlackMarketTemplate GetTemplateByID(uint marketId)
	{
		return _templates.LookupByKey(marketId);
	}
}