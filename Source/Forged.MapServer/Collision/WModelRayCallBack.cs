// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Collision.Models;
using Framework.GameMath;

namespace Forged.MapServer.Collision;

public class WModelRayCallBack : WorkerCallback
{
	public bool Hit;

	readonly List<GroupModel> _models;

	public WModelRayCallBack(List<GroupModel> mod)
	{
		_models = mod;
		Hit = false;
	}

	public override bool Invoke(Ray ray, int entry, ref float distance, bool pStopAtFirstHit)
	{
		var result = _models[entry].IntersectRay(ray, ref distance, pStopAtFirstHit);
		if (result) Hit = true;

		return Hit;
	}
}