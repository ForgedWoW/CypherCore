// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Numerics;
using Forged.MapServer.Collision.Models;
using Framework.GameMath;

namespace Forged.MapServer.Collision;

public class RegularGrid2D<T, Node> where T : IModel where Node : BIHWrap<T>, new()
{
    public const int CELL_NUMBER = 64;
    public const float HGRID_MAP_SIZE = (533.33333f * 64.0f); // shouldn't be changed
    public const float CELL_SIZE = HGRID_MAP_SIZE / CELL_NUMBER;

    private readonly MultiMap<T, Node> _memberTable = new();
    private readonly Node[][] _nodes = new Node[CELL_NUMBER][];

    public RegularGrid2D()
    {
        for (var x = 0; x < CELL_NUMBER; ++x)
            _nodes[x] = new Node[CELL_NUMBER];
    }

    public virtual void Insert(T value)
    {
        lock (_memberTable)
        {
            var bounds = value.GetBounds();
            var low = Cell.ComputeCell(bounds.Lo.X, bounds.Lo.Y);
            var high = Cell.ComputeCell(bounds.Hi.X, bounds.Hi.Y);

            for (var x = low.x; x <= high.x; ++x)
            {
                for (var y = low.y; y <= high.y; ++y)
                {
                    var node = GetGrid(x, y);
                    node.Insert(value);
                    _memberTable.Add(value, node);
                }
            }
        }
    }

    public virtual void Remove(T value)
    {
        // Remove the member

        lock (_memberTable)
        {
            _memberTable.Remove(value);
        }
    }

    public virtual void Balance()
    {
        for (var x = 0; x < CELL_NUMBER; ++x)
        {
            for (var y = 0; y < CELL_NUMBER; ++y)
            {
                var n = _nodes[x][y];

                if (n != null)
                    n.Balance();
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

    public void IntersectRay(Ray ray, WorkerCallback intersectCallback, ref float max_dist)
    {
        IntersectRay(ray, intersectCallback, ref max_dist, ray.Origin + ray.Direction * max_dist);
    }

    public void IntersectRay(Ray ray, WorkerCallback intersectCallback, ref float max_dist, Vector3 end)
    {
        var cell = Cell.ComputeCell(ray.Origin.X, ray.Origin.Y);

        if (!cell.IsValid())
            return;

        var last_cell = Cell.ComputeCell(end.X, end.Y);

        if (cell == last_cell)
        {
            var node = _nodes[cell.x][cell.y];

            if (node != null)
                node.IntersectRay(ray, intersectCallback, ref max_dist);

            return;
        }

        var voxel = CELL_SIZE;
        float kx_inv = ray.invDirection().X, bx = ray.Origin.X;
        float ky_inv = ray.invDirection().Y, by = ray.Origin.Y;

        int stepX, stepY;
        float tMaxX, tMaxY;

        if (kx_inv >= 0)
        {
            stepX = 1;
            var x_border = (cell.x + 1) * voxel;
            tMaxX = (x_border - bx) * kx_inv;
        }
        else
        {
            stepX = -1;
            var x_border = (cell.x - 1) * voxel;
            tMaxX = (x_border - bx) * kx_inv;
        }

        if (ky_inv >= 0)
        {
            stepY = 1;
            var y_border = (cell.y + 1) * voxel;
            tMaxY = (y_border - by) * ky_inv;
        }
        else
        {
            stepY = -1;
            var y_border = (cell.y - 1) * voxel;
            tMaxY = (y_border - by) * ky_inv;
        }

        var tDeltaX = voxel * Math.Abs(kx_inv);
        var tDeltaY = voxel * Math.Abs(ky_inv);

        do
        {
            var node = _nodes[cell.x][cell.y];

            if (node != null)
                node.IntersectRay(ray, intersectCallback, ref max_dist);

            if (cell == last_cell)
                break;

            if (tMaxX < tMaxY)
            {
                tMaxX += tDeltaX;
                cell.x += stepX;
            }
            else
            {
                tMaxY += tDeltaY;
                cell.y += stepY;
            }
        } while (cell.IsValid());
    }

    public void IntersectPoint(Vector3 point, WorkerCallback intersectCallback)
    {
        var cell = Cell.ComputeCell(point.X, point.Y);

        if (!cell.IsValid())
            return;

        var node = _nodes[cell.x][cell.y];

        if (node != null)
            node.IntersectPoint(point, intersectCallback);
    }

    // Optimized verson of intersectRay function for rays with vertical directions
    public void IntersectZAllignedRay(Ray ray, WorkerCallback intersectCallback, ref float max_dist)
    {
        var cell = Cell.ComputeCell(ray.Origin.X, ray.Origin.Y);

        if (!cell.IsValid())
            return;

        var node = _nodes[cell.x][cell.y];

        if (node != null)
            node.IntersectRay(ray, intersectCallback, ref max_dist);
    }

    private Node GetGrid(int x, int y)
    {
        if (_nodes[x][y] == null)
            _nodes[x][y] = new Node();

        return _nodes[x][y];
    }

    public struct Cell
    {
        public int x, y;

        public static bool operator ==(Cell c1, Cell c2)
        {
            return c1.x == c2.x && c1.y == c2.y;
        }

        public static bool operator !=(Cell c1, Cell c2)
        {
            return !(c1 == c2);
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return x.GetHashCode() ^ y.GetHashCode();
        }

        public static Cell ComputeCell(float fx, float fy)
        {
            Cell c = new()
            {
                x = (int)(fx * (1.0f / CELL_SIZE) + (CELL_NUMBER / 2f)),
                y = (int)(fy * (1.0f / CELL_SIZE) + (CELL_NUMBER / 2f))
            };

            return c;
        }

        public bool IsValid()
        {
            return x is >= 0 and < CELL_NUMBER && y is >= 0 and < CELL_NUMBER;
        }
    }
}