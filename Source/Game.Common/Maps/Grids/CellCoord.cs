// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;

namespace Game.Common.Maps.Grids;

public class CellCoord : ICoord
{
	const int Limit = MapConst.TotalCellsPerMap;

	public uint X_Coord { get; set; }
	public uint Y_Coord { get; set; }

	public CellCoord(uint x, uint y)
	{
		X_Coord = x;
		Y_Coord = y;
	}

	public CellCoord(CellCoord obj)
	{
		X_Coord = obj.X_Coord;
		Y_Coord = obj.Y_Coord;
	}

	public bool IsCoordValid()
	{
		return X_Coord < Limit && Y_Coord < Limit;
	}

	public ICoord Normalize()
	{
		X_Coord = Math.Min(X_Coord, Limit - 1);
		Y_Coord = Math.Min(Y_Coord, Limit - 1);

		return this;
	}

	public uint GetId()
	{
		return Y_Coord * Limit + X_Coord;
	}

	public void Dec_x(uint val)
	{
		if (X_Coord > val)
			X_Coord -= val;
		else
			X_Coord = 0;
	}

	public void Inc_x(uint val)
	{
		if (X_Coord + val < Limit)
			X_Coord += val;
		else
			X_Coord = Limit - 1;
	}

	public void Dec_y(uint val)
	{
		if (Y_Coord > val)
			Y_Coord -= val;
		else
			Y_Coord = 0;
	}

	public void Inc_y(uint val)
	{
		if (Y_Coord + val < Limit)
			Y_Coord += val;
		else
			Y_Coord = Limit - 1;
	}

	public static bool operator ==(CellCoord p1, CellCoord p2)
	{
		return p1.X_Coord == p2.X_Coord && p1.Y_Coord == p2.Y_Coord;
	}

	public static bool operator !=(CellCoord p1, CellCoord p2)
	{
		return !(p1 == p2);
	}

	public override bool Equals(object obj)
	{
		if (obj is CellCoord)
			return (CellCoord)obj == this;

		return false;
	}

	public override int GetHashCode()
	{
		return X_Coord.GetHashCode() ^ Y_Coord.GetHashCode();
	}
}