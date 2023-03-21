// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using System.Numerics;

namespace Forged.RealmServer.Collision;

public class WModelAreaCallback : WorkerCallback
{
	public GroupModel Hit;
	public float ZDist;

	readonly List<GroupModel> _prims;
	readonly Vector3 _zVec;

	public WModelAreaCallback(List<GroupModel> vals, Vector3 down)
	{
		_prims = vals;
		Hit = null;
		ZDist = float.PositiveInfinity;
		_zVec = down;
	}

	public override void Invoke(Vector3 point, int entry)
	{
		if (_prims[entry].IsInsideObject(point, _zVec, out var group_Z))
			if (group_Z < ZDist)
			{
				ZDist = group_Z;
				Hit = _prims[entry];
			}
	}
}