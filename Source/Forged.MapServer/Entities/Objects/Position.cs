// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Numerics;
using Forged.MapServer.Maps.Grids;

namespace Forged.MapServer.Entities.Objects;

public class Position
{
	private float _orientation;

	public float X { get; set; }
	public float Y { get; set; }
	public float Z { get; set; }

	public float Orientation
	{
		get => _orientation;
		set => _orientation = NormalizeOrientation(value);
	}

	public bool IsDefault => X == 0 && Y == 0 && Z == 0 && Orientation == 0;

	public bool IsPositionValid => GridDefines.IsValidMapCoord(X, Y, Z, Orientation);

	public Position(float x = 0f, float y = 0f, float z = 0f, float o = 0f)
	{
		X = x;
		Y = y;
		Z = z;
		_orientation = NormalizeOrientation(o);
	}

	public Position(Vector3 vector)
	{
		X = vector.X;
		Y = vector.Y;
		Z = vector.Z;
	}

	public Position(Vector4 vector)
	{
		X = vector.X;
		Y = vector.Y;
		Z = vector.Z;
		Orientation = NormalizeOrientation(vector.W);
	}

	public Position(Position position)
	{
		X = position.X;
		Y = position.Y;
		Z = position.Z;
		_orientation = position.Orientation;
	}

	public void Relocate(float x, float y)
	{
		X = x;
		Y = y;
	}

	public void Relocate(float x, float y, float z)
	{
		X = x;
		Y = y;
		Z = z;
	}

	public void Relocate(float x, float y, float z, float o)
	{
		X = x;
		Y = y;
		Z = z;
		Orientation = o;
	}

	public void Relocate(Position loc)
	{
		Relocate(loc.X, loc.Y, loc.Z, loc.Orientation);
	}

	public void Relocate(Vector3 pos)
	{
		Relocate(pos.X, pos.Y, pos.Z);
	}

	public void RelocateOffset(Position offset)
	{
		X = (float)(X + (offset.X * Math.Cos(Orientation) + offset.Y * Math.Sin(Orientation + MathFunctions.PI)));
		Y = (float)(Y + (offset.Y * Math.Cos(Orientation) + offset.X * Math.Sin(Orientation)));
		Z += offset.Z;
		Orientation = Orientation + offset.Orientation;
	}

	public float GetRelativeAngle(Position pos)
	{
		return ToRelativeAngle(GetAbsoluteAngle(pos));
	}

	public float GetRelativeAngle(float x, float y)
	{
		return ToRelativeAngle(GetAbsoluteAngle(x, y));
	}

	public void GetPositionOffsetTo(Position endPos, out Position retOffset)
	{
		retOffset = new Position();

		var dx = endPos.X - X;
		var dy = endPos.Y - Y;

		retOffset.X = (float)(dx * Math.Cos(Orientation) + dy * Math.Sin(Orientation));
		retOffset.Y = (float)(dy * Math.Cos(Orientation) - dx * Math.Sin(Orientation));
		retOffset.Z = endPos.Z - Z;
		retOffset.Orientation = endPos.Orientation - Orientation;
	}

	public Position GetPositionWithOffset(Position offset)
	{
		var ret = this;
		ret.RelocateOffset(offset);

		return ret;
	}

	public static float NormalizeOrientation(float o)
	{
		// fmod only supports positive numbers. Thus we have
		// to emulate negative numbers
		if (o < 0)
		{
			var mod = o * -1;
			mod %= (2.0f * MathFunctions.PI);
			mod = -mod + 2.0f * MathFunctions.PI;

			return mod;
		}

		return o % (2.0f * MathFunctions.PI);
	}

	public float GetExactDist(float x, float y, float z)
	{
		return (float)Math.Sqrt(GetExactDistSq(x, y, z));
	}

	public float GetExactDist(Position pos)
	{
		return (float)Math.Sqrt(GetExactDistSq(pos));
	}

	public float GetExactDistSq(float x, float y, float z)
	{
		var dz = z - Z;

		return GetExactDist2dSq(x, y) + dz * dz;
	}

	public float GetExactDistSq(Position pos)
	{
		var dx = X - pos.X;
		var dy = Y - pos.Y;
		var dz = Z - pos.Z;

		return dx * dx + dy * dy + dz * dz;
	}

	public float GetExactDist2d(float x, float y)
	{
		return (float)Math.Sqrt(GetExactDist2dSq(x, y));
	}

	public float GetExactDist2d(Position pos)
	{
		return (float)Math.Sqrt(GetExactDist2dSq(pos));
	}

