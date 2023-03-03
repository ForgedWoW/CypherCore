using System.Collections.Generic;
using Game.Entities;

namespace Game.Maps;

class AnyPlayerInPositionRangeCheck : ICheck<Player>
{
    public AnyPlayerInPositionRangeCheck(Position pos, float range, bool reqAlive = true)
    {
        _pos = pos;
        _range = range;
        _reqAlive = reqAlive;
    }

    public bool Invoke(Player u)
    {
        if (_reqAlive && !u.IsAlive())
            return false;

        if (!u.IsWithinDist3d(_pos, _range))
            return false;

        return true;
    }

    readonly Position _pos;
    readonly float _range;
    readonly bool _reqAlive;
}