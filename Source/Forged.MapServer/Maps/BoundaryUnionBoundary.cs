// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Maps;

internal class BoundaryUnionBoundary : AreaBoundary
{
	private readonly AreaBoundary _b1;
	private readonly AreaBoundary _b2;

	public BoundaryUnionBoundary(AreaBoundary b1, AreaBoundary b2, bool isInverted = false) : base(isInverted)
	{
		_b1 = b1;
		_b2 = b2;
	}

	public override bool IsWithinBoundaryArea(Position pos)
	{
		return _b1.IsWithinBoundary(pos) || _b2.IsWithinBoundary(pos);
	}
}