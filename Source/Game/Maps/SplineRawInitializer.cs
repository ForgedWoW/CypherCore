// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using System.Numerics;
using Game.Movement;

namespace Game.Maps;

public class SplineRawInitializer
{
	readonly List<Vector3> _points;

	public SplineRawInitializer(List<Vector3> points)
	{
		_points = points;
	}

	public void Initialize(ref EvaluationMode mode, ref bool cyclic, ref Vector3[] points, ref int lo, ref int hi)
	{
		mode   = EvaluationMode.Catmullrom;
		cyclic = false;
		points = new Vector3[_points.Count];

		for (var i = 0; i < _points.Count; ++i)
			points[i] = _points[i];

		lo = 1;
		hi = points.Length - 2;
	}
}