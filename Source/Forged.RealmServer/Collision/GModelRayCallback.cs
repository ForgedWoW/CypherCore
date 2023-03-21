// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Numerics;
using Framework.GameMath;

namespace Forged.RealmServer.Collision;

public class GModelRayCallback : WorkerCallback
{
	public bool Hit;

	readonly List<Vector3> _vertices;
	readonly List<MeshTriangle> _triangles;

	public GModelRayCallback(List<MeshTriangle> tris, List<Vector3> vert)
	{
		_vertices = vert;
		_triangles = tris;
		Hit = false;
	}

	public override bool Invoke(Ray ray, int entry, ref float distance, bool pStopAtFirstHit)
	{
		Hit = IntersectTriangle(_triangles[entry], _vertices, ray, ref distance) || Hit;

		return Hit;
	}

	bool IntersectTriangle(MeshTriangle tri, List<Vector3> points, Ray ray, ref float distance)
	{
		const float eps = 1e-5f;

		// See RTR2 ch. 13.7 for the algorithm.

		var e1 = points[(int)tri.Idx1] - points[(int)tri.Idx0];
		var e2 = points[(int)tri.Idx2] - points[(int)tri.Idx0];
		var p = Vector3.Cross(ray.Direction, e2);
		var a = Vector3.Dot(e1, p);

		if (Math.Abs(a) < eps)
			// Determinant is ill-conditioned; abort early
			return false;

		var f = 1.0f / a;
		var s = ray.Origin - points[(int)tri.Idx0];
		var u = f * Vector3.Dot(s, p);

		if ((u < 0.0f) || (u > 1.0f))
			// We hit the plane of the m_geometry, but outside the m_geometry
			return false;

		var q = Vector3.Cross(s, e1);
		var v = f * Vector3.Dot(ray.Direction, q);

		if ((v < 0.0f) || ((u + v) > 1.0f))
			// We hit the plane of the triangle, but outside the triangle
			return false;

		var t = f * Vector3.Dot(e2, q);

		if ((t > 0.0f) && (t < distance))
		{
			// This is a new hit, closer than the previous one
			distance = t;

			return true;
		}

		// This hit is after the previous hit, so ignore it
		return false;
	}
}