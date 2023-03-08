// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Numerics;
using Game.Collision;

namespace Game.Entities;

class GameObjectModelOwnerImpl : GameObjectModelOwnerBase
{
	readonly GameObject _owner;

	public GameObjectModelOwnerImpl(GameObject owner)
	{
		_owner = owner;
	}

	public override bool IsSpawned()
	{
		return _owner.IsSpawned();
	}

	public override uint GetDisplayId()
	{
		return _owner.GetDisplayId();
	}

	public override byte GetNameSetId()
	{
		return _owner.GetNameSetId();
	}

	public override bool IsInPhase(PhaseShift phaseShift)
	{
		return _owner.PhaseShift.CanSee(phaseShift);
	}

	public override Vector3 GetPosition()
	{
		return new Vector3(_owner.Location.X, _owner.Location.Y, _owner.Location.Z);
	}

	public override Quaternion GetRotation()
	{
		return new Quaternion(_owner.GetLocalRotation().X, _owner.GetLocalRotation().Y, _owner.GetLocalRotation().Z, _owner.GetLocalRotation().W);
	}

	public override float GetScale()
	{
		return _owner.ObjectScale;
	}
}