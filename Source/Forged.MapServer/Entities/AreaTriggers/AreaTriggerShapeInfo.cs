// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;

namespace Forged.MapServer.Entities.AreaTriggers;

public class AreaTriggerShapeInfo : AreaTriggerData
{
	public AreaTriggerTypes TriggerType;

	public AreaTriggerShapeInfo()
	{
		TriggerType = AreaTriggerTypes.Max;
	}

	public unsafe float GetMaxSearchRadius()
	{
		switch (TriggerType)
		{
			case AreaTriggerTypes.Sphere:
				return Math.Max(SphereDatas.Radius, SphereDatas.RadiusTarget);
			case AreaTriggerTypes.Box:
				return MathF.Sqrt(BoxDatas.Extents[0] * BoxDatas.Extents[0] / 4 + BoxDatas.Extents[1] * BoxDatas.Extents[1] / 4);
			case AreaTriggerTypes.Cylinder:
				return Math.Max(CylinderDatas.Radius, CylinderDatas.RadiusTarget);
			case AreaTriggerTypes.Disk:
				return Math.Max(DiskDatas.OuterRadius, DiskDatas.OuterRadiusTarget);
			case AreaTriggerTypes.BoundedPlane:
				return MathF.Sqrt(BoundedPlaneDatas.Extents[0] * BoundedPlaneDatas.Extents[0] / 4 + BoundedPlaneDatas.Extents[1] * BoundedPlaneDatas.Extents[1] / 4);
		}

		return 0.0f;
	}

	public bool IsSphere()
	{
		return TriggerType == AreaTriggerTypes.Sphere;
	}

	public bool IsBox()
	{
		return TriggerType == AreaTriggerTypes.Box;
	}

	public bool IsPolygon()
	{
		return TriggerType == AreaTriggerTypes.Polygon;
	}

	public bool IsCylinder()
	{
		return TriggerType == AreaTriggerTypes.Cylinder;
	}

	public bool IsDisk()
	{
		return TriggerType == AreaTriggerTypes.Disk;
	}

	public bool IsBoudedPlane()
	{
		return TriggerType == AreaTriggerTypes.BoundedPlane;
	}
}