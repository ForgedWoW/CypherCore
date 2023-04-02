// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Maps.Grids;

namespace Forged.MapServer.Maps;

public class CellArea
{
    public CellArea() { }

    public CellArea(CellCoord low, CellCoord high)
    {
        LowBound = low;
        HighBound = high;
    }

    public ICoord HighBound { get; set; }
    public ICoord LowBound { get; set; }
}