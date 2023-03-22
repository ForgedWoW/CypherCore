﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Numerics;

namespace Game.Collision;

public class DynamicTreeAreaInfoCallback : WorkerCallback
{
	readonly PhaseShift _phaseShift;
	readonly AreaInfo _areaInfo;

	public DynamicTreeAreaInfoCallback(PhaseShift phaseShift)
	{
		_phaseShift = phaseShift;
		_areaInfo = new AreaInfo();
	}

	public override void Invoke(Vector3 p, GameObjectModel obj)
	{
		obj.IntersectPoint(p, _areaInfo, _phaseShift);
	}

	public AreaInfo GetAreaInfo()
	{
		return _areaInfo;
	}
}