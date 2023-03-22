// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Framework.Constants;
using Game.DataStorage;
using Game.Maps;
using Game.Scripting.Interfaces.ITransport;

namespace Game.Entities;

public class Transport : GameObject, ITransport
{
	readonly TimeTracker _positionChangeTimer = new();
	readonly HashSet<WorldObject> _passengers = new();
	readonly HashSet<WorldObject> _staticPassengers = new();

	TransportTemplate _transportInfo;

	TransportMovementState _movementState;
	BitArray _eventsToTrigger;
	int _currentPathLeg;
	uint? _requestStopTimestamp;
	uint _pathProgress;

	bool _delayedAddModel;

	public Transport()
	{
		_updateFlag.ServerTime = true;
		_updateFlag.Stationary = true;
		_updateFlag.Rotation = true;
	}

	public void AddPassenger(WorldObject passenger)
	{
		if (!IsInWorld)
			return;

		lock (_staticPassengers)
		{
			if (_passengers.Add(passenger))
			{
				passenger.SetTransport(this);
				passenger.MovementInfo.Transport.Guid = GUID;

				var player = passenger.AsPlayer;

				if (player)
					Global.ScriptMgr.RunScript<ITransportOnAddPassenger>(p => p.OnAddPassenger(this, player), ScriptId);
			}
		}
	}

	public ITransport RemovePassenger(WorldObject passenger)
	{
		lock (_staticPassengers)
		{
			if (_passengers.Remove(passenger) || _staticPassengers.Remove(passenger)) // static passenger can remove itself in case of grid unload
			{
				passenger.SetTransport(null);
				passenger.MovementInfo.Transport.Reset();
				Log.outDebug(LogFilter.Transport, "Object {0} removed from transport {1}.", passenger.GetName(), GetName());

				var plr = passenger.AsPlayer;

				if (plr != null)
				{
					Global.ScriptMgr.RunScript<ITransportOnRemovePassenger>(p => p.OnRemovePassenger(this, plr), ScriptId);
					plr.SetFallInformation(0, plr.Location.Z);
				}
			}
		}

		return this;
	}

	public void CalculatePassengerPosition(Position pos)
	{
		ITransport.CalculatePassengerPosition(pos, Location.X, Location.Y, Location.Z, GetTransportOrientation());
	}

	public void CalculatePassengerOffset(Position pos)
	{
		ITransport.CalculatePassengerOffset(pos, Location.X, Location.Y, Location.Z, GetTransportOrientation());
	}

	public int GetMapIdForSpawning()
	{
		return Template.MoTransport.SpawnMap;
	}

	public ObjectGuid GetTransportGUID()
	{
		return GUID;
	}

	public float GetTransportOrientation()
	{
		return Location.Orientation;
	}

	public override void Dispose()
	{
		UnloadStaticPassengers();
		base.Dispose();
	}

