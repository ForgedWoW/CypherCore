// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Numerics;

namespace Game.Collision;

public class AreaInfoCallback : WorkerCallback
{
	public readonly AreaInfo AInfo = new();

	readonly ModelInstance[] _prims;

	public AreaInfoCallback(ModelInstance[] val)
	{
		_prims = val;
	}

	public override void Invoke(Vector3 point, int entry)
	{
		if (_prims[entry] == null)
			return;

		_prims[entry].IntersectPoint(point, AInfo);
	}
}