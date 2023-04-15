// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using Forged.MapServer.Collision.Management;
using Forged.MapServer.DataStorage;
using Framework.Constants;
using Framework.GameMath;
using Serilog;

namespace Forged.MapServer.Maps.Grids;

public class GridMap
{
    private readonly CliDB _cliDB;
    private readonly GridDefines _gridDefines;
    private uint _flags;
    private ushort _gridArea;
    private Func<float, float, float> _gridGetHeight;
    private float _gridHeight;
    private float _gridIntHeightMultiplier;
    private byte[] _holes;
    private ushort[] _liquidEntry;
    private byte[] _liquidFlags;
    private ushort _liquidGlobalEntry;
    private LiquidHeaderTypeFlags _liquidGlobalFlags;
    private byte _liquidHeight;
    //Liquid Map
    private float _liquidLevel;

    private float[] _liquidMap;
    private byte _liquidOffX;
    private byte _liquidOffY;
    private byte _liquidWidth;
    private Plane[] _minHeightPlanes;
    public GridMap(CliDB cliDB, GridDefines gridDefines)
    {
        _cliDB = cliDB;
        _gridDefines = gridDefines;
        // Height level data
        _gridHeight = MapConst.InvalidHeight;
        _gridGetHeight = GetHeightFromFlat;

        // Liquid data
        _liquidLevel = MapConst.InvalidHeight;
    }

    //Area data
    public ushort[] AreaMap { get; set; }

    public byte[] UbyteV8 { get; set; }
    public byte[] UbyteV9 { get; set; }
    public ushort[] Uint16V8 { get; set; }
    public ushort[] Uint16V9 { get; set; }
    public float[] V8 { get; set; }
    public float[] V9 { get; set; }
    public ushort GetArea(float x, float y)
    {
        if (AreaMap == null)
            return _gridArea;

        x = 16 * (32 - x / MapConst.SizeofGrids);
        y = 16 * (32 - y / MapConst.SizeofGrids);
        var lx = (int)x & 15;
        var ly = (int)y & 15;

        return AreaMap[lx * 16 + ly];
    }

    public float GetHeight(float x, float y)
    {
        return _gridGetHeight(x, y);
    }

    public float GetLiquidLevel(float x, float y)
    {
        if (_liquidMap == null)
            return _liquidLevel;

        x = MapConst.MapResolution * (32 - x / MapConst.SizeofGrids);
        y = MapConst.MapResolution * (32 - y / MapConst.SizeofGrids);

        var cxInt = ((int)x & MapConst.MapResolution - 1) - _liquidOffY;
        var cyInt = ((int)y & MapConst.MapResolution - 1) - _liquidOffX;

        if (cxInt < 0 || cxInt >= _liquidHeight)
            return MapConst.InvalidHeight;

        if (cyInt < 0 || cyInt >= _liquidWidth)
            return MapConst.InvalidHeight;

        return _liquidMap[cxInt * _liquidWidth + cyInt];
    }

