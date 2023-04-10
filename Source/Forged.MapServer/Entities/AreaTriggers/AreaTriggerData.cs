// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Runtime.InteropServices;
using Framework.Constants;

namespace Forged.MapServer.Entities.AreaTriggers;

[StructLayout(LayoutKind.Explicit)]
public unsafe class AreaTriggerData
{
    [FieldOffset(0)] public DefaultData DefaultDatas;

    [FieldOffset(0)] public SphereData SphereDatas;

    [FieldOffset(0)] public BoxData BoxDatas;

    [FieldOffset(0)] public PolygonData PolygonDatas;

    [FieldOffset(0)] public CylinderData CylinderDatas;

    [FieldOffset(0)] public DiskData DiskDatas;

    [FieldOffset(0)] public BoundedPlaneData BoundedPlaneDatas;

    public struct DefaultData
    {
        public fixed float Data[SharedConst.MaxAreatriggerEntityData];
    }

    // AREATRIGGER_TYPE_SPHERE
    public struct SphereData
    {
        public float Radius;
        public float RadiusTarget;
    }

    // AREATRIGGER_TYPE_BOX
    public struct BoxData
    {
        public fixed float Extents[3];
        public fixed float ExtentsTarget[3];
    }

    // AREATRIGGER_TYPE_POLYGON
    public struct PolygonData
    {
        public float Height;
        public float HeightTarget;
    }

    // AREATRIGGER_TYPE_CYLINDER
    public struct CylinderData
    {
        public float Radius;
        public float RadiusTarget;
        public float Height;
        public float HeightTarget;
        public float LocationZOffset;
        public float LocationZOffsetTarget;
    }

    // AREATRIGGER_TYPE_DISK
    public struct DiskData
    {
        public float InnerRadius;
        public float InnerRadiusTarget;
        public float OuterRadius;
        public float OuterRadiusTarget;
        public float Height;
        public float HeightTarget;
        public float LocationZOffset;
        public float LocationZOffsetTarget;
    }

    // AREATRIGGER_TYPE_BOUNDED_PLANE
    public struct BoundedPlaneData
    {
        public fixed float Extents[2];
        public fixed float ExtentsTarget[2];
    }
}