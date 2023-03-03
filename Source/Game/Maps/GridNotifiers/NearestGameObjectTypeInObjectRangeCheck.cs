using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;

namespace Game.Maps;

class NearestGameObjectTypeInObjectRangeCheck : ICheck<GameObject>
{
    public NearestGameObjectTypeInObjectRangeCheck(WorldObject obj, GameObjectTypes type, float range)
    {
        i_obj = obj;
        i_type = type;
        i_range = range;
    }

    public bool Invoke(GameObject go)
    {
        if (go.GetGoType() == i_type && i_obj.IsWithinDist(go, i_range))
        {
            i_range = i_obj.GetDistance(go);        // use found GO range as new range limit for next check
            return true;
        }
        return false;
    }

    readonly WorldObject i_obj;
    readonly GameObjectTypes i_type;
    float i_range;
}