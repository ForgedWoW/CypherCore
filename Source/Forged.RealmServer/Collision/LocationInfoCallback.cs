// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Numerics;

namespace Forged.RealmServer.Collision;

public class LocationInfoCallback : WorkerCallback
{
	public bool Result;

	readonly ModelInstance[] _prims;
	readonly LocationInfo _locInfo;

	public LocationInfoCallback(ModelInstance[] val, LocationInfo info)
	{
		_prims = val;
		_locInfo = info;
		Result = false;
	}

	public override void Invoke(Vector3 point, int entry)
	{
		if (_prims[entry] != null && _prims[entry].GetLocationInfo(point, _locInfo))
			Result = true;
	}
}