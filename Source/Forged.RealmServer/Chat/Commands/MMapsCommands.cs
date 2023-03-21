// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Framework.IO;
using Forged.RealmServer.Entities;
using Forged.RealmServer.Maps;
using Forged.RealmServer.Movement;

namespace Forged.RealmServer.Chat;

[CommandGroup("mmap")]
class MMapsCommands
{
	[Command("path", RBACPermissions.CommandMmapPath)]
	static bool HandleMmapPathCommand(CommandHandler handler, StringArguments args)
	{
		if (Global.MMapMgr.GetNavMesh(handler.Player.Location.MapId) == null)
		{
			handler.SendSysMessage("NavMesh not loaded for current map.");

			return true;
		}

		handler.SendSysMessage("mmap path:");

		// units
		var player = handler.Player;
		var target = handler.SelectedUnit;

		if (player == null || target == null)
		{
			handler.SendSysMessage("Invalid target/source selection.");

			return true;
		}

		var para = args.NextString();

		var useStraightPath = false;

		if (para.Equals("true", StringComparison.OrdinalIgnoreCase))
			useStraightPath = true;

		var useRaycast = false;

		if (para.Equals("line", StringComparison.OrdinalIgnoreCase) || para.Equals("ray", StringComparison.OrdinalIgnoreCase) || para.Equals("raycast", StringComparison.OrdinalIgnoreCase))
			useRaycast = true;

		// unit locations
		var pos = player.Location.Copy();

		// path
		PathGenerator path = new(target);
		path.SetUseStraightPath(useStraightPath);
		path.SetUseRaycast(useRaycast);
		var result = path.CalculatePath(pos, false);

		var pointPath = path.GetPath();
		handler.SendSysMessage("{0}'s path to {1}:", target.GetName(), player.GetName());
		handler.SendSysMessage("Building: {0}", useStraightPath ? "StraightPath" : useRaycast ? "Raycast" : "SmoothPath");
		handler.SendSysMessage("Result: {0} - Length: {1} - Type: {2}", (result ? "true" : "false"), pointPath.Length, path.GetPathType());

		var start = path.GetStartPosition();
		var end = path.GetEndPosition();
		var actualEnd = path.GetActualEndPosition();

		handler.SendSysMessage("StartPosition     ({0:F3}, {1:F3}, {2:F3})", start.X, start.Y, start.Z);
		handler.SendSysMessage("EndPosition       ({0:F3}, {1:F3}, {2:F3})", end.X, end.Y, end.Z);
		handler.SendSysMessage("ActualEndPosition ({0:F3}, {1:F3}, {2:F3})", actualEnd.X, actualEnd.Y, actualEnd.Z);

		if (!player.IsGameMaster)
			handler.SendSysMessage("Enable GM mode to see the path points.");

		for (uint i = 0; i < pointPath.Length; ++i)
			player.SummonCreature(1, new Position(pointPath[i].X, pointPath[i].Y, pointPath[i].Z, 0), TempSummonType.TimedDespawn, TimeSpan.FromSeconds(9));

		return true;
	}

	[Command("loc", RBACPermissions.CommandMmapLoc)]
	static bool HandleMmapLocCommand(CommandHandler handler)
	{
		handler.SendSysMessage("mmap tileloc:");

		// grid tile location
		var player = handler.Player;

		var gx = (int)(32 - player.Location.X / MapConst.SizeofGrids);
		var gy = (int)(32 - player.Location.Y / MapConst.SizeofGrids);


		handler.SendSysMessage("{0:D4}{1:D2}{2:D2}.mmtile", player.Location.MapId, gy, gx);
		handler.SendSysMessage("tileloc [{0}, {1}]", gx, gy);

		// calculate navmesh tile location
		var terrainMapId = PhasingHandler.GetTerrainMapId(player.PhaseShift, player.Location.MapId, player.Map.Terrain, player.Location.X, player.Location.Y);
		var navmesh = Global.MMapMgr.GetNavMesh(terrainMapId);
		var navmeshquery = Global.MMapMgr.GetNavMeshQuery(terrainMapId, player.InstanceId);

		if (navmesh == null || navmeshquery == null)
		{
			handler.SendSysMessage("NavMesh not loaded for current map.");

			return true;
		}

		var min = navmesh.getParams().orig;

		float[] location =
		{
			player.Location.Y, player.Location.Z, player.Location.X
		};

		float[] extents =
		{
			3.0f, 5.0f, 3.0f
		};

		var tilex = (int)((player.Location.Y - min[0]) / MapConst.SizeofGrids);
		var tiley = (int)((player.Location.X - min[2]) / MapConst.SizeofGrids);

		handler.SendSysMessage("Calc   [{0:D2}, {1:D2}]", tilex, tiley);

		// navmesh poly . navmesh tile location
		Detour.dtQueryFilter filter = new();
		var nothing = new float[3];
		ulong polyRef = 0;

		if (Detour.dtStatusFailed(navmeshquery.findNearestPoly(location, extents, filter, ref polyRef, ref nothing)))
		{
			handler.SendSysMessage("Dt     [??,??] (invalid poly, probably no tile loaded)");

			return true;
		}

		if (polyRef == 0)
		{
			handler.SendSysMessage("Dt     [??, ??] (invalid poly, probably no tile loaded)");
		}
		else
		{
			Detour.dtMeshTile tile = new();
			Detour.dtPoly poly = new();

			if (Detour.dtStatusSucceed(navmesh.getTileAndPolyByRef(polyRef, ref tile, ref poly)))
				if (tile != null)
				{
					handler.SendSysMessage("Dt     [{0:D2},{1:D2}]", tile.header.x, tile.header.y);

					return true;
				}

			handler.SendSysMessage("Dt     [??,??] (no tile loaded)");
		}

		return true;
	}

