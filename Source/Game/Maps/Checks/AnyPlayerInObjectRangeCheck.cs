using System.Collections.Generic;
using Game.Entities;

namespace Game.Maps;

public class AnyPlayerInObjectRangeCheck : ICheck<Player>
{
    public AnyPlayerInObjectRangeCheck(WorldObject obj, float range, bool reqAlive = true)
    {
        _obj = obj;
        _range = range;
        _reqAlive = reqAlive;
    }

    public bool Invoke(Player pl)
    {
        if (_reqAlive && !pl.IsAlive())
            return false;

        if (!_obj.IsWithinDist(pl, _range))
            return false;

        return true;
    }

    WorldObject _obj;
    float _range;
    bool _reqAlive;
}