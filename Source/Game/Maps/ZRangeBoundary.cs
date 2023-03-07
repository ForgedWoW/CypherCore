// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;

namespace Game.Maps;

public class ZRangeBoundary : AreaBoundary
{
	private readonly float _minZ;
	private readonly float _maxZ;

	public ZRangeBoundary(float minZ, float maxZ, bool isInverted = false) : base(isInverted)
	{
		_minZ = minZ;
		_maxZ = maxZ;
	}

	public override bool IsWithinBoundaryArea(Position pos)
	{
		return (_minZ <= pos.Z && pos.Z <= _maxZ);
	}
}