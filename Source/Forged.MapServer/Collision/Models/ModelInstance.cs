// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Numerics;
using Forged.MapServer.Collision.Maps;
using Framework.Constants;
using Framework.GameMath;

namespace Forged.MapServer.Collision.Models;

public class ModelInstance : ModelMinimalData
{
    private readonly Matrix4x4 _iInvRot;
    private readonly float _iInvScale;
    private WorldModel _iModel;

    public ModelInstance()
    {
        _iInvScale = 0.0f;
        _iModel = null;
    }

    public ModelInstance(ModelSpawn spawn, WorldModel model)
    {
        Flags = spawn.Flags;
        AdtId = spawn.AdtId;
        Id = spawn.Id;
        Pos = spawn.Pos;
        Scale = spawn.Scale;
        Bound = spawn.Bound;
        Name = spawn.Name;

        _iModel = model;

        Extensions.fromEulerAnglesZYX(MathFunctions.PI * spawn.Rot.Y / 180.0f, MathFunctions.PI * spawn.Rot.X / 180.0f, MathFunctions.PI * spawn.Rot.Z / 180.0f).Inverse(out _iInvRot);

        _iInvScale = 1.0f / Scale;
    }

    public bool GetLiquidLevel(Vector3 p, LocationInfo info, ref float liqHeight)
    {
        // child bounds are defined in object space:
        var pModel = _iInvRot.Multiply(p - Pos) * _iInvScale;

        //Vector3 zDirModel = iInvRot * Vector3(0.f, 0.f, -1.f);
        if (info.HitModel.GetLiquidLevel(pModel, out var zDist))
        {
            // calculate world height (zDist in model coords):
            // assume WMO not tilted (wouldn't make much sense anyway)
            liqHeight = zDist * Scale + Pos.Z;

            return true;
        }

        return false;
    }

    public bool GetLocationInfo(Vector3 p, LocationInfo info)
    {
        if (_iModel == null)
            return false;

        // M2 files don't contain area info, only WMO files
        if (Convert.ToBoolean(Flags & (uint)ModelFlags.M2))
            return false;

        if (!Bound.contains(p))
            return false;

        // child bounds are defined in object space:
        var pModel = _iInvRot.Multiply(p - Pos) * _iInvScale;
        var zDirModel = _iInvRot.Multiply(new Vector3(0.0f, 0.0f, -1.0f));

        GroupLocationInfo groupInfo = new();

        if (_iModel.GetLocationInfo(pModel, zDirModel, out var zDist, groupInfo))
        {
            var modelGround = pModel + zDist * zDirModel;
            // Transform back to world space. Note that:
            // Mat * vec == vec * Mat.transpose()
            // and for rotation matrices: Mat.inverse() == Mat.transpose()
            var worldZ = (_iInvRot.Multiply(modelGround * Scale) + Pos).Z;

            if (info.GroundZ < worldZ) // hm...could it be handled automatically with zDist at intersection?
            {
                info.RootId = groupInfo.RootId;
                info.HitModel = groupInfo.HitModel;
                info.GroundZ = worldZ;
                info.HitInstance = this;

                return true;
            }
        }

        return false;
    }

    public void IntersectPoint(Vector3 p, AreaInfo info)
    {
        if (_iModel == null)
            return;

        // M2 files don't contain area info, only WMO files
        if (Convert.ToBoolean(Flags & (uint)ModelFlags.M2))
            return;

        if (!Bound.contains(p))
            return;

        // child bounds are defined in object space:
        var pModel = _iInvRot.Multiply(p - Pos) * _iInvScale;
        var zDirModel = _iInvRot.Multiply(new Vector3(0.0f, 0.0f, -1.0f));

        if (_iModel.IntersectPoint(pModel, zDirModel, out var zDist, info))
        {
            var modelGround = pModel + zDist * zDirModel;
            // Transform back to world space. Note that:
            // Mat * vec == vec * Mat.transpose()
            // and for rotation matrices: Mat.inverse() == Mat.transpose()
            var worldZ = (_iInvRot.Multiply(modelGround) * Scale + Pos).Z;

            if (info.GroundZ < worldZ)
            {
                info.GroundZ = worldZ;
                info.AdtId = AdtId;
            }
        }
    }

    public bool IntersectRay(Ray pRay, ref float pMaxDist, bool pStopAtFirstHit, ModelIgnoreFlags ignoreFlags)
    {
        if (_iModel == null)
            return false;

        var time = pRay.intersectionTime(Bound);

        if (float.IsInfinity(time))
            return false;

        // child bounds are defined in object space:
        var p = _iInvRot.Multiply(pRay.Origin - Pos) * _iInvScale;
        var modRay = new Ray(p, _iInvRot.Multiply(pRay.Direction));
        var distance = pMaxDist * _iInvScale;
        var hit = _iModel.IntersectRay(modRay, ref distance, pStopAtFirstHit, ignoreFlags);

        if (hit)
        {
            distance *= Scale;
            pMaxDist = distance;
        }

        return hit;
    }

    public void SetUnloaded()
    {
        _iModel = null;
    }
}