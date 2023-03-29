// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum SellResult
{
    CantFindItem = 1,          // The item was not found.
    CantSellItem = 2,          // The merchant doesn't want that item.
    CantFindVendor = 3,        // The merchant doesn't like you.
    YouDontOwnThatItem = 4,    // You don't own that item.
    Unk = 5,                   // Nothing Appears...
    OnlyEmptyBag = 6,          // You can only do that with empty bags.
    CantSellToThisMerchant = 7 // You cannot sell items to this merchant.
}