	public bool Create(ulong guidlow, uint entry, float x, float y, float z, float ang)
	{
		Location.Relocate(x, y, z, ang);

		if (!Location.IsPositionValid)
		{
			Log.outError(LogFilter.Transport, $"Transport (GUID: {guidlow}) not created. Suggested coordinates isn't valid (X: {x} Y: {y})");

			return false;
		}

		Create(ObjectGuid.Create(HighGuid.Transport, guidlow));

		var goinfo = Global.ObjectMgr.GetGameObjectTemplate(entry);

		if (goinfo == null)
		{
			Log.outError(LogFilter.Sql, $"Transport not created: entry in `gameobject_template` not found, entry: {entry}");

			return false;
		}

		GoInfoProtected = goinfo;
		GoTemplateAddonProtected = Global.ObjectMgr.GetGameObjectTemplateAddon(entry);

		var tInfo = Global.TransportMgr.GetTransportTemplate(entry);

		if (tInfo == null)
		{
			Log.outError(LogFilter.Sql, "Transport {0} (name: {1}) will not be created, missing `transport_template` entry.", entry, goinfo.name);

			return false;
		}

		_transportInfo = tInfo;
		_eventsToTrigger = new BitArray(tInfo.Events.Count, true);

		var goOverride = GameObjectOverride;

		if (goOverride != null)
		{
			Faction = goOverride.Faction;
			ReplaceAllFlags(goOverride.Flags);
		}

		_pathProgress = goinfo.MoTransport.allowstopping == 0 ? Time.MSTime /*might be called before world update loop begins, don't use GameTime*/ % tInfo.TotalPathTime : 0;
		SetPathProgressForClient((float)_pathProgress / (float)tInfo.TotalPathTime);
		ObjectScale = goinfo.size;
		SetPeriod(tInfo.TotalPathTime);
		Entry = goinfo.entry;
		DisplayId = goinfo.displayId;
		SetGoState(goinfo.MoTransport.allowstopping == 0 ? GameObjectState.Ready : GameObjectState.Active);
		GoType = GameObjectTypes.MapObjTransport;
		SetGoAnimProgress(255);
		SetUpdateFieldValue(Values.ModifyValue(GameObjectFieldData).ModifyValue(GameObjectFieldData.SpawnTrackingStateAnimID), Global.DB2Mgr.GetEmptyAnimStateID());
		SetName(goinfo.name);
		SetLocalRotation(0.0f, 0.0f, 0.0f, 1.0f);
		SetParentRotation(Quaternion.Identity);

		var position = _transportInfo.ComputePosition(_pathProgress, out _, out var legIndex);

		if (position != null)
		{
			Location.Relocate(position.X, position.Y, position.Z, position.Orientation);
			_currentPathLeg = legIndex;
		}

		CreateModel();

		return true;
	}

	public override void CleanupsBeforeDelete(bool finalCleanup)
	{
		UnloadStaticPassengers();

		while (!_passengers.Empty())
		{
			var obj = _passengers.FirstOrDefault();
			RemovePassenger(obj);
		}

		base.CleanupsBeforeDelete(finalCleanup);
	}

