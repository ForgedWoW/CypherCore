// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Maps;

public class ZRangeBoundary : AreaBoundary
{
    private readonly float _maxZ;
    private readonly float _minZ;

    public ZRangeBoundary(float minZ, float maxZ, bool isInverted = false) : base(isInverted)
    {
        _minZ = minZ;
        _maxZ = maxZ;
    }

    public override bool IsWithinBoundaryArea(Position pos)
    {
        return _minZ <= pos.Z && pos.Z <= _maxZ;
    }
}