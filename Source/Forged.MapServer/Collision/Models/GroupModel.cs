// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using Framework.GameMath;

namespace Forged.MapServer.Collision.Models;

public class GroupModel : Model
{
    private readonly BIH _meshTree = new();
    private readonly List<MeshTriangle> _triangles = new();
    private readonly List<Vector3> _vertices = new();
    private AxisAlignedBox _iBound;
    private WmoLiquid _iLiquid;

    public GroupModel()
    {
        _iLiquid = null;
    }

    public GroupModel(GroupModel other)
    {
        _iBound = other._iBound;
        MogpFlags = other.MogpFlags;
        WmoID = other.WmoID;
        _vertices = other._vertices;
        _triangles = other._triangles;
        _meshTree = other._meshTree;
        _iLiquid = null;

        if (other._iLiquid != null)
            _iLiquid = new WmoLiquid(other._iLiquid);
    }

    public GroupModel(uint mogpFlags, uint groupWmoid, AxisAlignedBox bound)
    {
        _iBound = bound;
        MogpFlags = mogpFlags;
        WmoID = groupWmoid;
        _iLiquid = null;
    }

    public override AxisAlignedBox Bounds => _iBound;

    public uint LiquidType => _iLiquid?.GetLiquidType() ?? 0;

    public uint MogpFlags { get; private set; }

    public uint WmoID { get; private set; }

    public bool GetLiquidLevel(Vector3 pos, out float liqHeight)
    {
        liqHeight = 0f;

        if (_iLiquid != null)
            return _iLiquid.GetLiquidHeight(pos, out liqHeight);

        return false;
    }

    public override bool IntersectRay(Ray ray, ref float distance, bool stopAtFirstHit)
    {
        if (_triangles.Empty())
            return false;

        GModelRayCallback callback = new(_triangles, _vertices);
        _meshTree.IntersectRay(ray, callback, ref distance, stopAtFirstHit);

        return callback.Hit;
    }

    public bool IsInsideObject(Vector3 pos, Vector3 down, out float zDist)
    {
        zDist = 0f;

        if (_triangles.Empty() || !_iBound.contains(pos))
            return false;

        var rPos = pos - 0.1f * down;
        var dist = float.PositiveInfinity;
        Ray ray = new(rPos, down);
        var hit = IntersectRay(ray, ref dist, false);

        if (hit)
            zDist = dist - 0.1f;

        return hit;
    }

    public bool ReadFromFile(BinaryReader reader)
    {
        _triangles.Clear();
        _vertices.Clear();
        _iLiquid = null;

        var lo = reader.Read<Vector3>();
        var hi = reader.Read<Vector3>();
        _iBound = new AxisAlignedBox(lo, hi);
        MogpFlags = reader.ReadUInt32();
        WmoID = reader.ReadUInt32();

        // read vertices
        if (reader.ReadStringFromChars(4) != "VERT")
            return false;

        var chunkSize = reader.ReadUInt32();
        var count = reader.ReadUInt32();

        if (count == 0)
            return false;

        for (var i = 0; i < count; ++i)
            _vertices.Add(reader.Read<Vector3>());

        // read triangle mesh
        if (reader.ReadStringFromChars(4) != "TRIM")
            return false;

        chunkSize = reader.ReadUInt32();
        count = reader.ReadUInt32();

        for (var i = 0; i < count; ++i)
            _triangles.Add(reader.Read<MeshTriangle>());

        // read mesh BIH
        if (reader.ReadStringFromChars(4) != "MBIH")
            return false;

        _meshTree.ReadFromFile(reader);

        // write liquid data
        if (reader.ReadStringFromChars(4) != "LIQU")
            return false;

        chunkSize = reader.ReadUInt32();

        if (chunkSize > 0)
            _iLiquid = WmoLiquid.ReadFromFile(reader);

        return true;
    }
}