    // Get water state on map
    public ZLiquidStatus GetLiquidStatus(float x, float y, float z, LiquidHeaderTypeFlags? reqLiquidType, LiquidData data, float collisionHeight)
    {
        // Check water type (if no water return)
        if (_liquidGlobalFlags == LiquidHeaderTypeFlags.NoWater && _liquidFlags == null)
            return ZLiquidStatus.NoWater;

        // Get cell
        var cx = MapConst.MapResolution * (32 - x / MapConst.SizeofGrids);
        var cy = MapConst.MapResolution * (32 - y / MapConst.SizeofGrids);

        var xInt = (int)cx & MapConst.MapResolution - 1;
        var yInt = (int)cy & MapConst.MapResolution - 1;

        // Check water type in cell
        var idx = (xInt >> 3) * 16 + (yInt >> 3);
        var type = _liquidFlags != null ? (LiquidHeaderTypeFlags)_liquidFlags[idx] : _liquidGlobalFlags;
        uint entry = _liquidEntry != null ? _liquidEntry[idx] : _liquidGlobalEntry;
        if (_cliDB.LiquidTypeStorage.TryGetValue(entry, out var liquidEntry))
        {
            type &= LiquidHeaderTypeFlags.DarkWater;
            uint liqTypeIdx = liquidEntry.SoundBank;

            if (entry < 21)
            {
                if (_cliDB.AreaTableStorage.TryGetValue(GetArea(x, y), out var area))
                {
                    uint overrideLiquid = area.LiquidTypeID[liquidEntry.SoundBank];

                    if (overrideLiquid == 0 && area.ParentAreaID == 0)
                    {
                        area = _cliDB.AreaTableStorage.LookupByKey(area.ParentAreaID);

                        if (area != null)
                            overrideLiquid = area.LiquidTypeID[liquidEntry.SoundBank];
                    }

                    if (_cliDB.LiquidTypeStorage.TryGetValue(overrideLiquid, out var liq))
                    {
                        entry = overrideLiquid;
                        liqTypeIdx = liq.SoundBank;
                    }
                }
            }

            type |= (LiquidHeaderTypeFlags)(1 << (int)liqTypeIdx);
        }

        if (type == LiquidHeaderTypeFlags.NoWater)
            return ZLiquidStatus.NoWater;

        // Check req liquid type mask
        if (reqLiquidType.HasValue && (reqLiquidType & type) == LiquidHeaderTypeFlags.NoWater)
            return ZLiquidStatus.NoWater;

        // Check water level:
        // Check water height map
        var lxInt = xInt - _liquidOffY;
        var lyInt = yInt - _liquidOffX;

        if (lxInt < 0 || lxInt >= _liquidHeight)
            return ZLiquidStatus.NoWater;

        if (lyInt < 0 || lyInt >= _liquidWidth)
            return ZLiquidStatus.NoWater;

        // Get water level
        var liquidLevel = _liquidMap != null ? _liquidMap[lxInt * _liquidWidth + lyInt] : _liquidLevel;
        // Get ground level (sub 0.2 for fix some errors)
        var groundLevel = GetHeight(x, y);

        // Check water level and ground level
        if (liquidLevel < groundLevel || z < groundLevel)
            return ZLiquidStatus.NoWater;

        // All ok in water . store data
        if (data != null)
        {
            data.Entry = entry;
            data.TypeFlags = type;
            data.Level = liquidLevel;
            data.DepthLevel = groundLevel;
        }

        // For speed check as int values
        var delta = liquidLevel - z;

        if (delta > collisionHeight) // Under water
            return ZLiquidStatus.UnderWater;

        return delta switch
        {
            // In water
            > 0.0f => ZLiquidStatus.InWater,
            // Walk on water
            > -0.1f => ZLiquidStatus.WaterWalk,
            _       => ZLiquidStatus.AboveWater
        };

        // Above water
    }

    public float GetMinHeight(float x, float y)
    {
        if (_minHeightPlanes == null)
            return -500.0f;

        var gridCoord = _gridDefines.ComputeGridCoordSimple(x, y);

        var doubleGridX = (int)Math.Floor(-(x - MapConst.MapHalfSize) / MapConst.CenterGridOffset);
        var doubleGridY = (int)Math.Floor(-(y - MapConst.MapHalfSize) / MapConst.CenterGridOffset);

        var gx = x - ((int)gridCoord.X - MapConst.CenterGridId + 1) * MapConst.SizeofGrids;
        var gy = y - ((int)gridCoord.Y - MapConst.CenterGridId + 1) * MapConst.SizeofGrids;

        uint quarterIndex;

        if (Convert.ToBoolean(doubleGridY & 1))
        {
            if (Convert.ToBoolean(doubleGridX & 1))
                quarterIndex = 4 + (gx <= gy ? 1 : 0u);
            else
                quarterIndex = 2 + (-MapConst.SizeofGrids - gx > gy ? 1u : 0);
        }
        else if (Convert.ToBoolean(doubleGridX & 1))
        {
            quarterIndex = 6 + (-MapConst.SizeofGrids - gx <= gy ? 1u : 0);
        }
        else
        {
            quarterIndex = gx > gy ? 1u : 0;
        }

        Ray ray = new(new Vector3(gx, gy, 0.0f), Vector3.UnitZ);

        return ray.intersection(_minHeightPlanes[quarterIndex]).Z;
    }

