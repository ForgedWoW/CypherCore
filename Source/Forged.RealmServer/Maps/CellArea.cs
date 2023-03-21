// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.RealmServer.Maps.Grids;

namespace Forged.RealmServer.Maps;

public class CellArea
{
	public ICoord LowBound { get; set; }
	public ICoord HighBound { get; set; }

	public CellArea() { }

	public CellArea(CellCoord low, CellCoord high)
	{
		LowBound = low;
		HighBound = high;
	}
}