// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.RealmServer.Entities;

namespace Forged.RealmServer.Maps;

public class TriangleBoundary : AreaBoundary
{
	private readonly DoublePosition _a;
	private readonly DoublePosition _b;
	private readonly DoublePosition _c;
	private readonly double _abx;
	private readonly double _bcx;
	private readonly double _cax;
	private readonly double _aby;
	private readonly double _bcy;
	private readonly double _cay;

	public TriangleBoundary(Position pointA, Position pointB, Position pointC, bool isInverted = false) : base(isInverted)
	{
		_a = new DoublePosition(pointA);
		_b = new DoublePosition(pointB);
		_c = new DoublePosition(pointC);

		_abx = _b.GetDoublePositionX() - _a.GetDoublePositionX();
		_bcx = _c.GetDoublePositionX() - _b.GetDoublePositionX();
		_cax = _a.GetDoublePositionX() - _c.GetDoublePositionX();
		_aby = _b.GetDoublePositionY() - _a.GetDoublePositionY();
		_bcy = _c.GetDoublePositionY() - _b.GetDoublePositionY();
		_cay = _a.GetDoublePositionY() - _c.GetDoublePositionY();
	}

	public override bool IsWithinBoundaryArea(Position pos)
	{
		// half-plane signs
		var sign1 = ((-_b.GetDoublePositionX() + pos.X) * _aby - (-_b.GetDoublePositionY() + pos.Y) * _abx) < 0;
		var sign2 = ((-_c.GetDoublePositionX() + pos.X) * _bcy - (-_c.GetDoublePositionY() + pos.Y) * _bcx) < 0;
		var sign3 = ((-_a.GetDoublePositionX() + pos.X) * _cay - (-_a.GetDoublePositionY() + pos.Y) * _cax) < 0;

		// if all signs are the same, the point is inside the triangle
		return ((sign1 == sign2) && (sign2 == sign3));
	}
}