	public override void Update(uint diff)
	{
		var positionUpdateDelay = TimeSpan.FromMilliseconds(200);

		if (AI != null)
			AI.UpdateAI(diff);
		else if (!AIM_Initialize())
			Log.outError(LogFilter.Transport, "Could not initialize GameObjectAI for Transport");

		Global.ScriptMgr.RunScript<ITransportOnUpdate>(p => p.OnUpdate(this, diff), ScriptId);

		_positionChangeTimer.Update(diff);

		var cycleId = _pathProgress / GetTransportPeriod();

		if (Template.MoTransport.allowstopping == 0)
			_pathProgress = GameTime.GetGameTimeMS();
		else if (!_requestStopTimestamp.HasValue || _requestStopTimestamp > _pathProgress + diff)
			_pathProgress += diff;
		else
			_pathProgress = _requestStopTimestamp.Value;

		if (_pathProgress / GetTransportPeriod() != cycleId)
			// reset cycle
			_eventsToTrigger.SetAll(true);

		SetPathProgressForClient((float)_pathProgress / (float)GetTransportPeriod());

		var timer = _pathProgress % GetTransportPeriod();

		var eventToTriggerIndex = -1;

		for (var i = 0; i < _eventsToTrigger.Count; i++)
			if (_eventsToTrigger.Get(i))
			{
				eventToTriggerIndex = i;

				break;
			}

		if (eventToTriggerIndex != -1)
			while (eventToTriggerIndex < _transportInfo.Events.Count && _transportInfo.Events[eventToTriggerIndex].Timestamp < timer)
			{
				var leg = _transportInfo.GetLegForTime(_transportInfo.Events[eventToTriggerIndex].Timestamp);

				if (leg != null)
					if (leg.MapId == Location.MapId)
						GameEvents.Trigger(_transportInfo.Events[eventToTriggerIndex].EventId, this, this);

				_eventsToTrigger.Set(eventToTriggerIndex, false);
				++eventToTriggerIndex;
			}

		var newPosition = _transportInfo.ComputePosition(timer, out var moveState, out var legIndex);

		if (newPosition != null)
		{
			var justStopped = _movementState == TransportMovementState.Moving && moveState != TransportMovementState.Moving;
			_movementState = moveState;

			if (justStopped)
				if (_requestStopTimestamp != 0 && GoState != GameObjectState.Ready)
				{
					SetGoState(GameObjectState.Ready);
					SetDynamicFlag(GameObjectDynamicLowFlags.Stopped);
				}

			if (legIndex != _currentPathLeg)
			{
				var oldMapId = _transportInfo.PathLegs[_currentPathLeg].MapId;
				_currentPathLeg = legIndex;
				TeleportTransport(oldMapId, _transportInfo.PathLegs[legIndex].MapId, newPosition.X, newPosition.Y, newPosition.Z, newPosition.Orientation);

				return;
			}

			// set position
			if (_positionChangeTimer.Passed && GetExpectedMapId() == Location.MapId)
			{
				_positionChangeTimer.Reset(positionUpdateDelay);

				if (_movementState == TransportMovementState.Moving || justStopped)
				{
					UpdatePosition(newPosition.X, newPosition.Y, newPosition.Z, newPosition.Orientation);
				}
				else
				{
					/* There are four possible scenarios that trigger loading/unloading passengers:
					  1. transport moves from inactive to active grid
					  2. the grid that transport is currently in becomes active
					  3. transport moves from active to inactive grid
					  4. the grid that transport is currently in unloads
					*/
					var gridActive = Map.IsGridLoaded(Location.X, Location.Y);

					lock (_staticPassengers)
					{
						if (_staticPassengers.Empty() && gridActive) // 2.
							LoadStaticPassengers();
						else if (!_staticPassengers.Empty() && !gridActive)
							// 4. - if transports stopped on grid edge, some passengers can remain in active grids
							//      unload all static passengers otherwise passengers won't load correctly when the grid that transport is currently in becomes active
							UnloadStaticPassengers();
					}
				}
			}
		}

		// Add model to map after we are fully done with moving maps
		if (_delayedAddModel)
		{
			_delayedAddModel = false;

			if (Model != null)
				Map.InsertGameObjectModel(Model);
		}
	}

	public Creature CreateNPCPassenger(ulong guid, CreatureData data)
	{
		var map = Map;

		if (map.GetCreatureRespawnTime(guid) != 0)
			return null;

		var creature = Creature.CreateCreatureFromDB(guid, map, false, true);

		if (!creature)
			return null;

		var spawn = data.SpawnPoint.Copy();

		creature.SetTransport(this);
		creature.MovementInfo.Transport.Guid = GUID;
		creature.MovementInfo.Transport.Pos.Relocate(spawn);
		creature.MovementInfo.Transport.Seat = -1;
		CalculatePassengerPosition(spawn);
		creature.Location.Relocate(spawn);
		creature.SetHomePosition(creature.Location.X, creature.Location.Y, creature.Location.Z, creature.Location.Orientation);
		creature.TransportHomePosition = creature.MovementInfo.Transport.Pos;

		// @HACK - transport models are not added to map's dynamic LoS calculations
		//         because the current GameObjectModel cannot be moved without recreating
		creature.AddUnitState(UnitState.IgnorePathfinding);

		if (!creature.Location.IsPositionValid)
		{
			Log.outError(LogFilter.Transport, "Creature (guidlow {0}, entry {1}) not created. Suggested coordinates aren't valid (X: {2} Y: {3})", creature.GUID.ToString(), creature.Entry, creature.Location.X, creature.Location.Y);

			return null;
		}

		PhasingHandler.InitDbPhaseShift(creature.PhaseShift, data.PhaseUseFlags, data.PhaseId, data.PhaseGroup);
		PhasingHandler.InitDbVisibleMapId(creature.PhaseShift, data.terrainSwapMap);

		if (!map.AddToMap(creature))
			return null;

		lock (_staticPassengers)
		{
			_staticPassengers.Add(creature);
		}

		Global.ScriptMgr.RunScript<ITransportOnAddCreaturePassenger>(p => p.OnAddCreaturePassenger(this, creature), ScriptId);

		return creature;
	}

