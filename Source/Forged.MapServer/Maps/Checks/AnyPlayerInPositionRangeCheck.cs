// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;

namespace Forged.MapServer.Maps.Checks;

internal class AnyPlayerInPositionRangeCheck : ICheck<Player>
{
    private readonly Position _pos;
    private readonly float _range;
    private readonly bool _reqAlive;

    public AnyPlayerInPositionRangeCheck(Position pos, float range, bool reqAlive = true)
    {
        _pos = pos;
        _range = range;
        _reqAlive = reqAlive;
    }

    public bool Invoke(Player u)
    {
        if (_reqAlive && !u.IsAlive)
            return false;

        if (!u.Location.IsWithinDist3d(_pos, _range))
            return false;

        return true;
    }
}