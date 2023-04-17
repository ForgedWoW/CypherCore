// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.GameObjects;

namespace Forged.MapServer.Maps.Checks;

internal class GameObjectInRangeCheck : ICheck<GameObject>
{
    private readonly uint _entry;
    private readonly float _x, _y, _z, _range;

    public GameObjectInRangeCheck(float x, float y, float z, float range, uint entry = 0)
    {
        _x = x;
        _y = y;
        _z = z;
        _range = range;
        _entry = entry;
    }

    public bool Invoke(GameObject go)
    {
        if (_entry == 0 || (go.Template != null && go.Template.entry == _entry))
            return go.IsInRange(_x, _y, _z, _range);

        return false;
    }
}