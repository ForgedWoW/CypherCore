// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Maps.Grids;
using Framework.Database;

namespace Forged.MapServer.Movement;

public sealed class WaypointManager : Singleton<WaypointManager>
{
	readonly Dictionary<uint, WaypointPath> _waypointStore = new();
	WaypointManager() { }

	public void Load()
	{
		var oldMSTime = Time.MSTime;

		//                                          0    1         2           3          4            5           6        7      8           9
		var result = DB.World.Query("SELECT id, point, position_x, position_y, position_z, orientation, move_type, delay, action, action_chance FROM waypoint_data ORDER BY id, point");

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
				id = result.Read<uint>(1),
				x = x,
				y = y,
				z = z,
				orientation = o,
				moveType = (WaypointMoveType)result.Read<uint>(6)
			};

			if (waypoint.moveType >= WaypointMoveType.Max)
			{
				Log.Logger.Error($"Waypoint {waypoint.id} in waypoint_data has invalid move_type, ignoring");

				continue;
			}

			waypoint.delay = result.Read<uint>(7);
			waypoint.eventId = result.Read<uint>(8);
			waypoint.eventChance = result.Read<byte>(9);

			if (!_waypointStore.ContainsKey(pathId))
				_waypointStore[pathId] = new WaypointPath();

			var path = _waypointStore[pathId];
			path.id = pathId;
			path.nodes.Add(waypoint);

			++count;
		} while (result.NextRow());

		Log.Logger.Information($"Loaded {count} waypoints in {global::Time.GetMSTimeDiffToNow(oldMSTime)} ms");
	}

	public void ReloadPath(uint id)
	{
		_waypointStore.Remove(id);

		var stmt = DB.World.GetPreparedStatement(WorldStatements.SEL_WAYPOINT_DATA_BY_ID);
		stmt.AddValue(0, id);
		var result = DB.World.Query(stmt);

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
				id = result.Read<uint>(0),
				x = x,
				y = y,
				z = z,
				orientation = o,
				moveType = (WaypointMoveType)result.Read<uint>(5)
			};

			if (waypoint.moveType >= WaypointMoveType.Max)
			{
				Log.Logger.Error($"Waypoint {waypoint.id} in waypoint_data has invalid move_type, ignoring");

				continue;
			}

			waypoint.delay = result.Read<uint>(6);
			waypoint.eventId = result.Read<uint>(7);
			waypoint.eventChance = result.Read<byte>(8);

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
	public uint id;
	public float x, y, z;
	public float? orientation;
	public uint delay;
	public uint eventId;
	public WaypointMoveType moveType;
	public byte eventChance;

	public WaypointNode()
	{
		moveType = WaypointMoveType.Run;
	}

	public WaypointNode(uint _id, float _x, float _y, float _z, float? _orientation = null, uint _delay = 0)
	{
		id = _id;
		x = _x;
		y = _y;
		z = _z;
		orientation = _orientation;
		delay = _delay;
		eventId = 0;
		moveType = WaypointMoveType.Walk;
		eventChance = 100;
	}
}

public class WaypointPath
{
	public List<WaypointNode> nodes = new();
	public uint id;
	public WaypointPath() { }

	public WaypointPath(uint _id, List<WaypointNode> _nodes)
	{
		id = _id;
		nodes = _nodes;
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