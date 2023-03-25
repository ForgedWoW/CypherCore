// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Maps;

public class RectangleBoundary : AreaBoundary
{
	private readonly float _minX;
	private readonly float _maxX;
	private readonly float _minY;

	private readonly float _maxY;

	// X axis is north/south, Y axis is east/west, larger values are northwest
	public RectangleBoundary(float southX, float northX, float eastY, float westY, bool isInverted = false) : base(isInverted)
	{
		_minX = southX;
		_maxX = northX;
		_minY = eastY;
		_maxY = westY;
	}

	public override bool IsWithinBoundaryArea(Position pos)
	{
		return !(pos.X < _minX || pos.X > _maxX || pos.Y < _minY || pos.Y > _maxY);
	}
}