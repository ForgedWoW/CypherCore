using System.Collections.Generic;
using Game.Entities;

namespace Game.Maps;

class NearestGameObjectEntryInObjectRangeCheck : ICheck<GameObject>
{
    public NearestGameObjectEntryInObjectRangeCheck(WorldObject obj, uint entry, float range, bool spawnedOnly = true)
    {
        _obj = obj;
        _entry = entry;
        _range = range;
        _spawnedOnly = spawnedOnly;
    }

    public bool Invoke(GameObject go)
    {
        if ((!_spawnedOnly || go.IsSpawned()) && go.GetEntry() == _entry && go.GetGUID() != _obj.GetGUID() && _obj.IsWithinDist(go, _range))
        {
            _range = _obj.GetDistance(go);        // use found GO range as new range limit for next check
            return true;
        }
        return false;
    }

    WorldObject _obj;
    uint _entry;
    float _range;
    bool _spawnedOnly;
}