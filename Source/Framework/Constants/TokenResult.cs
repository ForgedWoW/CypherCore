// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum TokenResult
{
    Success = 1,
    Disabled = 2,
    Other = 3,
    NoneForSale = 4,
    TooManyTokens = 5,
    SuccessNo = 6,
    TransactionInProgress = 7,
    AuctionableTokenOwned = 8,
    TrialRestricted = 9
}