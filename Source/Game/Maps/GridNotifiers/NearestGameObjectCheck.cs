using System.Collections.Generic;
using Game.Entities;

namespace Game.Maps;

class NearestGameObjectCheck : ICheck<GameObject>
{
    public NearestGameObjectCheck(WorldObject obj)
    {
        i_obj = obj;
        i_range = 999;
    }

    public bool Invoke(GameObject go)
    {
        if (i_obj.IsWithinDist(go, i_range))
        {
            i_range = i_obj.GetDistance(go);        // use found GO range as new range limit for next check
            return true;
        }
        return false;
    }

    readonly WorldObject i_obj;
    float i_range;
}