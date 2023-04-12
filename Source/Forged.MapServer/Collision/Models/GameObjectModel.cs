// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using Forged.MapServer.Collision.Management;
using Forged.MapServer.Collision.Maps;
using Forged.MapServer.Phasing;
using Framework.Constants;
using Framework.GameMath;
using Serilog;

namespace Forged.MapServer.Collision.Models;

public class GameObjectModel : Model
{
    private bool _collisionEnabled;
    private AxisAlignedBox _iBound;
    private Matrix4x4 _iInvRot;
    private float _iInvScale;
    private WorldModel _iModel;
    private Vector3 _iPos;
    private float _iScale;
    private GameObjectModelOwnerBase _owner;

    public override AxisAlignedBox Bounds => _iBound;

    public bool IsMapObject { get; private set; }

    public byte NameSetId => _owner.NameSetId;

    public virtual Vector3 Position => _iPos;

    public static GameObjectModel Create(GameObjectModelOwnerBase modelOwner, VMapManager vMapManager)
    {
        GameObjectModel mdl = new();

        return !mdl.Initialize(modelOwner, vMapManager) ? null : mdl;
    }

    public static bool LoadGameObjectModelList(string dataPath)
    {
        var oldMSTime = Time.MSTime;
        var filename = dataPath + "/vmaps/GameObjectModels.dtree";

        if (!File.Exists(filename))
        {
            Log.Logger.Warning("Unable to open '{0}' file.", filename);

            return false;
        }

        try
        {
            using BinaryReader reader = new(new FileStream(filename, FileMode.Open, FileAccess.Read));
            var magic = reader.ReadStringFromChars(8);

            if (magic != MapConst.VMapMagic)
            {
                Log.Logger.Error($"File '{filename}' has wrong header, expected {MapConst.VMapMagic}.");

                return false;
            }

            var length = reader.BaseStream.Length;

            while (true)
            {
                if (reader.BaseStream.Position >= length)
                    break;

                var displayId = reader.ReadUInt32();
                var isWmo = reader.ReadBoolean();
                var nameLength = reader.ReadInt32();
                var name = reader.ReadString(nameLength);
                var v1 = reader.Read<Vector3>();
                var v2 = reader.Read<Vector3>();

                StaticModelList.Models.Add(displayId, new GameobjectModelData(name, v1, v2, isWmo));
            }
        }
        catch (EndOfStreamException ex)
        {
            Log.Logger.Error(ex);
        }

        Log.Logger.Information("Loaded {0} GameObject models in {1} ms", StaticModelList.Models.Count, Time.GetMSTimeDiffToNow(oldMSTime));

        return true;
    }

    public void EnableCollision(bool enable)
    {
        _collisionEnabled = enable;
    }

    public bool GetLiquidLevel(Vector3 point, LocationInfo info, ref float liqHeight)
    {
        // child bounds are defined in object space:
        var pModel = _iInvRot.Multiply(point - _iPos) * _iInvScale;

        //Vector3 zDirModel = iInvRot * Vector3(0.f, 0.f, -1.f);
        if (!info.HitModel.GetLiquidLevel(pModel, out var zDist))
            return false;

        // calculate world height (zDist in model coords):
        // assume WMO not tilted (wouldn't make much sense anyway)
        liqHeight = zDist * _iScale + _iPos.Z;

        return true;
    }

    public bool GetLocationInfo(Vector3 point, LocationInfo info, PhaseShift phaseShift)
    {
        if (!_collisionEnabled || !_owner.IsSpawned || !IsMapObject)
            return false;

        if (!_owner.IsInPhase(phaseShift))
            return false;

        if (!_iBound.contains(point))
            return false;

        // child bounds are defined in object space:
        var pModel = _iInvRot.Multiply(point - _iPos) * _iInvScale;
        var zDirModel = _iInvRot.Multiply(new Vector3(0.0f, 0.0f, -1.0f));

        GroupLocationInfo groupInfo = new();

        if (!_iModel.GetLocationInfo(pModel, zDirModel, out var zDist, groupInfo))
            return false;

        var modelGround = pModel + zDist * zDirModel;
        var worldZ = (_iInvRot.Multiply(modelGround) * _iScale + _iPos).Z;

        if (!(info.GroundZ < worldZ))
            return false;

        info.GroundZ = worldZ;

        return true;
    }

