// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Framework.Constants;
using Framework.Database;
using Framework.IO;
using Forged.RealmServer.DataStorage;
using Forged.RealmServer.Entities;
using Forged.RealmServer.Maps.Grids;

namespace Forged.RealmServer.Chat;

[CommandGroup("gobject")]
class GameObjectCommands
{
	[Command("activate", RBACPermissions.CommandGobjectActivate)]
	static bool HandleGameObjectActivateCommand(CommandHandler handler, ulong guidLow)
	{
		var obj = handler.GetObjectFromPlayerMapByDbGuid(guidLow);

		if (!obj)
		{
			handler.SendSysMessage(CypherStrings.CommandObjnotfound, guidLow);

			return false;
		}

		var autoCloseTime = obj.Template.GetAutoCloseTime() != 0 ? 10000u : 0u;

		// Activate
		obj.SetLootState(LootState.Ready);
		obj.UseDoorOrButton(autoCloseTime, false, handler.Session.Player);

		handler.SendSysMessage("Object activated!");

		return true;
	}

	[Command("delete", RBACPermissions.CommandGobjectDelete)]
	static bool HandleGameObjectDeleteCommand(CommandHandler handler, ulong spawnId)
	{
		var obj = handler.GetObjectFromPlayerMapByDbGuid(spawnId);

		if (obj != null)
		{
			var player = handler.Session.Player;
			var ownerGuid = obj.OwnerGUID;

			if (!ownerGuid.IsEmpty)
			{
				var owner = Global.ObjAccessor.GetUnit(player, ownerGuid);

				if (!owner || !ownerGuid.IsPlayer)
				{
					handler.SendSysMessage(CypherStrings.CommandDelobjrefercreature, ownerGuid.ToString(), obj.GUID.ToString());

					return false;
				}

				owner.RemoveGameObject(obj, false);
			}
		}

		if (GameObject.DeleteFromDB(spawnId))
		{
			handler.SendSysMessage(CypherStrings.CommandDelobjmessage, spawnId);

			return true;
		}

		handler.SendSysMessage(CypherStrings.CommandObjnotfound, obj.GUID.ToString());

		return false;
	}

	[Command("despawngroup", RBACPermissions.CommandGobjectDespawngroup)]
	static bool HandleGameObjectDespawnGroup(CommandHandler handler, string[] opts)
	{
		if (opts == null || opts.Empty())
			return false;

		var deleteRespawnTimes = false;
		uint groupId = 0;

		// Decode arguments
		foreach (var variant in opts)
			if (variant.Equals("removerespawntime", StringComparison.OrdinalIgnoreCase))
				deleteRespawnTimes = true;
			else
				uint.TryParse(variant, out groupId);

		var player = handler.Session.Player;

		if (!player.Map.SpawnGroupDespawn(groupId, deleteRespawnTimes, out var n))
		{
			handler.SendSysMessage(CypherStrings.SpawngroupBadgroup, groupId);

			return false;
		}

		handler.SendSysMessage($"Despawned a total of {n} objects.");

		return true;
	}

