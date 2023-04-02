// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;

namespace Forged.MapServer.Entities.Objects;

public class MovementForces
{
    private readonly List<MovementForce> _forces = new();

    public bool IsEmpty => _forces.Empty() && ModMagnitude == 1.0f;
    public float ModMagnitude { get; set; } = 1.0f;
    public bool Add(MovementForce newForce)
    {
        var movementForce = FindMovementForce(newForce.ID);

        if (movementForce != null)
            return false;

        _forces.Add(newForce);

        return true;
    }

    public List<MovementForce> GetForces()
    {
        return _forces;
    }
    public bool Remove(ObjectGuid id)
    {
        var movementForce = FindMovementForce(id);

        if (movementForce == null)
            return false;

        _forces.Remove(movementForce);

        return true;
    }

    private MovementForce FindMovementForce(ObjectGuid id)
    {
        return _forces.Find(force => force.ID == id);
    }
}