    public LoadResult LoadData(string filename)
    {
        // Unload old data if exist
        UnloadData();

        // Not return error if file not found
        if (!File.Exists(filename))
            return LoadResult.FileNotFound;

        using BinaryReader reader = new(new FileStream(filename, FileMode.Open, FileAccess.Read));
        var header = reader.Read<MapFileHeader>();

        if (header.MapMagic != MapConst.MapMagic || header.VersionMagic != MapConst.MapVersionMagic && header.VersionMagic != MapConst.MapVersionMagic2) // Hack for some different extractors using v2.0 header
        {
            Log.Logger.Error($"Map file '{filename}' is from an incompatible map version. Please recreate using the mapextractor.");

            return LoadResult.ReadFromFileFailed;
        }

        if (header.AreaMapOffset != 0 && !LoadAreaData(reader, header.AreaMapOffset))
        {
            Log.Logger.Error("Error loading map area data");

            return LoadResult.ReadFromFileFailed;
        }

        if (header.HeightMapOffset != 0 && !LoadHeightData(reader, header.HeightMapOffset))
        {
            Log.Logger.Error("Error loading map height data");

            return LoadResult.ReadFromFileFailed;
        }

        if (header.LiquidMapOffset != 0 && !LoadLiquidData(reader, header.LiquidMapOffset))
        {
            Log.Logger.Error("Error loading map liquids data");

            return LoadResult.ReadFromFileFailed;
        }

        if (header.HolesSize == 0 || LoadHolesData(reader, header.HolesOffset))
            return LoadResult.Success;

        Log.Logger.Error("Error loading map holes data");

        return LoadResult.ReadFromFileFailed;

    }

    public void UnloadData()
    {
        AreaMap = null;
        V9 = null;
        V8 = null;
        _liquidEntry = null;
        _liquidFlags = null;
        _liquidMap = null;
        _gridGetHeight = GetHeightFromFlat;
    }
    private float GetHeightFromFlat(float x, float y)
    {
        return _gridHeight;
    }

    private float GetHeightFromFloat(float x, float y)
    {
        if (Uint16V8 == null || Uint16V9 == null)
            return _gridHeight;

        x = MapConst.MapResolution * (32 - x / MapConst.SizeofGrids);
        y = MapConst.MapResolution * (32 - y / MapConst.SizeofGrids);

        var xInt = (int)x;
        var yInt = (int)y;
        x -= xInt;
        y -= yInt;
        xInt &= MapConst.MapResolution - 1;
        yInt &= MapConst.MapResolution - 1;

        if (IsHole(xInt, yInt))
            return MapConst.InvalidHeight;

        float a, b, c;

        if (x + y < 1)
        {
            if (x > y)
            {
                // 1 triangle (h1, h2, h5 points)
                var h1 = V9[xInt * 129 + yInt];
                var h2 = V9[(xInt + 1) * 129 + yInt];
                var h5 = 2 * V8[xInt * 128 + yInt];
                a = h2 - h1;
                b = h5 - h1 - h2;
                c = h1;
            }
            else
            {
                // 2 triangle (h1, h3, h5 points)
                var h1 = V9[xInt * 129 + yInt];
                var h3 = V9[xInt * 129 + yInt + 1];
                var h5 = 2 * V8[xInt * 128 + yInt];
                a = h5 - h1 - h3;
                b = h3 - h1;
                c = h1;
            }
        }
        else
        {
            if (x > y)
            {
                // 3 triangle (h2, h4, h5 points)
                var h2 = V9[(xInt + 1) * 129 + yInt];
                var h4 = V9[(xInt + 1) * 129 + yInt + 1];
                var h5 = 2 * V8[xInt * 128 + yInt];
                a = h2 + h4 - h5;
                b = h4 - h2;
                c = h5 - h4;
            }
            else
            {
                // 4 triangle (h3, h4, h5 points)
                var h3 = V9[xInt * 129 + yInt + 1];
                var h4 = V9[(xInt + 1) * 129 + yInt + 1];
                var h5 = 2 * V8[xInt * 128 + yInt];
                a = h4 - h3;
                b = h3 + h4 - h5;
                c = h5 - h4;
            }
        }

        // Calculate height
        return a * x + b * y + c;
    }