	[Command("info", RBACPermissions.CommandGobjectInfo)]
	static bool HandleGameObjectInfoCommand(CommandHandler handler, [OptionalArg] string isGuid, ulong data)
	{
		GameObject thisGO = null;
		GameObjectData spawnData = null;

		uint entry;
		ulong spawnId = 0;

		if (!isGuid.IsEmpty() && isGuid.Equals("guid", StringComparison.OrdinalIgnoreCase))
		{
			spawnId = data;
			spawnData = Global.ObjectMgr.GetGameObjectData(spawnId);

			if (spawnData == null)
			{
				handler.SendSysMessage(CypherStrings.CommandObjnotfound, spawnId);

				return false;
			}

			entry = spawnData.Id;
			thisGO = handler.GetObjectFromPlayerMapByDbGuid(spawnId);
		}
		else
		{
			entry = (uint)data;
		}

		var gameObjectInfo = Global.ObjectMgr.GetGameObjectTemplate(entry);

		if (gameObjectInfo == null)
		{
			handler.SendSysMessage(CypherStrings.GameobjectNotExist, entry);

			return false;
		}

		var type = gameObjectInfo.type;
		var displayId = gameObjectInfo.displayId;
		var name = gameObjectInfo.name;
		var lootId = gameObjectInfo.GetLootId();

		if (type == GameObjectTypes.Chest && lootId == 0)
			lootId = gameObjectInfo.Chest.chestPersonalLoot;

		// If we have a real object, send some info about it
		if (thisGO != null)
		{
			handler.SendSysMessage(CypherStrings.SpawninfoGuidinfo, thisGO.GUID.ToString());
			handler.SendSysMessage(CypherStrings.SpawninfoCompatibilityMode, thisGO.RespawnCompatibilityMode);

			if (thisGO.GameObjectData != null && thisGO.GameObjectData.SpawnGroupData.GroupId != 0)
			{
				var groupData = thisGO.AsGameObject.GameObjectData.SpawnGroupData;
				handler.SendSysMessage(CypherStrings.SpawninfoGroupId, groupData.Name, groupData.GroupId, groupData.Flags, thisGO.Map.IsSpawnGroupActive(groupData.GroupId));
			}

			var goOverride = Global.ObjectMgr.GetGameObjectOverride(spawnId);

			if (goOverride == null)
				goOverride = Global.ObjectMgr.GetGameObjectTemplateAddon(entry);

			if (goOverride != null)
				handler.SendSysMessage(CypherStrings.GoinfoAddon, goOverride.Faction, goOverride.Flags);
		}

		if (spawnData != null)
		{
			spawnData.Rotation.toEulerAnglesZYX(out var yaw, out var pitch, out var roll);
			handler.SendSysMessage(CypherStrings.SpawninfoSpawnidLocation, spawnData.SpawnId, spawnData.SpawnPoint.X, spawnData.SpawnPoint.Y, spawnData.SpawnPoint.Z);
			handler.SendSysMessage(CypherStrings.SpawninfoRotation, yaw, pitch, roll);
		}

		handler.SendSysMessage(CypherStrings.GoinfoEntry, entry);
		handler.SendSysMessage(CypherStrings.GoinfoType, type);
		handler.SendSysMessage(CypherStrings.GoinfoLootid, lootId);
		handler.SendSysMessage(CypherStrings.GoinfoDisplayid, displayId);
		handler.SendSysMessage(CypherStrings.GoinfoName, name);
		handler.SendSysMessage(CypherStrings.GoinfoSize, gameObjectInfo.size);

		handler.SendSysMessage(CypherStrings.ObjectinfoAiInfo, gameObjectInfo.AIName, Global.ObjectMgr.GetScriptName(gameObjectInfo.ScriptId));
		var ai = thisGO != null ? thisGO.AI : null;

		if (ai != null)
			handler.SendSysMessage(CypherStrings.ObjectinfoAiType, nameof(ai));

		var modelInfo = CliDB.GameObjectDisplayInfoStorage.LookupByKey(displayId);

		if (modelInfo != null)
			handler.SendSysMessage(CypherStrings.GoinfoModel, modelInfo.GeoBoxMax.X, modelInfo.GeoBoxMax.Y, modelInfo.GeoBoxMax.Z, modelInfo.GeoBoxMin.X, modelInfo.GeoBoxMin.Y, modelInfo.GeoBoxMin.Z);

		return true;
	}

