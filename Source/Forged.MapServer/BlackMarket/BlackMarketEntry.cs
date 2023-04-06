// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Cache;
using Forged.MapServer.Chrono;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Framework.Constants;
using Framework.Database;
using Serilog;

namespace Forged.MapServer.BlackMarket;

public class BlackMarketEntry
{
    private readonly CharacterDatabase _characterDatabase;
    private readonly BlackMarketManager _blackMarketManager;
    private readonly CharacterCache _characterCache;
    private uint _secondsRemaining;
    
    public ulong Bidder { get; private set; }
    public ulong CurrentBid { get; private set; }
    public bool MailSent { get; private set; }
    public uint MarketId { get; private set; }
    public ulong MinIncrement => CurrentBid / 20 - (CurrentBid / 20) % MoneyConstants.Gold;
    public uint NumBids { get; private set; }

    public BlackMarketEntry(CharacterDatabase characterDatabase, BlackMarketManager blackMarketManager, CharacterCache characterCache)
    {
        _characterDatabase = characterDatabase;
        _blackMarketManager = blackMarketManager;
        _characterCache = characterCache;
    }

    public string BuildAuctionMailBody()
    {
        return GetTemplate().SellerNPC + ":" + CurrentBid;
    }

    public string BuildAuctionMailSubject(BMAHMailAuctionAnswers response)
    {
        return GetTemplate().Item.ItemID + ":0:" + response + ':' + MarketId + ':' + GetTemplate().Quantity;
    }

    public void DeleteFromDB(SQLTransaction trans)
    {
        var stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_BLACKMARKET_AUCTIONS);
        stmt.AddValue(0, MarketId);
        trans.Append(stmt);
    }

    public uint GetSecondsRemaining()
    {
        return (uint)(_secondsRemaining - (GameTime.CurrentTime - _blackMarketManager.LastUpdate));
    }

    public BlackMarketTemplate GetTemplate()
    {
        return _blackMarketManager.GetTemplateByID(MarketId);
    }

    public void Initialize(uint marketId, uint duration)
    {
        MarketId = marketId;
        _secondsRemaining = duration;
    }

    public bool IsCompleted()
    {
        return GetSecondsRemaining() <= 0;
    }

    public bool LoadFromDB(SQLFields fields)
    {
        MarketId = fields.Read<uint>(0);

        // Invalid MarketID
        var templ = _blackMarketManager.GetTemplateByID(MarketId);

        if (templ == null)
        {
            Log.Logger.Error("Black market auction {0} does not have a valid id.", MarketId);

            return false;
        }

        CurrentBid = fields.Read<ulong>(1);
        _secondsRemaining = (uint)(fields.Read<long>(2) - _blackMarketManager.LastUpdate);
        NumBids = fields.Read<uint>(3);
        Bidder = fields.Read<ulong>(4);

        // Either no bidder or existing player
        if (Bidder == 0 || _characterCache.GetCharacterAccountIdByGuid(ObjectGuid.Create(HighGuid.Player, Bidder)) != 0) // Probably a better way to check if player exists
            return true;

        Log.Logger.Error("Black market auction {0} does not have a valid bidder (GUID: {1}).", MarketId, Bidder);

        return false;

    }

    public void PlaceBid(ulong bid, Player player, SQLTransaction trans) //Updated
    {
        if (bid < CurrentBid)
            return;

        CurrentBid = bid;
        ++NumBids;

        if (GetSecondsRemaining() < 30 * Time.MINUTE)
            _secondsRemaining += 30 * Time.MINUTE;

        Bidder = player.GUID.Counter;

        player.ModifyMoney(-(long)bid);


        var stmt = _characterDatabase.GetPreparedStatement(CharStatements.UPD_BLACKMARKET_AUCTIONS);

        stmt.AddValue(0, CurrentBid);
        stmt.AddValue(1, GetExpirationTime());
        stmt.AddValue(2, NumBids);
        stmt.AddValue(3, Bidder);
        stmt.AddValue(4, MarketId);

        trans.Append(stmt);

        _blackMarketManager.Update(true);
    }

    public void SaveToDB(SQLTransaction trans)
    {
        var stmt = _characterDatabase.GetPreparedStatement(CharStatements.INS_BLACKMARKET_AUCTIONS);

        stmt.AddValue(0, MarketId);
        stmt.AddValue(1, CurrentBid);
        stmt.AddValue(2, GetExpirationTime());
        stmt.AddValue(3, NumBids);
        stmt.AddValue(4, Bidder);

        trans.Append(stmt);
    }

    public void SetMailSent()
    {
        MailSent = true;
    }

    public void Update(long newTimeOfUpdate)
    {
        _secondsRemaining = (uint)(_secondsRemaining - (newTimeOfUpdate - _blackMarketManager.LastUpdate));
    }
    public bool ValidateBid(ulong bid)
    {
        if (bid <= CurrentBid)
            return false;

        if (bid < CurrentBid + MinIncrement)
            return false;

        return bid < BlackMarketConst.MaxBid;
    }
     // Set when mail has been sent

    private long GetExpirationTime()
    {
        return GameTime.CurrentTime + GetSecondsRemaining();
    }
}