    private float GetHeightFromUint16(float x, float y)
    {
        if (Uint16V8 == null || Uint16V9 == null)
            return _gridHeight;

        x = MapConst.MapResolution * (MapConst.CenterGridId - x / MapConst.SizeofGrids);
        y = MapConst.MapResolution * (MapConst.CenterGridId - y / MapConst.SizeofGrids);

        var xInt = (int)x;
        var yInt = (int)y;
        x -= xInt;
        y -= yInt;
        xInt &= MapConst.MapResolution - 1;
        yInt &= MapConst.MapResolution - 1;

        if (IsHole(xInt, yInt))
            return MapConst.InvalidHeight;

        unsafe
        {
            fixed (ushort* v9 = Uint16V9)
            {
                var v9H1Ptr = &v9[xInt * 128 + xInt + yInt];

                int a;
                int b;
                int c;

                if (x + y < 1)
                {
                    if (x > y)
                    {
                        // 1 triangle (h1, h2, h5 points)
                        int h1 = v9H1Ptr[0];
                        int h2 = v9H1Ptr[129];
                        var h5 = 2 * Uint16V8[xInt * 128 + yInt];
                        a = h2 - h1;
                        b = h5 - h1 - h2;
                        c = h1;
                    }
                    else
                    {
                        // 2 triangle (h1, h3, h5 points)
                        int h1 = v9H1Ptr[0];
                        int h3 = v9H1Ptr[1];
                        var h5 = 2 * Uint16V8[xInt * 128 + yInt];
                        a = h5 - h1 - h3;
                        b = h3 - h1;
                        c = h1;
                    }
                }
                else
                {
                    if (x > y)
                    {
                        // 3 triangle (h2, h4, h5 points)
                        int h2 = v9H1Ptr[129];
                        int h4 = v9H1Ptr[130];
                        var h5 = 2 * Uint16V8[xInt * 128 + yInt];
                        a = h2 + h4 - h5;
                        b = h4 - h2;
                        c = h5 - h4;
                    }
                    else
                    {
                        // 4 triangle (h3, h4, h5 points)
                        int h3 = v9H1Ptr[1];
                        int h4 = v9H1Ptr[130];
                        var h5 = 2 * Uint16V8[xInt * 128 + yInt];
                        a = h4 - h3;
                        b = h3 + h4 - h5;
                        c = h5 - h4;
                    }
                }

                // Calculate height
                return (a * x + b * y + c) * _gridIntHeightMultiplier + _gridHeight;
            }
        }
    }

