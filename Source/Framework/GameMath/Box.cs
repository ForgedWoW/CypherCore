﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Numerics;

namespace Framework.GameMath;

public class Box
{
	public Vector3[] _edgeVector = new Vector3[3];
	public Vector3 _center;
	public float _area;
	public float _volume;

	public Box(Vector3 min, Vector3 max)
	{
		_center = (max + min) * 0.5f;

		var bounds = new Vector3(max.X - min.X, max.Y - min.Y, max.Z - min.Z);
		_edgeVector[0] = new Vector3(bounds.X, 0, 0);
		_edgeVector[1] = new Vector3(0, bounds.Y, 0);
		_edgeVector[2] = new Vector3(0, 0, bounds.Z);
		var finiteExtent = true;

		for (var i = 0; i < 3; ++i)
			if (!float.IsFinite(_edgeVector[i].Length()))
			{
				finiteExtent = false;
				// If the extent is infinite along an axis, make the center zero to avoid NaNs
				_center.SetAt(0.0f, i);
			}


		if (finiteExtent)
			_volume = bounds.X * bounds.Y * bounds.Z;
		else
			_volume = float.PositiveInfinity;

		_area = 2 *
				(bounds.X * bounds.Y +
				bounds.Y * bounds.Z +
				bounds.Z * bounds.X);
	}

	public bool Contains(Vector3 point)
	{
		var u = _edgeVector[2];
		var v = _edgeVector[1];
		var w = _edgeVector[0];

		Matrix4x4 M = new(u.X, v.X, w.X, 0.0f, u.Y, v.Y, w.Y, 0.0f, u.Z, v.Z, w.Z, 0.0f, 0.0f, 0.0f, 0.0f, 1.0f);

		// M^-1 * (point - _corner[0]) = point in unit cube's object space
		// compute the inverse of M
		Matrix4x4.Invert(M, out M);
		var osPoint = M.Multiply(point - Corner(0));

		return (osPoint.X >= 0) &&
				(osPoint.Y >= 0) &&
				(osPoint.Z >= 0) &&
				(osPoint.X <= 1) &&
				(osPoint.Y <= 1) &&
				(osPoint.Z <= 1);
	}

	public bool isFinite()
	{
		return float.IsFinite(_volume);
	}

	Vector3 Corner(int i)
	{
		switch (i)
		{
			case 0:  return _center + (0.5f * (-_edgeVector[0] - _edgeVector[1] - _edgeVector[2]));
			case 1:  return _center + (0.5f * (_edgeVector[0] - _edgeVector[1] - _edgeVector[2]));
			case 2:  return _center + (0.5f * (-_edgeVector[0] + _edgeVector[1] - _edgeVector[2]));
			case 3:  return _center + (0.5f * (_edgeVector[0] + _edgeVector[1] - _edgeVector[2]));
			case 4:  return _center + (0.5f * (-_edgeVector[0] - _edgeVector[1] + _edgeVector[2]));
			case 5:  return _center + (0.5f * (_edgeVector[0] - _edgeVector[1] + _edgeVector[2]));
			case 6:  return _center + (0.5f * (-_edgeVector[0] + _edgeVector[1] + _edgeVector[2]));
			default: return _center + (0.5f * (_edgeVector[0] + _edgeVector[1] + _edgeVector[2])); //case 7
		}
	}
}