	public TempSummon SummonPassenger(uint entry, Position pos, TempSummonType summonType, SummonPropertiesRecord properties = null, uint duration = 0, Unit summoner = null, uint spellId = 0, uint vehId = 0)
	{
		var map = Map;

		if (map == null)
			return null;

		var mask = UnitTypeMask.Summon;

		if (properties != null)
			switch (properties.Control)
			{
				case SummonCategory.Pet:
					mask = UnitTypeMask.Guardian;

					break;
				case SummonCategory.Puppet:
					mask = UnitTypeMask.Puppet;

					break;
				case SummonCategory.Vehicle:
					mask = UnitTypeMask.Minion;

					break;
				case SummonCategory.Wild:
				case SummonCategory.Ally:
				case SummonCategory.Unk:
				{
					switch (properties.Title)
					{
						case SummonTitle.Minion:
						case SummonTitle.Guardian:
						case SummonTitle.Runeblade:
							mask = UnitTypeMask.Guardian;

							break;
						case SummonTitle.Totem:
						case SummonTitle.LightWell:
							mask = UnitTypeMask.Totem;

							break;
						case SummonTitle.Vehicle:
						case SummonTitle.Mount:
							mask = UnitTypeMask.Summon;

							break;
						case SummonTitle.Companion:
							mask = UnitTypeMask.Minion;

							break;
						default:
							if (properties.GetFlags().HasFlag(SummonPropertiesFlags.JoinSummonerSpawnGroup)) // Mirror Image, Summon Gargoyle
								mask = UnitTypeMask.Guardian;

							break;
					}

					break;
				}
				default:
					return null;
			}

		TempSummon summon = null;

		switch (mask)
		{
			case UnitTypeMask.Summon:
				summon = new TempSummon(properties, summoner, false);

				break;
			case UnitTypeMask.Guardian:
				summon = new Guardian(properties, summoner, false);

				break;
			case UnitTypeMask.Puppet:
				summon = new Puppet(properties, summoner);

				break;
			case UnitTypeMask.Totem:
				summon = new Totem(properties, summoner);

				break;
			case UnitTypeMask.Minion:
				summon = new Minion(properties, summoner, false);

				break;
		}

		var newPos = pos.Copy();
		CalculatePassengerPosition(newPos);

		if (!summon.Create(map.GenerateLowGuid(HighGuid.Creature), map, entry, newPos, null, vehId))
			return null;

		WorldObject phaseShiftOwner = this;

		if (summoner != null && !(properties != null && properties.GetFlags().HasFlag(SummonPropertiesFlags.IgnoreSummonerPhase)))
			phaseShiftOwner = summoner;

		if (phaseShiftOwner != null)
			PhasingHandler.InheritPhaseShift(summon, phaseShiftOwner);

		summon.SetCreatedBySpell(spellId);

		summon.SetTransport(this);
		summon.MovementInfo.Transport.Guid = GUID;
		summon.MovementInfo.Transport.Pos.Relocate(pos);
		summon.Location.Relocate(newPos);
		summon.HomePosition = newPos;
		summon.TransportHomePosition = pos;

		// @HACK - transport models are not added to map's dynamic LoS calculations
		//         because the current GameObjectModel cannot be moved without recreating
		summon.AddUnitState(UnitState.IgnorePathfinding);

		summon.InitStats(duration);

		if (!map.AddToMap(summon))
			return null;

		lock (_staticPassengers)
		{
			_staticPassengers.Add(summon);
		}

		summon.InitSummon();
		summon.SetTempSummonType(summonType);

		return summon;
	}

