// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Maps;

public class ParallelogramBoundary : AreaBoundary
{
    private readonly DoublePosition _a;
    private readonly double _abx;
    private readonly double _aby;
    private readonly DoublePosition _b;
    private readonly DoublePosition _c;
    private readonly DoublePosition _d;
    private readonly double _dax;
    private readonly double _day;

    // Note: AB must be orthogonal to AD
    public ParallelogramBoundary(Position cornerA, Position cornerB, Position cornerD, bool isInverted = false) : base(isInverted)
    {
        _a = new DoublePosition(cornerA);
        _b = new DoublePosition(cornerB);
        _d = new DoublePosition(cornerD);
        _c = new DoublePosition(_d.DoublePositionX + (_b.DoublePositionX - _a.DoublePositionX), _d.DoublePositionY + (_b.DoublePositionY - _a.DoublePositionY));
        _abx = _b.DoublePositionX - _a.DoublePositionX;
        _dax = _a.DoublePositionX - _d.DoublePositionX;
        _aby = _b.DoublePositionY - _a.DoublePositionY;
        _day = _a.DoublePositionY - _d.DoublePositionY;
    }

    public override bool IsWithinBoundaryArea(Position pos)
    {
        // half-plane signs
        var sign1 = (-_b.DoublePositionX + pos.X) * _aby - (-_b.DoublePositionY + pos.Y) * _abx < 0;
        var sign2 = (-_a.DoublePositionX + pos.X) * _day - (-_a.DoublePositionY + pos.Y) * _dax < 0;
        var sign3 = (-_d.DoublePositionY + pos.Y) * _abx - (-_d.DoublePositionX + pos.X) * _aby < 0; // AB = -CD
        var sign4 = (-_c.DoublePositionY + pos.Y) * _dax - (-_c.DoublePositionX + pos.X) * _day < 0; // DA = -BC

        // if all signs are equal, the point is inside
        return sign1 == sign2 && sign2 == sign3 && sign3 == sign4;
    }
}