    private float GetHeightFromUint8(float x, float y)
    {
        if (UbyteV8 == null || UbyteV9 == null)
            return _gridHeight;

        x = MapConst.MapResolution * (32 - x / MapConst.SizeofGrids);
        y = MapConst.MapResolution * (32 - y / MapConst.SizeofGrids);

        var xInt = (int)x;
        var yInt = (int)y;
        x -= xInt;
        y -= yInt;
        xInt &= MapConst.MapResolution - 1;
        yInt &= MapConst.MapResolution - 1;

        if (IsHole(xInt, yInt))
            return MapConst.InvalidHeight;

        
        unsafe
        {
            fixed (byte* v9 = UbyteV9)
            {
                var v9H1Ptr = &v9[xInt * 128 + xInt + yInt];
                int a, b, c;

                if (x + y < 1)
                {
                    if (x > y)
                    {
                        // 1 triangle (h1, h2, h5 points)
                        int h1 = v9H1Ptr[0];
                        int h2 = v9H1Ptr[129];
                        var h5 = 2 * UbyteV8[xInt * 128 + yInt];
                        a = h2 - h1;
                        b = h5 - h1 - h2;
                        c = h1;
                    }
                    else
                    {
                        // 2 triangle (h1, h3, h5 points)
                        int h1 = v9H1Ptr[0];
                        int h3 = v9H1Ptr[1];
                        var h5 = 2 * UbyteV8[xInt * 128 + yInt];
                        a = h5 - h1 - h3;
                        b = h3 - h1;
                        c = h1;
                    }
                }
                else
                {
                    if (x > y)
                    {
                        // 3 triangle (h2, h4, h5 points)
                        int h2 = v9H1Ptr[129];
                        int h4 = v9H1Ptr[130];
                        var h5 = 2 * UbyteV8[xInt * 128 + yInt];
                        a = h2 + h4 - h5;
                        b = h4 - h2;
                        c = h5 - h4;
                    }
                    else
                    {
                        // 4 triangle (h3, h4, h5 points)
                        int h3 = v9H1Ptr[1];
                        int h4 = v9H1Ptr[130];
                        var h5 = 2 * UbyteV8[xInt * 128 + yInt];
                        a = h4 - h3;
                        b = h3 + h4 - h5;
                        c = h5 - h4;
                    }
                }

                // Calculate height
                return (a * x + b * y + c) * _gridIntHeightMultiplier + _gridHeight;
            }
        }
    }

    private bool IsHole(int row, int col)
    {
        if (_holes == null)
            return false;

        var cellRow = row / 8; // 8 squares per cell
        var cellCol = col / 8;
        var holeRow = row % 8;
        var holeCol = col % 8;

        return (_holes[cellRow * 16 * 8 + cellCol * 8 + holeRow] & 1 << holeCol) != 0;
    }

    private bool LoadAreaData(BinaryReader reader, uint offset)
    {
        reader.BaseStream.Seek(offset, SeekOrigin.Begin);
        var areaHeader = reader.Read<MapAreaHeader>();

        if (areaHeader.Fourcc != MapConst.MapAreaMagic)
            return false;

        _gridArea = areaHeader.GridArea;

        if (!areaHeader.Flags.HasAnyFlag(AreaHeaderFlags.NoArea))
            AreaMap = reader.ReadArray<ushort>(16 * 16);

        return true;
    }

