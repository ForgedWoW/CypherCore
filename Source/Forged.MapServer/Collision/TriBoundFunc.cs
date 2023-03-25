// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using System.Numerics;
using Forged.MapServer.Collision.Models;
using Framework.GameMath;

namespace Forged.MapServer.Collision;

public class TriBoundFunc
{
	readonly List<Vector3> _vertices;

	public TriBoundFunc(List<Vector3> vert)
	{
		_vertices = vert;
	}

	public void Invoke(MeshTriangle tri, out AxisAlignedBox value)
	{
		var lo = _vertices[(int)tri.Idx0];
		var hi = lo;

		lo = Vector3.Min(Vector3.Min(lo, _vertices[(int)tri.Idx1]), _vertices[(int)tri.Idx2]);
		hi = Vector3.Max(Vector3.Max(hi, _vertices[(int)tri.Idx1]), _vertices[(int)tri.Idx2]);

		value = new AxisAlignedBox(lo, hi);
	}
}