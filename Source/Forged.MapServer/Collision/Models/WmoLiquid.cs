// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.IO;
using System.Numerics;
using Framework.Constants;

namespace Forged.MapServer.Collision.Models;

public class WmoLiquid
{
    private Vector3 _corner;
    private byte[] _flags;
    private float[] _height;
    private uint _tilesX;
    private uint _tilesY;
    private uint _type;
    public WmoLiquid() { }

    public WmoLiquid(uint width, uint height, Vector3 corner, uint type)
    {
        _tilesX = width;
        _tilesY = height;
        _corner = corner;
        _type = type;

        if (width != 0 && height != 0)
        {
            _height = new float[(width + 1) * (height + 1)];
            _flags = new byte[width * height];
        }
        else
        {
            _height = new float[1];
            _flags = null;
        }
    }

    public WmoLiquid(WmoLiquid other)
    {
        if (this == other)
            return;

        _tilesX = other._tilesX;
        _tilesY = other._tilesY;
        _corner = other._corner;
        _type = other._type;

        if (other._height != null)
        {
            _height = new float[(_tilesX + 1) * (_tilesY + 1)];
            Buffer.BlockCopy(other._height, 0, _height, 0, (int)((_tilesX + 1) * (_tilesY + 1)));
        }
        else
            _height = null;

        if (other._flags != null)
        {
            _flags = new byte[_tilesX * _tilesY];
            Buffer.BlockCopy(other._flags, 0, _flags, 0, (int)(_tilesX * _tilesY));
        }
        else
            _flags = null;
    }

    public static WmoLiquid ReadFromFile(BinaryReader reader)
    {
        WmoLiquid liquid = new()
        {
            _tilesX = reader.ReadUInt32(),
            _tilesY = reader.ReadUInt32(),
            _corner = reader.Read<Vector3>(),
            _type = reader.ReadUInt32()
        };

        if (liquid._tilesX != 0 && liquid._tilesY != 0)
        {
            var size = (liquid._tilesX + 1) * (liquid._tilesY + 1);
            liquid._height = reader.ReadArray<float>(size);

            size = liquid._tilesX * liquid._tilesY;
            liquid._flags = reader.ReadArray<byte>(size);
        }
        else
        {
            liquid._height = new float[1];
            liquid._height[0] = reader.ReadSingle();
        }

        return liquid;
    }

    public bool GetLiquidHeight(Vector3 pos, out float liqHeight)
    {
        // simple case
        if (_flags == null)
        {
            liqHeight = _height[0];

            return true;
        }

        liqHeight = 0f;
        var txF = (pos.X - _corner.X) / MapConst.LiquidTileSize;
        var tx = (uint)txF;

        if (txF < 0.0f || tx >= _tilesX)
            return false;

        var tyF = (pos.Y - _corner.Y) / MapConst.LiquidTileSize;
        var ty = (uint)tyF;

        if (tyF < 0.0f || ty >= _tilesY)
            return false;

        // check if tile shall be used for liquid level
        // checking for 0x08 *might* be enough, but disabled tiles always are 0x?F:
        if ((_flags[tx + ty * _tilesX] & 0x0F) == 0x0F)
            return false;

        // (dx, dy) coordinates inside tile, in [0, 1]^2
        var dx = txF - tx;
        var dy = tyF - ty;

        var rowOffset = _tilesX + 1;

        if (dx > dy) // case (a)
        {
            var sx = _height[tx + 1 + ty * rowOffset] - _height[tx + ty * rowOffset];
            var sy = _height[tx + 1 + (ty + 1) * rowOffset] - _height[tx + 1 + ty * rowOffset];
            liqHeight = _height[tx + ty * rowOffset] + dx * sx + dy * sy;
        }
        else // case (b)
        {
            var sx = _height[tx + 1 + (ty + 1) * rowOffset] - _height[tx + (ty + 1) * rowOffset];
            var sy = _height[tx + (ty + 1) * rowOffset] - _height[tx + ty * rowOffset];
            liqHeight = _height[tx + ty * rowOffset] + dx * sx + dy * sy;
        }

        return true;
    }

    public uint GetLiquidType()
    {
        return _type;
    }
}