// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Maps;

public class TriangleBoundary : AreaBoundary
{
    private readonly DoublePosition _a;
    private readonly double _abx;
    private readonly double _aby;
    private readonly DoublePosition _b;
    private readonly double _bcx;
    private readonly double _bcy;
    private readonly DoublePosition _c;
    private readonly double _cax;
    private readonly double _cay;

    public TriangleBoundary(Position pointA, Position pointB, Position pointC, bool isInverted = false) : base(isInverted)
    {
        _a = new DoublePosition(pointA);
        _b = new DoublePosition(pointB);
        _c = new DoublePosition(pointC);

        _abx = _b.DoublePositionX - _a.DoublePositionX;
        _bcx = _c.DoublePositionX - _b.DoublePositionX;
        _cax = _a.DoublePositionX - _c.DoublePositionX;
        _aby = _b.DoublePositionY - _a.DoublePositionY;
        _bcy = _c.DoublePositionY - _b.DoublePositionY;
        _cay = _a.DoublePositionY - _c.DoublePositionY;
    }

    public override bool IsWithinBoundaryArea(Position pos)
    {
        // half-plane signs
        var sign1 = (-_b.DoublePositionX + pos.X) * _aby - (-_b.DoublePositionY + pos.Y) * _abx < 0;
        var sign2 = (-_c.DoublePositionX + pos.X) * _bcy - (-_c.DoublePositionY + pos.Y) * _bcx < 0;
        var sign3 = (-_a.DoublePositionX + pos.X) * _cay - (-_a.DoublePositionY + pos.Y) * _cax < 0;

        // if all signs are the same, the point is inside the triangle
        return sign1 == sign2 && sign2 == sign3;
    }
}