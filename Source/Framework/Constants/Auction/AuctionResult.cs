// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum AuctionResult
{
	Ok = 0,
	Inventory = 1,
	DatabaseError = 2,
	NotEnoughMoney = 3,
	ItemNotFound = 4,
	HigherBid = 5,
	BidIncrement = 7,
	BidOwn = 10,
	RestrictedAccountTrial = 13,
	HasRestriction = 17,
	AuctionHouseBusy = 18,
	AuctionHouseUnavailable = 19,
	CommodityPurchaseFailed = 21,
	ItemHasQuote = 23
}