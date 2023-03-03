using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;

namespace Game.Maps;

class NearestGameObjectFishingHole : ICheck<GameObject>
{
    public NearestGameObjectFishingHole(WorldObject obj, float range)
    {
        i_obj = obj;
        i_range = range;
    }

    public bool Invoke(GameObject go)
    {
        if (go.GetGoInfo().type == GameObjectTypes.FishingHole && go.IsSpawned() && i_obj.IsWithinDist(go, i_range) && i_obj.IsWithinDist(go, go.GetGoInfo().FishingHole.radius))
        {
            i_range = i_obj.GetDistance(go);
            return true;
        }
        return false;
    }

    readonly WorldObject i_obj;
    float i_range;
}