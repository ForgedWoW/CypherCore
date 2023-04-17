// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.GameObjects;
using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Maps.GridNotifiers;

internal class NearestUnspawnedGameObjectEntryInObjectRangeCheck : ICheck<GameObject>
{
    private readonly uint _entry;
    private readonly WorldObject _obj;
    private float _range;

    public NearestUnspawnedGameObjectEntryInObjectRangeCheck(WorldObject obj, uint entry, float range)
    {
        _obj = obj;
        _entry = entry;
        _range = range;
    }

    public bool Invoke(GameObject go)
    {
        if (go.IsSpawned || go.Entry != _entry || go.GUID == _obj.GUID || !_obj.Location.IsWithinDist(go, _range))
            return false;

        _range = _obj.Location.GetDistance(go); // use found GO range as new range limit for next check

        return true;
    }
}