	[Command("move", RBACPermissions.CommandGobjectMove)]
	static bool HandleGameObjectMoveCommand(CommandHandler handler, ulong guidLow, float[] xyz)
	{
		var obj = handler.GetObjectFromPlayerMapByDbGuid(guidLow);

		if (!obj)
		{
			handler.SendSysMessage(CypherStrings.CommandObjnotfound, guidLow);

			return false;
		}

		Position pos;

		if (xyz != null)
		{
			pos = new Position(xyz[0], xyz[1], xyz[2]);

			if (!GridDefines.IsValidMapCoord(obj.Location.MapId, pos))
			{
				handler.SendSysMessage(CypherStrings.InvalidTargetCoord, pos.X, pos.Y, obj.Location.MapId);

				return false;
			}
		}
		else
		{
			pos = handler.Session.Player.Location;
		}

		var map = obj.Map;

		pos.Orientation = obj.Location.Orientation;
		obj.Location.Relocate(pos);

		// update which cell has this gameobject registered for loading
		Global.ObjectMgr.RemoveGameObjectFromGrid(obj.GameObjectData);
		obj.SaveToDB();
		Global.ObjectMgr.AddGameObjectToGrid(obj.GameObjectData);

		// Generate a completely new spawn with new guid
		// client caches recently deleted objects and brings them back to life
		// when CreateObject block for this guid is received again
		// however it entirely skips parsing that block and only uses already known location
		obj.Delete();

		obj = GameObject.CreateGameObjectFromDb(guidLow, map);

		if (!obj)
			return false;

		handler.SendSysMessage(CypherStrings.CommandMoveobjmessage, obj.SpawnId, obj.Template.name, obj.GUID.ToString());

		return true;
	}

	[Command("near", RBACPermissions.CommandGobjectNear)]
	static bool HandleGameObjectNearCommand(CommandHandler handler, float? dist)
	{
		var distance = dist.GetValueOrDefault(10f);
		uint count = 0;

		var player = handler.Player;

		var stmt = DB.World.GetPreparedStatement(WorldStatements.SEL_GAMEOBJECT_NEAREST);
		stmt.AddValue(0, player.Location.X);
		stmt.AddValue(1, player.Location.Y);
		stmt.AddValue(2, player.Location.Z);
		stmt.AddValue(3, player.Location.MapId);
		stmt.AddValue(4, player.Location.X);
		stmt.AddValue(5, player.Location.Y);
		stmt.AddValue(6, player.Location.Z);
		stmt.AddValue(7, distance * distance);
		var result = DB.World.Query(stmt);

		if (!result.IsEmpty())
			do
			{
				var guid = result.Read<ulong>(0);
				var entry = result.Read<uint>(1);
				var x = result.Read<float>(2);
				var y = result.Read<float>(3);
				var z = result.Read<float>(4);
				var mapId = result.Read<ushort>(5);

				var gameObjectInfo = Global.ObjectMgr.GetGameObjectTemplate(entry);

				if (gameObjectInfo == null)
					continue;

				handler.SendSysMessage(CypherStrings.GoListChat, guid, entry, guid, gameObjectInfo.name, x, y, z, mapId, "", "");

				++count;
			} while (result.NextRow());

		handler.SendSysMessage(CypherStrings.CommandNearobjmessage, distance, count);

		return true;
	}

	[Command("spawngroup", RBACPermissions.CommandGobjectSpawngroup)]
	static bool HandleGameObjectSpawnGroup(CommandHandler handler, StringArguments args)
	{
		if (args.Empty())
			return false;

		var ignoreRespawn = false;
		var force = false;
		uint groupId = 0;

		// Decode arguments
		var arg = args.NextString();

		while (!arg.IsEmpty())
		{
			var thisArg = arg.ToLower();

			if (thisArg == "ignorerespawn")
				ignoreRespawn = true;
			else if (thisArg == "force")
				force = true;
			else if (thisArg.IsEmpty() || !thisArg.IsNumber())
				return false;
			else
				groupId = uint.Parse(thisArg);

			arg = args.NextString();
		}

		var player = handler.Session.Player;

		List<WorldObject> creatureList = new();

		if (!player.Map.SpawnGroupSpawn(groupId, ignoreRespawn, force, creatureList))
		{
			handler.SendSysMessage(CypherStrings.SpawngroupBadgroup, groupId);

			return false;
		}

		handler.SendSysMessage(CypherStrings.SpawngroupSpawncount, creatureList.Count);

		foreach (var obj in creatureList)
			handler.SendSysMessage($"{obj.GetName()} ({obj.GUID})");

		return true;
	}