	public float GetExactDist2dSq(float x, float y)
	{
		var dx = x - X;
		var dy = y - Y;

		return dx * dx + dy * dy;
	}

	public float GetExactDist2dSq(Position pos)
	{
		var dx = pos.X - X;
		var dy = pos.Y - Y;

		return dx * dx + dy * dy;
	}

	public float GetAbsoluteAngle(float x, float y)
	{
		var dx = x - X;
		var dy = y - Y;

		return NormalizeOrientation(MathF.Atan2(dy, dx));
	}

	public float GetAbsoluteAngle(Position pos)
	{
		if (pos == null)
			return 0;

		return GetAbsoluteAngle(pos.X, pos.Y);
	}

	public float ToAbsoluteAngle(float relAngle)
	{
		return NormalizeOrientation(relAngle + Orientation);
	}

	public bool IsInDist(float x, float y, float z, float dist)
	{
		return GetExactDistSq(x, y, z) < dist * dist;
	}

	public bool IsInDist(Position pos, float dist)
	{
		return GetExactDistSq(pos) < dist * dist;
	}

	public bool IsInDist2d(float x, float y, float dist)
	{
		return GetExactDist2dSq(x, y) < dist * dist;
	}

	public bool IsInDist2d(Position pos, float dist)
	{
		return GetExactDist2dSq(pos) < dist * dist;
	}

	public bool IsWithinBox(Position center, float xradius, float yradius, float zradius)
	{
		// rotate the WorldObject position instead of rotating the whole cube, that way we can make a simplified
		// is-in-cube check and we have to calculate only one point instead of 4

		// 2PI = 360*, keep in mind that ingame orientation is counter-clockwise
		var rotation = 2 * Math.PI - center.Orientation;
		var sinVal = Math.Sin(rotation);
		var cosVal = Math.Cos(rotation);

		var boxDistX = X - center.X;
		var boxDistY = Y - center.Y;

		var rotX = (float)(center.X + boxDistX * cosVal - boxDistY * sinVal);
		var rotY = (float)(center.Y + boxDistY * cosVal + boxDistX * sinVal);

		// box edges are parallel to coordiante axis, so we can treat every dimension independently :D
		var dz = Z - center.Z;
		var dx = rotX - center.X;
		var dy = rotY - center.Y;

		if ((Math.Abs(dx) > xradius) || (Math.Abs(dy) > yradius) || (Math.Abs(dz) > zradius))
			return false;

		return true;
	}

	public bool IsWithinDoubleVerticalCylinder(Position center, float radius, float height)
	{
		var verticalDelta = Z - center.Z;

		return IsInDist2d(center, radius) && Math.Abs(verticalDelta) <= height;
	}

	public bool HasInArc(float arc, Position obj, float border = 2.0f)
	{
		// always have self in arc
		if (obj == this)
			return true;

		// move arc to range 0.. 2*pi
		arc = NormalizeOrientation(arc);

		// move angle to range -pi ... +pi
		var angle = GetRelativeAngle(obj);

		if (angle > MathFunctions.PI)
			angle -= 2.0f * MathFunctions.PI;

		var lborder = -1 * (arc / border); // in range -pi..0
		var rborder = (arc / border);      // in range 0..pi

		return ((angle >= lborder) && (angle <= rborder));
	}

	public bool HasInLine(Position pos, float objSize, float width)
	{
		if (!HasInArc(MathFunctions.PI, pos))
			return false;

		width += objSize;
		var angle = GetRelativeAngle(pos);

		return Math.Abs(Math.Sin(angle)) * GetExactDist2d(pos.X, pos.Y) < width;
	}

	public override string ToString()
	{
		return $"X: {X} Y: {Y} Z: {Z} O: {Orientation}";
	}

	public Position Copy()
	{
		return new Position(this);
	}

	public Vector3 ToVector3()
	{
		return new Vector3()
		{
			X = X,
			Y = Y,
			Z = Z
		};
	}

	public Vector4 ToVector4()
	{
		return new Vector4()
		{
			X = X,
			Y = Y,
			Z = Z,
			W = Orientation
		};
	}

	public static implicit operator Vector2(Position position)
	{
		return new Vector2(position.X, position.Y);
	}

	public static implicit operator Vector3(Position position)
	{
		return new Vector3(position.X, position.Y, position.Z);
	}

    private float ToRelativeAngle(float absAngle)
	{
		return NormalizeOrientation(absAngle - Orientation);
	}
}