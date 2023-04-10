// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Globals;
using Framework.Constants;

namespace Forged.MapServer.Entities.AreaTriggers;

public unsafe class AreaTriggerCreateProperties
{
    public AreaTriggerCreateProperties()
    {
        // legacy code from before it was known what each curve field does
        ExtraScale.Raw.Data[5] = 1065353217;
        // also OverrideActive does nothing on ExtraScale
        ExtraScale.Structured.OverrideActive = 1;
    }

    public int AnimId { get; set; }
    public uint AnimKitId { get; set; }
    public uint DecalPropertiesId { get; set; }
    public AreaTriggerScaleInfo ExtraScale { get; set; } = new();
    public uint FacingCurveId { get; set; }
    public bool HasSplines => SplinePoints.Count >= 2;
    public uint Id { get; set; }
    public float MaxSearchRadius
    {
        get
        {
            if (Shape.TriggerType != AreaTriggerTypes.Polygon)
                return Shape.GetMaxSearchRadius();

            Position center = new();

            return PolygonVertices.Select(vertice => center.GetExactDist2d(vertice.X, vertice.Y)).Prepend(0.0f).Max();
        }
    }

    public uint MorphCurveId { get; set; }
    public uint MoveCurveId { get; set; }
    public AreaTriggerOrbitInfo OrbitInfo { get; set; }
    public AreaTriggerScaleInfo OverrideScale { get; set; } = new();
    public List<Vector2> PolygonVertices { get; set; } = new();
    public List<Vector2> PolygonVerticesTarget { get; set; } = new();
    public uint ScaleCurveId { get; set; }
    public List<uint> ScriptIds { get; set; } = new();
    public AreaTriggerShapeInfo Shape { get; set; } = new();
    public List<Vector3> SplinePoints { get; set; } = new();
    public AreaTriggerTemplate Template { get; set; }
    public uint TimeToTarget { get; set; }
    public uint TimeToTargetScale { get; set; }
    public static AreaTriggerCreateProperties CreateDefault(uint areaTriggerId, GameObjectManager objectManager)
    {
        AreaTriggerCreateProperties ret = new()
        {
            Id = areaTriggerId,
            ScriptIds = objectManager.GetAreaTriggerScriptIds(areaTriggerId),
            Template = new AreaTriggerTemplate
            {
                Id = new AreaTriggerId(areaTriggerId, false),
                Flags = 0
            }
        };

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