	[Command("target", RBACPermissions.CommandGobjectTarget)]
	static bool HandleGameObjectTargetCommand(CommandHandler handler, string objectIdStr)
	{
		var player = handler.Session.Player;
		SQLResult result;
		var activeEventsList = Global.GameEventMgr.GetActiveEventList();

		if (objectIdStr.IsEmpty())
		{
			if (uint.TryParse(objectIdStr, out var objectId))
				result = DB.World.Query("SELECT guid, id, position_x, position_y, position_z, orientation, map, PhaseId, PhaseGroup, (POW(position_x - '{0}', 2) + POW(position_y - '{1}', 2) + POW(position_z - '{2}', 2)) AS order_ FROM gameobject WHERE map = '{3}' AND id = '{4}' ORDER BY order_ ASC LIMIT 1",
										player.Location.X,
										player.Location.Y,
										player.Location.Z,
										player.Location.MapId,
										objectId);
			else
				result = DB.World.Query("SELECT guid, id, position_x, position_y, position_z, orientation, map, PhaseId, PhaseGroup, (POW(position_x - {0}, 2) + POW(position_y - {1}, 2) + POW(position_z - {2}, 2)) AS order_ " +
										"FROM gameobject LEFT JOIN gameobject_template ON gameobject_template.entry = gameobject.id WHERE map = {3} AND name LIKE CONCAT('%%', '{4}', '%%') ORDER BY order_ ASC LIMIT 1",
										player.Location.X,
										player.Location.Y,
										player.Location.Z,
										player.Location.MapId,
										objectIdStr);
		}
		else
		{
			StringBuilder eventFilter = new();
			eventFilter.Append(" AND (eventEntry IS NULL ");
			var initString = true;

			foreach (var entry in activeEventsList)
				if (initString)
				{
					eventFilter.Append("OR eventEntry IN (" + entry);
					initString = false;
				}
				else
				{
					eventFilter.Append(',' + entry);
				}

			if (!initString)
				eventFilter.Append("))");
			else
				eventFilter.Append(')');

			result = DB.World.Query("SELECT gameobject.guid, id, position_x, position_y, position_z, orientation, map, PhaseId, PhaseGroup, " +
									"(POW(position_x - {0}, 2) + POW(position_y - {1}, 2) + POW(position_z - {2}, 2)) AS order_ FROM gameobject " +
									"LEFT OUTER JOIN game_event_gameobject on gameobject.guid = game_event_gameobject.guid WHERE map = '{3}' {4} ORDER BY order_ ASC LIMIT 10",
									handler.Session.Player.Location.X,
									handler.Session.Player.Location.Y,
									handler.Session.Player.Location.Z,
									handler.Session.Player.Location.MapId,
									eventFilter.ToString());
		}

		if (result.IsEmpty())
		{
			handler.SendSysMessage(CypherStrings.CommandTargetobjnotfound);

			return true;
		}

		var found = false;
		float x, y, z, o;
		ulong guidLow;
		uint id, phaseId, phaseGroup;
		ushort mapId;
		uint poolId;

		do
		{
			guidLow = result.Read<ulong>(0);
			id = result.Read<uint>(1);
			x = result.Read<float>(2);
			y = result.Read<float>(3);
			z = result.Read<float>(4);
			o = result.Read<float>(5);
			mapId = result.Read<ushort>(6);
			phaseId = result.Read<uint>(7);
			phaseGroup = result.Read<uint>(8);
			poolId = Global.PoolMgr.IsPartOfAPool<GameObject>(guidLow);

			if (poolId == 0 || Global.PoolMgr.IsSpawnedObject<GameObject>(guidLow))
				found = true;
		} while (result.NextRow() && !found);

		if (!found)
		{
			handler.SendSysMessage(CypherStrings.GameobjectNotExist, id);

			return false;
		}

		var objectInfo = Global.ObjectMgr.GetGameObjectTemplate(id);

		if (objectInfo == null)
		{
			handler.SendSysMessage(CypherStrings.GameobjectNotExist, id);

			return false;
		}

		var target = handler.GetObjectFromPlayerMapByDbGuid(guidLow);

		handler.SendSysMessage(CypherStrings.GameobjectDetail, guidLow, objectInfo.name, guidLow, id, x, y, z, mapId, o, phaseId, phaseGroup);

		if (target)
		{
			var curRespawnDelay = (int)(target.RespawnTimeEx - GameTime.GetGameTime());

			if (curRespawnDelay < 0)
				curRespawnDelay = 0;

			var curRespawnDelayStr = Time.secsToTimeString((uint)curRespawnDelay, TimeFormat.ShortText);
			var defRespawnDelayStr = Time.secsToTimeString(target.RespawnDelay, TimeFormat.ShortText);

			handler.SendSysMessage(CypherStrings.CommandRawpawntimes, defRespawnDelayStr, curRespawnDelayStr);
		}

		return true;
	}

