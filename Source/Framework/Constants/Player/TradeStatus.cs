// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum TradeStatus
{
    PlayerBusy = 0,
    Proposed = 1,
    Initiated = 2,
    Cancelled = 3,
    Accepted = 4,
    AlreadyTrading = 5,
    NoTarget = 6,
    Unaccepted = 7,
    Complete = 8,
    StateChanged = 9,
    TooFarAway = 10,
    WrongFaction = 11,
    Failed = 12,
    Petition = 13,
    PlayerIgnored = 14,
    Stunned = 15,
    TargetStunned = 16,
    Dead = 17,
    TargetDead = 18,
    LoggingOut = 19,
    TargetLoggingOut = 20,
    RestrictedAccount = 21,
    WrongRealm = 22,
    NotOnTaplist = 23,
    CurrencyNotTradable = 24,
    NotEnoughCurrency = 25,
}