// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using System.Numerics;
using Framework.Constants;

namespace Forged.RealmServer.Entities;

public unsafe class AreaTriggerCreateProperties
{
	public uint Id;
	public AreaTriggerTemplate Template;

	public uint MoveCurveId;
	public uint ScaleCurveId;
	public uint MorphCurveId;
	public uint FacingCurveId;

	public int AnimId;
	public uint AnimKitId;

	public uint DecalPropertiesId;

	public uint TimeToTarget;
	public uint TimeToTargetScale;

	public AreaTriggerScaleInfo OverrideScale = new();
	public AreaTriggerScaleInfo ExtraScale = new();

	public AreaTriggerShapeInfo Shape = new();
	public List<Vector2> PolygonVertices = new();
	public List<Vector2> PolygonVerticesTarget = new();
	public List<Vector3> SplinePoints = new();
	public AreaTriggerOrbitInfo OrbitInfo;

	public List<uint> ScriptIds = new();

	public AreaTriggerCreateProperties()
	{
		// legacy code from before it was known what each curve field does
		ExtraScale.Raw.Data[5] = 1065353217;
		// also OverrideActive does nothing on ExtraScale
		ExtraScale.Structured.OverrideActive = 1;
	}

	public bool HasSplines()
	{
		return SplinePoints.Count >= 2;
	}

	public float GetMaxSearchRadius()
	{
		if (Shape.TriggerType == AreaTriggerTypes.Polygon)
		{
			Position center = new(0.0f, 0.0f);
			var maxSearchRadius = 0.0f;

			foreach (var vertice in PolygonVertices)
			{
				var pointDist = center.GetExactDist2d(vertice.X, vertice.Y);

				if (pointDist > maxSearchRadius)
					maxSearchRadius = pointDist;
			}

			return maxSearchRadius;
		}

		return Shape.GetMaxSearchRadius();
	}

	public static AreaTriggerCreateProperties CreateDefault(uint areaTriggerId)
	{
		AreaTriggerCreateProperties ret = new();
		ret.Id = areaTriggerId;
		ret.ScriptIds = GameObjectManager.Instance.GetAreaTriggerScriptIds(areaTriggerId);
		ret.Template = new AreaTriggerTemplate();
		ret.Template.Id = new AreaTriggerId(areaTriggerId, false);
		ret.Template.Flags = 0;

		ret.Template.Actions.Add(new AreaTriggerAction
		{
			ActionType = AreaTriggerActionTypes.Cast,
			Param = 0,
			TargetType = AreaTriggerActionUserTypes.Friend
		});

		ret.MoveCurveId = 0;
		ret.ScaleCurveId = 0;
		ret.MorphCurveId = 0;
		ret.FacingCurveId = 0;
		ret.AnimId = 0;
		ret.AnimKitId = 0;
		ret.DecalPropertiesId = 0;
		ret.TimeToTarget = 0;
		ret.TimeToTargetScale = 0;

		return ret;
	}
}