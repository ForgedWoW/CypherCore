// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;

namespace Forged.MapServer.Entities.Objects;

public class MovementForces
{
    private readonly List<MovementForce> _forces = new();

    public float ModMagnitude { get; set; } = 1.0f;

    public bool IsEmpty => _forces.Empty() && ModMagnitude == 1.0f;

    public List<MovementForce> GetForces()
    {
        return _forces;
    }

    public bool Add(MovementForce newForce)
    {
        var movementForce = FindMovementForce(newForce.ID);

        if (movementForce == null)
        {
            _forces.Add(newForce);

            return true;
        }

        return false;
    }

    public bool Remove(ObjectGuid id)
    {
        var movementForce = FindMovementForce(id);

        if (movementForce != null)
        {
            _forces.Remove(movementForce);

            return true;
        }

        return false;
    }

    private MovementForce FindMovementForce(ObjectGuid id)
    {
        return _forces.Find(force => force.ID == id);
    }
}