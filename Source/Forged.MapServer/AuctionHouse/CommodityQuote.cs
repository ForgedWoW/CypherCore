// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Forged.MapServer.AuctionHouse;

public class CommodityQuote
{
    public uint Quantity { get; set; }
    public ulong TotalPrice { get; set; }
    public DateTime ValidTo { get; set; } = DateTime.MinValue;
}