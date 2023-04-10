// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Numerics;
using Forged.MapServer.Collision.Models;
using Framework.GameMath;

namespace Forged.MapServer.Collision;

public class RegularGrid2D<T, TNode> where T : Model where TNode : BIHWrap<T>, new()
{
    public const int CELL_NUMBER = 64;
    public const float CELL_SIZE = HGRID_MAP_SIZE / CELL_NUMBER;
    public const float HGRID_MAP_SIZE = (533.33333f * 64.0f); // shouldn't be changed
    private readonly MultiMap<T, TNode> _memberTable = new();
    private readonly TNode[][] _nodes = new TNode[CELL_NUMBER][];

    public RegularGrid2D()
    {
        for (var x = 0; x < CELL_NUMBER; ++x)
            _nodes[x] = new TNode[CELL_NUMBER];
    }

    public virtual void Balance()
    {
        for (var x = 0; x < CELL_NUMBER; ++x)
        {
            for (var y = 0; y < CELL_NUMBER; ++y)
            {
                var n = _nodes[x][y];

                n?.Balance();
            }
        }
    }

    public bool Contains(T value)
    {
        lock (_memberTable)
        {
            return _memberTable.ContainsKey(value);
        }
    }

    public bool Empty()
    {
        lock (_memberTable)
        {
            return _memberTable.Empty();
        }
    }

    public virtual void Insert(T value)
    {
        lock (_memberTable)
        {
            var bounds = value.Bounds;
            var low = Cell.ComputeCell(bounds.Lo.X, bounds.Lo.Y);
            var high = Cell.ComputeCell(bounds.Hi.X, bounds.Hi.Y);

            for (var x = low.X; x <= high.X; ++x)
            {
                for (var y = low.Y; y <= high.Y; ++y)
                {
                    var node = GetGrid(x, y);
                    node.Insert(value);
                    _memberTable.Add(value, node);
                }
            }
        }
    }

    public void IntersectPoint(Vector3 point, WorkerCallback intersectCallback)
    {
        var cell = Cell.ComputeCell(point.X, point.Y);

        if (!cell.IsValid())
            return;

        var node = _nodes[cell.X][cell.Y];

        node?.IntersectPoint(point, intersectCallback);
    }

    public void IntersectRay(Ray ray, WorkerCallback intersectCallback, ref float maxDist)
    {
        IntersectRay(ray, intersectCallback, ref maxDist, ray.Origin + ray.Direction * maxDist);
    }

    public void IntersectRay(Ray ray, WorkerCallback intersectCallback, ref float maxDist, Vector3 end)
    {
        var cell = Cell.ComputeCell(ray.Origin.X, ray.Origin.Y);

        if (!cell.IsValid())
            return;

        var lastCell = Cell.ComputeCell(end.X, end.Y);

        if (cell == lastCell)
        {
            var node = _nodes[cell.X][cell.Y];

            node?.IntersectRay(ray, intersectCallback, ref maxDist);

            return;
        }

        var voxel = CELL_SIZE;
        float kxInv = ray.invDirection().X, bx = ray.Origin.X;
        float kyInv = ray.invDirection().Y, by = ray.Origin.Y;

        int stepX, stepY;
        float tMaxX, tMaxY;

        if (kxInv >= 0)
        {
            stepX = 1;
            var xBorder = (cell.X + 1) * voxel;
            tMaxX = (xBorder - bx) * kxInv;
        }
        else
        {
            stepX = -1;
            var xBorder = (cell.X - 1) * voxel;
            tMaxX = (xBorder - bx) * kxInv;
        }

        if (kyInv >= 0)
        {
            stepY = 1;
            var yBorder = (cell.Y + 1) * voxel;
            tMaxY = (yBorder - by) * kyInv;
        }
        else
        {
            stepY = -1;
            var yBorder = (cell.Y - 1) * voxel;
            tMaxY = (yBorder - by) * kyInv;
        }

        var tDeltaX = voxel * Math.Abs(kxInv);
        var tDeltaY = voxel * Math.Abs(kyInv);

        do
        {
            var node = _nodes[cell.X][cell.Y];

            node?.IntersectRay(ray, intersectCallback, ref maxDist);

            if (cell == lastCell)
                break;

            if (tMaxX < tMaxY)
            {
                tMaxX += tDeltaX;
                cell.X += stepX;
            }
            else
            {
                tMaxY += tDeltaY;
                cell.Y += stepY;
            }
        } while (cell.IsValid());
    }

    // Optimized verson of intersectRay function for rays with vertical directions
    public void IntersectZAllignedRay(Ray ray, WorkerCallback intersectCallback, ref float maxDist)
    {
        var cell = Cell.ComputeCell(ray.Origin.X, ray.Origin.Y);

        if (!cell.IsValid())
            return;

        var node = _nodes[cell.X][cell.Y];

        node?.IntersectRay(ray, intersectCallback, ref maxDist);
    }

    public virtual void Remove(T value)
    {
        // Remove the member

        lock (_memberTable)
        {
            _memberTable.Remove(value);
        }
    }

    private TNode GetGrid(int x, int y)
    {
        if (_nodes[x][y] == null)
            _nodes[x][y] = new TNode();

        return _nodes[x][y];
    }

    public struct Cell
    {
        public int X, Y;

        public static Cell ComputeCell(float fx, float fy)
        {
            Cell c = new()
            {
                X = (int)(fx * (1.0f / CELL_SIZE) + (CELL_NUMBER / 2f)),
                Y = (int)(fy * (1.0f / CELL_SIZE) + (CELL_NUMBER / 2f))
            };

            return c;
        }

        public static bool operator !=(Cell c1, Cell c2)
        {
            return !(c1 == c2);
        }

        public static bool operator ==(Cell c1, Cell c2)
        {
            return c1.X == c2.X && c1.Y == c2.Y;
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return X.GetHashCode() ^ Y.GetHashCode();
        }

        public bool IsValid()
        {
            return X is >= 0 and < CELL_NUMBER && Y is >= 0 and < CELL_NUMBER;
        }
    }
}