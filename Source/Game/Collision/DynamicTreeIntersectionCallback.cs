// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Framework.GameMath;

namespace Game.Collision;

public class DynamicTreeIntersectionCallback : WorkerCallback
{
	readonly PhaseShift _phaseShift;

	bool _didHit;

	public DynamicTreeIntersectionCallback(PhaseShift phaseShift)
	{
		_didHit = false;
		_phaseShift = phaseShift;
	}

	public override bool Invoke(Ray r, IModel obj, ref float distance)
	{
		_didHit = obj.IntersectRay(r, ref distance, true, _phaseShift, ModelIgnoreFlags.Nothing);

		return _didHit;
	}

	public bool DidHit()
	{
		return _didHit;
	}
}