	public void UpdatePosition(float x, float y, float z, float o)
	{
		Global.ScriptMgr.RunScript<ITransportOnRelocate>(p => p.OnRelocate(this, Location.MapId, x, y, z), ScriptId);

		var newActive = Map.IsGridLoaded(x, y);
		Cell oldCell = new(Location.X, Location.Y);

		Location.Relocate(x, y, z, o);
		StationaryPosition.Orientation = o;
		UpdateModelPosition();

		UpdatePassengerPositions(_passengers);

		/* There are four possible scenarios that trigger loading/unloading passengers:
		1. transport moves from inactive to active grid
		2. the grid that transport is currently in becomes active
		3. transport moves from active to inactive grid
		4. the grid that transport is currently in unloads
		*/
		lock (_staticPassengers)
		{
			if (_staticPassengers.Empty() && newActive) // 1. and 2.
				LoadStaticPassengers();
			else if (!_staticPassengers.Empty() && !newActive && oldCell.DiffGrid(new Cell(Location.X, Location.Y))) // 3.
				UnloadStaticPassengers();
			else
				UpdatePassengerPositions(_staticPassengers);
		}
		// 4. is handed by grid unload
	}

	public void EnableMovement(bool enabled)
	{
		if (Template.MoTransport.allowstopping == 0)
			return;

		if (!enabled)
		{
			_requestStopTimestamp = (_pathProgress / GetTransportPeriod()) * GetTransportPeriod() + _transportInfo.GetNextPauseWaypointTimestamp(_pathProgress);
		}
		else
		{
			_requestStopTimestamp = null;
			SetGoState(GameObjectState.Active);
			RemoveDynamicFlag(GameObjectDynamicLowFlags.Stopped);
		}
	}

	public void SetDelayedAddModelToMap()
	{
		_delayedAddModel = true;
	}

	public override void BuildUpdate(Dictionary<Player, UpdateData> data_map)
	{
		var players = Map.Players;

		if (players.Empty())
			return;

		foreach (var playerReference in players)
			if (playerReference.InSamePhase(this))
				BuildFieldsUpdate(playerReference, data_map);

		ClearUpdateMask(true);
	}

	public uint GetExpectedMapId()
	{
		return _transportInfo.PathLegs[_currentPathLeg].MapId;
	}

	public HashSet<WorldObject> GetPassengers()
	{
		return _passengers;
	}

	public uint GetTransportPeriod()
	{
		return GameObjectFieldData.Level;
	}

	public void SetPeriod(uint period)
	{
		SetLevel(period);
	}

	public uint GetTimer()
	{
		return _pathProgress;
	}

	GameObject CreateGOPassenger(ulong guid, GameObjectData data)
	{
		var map = Map;

		if (map.GetGORespawnTime(guid) != 0)
			return null;

		var go = CreateGameObjectFromDb(guid, map, false);

		if (!go)
			return null;

		var spawn = data.SpawnPoint.Copy();

		go.SetTransport(this);
		go.MovementInfo.Transport.Guid = GUID;
		go.MovementInfo.Transport.Pos.Relocate(spawn);
		go.MovementInfo.Transport.Seat = -1;
		CalculatePassengerPosition(spawn);
		go.Location.Relocate(spawn);
		go.RelocateStationaryPosition(spawn);

		if (!go.Location.IsPositionValid)
		{
			Log.outError(LogFilter.Transport, "GameObject (guidlow {0}, entry {1}) not created. Suggested coordinates aren't valid (X: {2} Y: {3})", go.GUID.ToString(), go.Entry, go.Location.X, go.Location.Y);

			return null;
		}

		PhasingHandler.InitDbPhaseShift(go.PhaseShift, data.PhaseUseFlags, data.PhaseId, data.PhaseGroup);
		PhasingHandler.InitDbVisibleMapId(go.PhaseShift, data.terrainSwapMap);

		if (!map.AddToMap(go))
			return null;

		lock (_staticPassengers)
		{
			_staticPassengers.Add(go);
		}

		return go;
	}

