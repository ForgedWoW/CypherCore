// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Numerics;
using Forged.MapServer.Collision.Management;
using Forged.MapServer.Collision.Models;
using Forged.MapServer.DataStorage;
using Forged.MapServer.Phasing;
using Framework.GameMath;

namespace Forged.MapServer.Collision;

public class DynamicMapTree
{
    private readonly DB2Manager _db2Manager;
    private readonly DynTreeImpl _impl;

    public DynamicMapTree(DB2Manager db2Manager)
    {
        _db2Manager = db2Manager;
        _impl = new DynTreeImpl();
    }

    public void Balance()
    {
        _impl.Balance();
    }

    public bool Contains(GameObjectModel mdl)
    {
        return _impl.Contains(mdl);
    }

    public AreaAndLiquidData GetAreaAndLiquidData(float x, float y, float z, PhaseShift phaseShift, byte reqLiquidType)
    {
        AreaAndLiquidData data = new();

        Vector3 v = new(x, y, z + 0.5f);
        DynamicTreeLocationInfoCallback intersectionCallBack = new(phaseShift);
        _impl.IntersectPoint(v, intersectionCallBack);

        if (intersectionCallBack.LocationInfo.HitModel != null)
        {
            data.FloorZ = intersectionCallBack.LocationInfo.GroundZ;
            var liquidType = intersectionCallBack.LocationInfo.HitModel.LiquidType;
            float liquidLevel = 0;

            if (reqLiquidType == 0 || (_db2Manager.GetLiquidFlags(liquidType) & reqLiquidType) != 0)
                if (intersectionCallBack.HitModel.GetLiquidLevel(v, intersectionCallBack.LocationInfo, ref liquidLevel))
                    data.LiquidInfo = new AreaAndLiquidData.LiquidInfoModel(liquidType, liquidLevel);

            data.AreaInfo = new AreaAndLiquidData.AreaInfoModel(intersectionCallBack.HitModel.NameSetId,
                                                                intersectionCallBack.LocationInfo.RootId,
                                                                (int)intersectionCallBack.LocationInfo.HitModel.WmoID,
                                                                intersectionCallBack.LocationInfo.HitModel.MogpFlags);
        }

        return data;
    }

    public bool GetAreaInfo(float x, float y, ref float z, PhaseShift phaseShift, out uint flags, out int adtId, out int rootId, out int groupId)
    {
        flags = 0;
        adtId = 0;
        rootId = 0;
        groupId = 0;

        Vector3 v = new(x, y, z + 0.5f);
        DynamicTreeAreaInfoCallback intersectionCallBack = new(phaseShift);
        _impl.IntersectPoint(v, intersectionCallBack);

        if (!intersectionCallBack.AreaInfo.Result)
            return false;

        flags = intersectionCallBack.AreaInfo.Flags;
        adtId = intersectionCallBack.AreaInfo.AdtId;
        rootId = intersectionCallBack.AreaInfo.RootId;
        groupId = intersectionCallBack.AreaInfo.GroupId;
        z = intersectionCallBack.AreaInfo.GroundZ;

        return true;
    }

    public float GetHeight(float x, float y, float z, float maxSearchDist, PhaseShift phaseShift)
    {
        Vector3 v = new(x, y, z);
        Ray r = new(v, new Vector3(0, 0, -1));
        DynamicTreeIntersectionCallback callback = new(phaseShift);
        _impl.IntersectZAllignedRay(r, callback, ref maxSearchDist);

        if (callback.DidHit)
            return v.Z - maxSearchDist;
        else
            return float.NegativeInfinity;
    }

    public bool GetIntersectionTime(Ray ray, Vector3 endPos, PhaseShift phaseShift, ref float maxDist)
    {
        var distance = maxDist;
        DynamicTreeIntersectionCallback callback = new(phaseShift);
        _impl.IntersectRay(ray, callback, ref distance, endPos);

        if (callback.DidHit)
            maxDist = distance;

        return callback.DidHit;
    }

    public bool GetObjectHitPos(Vector3 startPos, Vector3 endPos, ref Vector3 resultHitPos, float modifyDist, PhaseShift phaseShift)
    {
        bool result;
        var maxDist = (endPos - startPos).Length();
        // valid map coords should *never ever* produce float overflow, but this would produce NaNs too

        // prevent NaN values which can cause BIH intersection to enter infinite loop
        if (maxDist < 1e-10f)
        {
            resultHitPos = endPos;

            return false;
        }

        var dir = (endPos - startPos) / maxDist; // direction with length of 1
        Ray ray = new(startPos, dir);
        var dist = maxDist;

        if (GetIntersectionTime(ray, endPos, phaseShift, ref dist))
        {
            resultHitPos = startPos + dir * dist;

            if (modifyDist < 0)
            {
                if ((resultHitPos - startPos).Length() > -modifyDist)
                    resultHitPos += dir * modifyDist;
                else
                    resultHitPos = startPos;
            }
            else
            {
                resultHitPos += dir * modifyDist;
            }

            result = true;
        }
        else
        {
            resultHitPos = endPos;
            result = false;
        }

        return result;
    }

    public void Insert(GameObjectModel mdl)
    {
        _impl.Insert(mdl);
    }

    public bool IsInLineOfSight(Vector3 startPos, Vector3 endPos, PhaseShift phaseShift)
    {
        var maxDist = (endPos - startPos).Length();

        if (!MathFunctions.fuzzyGt(maxDist, 0))
            return true;

        Ray r = new(startPos, (endPos - startPos) / maxDist);
        DynamicTreeIntersectionCallback callback = new(phaseShift);
        _impl.IntersectRay(r, callback, ref maxDist, endPos);

        return !callback.DidHit;
    }

    public void Remove(GameObjectModel mdl)
    {
        _impl.Remove(mdl);
    }

    public void Update(uint diff)
    {
        _impl.Update(diff);
    }
}