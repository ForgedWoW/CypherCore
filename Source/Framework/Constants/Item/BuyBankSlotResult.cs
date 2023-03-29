// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum BuyBankSlotResult
{
    FailedTooMany = 0,
    InsufficientFunds = 1,
    NotBanker = 2,
    OK = 3
}