	void LoadStaticPassengers()
	{
		var mapId = (uint)Template.MoTransport.SpawnMap;
		var cells = Global.ObjectMgr.GetMapObjectGuids(mapId, Map.DifficultyID);

		if (cells == null)
			return;

		foreach (var cell in cells)
		{
			// Creatures on transport
			foreach (var npc in cell.Value.creatures)
				CreateNPCPassenger(npc, Global.ObjectMgr.GetCreatureData(npc));

			// GameObjects on transport
			foreach (var go in cell.Value.gameobjects)
				CreateGOPassenger(go, Global.ObjectMgr.GetGameObjectData(go));
		}
	}

	void UnloadStaticPassengers()
	{
		lock (_staticPassengers)
		{
			while (!_staticPassengers.Empty())
			{
				var obj = _staticPassengers.First();
				obj.AddObjectToRemoveList(); // also removes from _staticPassengers
			}
		}
	}

	bool TeleportTransport(uint oldMapId, uint newMapId, float x, float y, float z, float o)
	{
		if (oldMapId != newMapId)
		{
			UnloadStaticPassengers();
			TeleportPassengersAndHideTransport(newMapId, x, y, z, o);

			return true;
		}
		else
		{
			UpdatePosition(x, y, z, o);

			// Teleport players, they need to know it
			foreach (var obj in _passengers)
				if (obj.IsTypeId(TypeId.Player))
				{
					// will be relocated in UpdatePosition of the vehicle
					var veh = obj.AsUnit.VehicleBase;

					if (veh)
						if (veh.Transport == this)
							continue;

					var pos = obj.MovementInfo.Transport.Pos.Copy();
					ITransport.CalculatePassengerPosition(pos, x, y, z, o);

					obj.AsUnit.NearTeleportTo(pos);
				}

			return false;
		}
	}

	void TeleportPassengersAndHideTransport(uint newMapid, float x, float y, float z, float o)
	{
		if (newMapid == Location.MapId)
		{
			AddToWorld();

			foreach (var player in Map.Players)
				if (player.Transport != this && player.InSamePhase(this))
				{
					UpdateData data = new(Map.Id);
					BuildCreateUpdateBlockForPlayer(data, player);
					player.VisibleTransports.Add(GUID);
					data.BuildPacket(out var packet);
					player.SendPacket(packet);
				}
		}
		else
		{
			UpdateData data = new(Map.Id);
			BuildOutOfRangeUpdateBlock(data);

			data.BuildPacket(out var packet);

			foreach (var player in Map.Players)
				if (player.Transport != this && player.VisibleTransports.Contains(GUID))
				{
					player.SendPacket(packet);
					player.VisibleTransports.Remove(GUID);
				}

			RemoveFromWorld();
		}

		List<WorldObject> passengersToTeleport = new(_passengers);

		foreach (var obj in passengersToTeleport)
		{
			var newPos = obj.MovementInfo.Transport.Pos.Copy();
			ITransport.CalculatePassengerPosition(newPos, x, y, z, o);

			switch (obj.TypeId)
			{
				case TypeId.Player:
					if (!obj.AsPlayer.TeleportTo(newMapid, newPos, TeleportToOptions.NotLeaveTransport))
						RemovePassenger(obj);

					break;
				case TypeId.DynamicObject:
				case TypeId.AreaTrigger:
					obj.AddObjectToRemoveList();

					break;
				default:
					RemovePassenger(obj);

					break;
			}
		}
	}

	void UpdatePassengerPositions(HashSet<WorldObject> passengers)
	{
		foreach (var passenger in passengers)
		{
			var pos = passenger.MovementInfo.Transport.Pos.Copy();
			CalculatePassengerPosition(pos);
			ITransport.UpdatePassengerPosition(this, Map, passenger, pos, true);
		}
	}
}