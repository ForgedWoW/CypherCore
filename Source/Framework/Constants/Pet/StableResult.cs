// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum StableResult
{
    NotEnoughMoney = 1,     // "you don't have enough money"
    InvalidSlot = 3,        // "That slot is locked"
    StableSuccess = 8,      // stable success
    UnstableSuccess = 9,    // unstable/swap success
    BuySlotSuccess = 10,    // buy slot success
    CantControlExotic = 11, // "you are unable to control exotic creatures"
    InternalError = 12,     // "Internal pet error"
}