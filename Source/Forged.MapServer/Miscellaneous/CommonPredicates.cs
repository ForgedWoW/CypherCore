// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Units;

namespace Forged.MapServer.Miscellaneous;

/// Only returns true for the given attacker's current victim, if any
public class IsVictimOf : ICheck<WorldObject>
{
    private readonly WorldObject _victim;

    public IsVictimOf(Unit attacker)
    {
        _victim = attacker?.Victim;
    }

    public bool Invoke(WorldObject obj)
    {
        return obj != null && (_victim == obj);
    }
}