	[Command("turn", RBACPermissions.CommandGobjectTurn)]
	static bool HandleGameObjectTurnCommand(CommandHandler handler, ulong guidLow, float? oz, float? oy, float? ox)
	{
		var obj = handler.GetObjectFromPlayerMapByDbGuid(guidLow);

		if (!obj)
		{
			handler.SendSysMessage(CypherStrings.CommandObjnotfound, guidLow);

			return false;
		}

		if (!oz.HasValue)
			oz = handler.Session.Player.Location.Orientation;

		var map = obj.Map;

		obj.Location.Relocate(obj.Location.X, obj.Location.Y, obj.Location.Z, oz.Value);
		obj.SetLocalRotationAngles(oz.Value, oy.GetValueOrDefault(0f), ox.GetValueOrDefault(0f));
		obj.SaveToDB();

		// Generate a completely new spawn with new guid
		// client caches recently deleted objects and brings them back to life
		// when CreateObject block for this guid is received again
		// however it entirely skips parsing that block and only uses already known location
		obj.Delete();

		obj = GameObject.CreateGameObjectFromDb(guidLow, map);

		if (!obj)
			return false;

		handler.SendSysMessage(CypherStrings.CommandTurnobjmessage, obj.SpawnId, obj.Template.name, obj.GUID.ToString(), obj.Location.Orientation);

		return true;
	}

	[CommandGroup("add")]
	class AddCommands
	{
		[Command("", RBACPermissions.CommandGobjectAdd)]
		static bool HandleGameObjectAddCommand(CommandHandler handler, uint objectId, int? spawnTimeSecs)
		{
			if (objectId == 0)
				return false;

			var objectInfo = Global.ObjectMgr.GetGameObjectTemplate(objectId);

			if (objectInfo == null)
			{
				handler.SendSysMessage(CypherStrings.GameobjectNotExist, objectId);

				return false;
			}

			if (objectInfo.displayId != 0 && !CliDB.GameObjectDisplayInfoStorage.ContainsKey(objectInfo.displayId))
			{
				// report to DB errors log as in loading case
				Log.outError(LogFilter.Sql, "Gameobject (Entry {0} GoType: {1}) have invalid displayId ({2}), not spawned.", objectId, objectInfo.type, objectInfo.displayId);
				handler.SendSysMessage(CypherStrings.GameobjectHaveInvalidData, objectId);

				return false;
			}

			var player = handler.Player;
			var map = player.Map;

			var obj = GameObject.CreateGameObject(objectInfo.entry, map, player.Location, Quaternion.CreateFromRotationMatrix(Extensions.fromEulerAnglesZYX(player.Location.Orientation, 0.0f, 0.0f)), 255, GameObjectState.Ready);

			if (!obj)
				return false;

			PhasingHandler.InheritPhaseShift(obj, player);

			if (spawnTimeSecs.HasValue)
				obj.SetRespawnTime(spawnTimeSecs.Value);

			// fill the gameobject data and save to the db
			obj.SaveToDB(map.Id,
						new List<Difficulty>()
						{
							map.DifficultyID
						});

			var spawnId = obj.SpawnId;

			// this will generate a new guid if the object is in an instance
			obj = GameObject.CreateGameObjectFromDb(spawnId, map);

			if (!obj)
				return false;

			// TODO: is it really necessary to add both the real and DB table guid here ?
			Global.ObjectMgr.AddGameObjectToGrid(Global.ObjectMgr.GetGameObjectData(spawnId));
			handler.SendSysMessage(CypherStrings.GameobjectAdd, objectId, objectInfo.name, spawnId, player.Location.X, player.Location.Y, player.Location.Z);

			return true;
		}

