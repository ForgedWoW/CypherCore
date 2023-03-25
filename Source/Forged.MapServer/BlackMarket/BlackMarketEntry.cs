// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Time;
using Framework.Constants;
using Framework.Database;

namespace Forged.MapServer.BlackMarket;

public class BlackMarketEntry
{
	uint _marketId;
	ulong _currentBid;
	uint _numBids;
	ulong _bidder;
	uint _secondsRemaining;
	bool _mailSent;


	public uint MarketId => _marketId;

	public ulong CurrentBid
	{
		get => _currentBid;
		private set => _currentBid = value;
	}

	public uint NumBids
	{
		get => _numBids;
		private set => _numBids = value;
	}

	public ulong Bidder
	{
		get => _bidder;
		private set => _bidder = value;
	}

	public ulong MinIncrement => (_currentBid / 20) - ((_currentBid / 20) % MoneyConstants.Gold);
	public bool MailSent => _mailSent;

	public void Initialize(uint marketId, uint duration)
	{
		_marketId = marketId;
		_secondsRemaining = duration;
	}

	public void Update(long newTimeOfUpdate)
	{
		_secondsRemaining = (uint)(_secondsRemaining - (newTimeOfUpdate - Global.BlackMarketMgr.LastUpdate));
	}

	public BlackMarketTemplate GetTemplate()
	{
		return Global.BlackMarketMgr.GetTemplateByID(_marketId);
	}

	public uint GetSecondsRemaining()
	{
		return (uint)(_secondsRemaining - (GameTime.GetGameTime() - Global.BlackMarketMgr.LastUpdate));
	}

	public bool IsCompleted()
	{
		return GetSecondsRemaining() <= 0;
	}

	public bool LoadFromDB(SQLFields fields)
	{
		_marketId = fields.Read<uint>(0);

		// Invalid MarketID
		var templ = Global.BlackMarketMgr.GetTemplateByID(_marketId);

		if (templ == null)
		{
			Log.Logger.Error("Black market auction {0} does not have a valid id.", _marketId);

			return false;
		}

		_currentBid = fields.Read<ulong>(1);
		_secondsRemaining = (uint)(fields.Read<long>(2) - Global.BlackMarketMgr.LastUpdate);
		_numBids = fields.Read<uint>(3);
		_bidder = fields.Read<ulong>(4);

		// Either no bidder or existing player
		if (_bidder != 0 && Global.CharacterCacheStorage.GetCharacterAccountIdByGuid(ObjectGuid.Create(HighGuid.Player, _bidder)) == 0) // Probably a better way to check if player exists
		{
			Log.Logger.Error("Black market auction {0} does not have a valid bidder (GUID: {1}).", _marketId, _bidder);

			return false;
		}

		return true;
	}

	public void SaveToDB(SQLTransaction trans)
	{
		var stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_BLACKMARKET_AUCTIONS);

		stmt.AddValue(0, _marketId);
		stmt.AddValue(1, _currentBid);
		stmt.AddValue(2, GetExpirationTime());
		stmt.AddValue(3, _numBids);
		stmt.AddValue(4, _bidder);

		trans.Append(stmt);
	}

	public void DeleteFromDB(SQLTransaction trans)
	{
		var stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_BLACKMARKET_AUCTIONS);
		stmt.AddValue(0, _marketId);
		trans.Append(stmt);
	}

	public bool ValidateBid(ulong bid)
	{
		if (bid <= _currentBid)
			return false;

		if (bid < _currentBid + MinIncrement)
			return false;

		if (bid >= BlackMarketConst.MaxBid)
			return false;

		return true;
	}

	public void PlaceBid(ulong bid, Player player, SQLTransaction trans) //Updated
	{
		if (bid < _currentBid)
			return;

		_currentBid = bid;
		++_numBids;

		if (GetSecondsRemaining() < 30 * global::Time.Minute)
			_secondsRemaining += 30 * global::Time.Minute;

		_bidder = player.GUID.Counter;

		player.ModifyMoney(-(long)bid);


		var stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_BLACKMARKET_AUCTIONS);

		stmt.AddValue(0, _currentBid);
		stmt.AddValue(1, GetExpirationTime());
		stmt.AddValue(2, _numBids);
		stmt.AddValue(3, _bidder);
		stmt.AddValue(4, _marketId);

		trans.Append(stmt);

		Global.BlackMarketMgr.Update(true);
	}

	public string BuildAuctionMailSubject(BMAHMailAuctionAnswers response)
	{
		return GetTemplate().Item.ItemID + ":0:" + response + ':' + MarketId + ':' + GetTemplate().Quantity;
	}

	public string BuildAuctionMailBody()
	{
		return GetTemplate().SellerNPC + ":" + _currentBid;
	}

	public void SetMailSent()
	{
		_mailSent = true;
	} // Set when mail has been sent

	long GetExpirationTime()
	{
		return GameTime.GetGameTime() + GetSecondsRemaining();
	}
}