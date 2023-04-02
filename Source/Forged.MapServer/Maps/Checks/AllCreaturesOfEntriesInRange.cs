// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Maps.Checks;

public class AllCreaturesOfEntriesInRange : ICheck<Creature>
{
    private readonly float _fRange;
    private readonly WorldObject _pObject;
    private readonly uint[] _uiEntry;
    public AllCreaturesOfEntriesInRange(WorldObject obj, uint[] entry, float maxRange = 0f)
    {
        _pObject = obj;
        _uiEntry = entry;
        _fRange = maxRange;
    }

    public bool Invoke(Creature creature)
    {
        if (_uiEntry != null)
        {
            var match = false;

            foreach (var entry in _uiEntry)
                if (entry != 0 && creature.Entry == entry)
                    match = true;

            if (!match)
                return false;
        }

        if (_fRange != 0f)
        {
            if (_fRange > 0.0f && !_pObject.Location.IsWithinDist(creature, _fRange, false))
                return false;

            if (_fRange < 0.0f && _pObject.Location.IsWithinDist(creature, _fRange, false))
                return false;
        }

        return true;
    }
}