using System.Collections.Generic;
using Game.Entities;

namespace Game.Maps;

class GameObjectInRangeCheck : ICheck<GameObject>
{
    public GameObjectInRangeCheck(float _x, float _y, float _z, float _range, uint _entry = 0)
    {
        x = _x;
        y = _y;
        z = _z;
        range = _range;
        entry = _entry;
    }

    public bool Invoke(GameObject go)
    {
        if (entry == 0 || (go.GetGoInfo() != null && go.GetGoInfo().entry == entry))
            return go.IsInRange(x, y, z, range);
        else return false;
    }

    float x, y, z, range;
    uint entry;
}