		[Command("temp", RBACPermissions.CommandGobjectAddTemp)]
		static bool HandleGameObjectAddTempCommand(CommandHandler handler, uint objectId, ulong? spawntime)
		{
			var player = handler.Player;
			var spawntm = TimeSpan.FromSeconds(spawntime.GetValueOrDefault(300));

			var rotation = Quaternion.CreateFromRotationMatrix(Extensions.fromEulerAnglesZYX(player.Location.Orientation, 0.0f, 0.0f));

			if (Global.ObjectMgr.GetGameObjectTemplate(objectId) == null)
			{
				handler.SendSysMessage(CypherStrings.GameobjectNotExist, objectId);

				return false;
			}

			player.SummonGameObject(objectId, player.Location, rotation, spawntm);

			return true;
		}
	}

	[CommandGroup("set")]
	class SetCommands
	{
		[Command("phase", RBACPermissions.CommandGobjectSetPhase)]
		static bool HandleGameObjectSetPhaseCommand(CommandHandler handler, ulong guidLow, uint phaseId)
		{
			if (guidLow == 0)
				return false;

			var obj = handler.GetObjectFromPlayerMapByDbGuid(guidLow);

			if (!obj)
			{
				handler.SendSysMessage(CypherStrings.CommandObjnotfound, guidLow);

				return false;
			}

			if (phaseId == 0)
			{
				handler.SendSysMessage(CypherStrings.BadValue);

				return false;
			}

			PhasingHandler.AddPhase(obj, phaseId, true);
			obj.SaveToDB();

			return true;
		}

		[Command("state", RBACPermissions.CommandGobjectSetState)]
		static bool HandleGameObjectSetStateCommand(CommandHandler handler, ulong guidLow, int objectType, uint? objectState)
		{
			if (guidLow == 0)
				return false;

			var obj = handler.GetObjectFromPlayerMapByDbGuid(guidLow);

			if (!obj)
			{
				handler.SendSysMessage(CypherStrings.CommandObjnotfound, guidLow);

				return false;
			}

			if (objectType < 0)
			{
				if (objectType == -1)
					obj.SendGameObjectDespawn();
				else if (objectType == -2)
					return false;

				return true;
			}

			if (objectState == 0)
				return false;

			switch (objectType)
			{
				case 0:
					obj.SetGoState((GameObjectState)objectState);

					break;
				case 1:
					obj.GoType = (GameObjectTypes)objectState;

					break;
				case 2:
					obj.GoArtKit = objectState.Value;

					break;
				case 3:
					obj.SetGoAnimProgress(objectState.Value);

					break;
				case 4:
					obj.SendCustomAnim(objectState.Value);

					break;
				case 5:
					if (objectState < 0 || objectState > (uint)GameObjectDestructibleState.Rebuilding)
						return false;

					obj.SetDestructibleState((GameObjectDestructibleState)objectState);

					break;
				default:
					break;
			}

			handler.SendSysMessage("Set gobject type {0} state {1}", objectType, objectState);

			return true;
		}
	}
}