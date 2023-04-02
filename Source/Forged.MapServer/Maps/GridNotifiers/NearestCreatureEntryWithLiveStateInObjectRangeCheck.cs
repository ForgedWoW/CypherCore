// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Maps.GridNotifiers;

internal class NearestCreatureEntryWithLiveStateInObjectRangeCheck : ICheck<Creature>
{
    private readonly bool _alive;
    private readonly uint _entry;
    private readonly WorldObject _obj;
    private float _range;

    public NearestCreatureEntryWithLiveStateInObjectRangeCheck(WorldObject obj, uint entry, bool alive, float range)
    {
        _obj = obj;
        _entry = entry;
        _alive = alive;
        _range = range;
    }

    public bool Invoke(Creature u)
    {
        if (u.DeathState != DeathState.Dead && u.Entry == _entry && u.IsAlive == _alive && u.GUID != _obj.GUID && _obj.Location.IsWithinDist(u, _range) && u.Visibility.CheckPrivateObjectOwnerVisibility(_obj))
        {
            _range = _obj.Location.GetDistance(u); // use found unit range as new range limit for next check

            return true;
        }

        return false;
    }
}