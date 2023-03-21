// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.RealmServer.Entities;

namespace Forged.RealmServer.Maps;

public class AreaBoundary
{
	private readonly bool _isInvertedBoundary;

	public AreaBoundary(bool isInverted)
	{
		_isInvertedBoundary = isInverted;
	}

	public bool IsWithinBoundary(Position pos)
	{
		return pos != null && (IsWithinBoundaryArea(pos) != _isInvertedBoundary);
	}

	public virtual bool IsWithinBoundaryArea(Position pos)
	{
		return false;
	}

	public class DoublePosition : Position
	{
		private readonly double _doublePosX;
		private readonly double _doublePosY;
		private readonly double _doublePosZ;

		public DoublePosition(double x = 0.0, double y = 0.0, double z = 0.0, float o = 0f) : base((float)x, (float)y, (float)z, o)
		{
			_doublePosX = x;
			_doublePosY = y;
			_doublePosZ = z;
		}

		public DoublePosition(float x, float y = 0f, float z = 0f, float o = 0f) : base(x, y, z, o)
		{
			_doublePosX = x;
			_doublePosY = y;
			_doublePosZ = z;
		}

		public DoublePosition(Position pos) : this(pos.X, pos.Y, pos.Z, pos.Orientation) { }

		public double GetDoublePositionX()
		{
			return _doublePosX;
		}

		public double GetDoublePositionY()
		{
			return _doublePosY;
		}

		public double GetDoubleExactDist2dSq(DoublePosition pos)
		{
			var offX = GetDoublePositionX() - pos.GetDoublePositionX();
			var offY = GetDoublePositionY() - pos.GetDoublePositionY();

			return (offX * offX) + (offY * offY);
		}
	}
}