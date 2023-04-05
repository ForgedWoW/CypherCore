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

    public override uint DisplayId => _owner.DisplayId;

    public override bool IsSpawned => _owner.IsSpawned;
    public override byte NameSetId => _owner.GetNameSetId();

    public override Vector3 Position => _owner.Location;

    public override Quaternion Rotation => _owner.LocalRotation;

    public override float Scale => _owner.ObjectScale;

    public override bool IsInPhase(PhaseShift phaseShift)
    {
        return _owner.Location.PhaseShift.CanSee(phaseShift);
    }
}