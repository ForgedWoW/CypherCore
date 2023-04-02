// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.GameObjects;
using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Maps.Checks;

internal class AllGameObjectsWithEntryInRange : ICheck<GameObject>
{
    private readonly float _fRange;
    private readonly WorldObject _pObject;
    private readonly uint _uiEntry;
    public AllGameObjectsWithEntryInRange(WorldObject obj, uint entry, float maxRange)
    {
        _pObject = obj;
        _uiEntry = entry;
        _fRange = maxRange;
    }

    public bool Invoke(GameObject go)
    {
        if (_uiEntry == 0 || go.Entry == _uiEntry && _pObject.Location.IsWithinDist(go, _fRange, false))
            return true;

        return false;
    }
}