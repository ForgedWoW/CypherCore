// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Units;

namespace Forged.MapServer.Movement;

public class AbstractFollower
{
    public AbstractFollower(Unit target = null)
    {
        SetTarget(target);
    }

    public Unit Target { get; private set; }

    public void SetTarget(Unit unit)
    {
        if (unit == Target)
            return;

        Target?.FollowerRemoved(this);

        Target = unit;

        Target?.FollowerAdded(this);
    }
}