    public override void IntersectPoint(Vector3 point, AreaInfo info, PhaseShift phaseShift)
    {
        if (!_collisionEnabled || !_owner.IsSpawned || !IsMapObject)
            return;

        if (!_owner.IsInPhase(phaseShift))
            return;

        if (!_iBound.contains(point))
            return;

        // child bounds are defined in object space:
        var pModel = _iInvRot.Multiply(point - _iPos) * _iInvScale;
        var zDirModel = _iInvRot.Multiply(new Vector3(0.0f, 0.0f, -1.0f));

        if (!_iModel.IntersectPoint(pModel, zDirModel, out var zDist, info))
            return;

        var modelGround = pModel + zDist * zDirModel;
        var worldZ = (_iInvRot.Multiply(modelGround) * _iScale + _iPos).Z;

        if (!(info.GroundZ < worldZ))
            return;

        info.GroundZ = worldZ;
        info.AdtId = _owner.NameSetId;
    }

    public override bool IntersectRay(Ray ray, ref float maxDist, bool stopAtFirstHit, PhaseShift phaseShift, ModelIgnoreFlags ignoreFlags)
    {
        if (!_collisionEnabled || !_owner.IsSpawned)
            return false;

        if (!_owner.IsInPhase(phaseShift))
            return false;

        var time = ray.intersectionTime(_iBound);

        if (time == float.PositiveInfinity)
            return false;

        // child bounds are defined in object space:
        var p = _iInvRot.Multiply(ray.Origin - _iPos) * _iInvScale;
        var modRay = new Ray(p, _iInvRot.Multiply(ray.Direction));
        var distance = maxDist * _iInvScale;
        var hit = _iModel.IntersectRay(modRay, ref distance, stopAtFirstHit, ignoreFlags);

        if (!hit)
            return false;

        distance *= _iScale;
        maxDist = distance;

        return true;
    }

    public bool UpdatePosition()
    {
        if (_iModel == null)
            return false;

        if (!StaticModelList.Models.TryGetValue(_owner.DisplayId, out var it))
            return false;

        AxisAlignedBox mdlBox = new(it.Bound);

        // ignore models with no bounds
        if (mdlBox == AxisAlignedBox.Zero())
        {
            Log.Logger.Error("GameObject model {0} has zero bounds, loading skipped", it.Name);

            return false;
        }

        _iPos = _owner.Position;

        var iRotation = _owner.Rotation.ToMatrix();
        iRotation.Inverse(out _iInvRot);
        // transform bounding box:
        mdlBox = new AxisAlignedBox(mdlBox.Lo * _iScale, mdlBox.Hi * _iScale);
        AxisAlignedBox rotatedBounds = new();

        for (var i = 0; i < 8; ++i)
            rotatedBounds.merge(iRotation.Multiply(mdlBox.corner(i)));

        _iBound = rotatedBounds + _iPos;

        return true;
    }

    private bool Initialize(GameObjectModelOwnerBase modelOwner, VMapManager vMapManager)
    {
        if (!StaticModelList.Models.TryGetValue(modelOwner.DisplayId, out var modelData))
            return false;

        AxisAlignedBox mdlBox = new(modelData.Bound);

        // ignore models with no bounds
        if (mdlBox == AxisAlignedBox.Zero())
        {
            Log.Logger.Error("GameObject model {0} has zero bounds, loading skipped", modelData.Name);

            return false;
        }

        _iModel = vMapManager.AcquireModelInstance(modelData.Name);

        if (_iModel == null)
            return false;

        _iPos = modelOwner.Position;
        _iScale = modelOwner.Scale;
        _iInvScale = 1.0f / _iScale;

        var iRotation = modelOwner.Rotation.ToMatrix();
        iRotation.Inverse(out _iInvRot);
        // transform bounding box:
        mdlBox = new AxisAlignedBox(mdlBox.Lo * _iScale, mdlBox.Hi * _iScale);
        AxisAlignedBox rotatedBounds = new();

        for (var i = 0; i < 8; ++i)
            rotatedBounds.merge(iRotation.Multiply(mdlBox.corner(i)));

        _iBound = rotatedBounds + _iPos;
        _owner = modelOwner;
        IsMapObject = modelData.IsWmo;

        return true;
    }
}