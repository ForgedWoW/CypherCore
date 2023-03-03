using System.Collections.Generic;
using Game.Entities;

namespace Game.Maps;

class NearestUnspawnedGameObjectEntryInObjectRangeCheck : ICheck<GameObject>
{
    WorldObject i_obj;
    uint i_entry;
    float i_range;

    public NearestUnspawnedGameObjectEntryInObjectRangeCheck(WorldObject obj, uint entry, float range)
    {
        i_obj = obj;
        i_entry = entry;
        i_range = range;
    }

    public bool Invoke(GameObject go)
    {
        if (!go.IsSpawned() && go.GetEntry() == i_entry && go.GetGUID() != i_obj.GetGUID() && i_obj.IsWithinDist(go, i_range))
        {
            i_range = i_obj.GetDistance(go);        // use found GO range as new range limit for next check
            return true;
        }
        return false;
    }
}