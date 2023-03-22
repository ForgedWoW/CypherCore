// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Framework.GameMath;

namespace Game.Collision;

public class MapRayCallback : WorkerCallback
{
	readonly ModelInstance[] _prims;
	readonly ModelIgnoreFlags _flags;
	bool _hit;

	public MapRayCallback(ModelInstance[] val, ModelIgnoreFlags ignoreFlags)
	{
		_prims = val;
		_hit = false;
		_flags = ignoreFlags;
	}

	public override bool Invoke(Ray ray, int entry, ref float distance, bool pStopAtFirstHit = true)
	{
		if (_prims[entry] == null)
			return false;

		var result = _prims[entry].IntersectRay(ray, ref distance, pStopAtFirstHit, _flags);

		if (result)
			_hit = true;

		return result;
	}

	public bool DidHit()
	{
		return _hit;
	}
}