    private bool LoadHeightData(BinaryReader reader, uint offset)
    {
        reader.BaseStream.Seek(offset, SeekOrigin.Begin);
        var mapHeader = reader.Read<MapHeightHeader>();

        if (mapHeader.Fourcc != MapConst.MapHeightMagic)
            return false;

        _gridHeight = mapHeader.GridHeight;
        _flags = (uint)mapHeader.Flags;

        if (!mapHeader.Flags.HasAnyFlag(HeightHeaderFlags.NoHeight))
        {
            if (mapHeader.Flags.HasAnyFlag(HeightHeaderFlags.HeightAsInt16))
            {
                Uint16V9 = reader.ReadArray<ushort>(129 * 129);
                Uint16V8 = reader.ReadArray<ushort>(128 * 128);

                _gridIntHeightMultiplier = (mapHeader.GridMaxHeight - mapHeader.GridHeight) / 65535;
                _gridGetHeight = GetHeightFromUint16;
            }
            else if (mapHeader.Flags.HasAnyFlag(HeightHeaderFlags.HeightAsInt8))
            {
                UbyteV9 = reader.ReadBytes(129 * 129);
                UbyteV8 = reader.ReadBytes(128 * 128);
                _gridIntHeightMultiplier = (mapHeader.GridMaxHeight - mapHeader.GridHeight) / 255;
                _gridGetHeight = GetHeightFromUint8;
            }
            else
            {
                V9 = reader.ReadArray<float>(129 * 129);
                V8 = reader.ReadArray<float>(128 * 128);

                _gridGetHeight = GetHeightFromFloat;
            }
        }
        else
        {
            _gridGetHeight = GetHeightFromFlat;
        }

        if (!mapHeader.Flags.HasAnyFlag(HeightHeaderFlags.HasFlightBounds))
            return true;

        reader.ReadArray<short>(3 * 3);
        var minHeights = reader.ReadArray<short>(3 * 3);

        uint[][] indices =
        {
            new uint[]
            {
                3, 0, 4
            },
            new uint[]
            {
                0, 1, 4
            },
            new uint[]
            {
                1, 2, 4
            },
            new uint[]
            {
                2, 5, 4
            },
            new uint[]
            {
                5, 8, 4
            },
            new uint[]
            {
                8, 7, 4
            },
            new uint[]
            {
                7, 6, 4
            },
            new uint[]
            {
                6, 3, 4
            }
        };

        float[][] boundGridCoords =
        {
            new[]
            {
                0.0f, 0.0f
            },
            new[]
            {
                0.0f, -266.66666f
            },
            new[]
            {
                0.0f, -533.33331f
            },
            new[]
            {
                -266.66666f, 0.0f
            },
            new[]
            {
                -266.66666f, -266.66666f
            },
            new[]
            {
                -266.66666f, -533.33331f
            },
            new[]
            {
                -533.33331f, 0.0f
            },
            new[]
            {
                -533.33331f, -266.66666f
            },
            new[]
            {
                -533.33331f, -533.33331f
            }
        };

        _minHeightPlanes = new Plane[8];

        for (uint quarterIndex = 0; quarterIndex < 8; ++quarterIndex)
            _minHeightPlanes[quarterIndex] = Plane.CreateFromVertices(new Vector3(boundGridCoords[indices[quarterIndex][0]][0], boundGridCoords[indices[quarterIndex][0]][1], minHeights[indices[quarterIndex][0]]),
                                                                      new Vector3(boundGridCoords[indices[quarterIndex][1]][0], boundGridCoords[indices[quarterIndex][1]][1], minHeights[indices[quarterIndex][1]]),
                                                                      new Vector3(boundGridCoords[indices[quarterIndex][2]][0], boundGridCoords[indices[quarterIndex][2]][1], minHeights[indices[quarterIndex][2]]));

        return true;
    }

    private bool LoadHolesData(BinaryReader reader, uint offset)
    {
        reader.BaseStream.Seek(offset, SeekOrigin.Begin);

        _holes = reader.ReadArray<byte>(16 * 16 * 8);

        return true;
    }

    private bool LoadLiquidData(BinaryReader reader, uint offset)
    {
        reader.BaseStream.Seek(offset, SeekOrigin.Begin);
        var liquidHeader = reader.Read<MapLiquidHeader>();

        if (liquidHeader.Fourcc != MapConst.MapLiquidMagic)
            return false;

        _liquidGlobalEntry = liquidHeader.LiquidType;
        _liquidGlobalFlags = (LiquidHeaderTypeFlags)liquidHeader.LiquidFlags;
        _liquidOffX = liquidHeader.OffsetX;
        _liquidOffY = liquidHeader.OffsetY;
        _liquidWidth = liquidHeader.Width;
        _liquidHeight = liquidHeader.Height;
        _liquidLevel = liquidHeader.LiquidLevel;

        if (!liquidHeader.Flags.HasAnyFlag(LiquidHeaderFlags.NoType))
        {
            _liquidEntry = reader.ReadArray<ushort>(16 * 16);
            _liquidFlags = reader.ReadBytes(16 * 16);
        }

        if (!liquidHeader.Flags.HasAnyFlag(LiquidHeaderFlags.NoHeight))
            _liquidMap = reader.ReadArray<float>((uint)(_liquidWidth * _liquidHeight));

        return true;
    }
}