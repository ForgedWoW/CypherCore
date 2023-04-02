// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Numerics;
using Forged.MapServer.Phasing;

namespace Forged.MapServer.Collision.Models;

public abstract class GameObjectModelOwnerBase
{
    public abstract uint GetDisplayId();

    public abstract byte GetNameSetId();

    public abstract Vector3 GetPosition();

    public abstract Quaternion GetRotation();

    public abstract float GetScale();

    public abstract bool IsInPhase(PhaseShift phaseShift);

    public abstract bool IsSpawned();
}