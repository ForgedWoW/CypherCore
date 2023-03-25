// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Maps;

public class ParallelogramBoundary : AreaBoundary
{
	private readonly DoublePosition _a;
	private readonly DoublePosition _b;
	private readonly DoublePosition _d;
	private readonly DoublePosition _c;
	private readonly double _abx;
	private readonly double _dax;
	private readonly double _aby;

	private readonly double _day;

	// Note: AB must be orthogonal to AD
	public ParallelogramBoundary(Position cornerA, Position cornerB, Position cornerD, bool isInverted = false) : base(isInverted)
	{
		_a = new DoublePosition(cornerA);
		_b = new DoublePosition(cornerB);
		_d = new DoublePosition(cornerD);
		_c = new DoublePosition(_d.GetDoublePositionX() + (_b.GetDoublePositionX() - _a.GetDoublePositionX()), _d.GetDoublePositionY() + (_b.GetDoublePositionY() - _a.GetDoublePositionY()));
		_abx = _b.GetDoublePositionX() - _a.GetDoublePositionX();
		_dax = _a.GetDoublePositionX() - _d.GetDoublePositionX();
		_aby = _b.GetDoublePositionY() - _a.GetDoublePositionY();
		_day = _a.GetDoublePositionY() - _d.GetDoublePositionY();
	}

	public override bool IsWithinBoundaryArea(Position pos)
	{
		// half-plane signs
		var sign1 = ((-_b.GetDoublePositionX() + pos.X) * _aby - (-_b.GetDoublePositionY() + pos.Y) * _abx) < 0;
		var sign2 = ((-_a.GetDoublePositionX() + pos.X) * _day - (-_a.GetDoublePositionY() + pos.Y) * _dax) < 0;
		var sign3 = ((-_d.GetDoublePositionY() + pos.Y) * _abx - (-_d.GetDoublePositionX() + pos.X) * _aby) < 0; // AB = -CD
		var sign4 = ((-_c.GetDoublePositionY() + pos.Y) * _dax - (-_c.GetDoublePositionX() + pos.X) * _day) < 0; // DA = -BC

		// if all signs are equal, the point is inside
		return ((sign1 == sign2) && (sign2 == sign3) && (sign3 == sign4));
	}
}