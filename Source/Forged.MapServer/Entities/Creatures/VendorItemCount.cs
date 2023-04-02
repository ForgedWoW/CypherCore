// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Chrono;

namespace Forged.MapServer.Entities.Creatures;

public class VendorItemCount
{
    public VendorItemCount(uint item, uint count)
    {
        ItemId = item;
        Count = count;
        LastIncrementTime = GameTime.CurrentTime;
    }

    public uint Count { get; set; }
    public uint ItemId { get; set; }
    public long LastIncrementTime { get; set; }
}