// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Numerics;

namespace Forged.RealmServer.Collision;

public abstract class GameObjectModelOwnerBase
{
	public abstract bool IsSpawned();
	public abstract uint GetDisplayId();
	public abstract byte GetNameSetId();
	public abstract bool IsInPhase(PhaseShift phaseShift);
	public abstract Vector3 GetPosition();
	public abstract Quaternion GetRotation();
	public abstract float GetScale();
}