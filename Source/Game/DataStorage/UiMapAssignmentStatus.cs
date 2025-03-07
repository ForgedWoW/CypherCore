﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Game.DataStorage;

class UiMapAssignmentStatus
{
	public UiMapAssignmentRecord UiMapAssignment;
	public InsideStruct Inside;
	public OutsideStruct Outside;
	public sbyte MapPriority;
	public sbyte AreaPriority;
	public sbyte WmoPriority;

	public UiMapAssignmentStatus()
	{
		Inside = new InsideStruct();
		Outside = new OutsideStruct();
		MapPriority = 3;
		AreaPriority = -1;
		WmoPriority = 3;
	}

	public static bool operator <(UiMapAssignmentStatus left, UiMapAssignmentStatus right)
	{
		var leftInside = left.IsInside();
		var rightInside = right.IsInside();

		if (leftInside != rightInside)
			return leftInside;

		if (left.UiMapAssignment != null &&
			right.UiMapAssignment != null &&
			left.UiMapAssignment.UiMapID == right.UiMapAssignment.UiMapID &&
			left.UiMapAssignment.OrderIndex != right.UiMapAssignment.OrderIndex)
			return left.UiMapAssignment.OrderIndex < right.UiMapAssignment.OrderIndex;

		if (left.WmoPriority != right.WmoPriority)
			return left.WmoPriority < right.WmoPriority;

		if (left.AreaPriority != right.AreaPriority)
			return left.AreaPriority < right.AreaPriority;

		if (left.MapPriority != right.MapPriority)
			return left.MapPriority < right.MapPriority;

		if (leftInside)
		{
			if (left.Inside.DistanceToRegionBottom != right.Inside.DistanceToRegionBottom)
				return left.Inside.DistanceToRegionBottom < right.Inside.DistanceToRegionBottom;

			var leftUiSizeX = left.UiMapAssignment != null ? (left.UiMapAssignment.UiMax.X - left.UiMapAssignment.UiMin.X) : 0.0f;
			var rightUiSizeX = right.UiMapAssignment != null ? (right.UiMapAssignment.UiMax.X - right.UiMapAssignment.UiMin.X) : 0.0f;

			if (leftUiSizeX > float.Epsilon && rightUiSizeX > float.Epsilon)
			{
				var leftScale = (left.UiMapAssignment.Region[1].X - left.UiMapAssignment.Region[0].X) / leftUiSizeX;
				var rightScale = (right.UiMapAssignment.Region[1].X - right.UiMapAssignment.Region[0].X) / rightUiSizeX;

				if (leftScale != rightScale)
					return leftScale < rightScale;
			}

			if (left.Inside.DistanceToRegionCenterSquared != right.Inside.DistanceToRegionCenterSquared)
				return left.Inside.DistanceToRegionCenterSquared < right.Inside.DistanceToRegionCenterSquared;
		}
		else
		{
			if (left.Outside.DistanceToRegionTop != right.Outside.DistanceToRegionTop)
				return left.Outside.DistanceToRegionTop < right.Outside.DistanceToRegionTop;

			if (left.Outside.DistanceToRegionBottom != right.Outside.DistanceToRegionBottom)
				return left.Outside.DistanceToRegionBottom < right.Outside.DistanceToRegionBottom;

			if (left.Outside.DistanceToRegionEdgeSquared != right.Outside.DistanceToRegionEdgeSquared)
				return left.Outside.DistanceToRegionEdgeSquared < right.Outside.DistanceToRegionEdgeSquared;
		}

		return true;
	}

	public static bool operator >(UiMapAssignmentStatus left, UiMapAssignmentStatus right)
	{
		var leftInside = left.IsInside();
		var rightInside = right.IsInside();

		if (leftInside != rightInside)
			return leftInside;

		if (left.UiMapAssignment != null &&
			right.UiMapAssignment != null &&
			left.UiMapAssignment.UiMapID == right.UiMapAssignment.UiMapID &&
			left.UiMapAssignment.OrderIndex != right.UiMapAssignment.OrderIndex)
			return left.UiMapAssignment.OrderIndex > right.UiMapAssignment.OrderIndex;

		if (left.WmoPriority != right.WmoPriority)
			return left.WmoPriority > right.WmoPriority;

		if (left.AreaPriority != right.AreaPriority)
			return left.AreaPriority > right.AreaPriority;

		if (left.MapPriority != right.MapPriority)
			return left.MapPriority > right.MapPriority;

		if (leftInside)
		{
			if (left.Inside.DistanceToRegionBottom != right.Inside.DistanceToRegionBottom)
				return left.Inside.DistanceToRegionBottom > right.Inside.DistanceToRegionBottom;

			var leftUiSizeX = left.UiMapAssignment != null ? (left.UiMapAssignment.UiMax.X - left.UiMapAssignment.UiMin.X) : 0.0f;
			var rightUiSizeX = right.UiMapAssignment != null ? (right.UiMapAssignment.UiMax.X - right.UiMapAssignment.UiMin.X) : 0.0f;

			if (leftUiSizeX > float.Epsilon && rightUiSizeX > float.Epsilon)
			{
				var leftScale = (left.UiMapAssignment.Region[1].X - left.UiMapAssignment.Region[0].X) / leftUiSizeX;
				var rightScale = (right.UiMapAssignment.Region[1].X - right.UiMapAssignment.Region[0].X) / rightUiSizeX;

				if (leftScale != rightScale)
					return leftScale > rightScale;
			}

			if (left.Inside.DistanceToRegionCenterSquared != right.Inside.DistanceToRegionCenterSquared)
				return left.Inside.DistanceToRegionCenterSquared > right.Inside.DistanceToRegionCenterSquared;
		}
		else
		{
			if (left.Outside.DistanceToRegionTop != right.Outside.DistanceToRegionTop)
				return left.Outside.DistanceToRegionTop > right.Outside.DistanceToRegionTop;

			if (left.Outside.DistanceToRegionBottom != right.Outside.DistanceToRegionBottom)
				return left.Outside.DistanceToRegionBottom > right.Outside.DistanceToRegionBottom;

			if (left.Outside.DistanceToRegionEdgeSquared != right.Outside.DistanceToRegionEdgeSquared)
				return left.Outside.DistanceToRegionEdgeSquared > right.Outside.DistanceToRegionEdgeSquared;
		}

		return true;
	}

	bool IsInside()
	{
		return Outside.DistanceToRegionEdgeSquared < float.Epsilon &&
				Math.Abs(Outside.DistanceToRegionTop) < float.Epsilon &&
				Math.Abs(Outside.DistanceToRegionBottom) < float.Epsilon;
	}

	// distances if inside
	public class InsideStruct
	{
		public float DistanceToRegionCenterSquared = float.MaxValue;
		public float DistanceToRegionBottom = float.MaxValue;
	}

	// distances if outside
	public class OutsideStruct
	{
		public float DistanceToRegionEdgeSquared = float.MaxValue;
		public float DistanceToRegionTop = float.MaxValue;
		public float DistanceToRegionBottom = float.MaxValue;
	}
}