	[Command("loadedtiles", RBACPermissions.CommandMmapLoadedtiles)]
	static bool HandleMmapLoadedTilesCommand(CommandHandler handler)
	{
		var player = handler.Session.Player;
		var terrainMapId = PhasingHandler.GetTerrainMapId(player.PhaseShift, player.Location.MapId, player.Map.Terrain, player.Location.X, player.Location.Y);
		var navmesh = Global.MMapMgr.GetNavMesh(terrainMapId);
		var navmeshquery = Global.MMapMgr.GetNavMeshQuery(terrainMapId, handler.Player.InstanceId);

		if (navmesh == null || navmeshquery == null)
		{
			handler.SendSysMessage("NavMesh not loaded for current map.");

			return true;
		}

		handler.SendSysMessage("mmap loadedtiles:");

		for (var i = 0; i < navmesh.getMaxTiles(); ++i)
		{
			var tile = navmesh.getTile(i);

			if (tile.header == null)
				continue;

			handler.SendSysMessage("[{0:D2}, {1:D2}]", tile.header.x, tile.header.y);
		}

		return true;
	}

	[Command("stats", RBACPermissions.CommandMmapStats)]
	static bool HandleMmapStatsCommand(CommandHandler handler)
	{
		var player = handler.Session.Player;
		var terrainMapId = PhasingHandler.GetTerrainMapId(player.PhaseShift, player.Location.MapId, player.Map.Terrain, player.Location.X, player.Location.Y);
		handler.SendSysMessage("mmap stats:");
		handler.SendSysMessage("  global mmap pathfinding is {0}abled", Global.DisableMgr.IsPathfindingEnabled(player.Location.MapId) ? "En" : "Dis");
		handler.SendSysMessage(" {0} maps loaded with {1} tiles overall", Global.MMapMgr.GetLoadedMapsCount(), Global.MMapMgr.GetLoadedTilesCount());

		var navmesh = Global.MMapMgr.GetNavMesh(terrainMapId);

		if (navmesh == null)
		{
			handler.SendSysMessage("NavMesh not loaded for current map.");

			return true;
		}

		uint tileCount = 0;
		var nodeCount = 0;
		var polyCount = 0;
		var vertCount = 0;
		var triCount = 0;
		var triVertCount = 0;

		for (var i = 0; i < navmesh.getMaxTiles(); ++i)
		{
			var tile = navmesh.getTile(i);

			if (tile.header == null)
				continue;

			tileCount++;
			nodeCount += tile.header.bvNodeCount;
			polyCount += tile.header.polyCount;
			vertCount += tile.header.vertCount;
			triCount += tile.header.detailTriCount;
			triVertCount += tile.header.detailVertCount;
		}

		handler.SendSysMessage("Navmesh stats:");
		handler.SendSysMessage(" {0} tiles loaded", tileCount);
		handler.SendSysMessage(" {0} BVTree nodes", nodeCount);
		handler.SendSysMessage(" {0} polygons ({1} vertices)", polyCount, vertCount);
		handler.SendSysMessage(" {0} triangles ({1} vertices)", triCount, triVertCount);

		return true;
	}

	[Command("testarea", RBACPermissions.CommandMmapTestarea)]
	static bool HandleMmapTestArea(CommandHandler handler)
	{
		var radius = 40.0f;
		WorldObject obj = handler.Player;

		// Get Creatures
		List<Unit> creatureList = new();

		var go_check = new AnyUnitInObjectRangeCheck(obj, radius);
		var go_search = new UnitListSearcher(obj, creatureList, go_check, GridType.Grid);

		Cell.VisitGrid(obj, go_search, radius);

		if (!creatureList.Empty())
		{
			handler.SendSysMessage("Found {0} Creatures.", creatureList.Count);

			uint paths = 0;
			var uStartTime = Time.MSTime;

			foreach (var creature in creatureList)
			{
				PathGenerator path = new(creature);
				path.CalculatePath(obj.Location);
				++paths;
			}

			var uPathLoadTime = Time.GetMSTimeDiffToNow(uStartTime);
			handler.SendSysMessage("Generated {0} paths in {1} ms", paths, uPathLoadTime);
		}
		else
		{
			handler.SendSysMessage("No creatures in {0} yard range.", radius);
		}

		return true;
	}
}