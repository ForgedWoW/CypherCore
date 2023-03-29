// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Forged.MapServer.AuctionHouse;

public class CommodityQuote
{
    public ulong TotalPrice;
    public uint Quantity;
    public DateTime ValidTo = DateTime.MinValue;
}