// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Numerics;
using Forged.MapServer.Collision.Models;
using Forged.MapServer.Phasing;

namespace Forged.MapServer.Entities.GameObjects;

internal class GameObjectModelOwnerImpl : GameObjectModelOwnerBase
{
    private readonly GameObject _owner;

    public GameObjectModelOwnerImpl(GameObject owner)
    {
        _owner = owner;
    }

    public override bool IsSpawned()
    {
        return _owner.IsSpawned;
    }

    public override uint GetDisplayId()
    {
        return _owner.DisplayId;
    }

    public override byte GetNameSetId()
    {
        return _owner.GetNameSetId();
    }

    public override bool IsInPhase(PhaseShift phaseShift)
    {
        return _owner.Location.PhaseShift.CanSee(phaseShift);
    }

    public override Vector3 GetPosition()
    {
        return new Vector3(_owner.Location.X, _owner.Location.Y, _owner.Location.Z);
    }

    public override Quaternion GetRotation()
    {
        return new Quaternion(_owner.LocalRotation.X, _owner.LocalRotation.Y, _owner.LocalRotation.Z, _owner.LocalRotation.W);
    }

    public override float GetScale()
    {
        return _owner.ObjectScale;
    }
}