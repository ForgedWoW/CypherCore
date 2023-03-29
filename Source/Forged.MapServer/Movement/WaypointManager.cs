// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Maps.Grids;
using Framework.Database;
using Serilog;

namespace Forged.MapServer.Movement;

public sealed class WaypointManager
{
    private readonly WorldDatabase _worldDatabase;
    private readonly Dictionary<uint, WaypointPath> _waypointStore = new();

    public WaypointManager(WorldDatabase worldDatabase)
    {
        _worldDatabase = worldDatabase;
    }

    public void Load()
    {
        var oldMSTime = Time.MSTime;

        //                                          0    1         2           3          4            5           6        7      8           9
        var result = _worldDatabase.Query("SELECT id, point, position_x, position_y, position_z, orientation, move_type, delay, action, action_chance FROM waypoint_data ORDER BY id, point");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 waypoints. DB table `waypoint_data` is empty!");

            return;
        }

        uint count = 0;

        do
        {
            var pathId = result.Read<uint>(0);

            var x = result.Read<float>(2);
            var y = result.Read<float>(3);
            var z = result.Read<float>(4);
            float? o = null;

            if (!result.IsNull(5))
                o = result.Read<float>(5);

            x = GridDefines.NormalizeMapCoord(x);
            y = GridDefines.NormalizeMapCoord(y);

            WaypointNode waypoint = new()
            {
                ID = result.Read<uint>(1),
                X = x,
                Y = y,
                Z = z,
                Orientation = o,
                MoveType = (WaypointMoveType)result.Read<uint>(6)
            };

            if (waypoint.MoveType >= WaypointMoveType.Max)
            {
                Log.Logger.Error($"Waypoint {waypoint.ID} in waypoint_data has invalid move_type, ignoring");

                continue;
            }

            waypoint.Delay = result.Read<uint>(7);
            waypoint.EventId = result.Read<uint>(8);
            waypoint.EventChance = result.Read<byte>(9);

            if (!_waypointStore.ContainsKey(pathId))
                _waypointStore[pathId] = new WaypointPath();

            var path = _waypointStore[pathId];
            path.ID = pathId;
            path.Nodes.Add(waypoint);

            ++count;
        } while (result.NextRow());

        Log.Logger.Information($"Loaded {count} waypoints in {Time.GetMSTimeDiffToNow(oldMSTime)} ms");
    }

    public void ReloadPath(uint id)
    {
        _waypointStore.Remove(id);

        var stmt = _worldDatabase.GetPreparedStatement(WorldStatements.SEL_WAYPOINT_DATA_BY_ID);
        stmt.AddValue(0, id);
        var result = _worldDatabase.Query(stmt);

        if (result.IsEmpty())
            return;

        List<WaypointNode> values = new();

        do
        {
            var x = result.Read<float>(1);
            var y = result.Read<float>(2);
            var z = result.Read<float>(3);
            float? o = null;

            if (!result.IsNull(4))
                o = result.Read<float>(4);

            x = GridDefines.NormalizeMapCoord(x);
            y = GridDefines.NormalizeMapCoord(y);

            WaypointNode waypoint = new()
            {
                ID = result.Read<uint>(0),
                X = x,
                Y = y,
                Z = z,
                Orientation = o,
                MoveType = (WaypointMoveType)result.Read<uint>(5)
            };

            if (waypoint.MoveType >= WaypointMoveType.Max)
            {
                Log.Logger.Error($"Waypoint {waypoint.ID} in waypoint_data has invalid move_type, ignoring");

                continue;
            }

            waypoint.Delay = result.Read<uint>(6);
            waypoint.EventId = result.Read<uint>(7);
            waypoint.EventChance = result.Read<byte>(8);

            values.Add(waypoint);
        } while (result.NextRow());

        _waypointStore[id] = new WaypointPath(id, values);
    }

    public WaypointPath GetPath(uint id)
    {
        return _waypointStore.LookupByKey(id);
    }
}

public class WaypointNode
{
    public uint ID;
    public float X, Y, Z;
    public float? Orientation;
    public uint Delay;
    public uint EventId;
    public WaypointMoveType MoveType;
    public byte EventChance;

    public WaypointNode()
    {
        MoveType = WaypointMoveType.Run;
    }

    public WaypointNode(uint id, float x, float y, float z, float? orientation = null, uint delay = 0)
    {
        ID = id;
        X = x;
        Y = y;
        Z = z;
        Orientation = orientation;
        Delay = delay;
        EventId = 0;
        MoveType = WaypointMoveType.Walk;
        EventChance = 100;
    }
}

public class WaypointPath
{
    public List<WaypointNode> Nodes = new();
    public uint ID;
    public WaypointPath() { }

    public WaypointPath(uint id, List<WaypointNode> nodes)
    {
        ID = id;
        Nodes = nodes;
    }
}

public enum WaypointMoveType
{
    Walk,
    Run,
    Land,
    Takeoff,

    Max
}