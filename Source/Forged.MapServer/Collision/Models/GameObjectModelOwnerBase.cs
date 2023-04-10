// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Numerics;
using Forged.MapServer.Phasing;

namespace Forged.MapServer.Collision.Models;

public abstract class GameObjectModelOwnerBase
{
    public abstract uint DisplayId { get; }

    public abstract bool IsSpawned { get; }
    public abstract byte NameSetId { get; }

    public abstract Vector3 Position { get; }

    public abstract Quaternion Rotation { get; }

    public abstract float Scale { get; }

    public abstract bool IsInPhase(PhaseShift phaseShift);
}