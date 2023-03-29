// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum BlackMarketError
{
    Ok = 0,
    ItemNotFound = 1,
    AlreadyBid = 2,
    HigherBid = 4,
    DatabaseError = 6,
    NotEnoughMoney = 7,
    RestrictedAccountTrial = 9
}