// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Maps.Checks;

public class AllCreaturesWithinRange : ICheck<Creature>
{
    private readonly float _fRange;
    private readonly WorldObject _pObject;

    public AllCreaturesWithinRange(WorldObject obj, float maxRange = 0f)
    {
        _pObject = obj;
        _fRange = maxRange;
    }

    public bool Invoke(Creature creature)
    {
        if (_fRange == 0f)
            return true;

        if (_fRange > 0.0f && !_pObject.Location.IsWithinDist(creature, _fRange, false))
            return false;

        return !(_fRange < 0.0f) || !_pObject.Location.IsWithinDist(creature, _fRange, false);
    }
}