// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks.Dataflow;
using Framework.Configuration;
using Framework.Constants;
using Framework.Database;
using Framework.Threading;
using Game.Collision;
using Game.DataStorage;
using Game.Entities;
using Game.Maps.Grids;
using Game.Maps.Interfaces;
using Game.Networking;
using Game.Networking.Packets;
using Game.Scripting.Interfaces.IMap;
using Game.Scripting.Interfaces.IPlayer;
using Game.Scripting.Interfaces.IWorldState;

namespace Game.Maps;

public class Map : IDisposable
{
	private readonly LimitedThreadTaskManager _threadManager = new(ConfigMgr.GetDefaultValue("Map.ParellelUpdateTasks", 20));
	private readonly ActionBlock<uint> _processRelocationQueue;
	private readonly LimitedThreadTaskManager _processTransportaionQueue = new(1);
	private readonly Dictionary<uint, Dictionary<uint, object>> _locks = new();
	private readonly object _scriptLock = new();
	private readonly List<Creature> _creaturesToMove = new();
	private readonly List<GameObject> _gameObjectsToMove = new();
	private readonly List<DynamicObject> _dynamicObjectsToMove = new();
	private readonly List<AreaTrigger> _areaTriggersToMove = new();
	private readonly DynamicMapTree _dynamicTree = new();
	private readonly SortedSet<RespawnInfo> _respawnTimes = new(new CompareRespawnInfo());
	private readonly Dictionary<ulong, RespawnInfo> _creatureRespawnTimesBySpawnId = new();
	private readonly Dictionary<ulong, RespawnInfo> _gameObjectRespawnTimesBySpawnId = new();
	private readonly List<uint> _toggledSpawnGroupIds = new();
	private readonly Dictionary<uint, uint> _zonePlayerCountMap = new();
	private readonly List<Transport> _transports = new();
	private readonly MapRecord _mapRecord;
	private readonly List<WorldObject> _objectsToRemove = new();
	private readonly Dictionary<WorldObject, bool> _objectsToSwitch = new();
	private readonly Difficulty _spawnMode;
	private readonly List<WorldObject> _worldObjects = new();
	private readonly TerrainInfo _terrain;
	private readonly SortedDictionary<long, List<ScriptAction>> _scriptSchedule = new();
	private readonly BitSet _markedCells = new(MapConst.TotalCellsPerMap * MapConst.TotalCellsPerMap);
	private readonly Dictionary<uint, ZoneDynamicInfo> _zoneDynamicInfo = new();
	private readonly IntervalTimer _weatherUpdateTimer;
	private readonly Dictionary<HighGuid, ObjectGuidGenerator> _guidGenerators = new();
	private readonly Dictionary<ObjectGuid, WorldObject> _objectsStore = new();
	private readonly MultiMap<ulong, Creature> _creatureBySpawnIdStore = new();
	private readonly MultiMap<ulong, GameObject> _gameobjectBySpawnIdStore = new();
	private readonly MultiMap<ulong, AreaTrigger> _areaTriggerBySpawnIdStore = new();
	private readonly MultiMap<uint, Corpse> _corpsesByCell = new();
	private readonly Dictionary<ObjectGuid, Corpse> _corpsesByPlayer = new();
	private readonly List<Corpse> _corpseBones = new();
	private readonly List<WorldObject> _updateObjects = new();
	private readonly Queue<Action<Map>> _farSpellCallbacks = new();
	private readonly MultiPersonalPhaseTracker _multiPersonalPhaseTracker = new();
	private readonly Dictionary<int, int> _worldStateValues = new();
	private readonly List<WorldObject> _activeNonPlayers = new();
#if DEBUGMETRIC
	private readonly MetricFactory _metricFactory = new MetricFactory(500, true);
#endif

	private long _gridExpiry;
	private uint _respawnCheckTimer;
	private readonly SpawnedPoolData _poolData;

	public Dictionary<ulong, CreatureGroup> CreatureGroupHolder { get; set; } = new();

	protected List<Player> ActivePlayers { get; } = new();
	internal object MapLock { get; set; } = new();
	internal uint InstanceIdInternal { get; set; }
	internal uint UnloadTimer { get; set; }

	public int VisibilityNotifyPeriod { get; set; }
	public float VisibleDistance { get; set; }

	public Dictionary<uint, Dictionary<uint, Grid>> Grids { get; } = new();

	public string MapName => _mapRecord.MapName[Global.WorldMgr.DefaultDbcLocale];

	public MapRecord Entry => _mapRecord;

	public float VisibilityRange => VisibleDistance;

	public TerrainInfo Terrain => _terrain;

	public uint InstanceId => InstanceIdInternal;

	public Difficulty DifficultyID => _spawnMode;

	public MapDifficultyRecord MapDifficulty => Global.DB2Mgr.GetMapDifficultyData(Id, DifficultyID);

	public uint Id => _mapRecord.Id;

	public bool Instanceable => _mapRecord != null && _mapRecord.Instanceable();

	public bool IsDungeon => _mapRecord != null && _mapRecord.IsDungeon();

	public bool IsNonRaidDungeon => _mapRecord != null && _mapRecord.IsNonRaidDungeon();

	public bool IsRaid => _mapRecord != null && _mapRecord.IsRaid();

	public bool IsHeroic
	{
		get
		{
			var difficulty = CliDB.DifficultyStorage.LookupByKey(_spawnMode);

			if (difficulty != null)
				return difficulty.Flags.HasAnyFlag(DifficultyFlags.Heroic);

			return false;
		}
	}

	// since 25man difficulties are 1 and 3, we can check them like that
	public bool Is25ManRaid => IsRaid && (_spawnMode == Difficulty.Raid25N || _spawnMode == Difficulty.Raid25HC);

	public bool IsBattleground => _mapRecord != null && _mapRecord.IsBattleground();

	public bool IsBattleArena => _mapRecord != null && _mapRecord.IsBattleArena();

	public bool IsBattlegroundOrArena => _mapRecord != null && _mapRecord.IsBattlegroundOrArena();

	public bool IsScenario => _mapRecord != null && _mapRecord.IsScenario();

	public bool IsGarrison => _mapRecord != null && _mapRecord.IsGarrison();

	public bool HavePlayers => !ActivePlayers.Empty();

	public List<Player> Players => ActivePlayers;

	public int ActiveNonPlayersCount => _activeNonPlayers.Count;

	public Dictionary<ObjectGuid, WorldObject> ObjectsStore => _objectsStore;

	public MultiMap<ulong, Creature> CreatureBySpawnIdStore => _creatureBySpawnIdStore;

	public MultiMap<ulong, GameObject> GameObjectBySpawnIdStore => _gameobjectBySpawnIdStore;

	public MultiMap<ulong, AreaTrigger> AreaTriggerBySpawnIdStore => _areaTriggerBySpawnIdStore;

	public InstanceMap ToInstanceMap => IsDungeon ? (this as InstanceMap) : null;

	public BattlegroundMap ToBattlegroundMap => IsBattlegroundOrArena ? (this as BattlegroundMap) : null;

	public MultiPersonalPhaseTracker MultiPersonalPhaseTracker => _multiPersonalPhaseTracker;

	public SpawnedPoolData PoolData => _poolData;

	public Map(uint id, long expiry, uint instanceId, Difficulty spawnmode)
	{
		_mapRecord = CliDB.MapStorage.LookupByKey(id);
		_spawnMode = spawnmode;
		InstanceIdInternal = instanceId;
		VisibleDistance = SharedConst.DefaultVisibilityDistance;
		VisibilityNotifyPeriod = SharedConst.DefaultVisibilityNotifyPeriod;
		_gridExpiry = expiry;
		_terrain = Global.TerrainMgr.LoadTerrain(id);
		_zonePlayerCountMap.Clear();

		//lets initialize visibility distance for map
		InitVisibilityDistance();
		_weatherUpdateTimer = new IntervalTimer();
		_weatherUpdateTimer.Interval = 1 * Time.InMilliseconds;

		GetGuidSequenceGenerator(HighGuid.Transport).Set(Global.ObjectMgr.GetGenerator(HighGuid.Transport).GetNextAfterMaxUsed());

		_poolData = Global.PoolMgr.InitPoolsForMap(this);

		Global.TransportMgr.CreateTransportsForMap(this);

		Global.MMapMgr.LoadMapInstance(Global.WorldMgr.DataPath, Id, InstanceIdInternal);

		_worldStateValues = Global.WorldStateMgr.GetInitialWorldStatesForMap(this);

		Global.OutdoorPvPMgr.CreateOutdoorPvPForMap(this);
		Global.BattleFieldMgr.CreateBattlefieldsForMap(this);

		_processRelocationQueue = new ActionBlock<uint>(ProcessRelocationNotifies,
														new ExecutionDataflowBlockOptions()
														{
															MaxDegreeOfParallelism = 1,
															EnsureOrdered = true,
															MaxMessagesPerTask = 1
														});

		OnCreateMap(this);
	}

	public void Dispose()
	{
		OnDestroyMap(this);

		// Delete all waiting spawns
		// This doesn't delete from database.
		UnloadAllRespawnInfos();

		for (var i = 0; i < _worldObjects.Count; ++i)
		{
			var obj = _worldObjects[i];
			obj.RemoveFromWorld();
			obj.ResetMap();
		}

		if (!_scriptSchedule.Empty())
			Global.MapMgr.DecreaseScheduledScriptCount((uint)_scriptSchedule.Sum(kvp => kvp.Value.Count));

		Global.OutdoorPvPMgr.DestroyOutdoorPvPForMap(this);
		Global.BattleFieldMgr.DestroyBattlefieldsForMap(this);

		Global.MMapMgr.UnloadMapInstance(Id, InstanceIdInternal);
	}

	public IEnumerable<uint> GridXKeys()
	{
		return Grids.Keys.ToList();
	}

	public IEnumerable<uint> GridYKeys(uint x)
	{
		lock (Grids)
		{
			if (Grids.TryGetValue(x, out var yGrid))
				return yGrid.Keys.ToList();
		}

		return Enumerable.Empty<uint>();
	}

	public void LoadAllCells()
	{
		var _manager = new LimitedThreadTaskManager(50);

		for (uint cellX = 0; cellX < MapConst.TotalCellsPerMap; cellX++)
			for (uint cellY = 0; cellY < MapConst.TotalCellsPerMap; cellY++)
				_manager.Schedule(() =>
									LoadGrid((cellX + 0.5f - MapConst.CenterGridCellId) * MapConst.SizeofCells, (cellY + 0.5f - MapConst.CenterGridCellId) * MapConst.SizeofCells));

		_manager.Wait();
	}

	public virtual void InitVisibilityDistance()
	{
		//init visibility for continents
		VisibleDistance = Global.WorldMgr.MaxVisibleDistanceOnContinents;
		VisibilityNotifyPeriod = Global.WorldMgr.VisibilityNotifyPeriodOnContinents;
	}

	public void AddToGrid<T>(T obj, Cell cell) where T : WorldObject
	{
		var grid = GetGrid(cell.GetGridX(), cell.GetGridY());

		switch (obj.TypeId)
		{
			case TypeId.Corpse:
				if (grid.IsGridObjectDataLoaded())
				{
					// Corpses are a special object type - they can be added to grid via a call to AddToMap
					// or loaded through ObjectGridLoader.
					// Both corpses loaded from database and these freshly generated by Player::CreateCoprse are added to _corpsesByCell
					// ObjectGridLoader loads all corpses from _corpsesByCell even if they were already added to grid before it was loaded
					// so we need to explicitly check it here (Map::AddToGrid is only called from Player::BuildPlayerRepop, not from ObjectGridLoader)
					// to avoid failing an assertion in GridObject::AddToGrid
					if (obj.IsWorldObject())
					{
						obj.Location.SetCurrentCell(cell);
						grid.GetGridCell(cell.GetCellX(), cell.GetCellY()).AddWorldObject(obj);
					}
					else
					{
						grid.GetGridCell(cell.GetCellX(), cell.GetCellY()).AddGridObject(obj);
					}
				}

				return;
			case TypeId.GameObject:
			case TypeId.AreaTrigger:
				grid.GetGridCell(cell.GetCellX(), cell.GetCellY()).AddGridObject(obj);

				break;
			case TypeId.DynamicObject:
			default:
				if (obj.IsWorldObject())
					grid.GetGridCell(cell.GetCellX(), cell.GetCellY()).AddWorldObject(obj);
				else
					grid.GetGridCell(cell.GetCellX(), cell.GetCellY()).AddGridObject(obj);

				break;
		}

		obj.Location.SetCurrentCell(cell);
	}

	public void RemoveFromGrid(WorldObject obj, Cell cell)
	{
		if (cell == null)
			return;

		var grid = GetGrid(cell.GetGridX(), cell.GetGridY());

		if (grid == null)
			return;

		if (obj.IsWorldObject())
			grid.GetGridCell(cell.GetCellX(), cell.GetCellY()).RemoveWorldObject(obj);
		else
			grid.GetGridCell(cell.GetCellX(), cell.GetCellY()).RemoveGridObject(obj);

		obj.Location.SetCurrentCell(null);
	}

	public virtual void LoadGridObjects(Grid grid, Cell cell)
	{
		if (grid == null)
			return;

		ObjectGridLoader loader = new(grid, this, cell, GridType.Grid);
		loader.LoadN();
	}

	public void LoadGrid(float x, float y)
	{
		EnsureGridLoaded(new Cell(x, y));
	}

	public void LoadGridForActiveObject(float x, float y, WorldObject obj)
	{
		EnsureGridLoadedForActiveObject(new Cell(x, y), obj);
	}

	public virtual bool AddPlayerToMap(Player player, bool initPlayer = true)
	{
		var cellCoord = GridDefines.ComputeCellCoord(player.Location.X, player.Location.Y);

		if (!cellCoord.IsCoordValid())
		{
			Log.outError(LogFilter.Maps,
						"Map.AddPlayer (GUID: {0}) has invalid coordinates X:{1} Y:{2}",
						player.GUID.ToString(),
						player.Location.X,
						player.Location.Y);

			return false;
		}

		var cell = new Cell(cellCoord);
		EnsureGridLoadedForActiveObject(cell, player);
		AddToGrid(player, cell);

		player.Map = this;
		player.AddToWorld();

		if (initPlayer)
			SendInitSelf(player);

		SendInitTransports(player);

		if (initPlayer)
			player.ClientGuiDs.Clear();

		player.UpdateObjectVisibility(false);
		PhasingHandler.SendToPlayer(player);

		if (player.IsAlive)
			ConvertCorpseToBones(player.GUID);

		ActivePlayers.Add(player);

		OnPlayerEnterMap(this, player);

		return true;
	}

	public void UpdatePersonalPhasesForPlayer(Player player)
	{
		Cell cell = new(player.Location.X, player.Location.Y);
		MultiPersonalPhaseTracker.OnOwnerPhaseChanged(player, GetGrid(cell.GetGridX(), cell.GetGridY()), this, cell);
	}

	public int GetWorldStateValue(int worldStateId)
	{
		return _worldStateValues.LookupByKey(worldStateId);
	}

	public Dictionary<int, int> GetWorldStateValues()
	{
		return _worldStateValues;
	}

	public void SetWorldStateValue(int worldStateId, int value, bool hidden)
	{
		var oldValue = 0;

		if (!_worldStateValues.TryAdd(worldStateId, 0))
		{
			oldValue = _worldStateValues[worldStateId];

			if (oldValue == value)
				return;
		}

		_worldStateValues[worldStateId] = value;

		var worldStateTemplate = Global.WorldStateMgr.GetWorldStateTemplate(worldStateId);

		if (worldStateTemplate != null)
			Global.ScriptMgr.RunScript<IWorldStateOnValueChange>(script => script.OnValueChange(worldStateTemplate.Id, oldValue, value, this), worldStateTemplate.ScriptId);

		// Broadcast update to all players on the map
		UpdateWorldState updateWorldState = new();
		updateWorldState.VariableID = (uint)worldStateId;
		updateWorldState.Value = value;
		updateWorldState.Hidden = hidden;
		updateWorldState.Write();

		foreach (var player in Players)
		{
			if (worldStateTemplate != null && !worldStateTemplate.AreaIds.Empty())
			{
				var isInAllowedArea = worldStateTemplate.AreaIds.Any(requiredAreaId => Global.DB2Mgr.IsInArea(player.Area, requiredAreaId));

				if (!isInAllowedArea)
					continue;
			}

			player.SendPacket(updateWorldState);
		}
	}

	public bool AddToMap(WorldObject obj)
	{
		//TODO: Needs clean up. An object should not be added to map twice.
		if (obj.IsInWorld)
		{
			obj.UpdateObjectVisibility(true);

			return true;
		}

		var cellCoord = GridDefines.ComputeCellCoord(obj.Location.X, obj.Location.Y);

		if (!cellCoord.IsCoordValid())
		{
			Log.outError(LogFilter.Maps,
						"Map.Add: Object {0} has invalid coordinates X:{1} Y:{2} grid cell [{3}:{4}]",
						obj.GUID,
						obj.Location.X,
						obj.Location.Y,
						cellCoord.X_Coord,
						cellCoord.Y_Coord);

			return false; //Should delete object
		}

		var cell = new Cell(cellCoord);

		if (obj.IsActiveObject)
			EnsureGridLoadedForActiveObject(cell, obj);
		else
			EnsureGridCreated(new GridCoord(cell.GetGridX(), cell.GetGridY()));

		AddToGrid(obj, cell);
		Log.outDebug(LogFilter.Maps, "Object {0} enters grid[{1}, {2}]", obj.GUID.ToString(), cell.GetGridX(), cell.GetGridY());

		obj.AddToWorld();

		InitializeObject(obj);

		if (obj.IsActiveObject)
			AddToActive(obj);

		//something, such as vehicle, needs to be update immediately
		//also, trigger needs to cast spell, if not update, cannot see visual
		obj.SetIsNewObject(true);
		obj.UpdateObjectVisibilityOnCreate();
		obj.SetIsNewObject(false);

		return true;
	}

	public bool AddToMap(Transport obj)
	{
		//TODO: Needs clean up. An object should not be added to map twice.
		if (obj.IsInWorld)
			return true;

		var cellCoord = GridDefines.ComputeCellCoord(obj.Location.X, obj.Location.Y);

		if (!cellCoord.IsCoordValid())
		{
			Log.outError(LogFilter.Maps,
						"Map.Add: Object {0} has invalid coordinates X:{1} Y:{2} grid cell [{3}:{4}]",
						obj.GUID,
						obj.Location.X,
						obj.Location.Y,
						cellCoord.X_Coord,
						cellCoord.Y_Coord);

			return false; //Should delete object
		}

		_transports.Add(obj);

		if (obj.GetExpectedMapId() == Id)
		{
			obj.AddToWorld();

			// Broadcast creation to players
			foreach (var player in Players)
				if (player.Transport != obj && player.InSamePhase(obj))
				{
					var data = new UpdateData(Id);
					obj.BuildCreateUpdateBlockForPlayer(data, player);
					player.VisibleTransports.Add(obj.GUID);
					data.BuildPacket(out var packet);
					player.SendPacket(packet);
				}
		}

		return true;
	}

	public bool IsGridLoaded(uint gridId)
	{
		return IsGridLoaded(gridId % MapConst.MaxGrids, gridId / MapConst.MaxGrids);
	}

	public bool IsGridLoaded(float x, float y)
	{
		return IsGridLoaded(GridDefines.ComputeGridCoord(x, y));
	}

	public bool IsGridLoaded(Position pos)
	{
		return IsGridLoaded(pos.X, pos.Y);
	}

	public bool IsGridLoaded(uint x, uint y)
	{
		return (GetGrid(x, y) != null && IsGridObjectDataLoaded(x, y));
	}

	public bool IsGridLoaded(GridCoord p)
	{
		return (GetGrid(p.X_Coord, p.Y_Coord) != null && IsGridObjectDataLoaded(p.X_Coord, p.Y_Coord));
	}

	public void UpdatePlayerZoneStats(uint oldZone, uint newZone)
	{
		// Nothing to do if no change
		if (oldZone == newZone)
			return;

		if (oldZone != MapConst.InvalidZone)
		{
			--_zonePlayerCountMap[oldZone];
		}

		if (!_zonePlayerCountMap.ContainsKey(newZone))
			_zonePlayerCountMap[newZone] = 0;

		++_zonePlayerCountMap[newZone];
	}

	public virtual void Update(uint diff)
	{
#if DEBUGMETRIC
        _metricFactory.Meter("_dynamicTree Update").StartMark();
#endif

		_dynamicTree.Update(diff);

#if DEBUGMETRIC
        _metricFactory.Meter("_dynamicTree Update").StopMark();
#endif

		// update worldsessions for existing players
		for (var i = 0; i < ActivePlayers.Count; ++i)
		{
			var player = ActivePlayers[i];

			if (player.IsInWorld)
			{
				var session = player.Session;
				_threadManager.Schedule(() => session.UpdateMap(diff));
			}
		}

		/// process any due respawns
		if (_respawnCheckTimer <= diff)
		{
			_threadManager.Schedule(ProcessRespawns);
			_threadManager.Schedule(UpdateSpawnGroupConditions);
			_respawnCheckTimer = WorldConfig.GetUIntValue(WorldCfg.RespawnMinCheckIntervalMs);
		}
		else
		{
			_respawnCheckTimer -= diff;
		}

#if DEBUGMETRIC
        _metricFactory.Meter("_respawnCheckTimer & MapSessionFilter Update").StartMark();
#endif
		_threadManager.Wait();

#if DEBUGMETRIC
        _metricFactory.Meter("_respawnCheckTimer & MapSessionFilter Update").StopMark();
#endif
		// update active cells around players and active objects
		ResetMarkedCells();

		var update = new UpdaterNotifier(diff, GridType.All);

#if DEBUGMETRIC
        _metricFactory.Meter("Load UpdaterNotifier").StartMark();
#endif
		for (var i = 0; i < ActivePlayers.Count; ++i)
		{
			var player = ActivePlayers[i];

			if (!player.IsInWorld)
				continue;

			// update players at tick
			_threadManager.Schedule(() => player.Update(diff));

			_threadManager.Schedule(() => VisitNearbyCellsOf(player, update));

			// If player is using far sight or mind vision, visit that object too
			var viewPoint = player.Viewpoint;

			if (viewPoint)
				_threadManager.Schedule(() => VisitNearbyCellsOf(viewPoint, update));

			List<Unit> toVisit = new();

			// Handle updates for creatures in combat with player and are more than 60 yards away
			if (player.IsInCombat)
			{
				foreach (var pair in player.GetCombatManager().PvECombatRefs)
				{
					var unit = pair.Value.GetOther(player).AsCreature;

					if (unit != null)
						if (unit.Location.MapId == player.Location.MapId && !unit.IsWithinDistInMap(player, VisibilityRange, false))
							toVisit.Add(unit);
				}

				foreach (var unit in toVisit)
					_threadManager.Schedule(() => VisitNearbyCellsOf(unit, update));
			}

			// Update any creatures that own auras the player has applications of
			toVisit.Clear();

			player.GetAppliedAurasQuery()
				.IsPlayer(false)
				.ForEachResult(aur =>
				{
					var caster = aur.Base.Caster;

					if (caster != null)
						if (!caster.IsWithinDistInMap(player, VisibilityRange, false))
							toVisit.Add(caster);
				});

			foreach (var unit in toVisit)
				_threadManager.Schedule(() => VisitNearbyCellsOf(unit, update));

			// Update player's summons
			toVisit.Clear();

			// Totems
			foreach (var summonGuid in player.SummonSlot)
				if (!summonGuid.IsEmpty)
				{
					var unit = GetCreature(summonGuid);

					if (unit != null)
						if (unit.Location.MapId == player.Location.MapId && !unit.IsWithinDistInMap(player, VisibilityRange, false))
							toVisit.Add(unit);
				}

			foreach (var unit in toVisit)
				_threadManager.Schedule(() => VisitNearbyCellsOf(unit, update));
		}

		for (var i = 0; i < _activeNonPlayers.Count; ++i)
		{
			var obj = _activeNonPlayers[i];

			if (!obj.IsInWorld)
				continue;

			VisitNearbyCellsOf(obj, update);
		}

#if DEBUGMETRIC
        _metricFactory.Meter("Load UpdaterNotifier").StopMark();

        // all the visits are queued in the thread manager, we wait to gather all the world objects that need
        // updating. Also guarntees objects only get updated once.

        _metricFactory.Meter("VisitNearbyCellsOf Update").StartMark();
#endif
		_threadManager.Wait();

#if DEBUGMETRIC
        _metricFactory.Meter("VisitNearbyCellsOf Update").StopMark();
#endif
#if DEBUGMETRIC
        _metricFactory.Meter("update.ExecuteUpdate").StartMark();
#endif
		update.ExecuteUpdate();
#if DEBUGMETRIC
        _metricFactory.Meter("update.ExecuteUpdate").StopMark();
#endif
		for (var i = 0; i < _transports.Count; ++i)
		{
			var transport = _transports[i];

			if (!transport)
				continue;

			_processTransportaionQueue.Schedule(() => transport.Update(diff));
		}

#if DEBUGMETRIC
        _metricFactory.Meter("_transports Update").StartMark();
#endif

#if DEBUGMETRIC
        _metricFactory.Meter("_transports Update").StopMark();
        _metricFactory.Meter("SendObjectUpdates Update").StartMark();
#endif
		_threadManager.Schedule(SendObjectUpdates);

		// Process necessary scripts
		if (!_scriptSchedule.Empty())
			lock (_scriptLock)
			{
				ScriptsProcess();
			}

		_weatherUpdateTimer.Update(diff);

		if (_weatherUpdateTimer.Passed)
		{
			foreach (var zoneInfo in _zoneDynamicInfo)
				if (zoneInfo.Value.DefaultWeather != null && !zoneInfo.Value.DefaultWeather.Update((uint)_weatherUpdateTimer.Interval))
					zoneInfo.Value.DefaultWeather = null;

			_weatherUpdateTimer.Reset();
		}

		// update phase shift objects
		_threadManager.Schedule(() => MultiPersonalPhaseTracker.Update(this, diff));
		_threadManager.Wait();
#if DEBUGMETRIC
        _metricFactory.Meter("SendObjectUpdates Update").StopMark();
        _metricFactory.Meter("MoveAll Update").StartMark();
#endif
		_threadManager.Schedule(MoveAllCreaturesInMoveList);
		_threadManager.Schedule(MoveAllGameObjectsInMoveList);
		_threadManager.Schedule(MoveAllAreaTriggersInMoveList);

		_threadManager.Wait();
#if DEBUGMETRIC
        _metricFactory.Meter("MoveAll Update").StopMark();
#endif

		if (!ActivePlayers.Empty() || !_activeNonPlayers.Empty())
		{
#if DEBUGMETRIC
            _metricFactory.Meter("ProcessRelocationNotifies Update").StartMark();
#endif
			_processRelocationQueue.Post(diff);

#if DEBUGMETRIC
            _metricFactory.Meter("ProcessRelocationNotifies Update").StopMark();
#endif
		}

#if DEBUGMETRIC
        _metricFactory.Meter("OnMapUpdate Update").StartMark();
#endif
		OnMapUpdate(this, diff);

#if DEBUGMETRIC
        _metricFactory.Meter("OnMapUpdate Update").StopMark();
#endif
	}

	public virtual void RemovePlayerFromMap(Player player, bool remove)
	{
		// Before leaving map, update zone/area for stats
		player.UpdateZone(MapConst.InvalidZone, 0);
		OnPlayerLeaveMap(this, player);

		MultiPersonalPhaseTracker.MarkAllPhasesForDeletion(player.GUID);

		player.CombatStop();

		var inWorld = player.IsInWorld;
		player.RemoveFromWorld();
		SendRemoveTransports(player);

		if (!inWorld) // if was in world, RemoveFromWorld() called DestroyForNearbyPlayers()
			player.UpdateObjectVisibilityOnDestroy();

		var cell = player.Location.GetCurrentCell();
		RemoveFromGrid(player, cell);

		ActivePlayers.Remove(player);

		if (remove)
			DeleteFromWorld(player);
	}

	public void RemoveFromMap(WorldObject obj, bool remove)
	{
		var inWorld = obj.IsInWorld && obj.TypeId >= TypeId.Unit && obj.TypeId <= TypeId.GameObject;
		obj.RemoveFromWorld();

		if (obj.IsActiveObject)
			RemoveFromActive(obj);

		MultiPersonalPhaseTracker.UnregisterTrackedObject(obj);

		if (!inWorld) // if was in world, RemoveFromWorld() called DestroyForNearbyPlayers()
			obj.UpdateObjectVisibilityOnDestroy();

		var cell = obj.Location.GetCurrentCell();
		RemoveFromGrid(obj, cell);

		obj.ResetMap();

		if (remove)
			DeleteFromWorld(obj);
	}

	public void RemoveFromMap(Transport obj, bool remove)
	{
		if (obj.IsInWorld)
		{
			obj.RemoveFromWorld();

			UpdateData data = new(Id);

			if (obj.IsDestroyedObject)
				obj.BuildDestroyUpdateBlock(data);
			else
				obj.BuildOutOfRangeUpdateBlock(data);

			data.BuildPacket(out var packet);

			foreach (var player in Players)
				if (player.Transport != obj && player.VisibleTransports.Contains(obj.GUID))
				{
					player.SendPacket(packet);
					player.VisibleTransports.Remove(obj.GUID);
				}
		}

		if (!_transports.Contains(obj))
			return;

		_transports.Remove(obj);

		obj.ResetMap();

		if (remove)
			DeleteFromWorld(obj);
	}

	public void PlayerRelocation(Player player, Position pos)
	{
		PlayerRelocation(player, pos.X, pos.Y, pos.Z, pos.Orientation);
	}

	public void PlayerRelocation(Player player, float x, float y, float z, float orientation)
	{
		var oldcell = player.Location.GetCurrentCell();
		var newcell = new Cell(x, y);

		player.Location.Relocate(x, y, z, orientation);

		if (player.IsVehicle)
			player.VehicleKit1.RelocatePassengers();

		if (oldcell.DiffGrid(newcell) || oldcell.DiffCell(newcell))
		{
			Log.outDebug(LogFilter.Maps,
						"Player {0} relocation grid[{1}, {2}]cell[{3}, {4}].grid[{5}, {6}]cell[{7}, {8}]",
						player.GetName(),
						oldcell.GetGridX(),
						oldcell.GetGridY(),
						oldcell.GetCellX(),
						oldcell.GetCellY(),
						newcell.GetGridX(),
						newcell.GetGridY(),
						newcell.GetCellX(),
						newcell.GetCellY());

			RemoveFromGrid(player, oldcell);

			if (oldcell.DiffGrid(newcell))
				EnsureGridLoadedForActiveObject(newcell, player);

			AddToGrid(player, newcell);
		}

		player.UpdatePositionData();
		player.UpdateObjectVisibility(false);
	}

	public void CreatureRelocation(Creature creature, Position p, bool respawnRelocationOnFail = true)
	{
		CreatureRelocation(creature, p.X, p.Y, p.Z, p.Orientation, respawnRelocationOnFail);
	}

	public void CreatureRelocation(Creature creature, float x, float y, float z, float ang, bool respawnRelocationOnFail = true)
	{
		var new_cell = new Cell(x, y);

		if (!respawnRelocationOnFail && GetGrid(new_cell.GetGridX(), new_cell.GetGridY()) == null)
			return;

		var old_cell = creature.Location.GetCurrentCell();

		// delay creature move for grid/cell to grid/cell moves
		if (old_cell.DiffCell(new_cell) || old_cell.DiffGrid(new_cell))
		{
			AddCreatureToMoveList(creature, x, y, z, ang);
			// in diffcell/diffgrid case notifiers called at finishing move creature in MoveAllCreaturesInMoveList
		}
		else
		{
			creature.Location.Relocate(x, y, z, ang);

			if (creature.IsVehicle)
				creature.VehicleKit1.RelocatePassengers();

			creature.UpdateObjectVisibility(false);
			creature.UpdatePositionData();
			RemoveCreatureFromMoveList(creature);
		}
	}

	public void GameObjectRelocation(GameObject go, Position pos, bool respawnRelocationOnFail = true)
	{
		GameObjectRelocation(go, pos.X, pos.Y, pos.Z, pos.Orientation, respawnRelocationOnFail);
	}

	public void GameObjectRelocation(GameObject go, float x, float y, float z, float orientation, bool respawnRelocationOnFail = true)
	{
		var new_cell = new Cell(x, y);

		if (!respawnRelocationOnFail && GetGrid(new_cell.GetGridX(), new_cell.GetGridY()) == null)
			return;

		var old_cell = go.Location.GetCurrentCell();

		// delay creature move for grid/cell to grid/cell moves
		if (old_cell.DiffCell(new_cell) || old_cell.DiffGrid(new_cell))
		{
			Log.outDebug(LogFilter.Maps,
						"GameObject (GUID: {0} Entry: {1}) added to moving list from grid[{2}, {3}]cell[{4}, {5}] to grid[{6}, {7}]cell[{8}, {9}].",
						go.GUID.ToString(),
						go.Entry,
						old_cell.GetGridX(),
						old_cell.GetGridY(),
						old_cell.GetCellX(),
						old_cell.GetCellY(),
						new_cell.GetGridX(),
						new_cell.GetGridY(),
						new_cell.GetCellX(),
						new_cell.GetCellY());

			AddGameObjectToMoveList(go, x, y, z, orientation);
			// in diffcell/diffgrid case notifiers called at finishing move go in Map.MoveAllGameObjectsInMoveList
		}
		else
		{
			go.Location.Relocate(x, y, z, orientation);
			go.AfterRelocation();
			RemoveGameObjectFromMoveList(go);
		}
	}

	public void DynamicObjectRelocation(DynamicObject dynObj, Position pos)
	{
		DynamicObjectRelocation(dynObj, pos.X, pos.Y, pos.Z, pos.Orientation);
	}

	public void DynamicObjectRelocation(DynamicObject dynObj, float x, float y, float z, float orientation)
	{
		Cell new_cell = new(x, y);

		if (GetGrid(new_cell.GetGridX(), new_cell.GetGridY()) == null)
			return;

		var old_cell = dynObj.Location.GetCurrentCell();

		// delay creature move for grid/cell to grid/cell moves
		if (old_cell.DiffCell(new_cell) || old_cell.DiffGrid(new_cell))
		{
			Log.outDebug(LogFilter.Maps,
						"DynamicObject (GUID: {0}) added to moving list from grid[{1}, {2}]cell[{3}, {4}] to grid[{5}, {6}]cell[{7}, {8}].",
						dynObj.GUID.ToString(),
						old_cell.GetGridX(),
						old_cell.GetGridY(),
						old_cell.GetCellX(),
						old_cell.GetCellY(),
						new_cell.GetGridX(),
						new_cell.GetGridY(),
						new_cell.GetCellX(),
						new_cell.GetCellY());

			AddDynamicObjectToMoveList(dynObj, x, y, z, orientation);
			// in diffcell/diffgrid case notifiers called at finishing move dynObj in Map.MoveAllGameObjectsInMoveList
		}
		else
		{
			dynObj.Location.Relocate(x, y, z, orientation);
			dynObj.UpdatePositionData();
			dynObj.UpdateObjectVisibility(false);
			RemoveDynamicObjectFromMoveList(dynObj);
		}
	}

	public void AreaTriggerRelocation(AreaTrigger at, Position pos)
	{
		AreaTriggerRelocation(at, pos.X, pos.Y, pos.Z, pos.Orientation);
	}

	public void AreaTriggerRelocation(AreaTrigger at, float x, float y, float z, float orientation)
	{
		Cell new_cell = new(x, y);

		if (GetGrid(new_cell.GetGridX(), new_cell.GetGridY()) == null)
			return;

		var old_cell = at.Location.GetCurrentCell();

		// delay areatrigger move for grid/cell to grid/cell moves
		if (old_cell.DiffCell(new_cell) || old_cell.DiffGrid(new_cell))
		{
			Log.outDebug(LogFilter.Maps, "AreaTrigger ({0}) added to moving list from {1} to {2}.", at.GUID.ToString(), old_cell.ToString(), new_cell.ToString());

			AddAreaTriggerToMoveList(at, x, y, z, orientation);
			// in diffcell/diffgrid case notifiers called at finishing move at in Map::MoveAllAreaTriggersInMoveList
		}
		else
		{
			at.Location.Relocate(x, y, z, orientation);
			at.UpdateShape();
			at.UpdateObjectVisibility(false);
			RemoveAreaTriggerFromMoveList(at);
		}
	}

	public bool CreatureRespawnRelocation(Creature c, bool diffGridOnly)
	{
		var respPos = c.RespawnPosition;
		var resp_cell = new Cell(respPos.X, respPos.Y);

		//creature will be unloaded with grid
		if (diffGridOnly && !c.Location.GetCurrentCell().DiffGrid(resp_cell))
			return true;

		c.CombatStop();
		c.MotionMaster.Clear();

		// teleport it to respawn point (like normal respawn if player see)
		if (CreatureCellRelocation(c, resp_cell))
		{
			c.Location.Relocate(respPos);
			c.MotionMaster.Initialize(); // prevent possible problems with default move generators
			c.UpdatePositionData();
			c.UpdateObjectVisibility(false);

			return true;
		}

		return false;
	}

	public bool GameObjectRespawnRelocation(GameObject go, bool diffGridOnly)
	{
		var respawnPos = go.GetRespawnPosition();
		var resp_cell = new Cell(respawnPos.X, respawnPos.Y);

		//GameObject will be unloaded with grid
		if (diffGridOnly && !go.Location.GetCurrentCell().DiffGrid(resp_cell))
			return true;

		Log.outDebug(LogFilter.Maps,
					"GameObject (GUID: {0} Entry: {1}) moved from grid[{2}, {3}] to respawn grid[{4}, {5}].",
					go.GUID.ToString(),
					go.Entry,
					go.Location.GetCurrentCell().GetGridX(),
					go.Location.GetCurrentCell().GetGridY(),
					resp_cell.GetGridX(),
					resp_cell.GetGridY());

		// teleport it to respawn point (like normal respawn if player see)
		if (GameObjectCellRelocation(go, resp_cell))
		{
			go.Location.Relocate(respawnPos);
			go.UpdatePositionData();
			go.UpdateObjectVisibility(false);

			return true;
		}

		return false;
	}

	public bool UnloadGrid(Grid grid, bool unloadAll)
	{
		var x = grid.GetX();
		var y = grid.GetY();

		if (!unloadAll)
		{
			//pets, possessed creatures (must be active), transport passengers
			if (grid.GetWorldObjectCountInNGrid<Creature>() != 0)
				return false;

			if (ActiveObjectsNearGrid(grid))
				return false;
		}

		Log.outDebug(LogFilter.Maps, "Unloading grid[{0}, {1}] for map {2}", x, y, Id);

		if (!unloadAll)
		{
			// Finish creature moves, remove and delete all creatures with delayed remove before moving to respawn grids
			// Must know real mob position before move
			_threadManager.Schedule(MoveAllCreaturesInMoveList);
			_threadManager.Schedule(MoveAllGameObjectsInMoveList);
			_threadManager.Schedule(MoveAllAreaTriggersInMoveList);
			_threadManager.Wait();
			// move creatures to respawn grids if this is diff.grid or to remove list
			ObjectGridEvacuator worker = new(GridType.Grid);
			grid.VisitAllGrids(worker);

			// Finish creature moves, remove and delete all creatures with delayed remove before unload
			_threadManager.Schedule(MoveAllCreaturesInMoveList);
			_threadManager.Schedule(MoveAllGameObjectsInMoveList);
			_threadManager.Schedule(MoveAllAreaTriggersInMoveList);
			_threadManager.Wait();
		}

		{
			ObjectGridCleaner worker = new(GridType.Grid);
			grid.VisitAllGrids(worker);
		}

		RemoveAllObjectsInRemoveList();

		// After removing all objects from the map, purge empty tracked phases
		MultiPersonalPhaseTracker.UnloadGrid(grid);

		{
			ObjectGridUnloader worker = new();
			grid.VisitAllGrids(worker);
		}

		lock (Grids)
		{
			Grids.Remove(x, y);
		}

		var gx = (int)((MapConst.MaxGrids - 1) - x);
		var gy = (int)((MapConst.MaxGrids - 1) - y);

		_terrain.UnloadMap(gx, gy);

		Log.outDebug(LogFilter.Maps, "Unloading grid[{0}, {1}] for map {2} finished", x, y, Id);

		return true;
	}

	public virtual void RemoveAllPlayers()
	{
		if (HavePlayers)
			foreach (var pl in ActivePlayers)
				if (!pl.IsBeingTeleportedFar)
				{
					// this is happening for bg
					Log.outError(LogFilter.Maps, $"Map.UnloadAll: player {pl.GetName()} is still in map {Id} during unload, this should not happen!");
					pl.TeleportTo(pl.Homebind);
				}
	}

	public void UnloadAll()
	{
		// clear all delayed moves, useless anyway do this moves before map unload.
		_creaturesToMove.Clear();
		_gameObjectsToMove.Clear();

		foreach (var x in GridXKeys())
		{
			foreach (var y in GridYKeys(x))
			{
				var grid = GetGrid(x, y);

				if (grid == null)
					continue;

				UnloadGrid(grid, true); // deletes the grid and removes it from the GridRefManager
			}
		}

		for (var i = 0; i < _transports.Count; ++i)
			RemoveFromMap(_transports[i], true);

		_transports.Clear();

		foreach (var corpse in _corpsesByCell.Values.ToList())
		{
			corpse.RemoveFromWorld();
			corpse.ResetMap();
			corpse.Dispose();
		}

		_corpsesByCell.Clear();
		_corpsesByPlayer.Clear();
		_corpseBones.Clear();
	}

	public static bool IsInWMOInterior(uint mogpFlags)
	{
		return (mogpFlags & 0x2000) != 0;
	}

	public void GetFullTerrainStatusForPosition(PhaseShift phaseShift, float x, float y, float z, PositionFullTerrainStatus data, LiquidHeaderTypeFlags reqLiquidType, float collisionHeight = MapConst.DefaultCollesionHeight)
	{
		_terrain.GetFullTerrainStatusForPosition(phaseShift, Id, x, y, z, data, reqLiquidType, collisionHeight, _dynamicTree);
	}

	public ZLiquidStatus GetLiquidStatus(PhaseShift phaseShift, Position pos, LiquidHeaderTypeFlags reqLiquidType, float collisionHeight = MapConst.DefaultCollesionHeight)
	{
		return GetLiquidStatus(phaseShift, pos.X, pos.Y, pos.Z, reqLiquidType, collisionHeight);
	}

	public ZLiquidStatus GetLiquidStatus(PhaseShift phaseShift, float x, float y, float z, LiquidHeaderTypeFlags reqLiquidType, float collisionHeight = MapConst.DefaultCollesionHeight)
	{
		return _terrain.GetLiquidStatus(phaseShift, Id, x, y, z, reqLiquidType, out _, collisionHeight);
	}

	public ZLiquidStatus GetLiquidStatus(PhaseShift phaseShift, Position pos, LiquidHeaderTypeFlags reqLiquidType, out LiquidData data, float collisionHeight = MapConst.DefaultCollesionHeight)
	{
		return _terrain.GetLiquidStatus(phaseShift, Id, pos.X, pos.Y, pos.Z, reqLiquidType, out data, collisionHeight);
	}

	public ZLiquidStatus GetLiquidStatus(PhaseShift phaseShift, float x, float y, float z, LiquidHeaderTypeFlags reqLiquidType, out LiquidData data, float collisionHeight = MapConst.DefaultCollesionHeight)
	{
		return _terrain.GetLiquidStatus(phaseShift, Id, x, y, z, reqLiquidType, out data, collisionHeight);
	}

	public uint GetAreaId(PhaseShift phaseShift, Position pos)
	{
		return _terrain.GetAreaId(phaseShift, Id, pos.X, pos.Y, pos.Z, _dynamicTree);
	}

	public uint GetAreaId(PhaseShift phaseShift, float x, float y, float z)
	{
		return _terrain.GetAreaId(phaseShift, Id, x, y, z, _dynamicTree);
	}

	public uint GetZoneId(PhaseShift phaseShift, Position pos)
	{
		return _terrain.GetZoneId(phaseShift, Id, pos.X, pos.Y, pos.Z, _dynamicTree);
	}

	public uint GetZoneId(PhaseShift phaseShift, float x, float y, float z)
	{
		return _terrain.GetZoneId(phaseShift, Id, x, y, z, _dynamicTree);
	}

	public void GetZoneAndAreaId(PhaseShift phaseShift, out uint zoneid, out uint areaid, Position pos)
	{
		_terrain.GetZoneAndAreaId(phaseShift, Id, out zoneid, out areaid, pos.X, pos.Y, pos.Z, _dynamicTree);
	}

	public void GetZoneAndAreaId(PhaseShift phaseShift, out uint zoneid, out uint areaid, float x, float y, float z)
	{
		_terrain.GetZoneAndAreaId(phaseShift, Id, out zoneid, out areaid, x, y, z, _dynamicTree);
	}

	public float GetHeight(PhaseShift phaseShift, float x, float y, float z, bool vmap = true, float maxSearchDist = MapConst.DefaultHeightSearch)
	{
		return Math.Max(GetStaticHeight(phaseShift, x, y, z, vmap, maxSearchDist), GetGameObjectFloor(phaseShift, x, y, z, maxSearchDist));
	}

	public float GetHeight(PhaseShift phaseShift, Position pos, bool vmap = true, float maxSearchDist = MapConst.DefaultHeightSearch)
	{
		return GetHeight(phaseShift, pos.X, pos.Y, pos.Z, vmap, maxSearchDist);
	}

	public float GetMinHeight(PhaseShift phaseShift, float x, float y)
	{
		return _terrain.GetMinHeight(phaseShift, Id, x, y);
	}

	public float GetGridHeight(PhaseShift phaseShift, float x, float y)
	{
		return _terrain.GetGridHeight(phaseShift, Id, x, y);
	}

	public float GetStaticHeight(PhaseShift phaseShift, float x, float y, float z, bool checkVMap = true, float maxSearchDist = MapConst.DefaultHeightSearch)
	{
		return _terrain.GetStaticHeight(phaseShift, Id, x, y, z, checkVMap, maxSearchDist);
	}

	public float GetWaterLevel(PhaseShift phaseShift, float x, float y)
	{
		return _terrain.GetWaterLevel(phaseShift, Id, x, y);
	}

	public bool IsInWater(PhaseShift phaseShift, float x, float y, float z, out LiquidData data)
	{
		return _terrain.IsInWater(phaseShift, Id, x, y, z, out data);
	}

	public bool IsUnderWater(PhaseShift phaseShift, float x, float y, float z)
	{
		return _terrain.IsUnderWater(phaseShift, Id, x, y, z);
	}

	public float GetWaterOrGroundLevel(PhaseShift phaseShift, float x, float y, float z, float collisionHeight = MapConst.DefaultCollesionHeight)
	{
		float ground = 0;

		return _terrain.GetWaterOrGroundLevel(phaseShift, Id, x, y, z, ref ground, false, collisionHeight, _dynamicTree);
	}

	public float GetWaterOrGroundLevel(PhaseShift phaseShift, float x, float y, float z, ref float ground, bool swim = false, float collisionHeight = MapConst.DefaultCollesionHeight)
	{
		return _terrain.GetWaterOrGroundLevel(phaseShift, Id, x, y, z, ref ground, swim, collisionHeight, _dynamicTree);
	}

	public bool IsInLineOfSight(PhaseShift phaseShift, Position position, Position position2, LineOfSightChecks checks, ModelIgnoreFlags ignoreFlags)
	{
		return IsInLineOfSight(phaseShift, position, position2.X, position2.Y, position2.Z, checks, ignoreFlags);
	}

	public bool IsInLineOfSight(PhaseShift phaseShift, Position position, float x2, float y2, float z2, LineOfSightChecks checks, ModelIgnoreFlags ignoreFlags)
	{
		return IsInLineOfSight(phaseShift, position.X, position.Y, position.Z, x2, y2, z2, checks, ignoreFlags);
	}

	public bool IsInLineOfSight(PhaseShift phaseShift, float x1, float y1, float z1, float x2, float y2, float z2, LineOfSightChecks checks, ModelIgnoreFlags ignoreFlags)
	{
		if (checks.HasAnyFlag(LineOfSightChecks.Vmap) && !Global.VMapMgr.IsInLineOfSight(PhasingHandler.GetTerrainMapId(phaseShift, Id, _terrain, x1, y1), x1, y1, z1, x2, y2, z2, ignoreFlags))
			return false;

		if (WorldConfig.GetBoolValue(WorldCfg.CheckGobjectLos) && checks.HasAnyFlag(LineOfSightChecks.Gobject) && !_dynamicTree.IsInLineOfSight(new Vector3(x1, y1, z1), new Vector3(x2, y2, z2), phaseShift))
			return false;

		return true;
	}

	public bool GetObjectHitPos(PhaseShift phaseShift, float x1, float y1, float z1, float x2, float y2, float z2, out float rx, out float ry, out float rz, float modifyDist)
	{
		var startPos = new Vector3(x1, y1, z1);
		var dstPos = new Vector3(x2, y2, z2);

		var resultPos = new Vector3();
		var result = _dynamicTree.GetObjectHitPos(startPos, dstPos, ref resultPos, modifyDist, phaseShift);

		rx = resultPos.X;
		ry = resultPos.Y;
		rz = resultPos.Z;

		return result;
	}

	public static TransferAbortParams PlayerCannotEnter(uint mapid, Player player)
	{
		var entry = CliDB.MapStorage.LookupByKey(mapid);

		if (entry == null)
			return new TransferAbortParams(TransferAbortReason.MapNotAllowed);

		if (!entry.IsDungeon())
			return null;

		var targetDifficulty = player.GetDifficultyId(entry);
		// Get the highest available difficulty if current setting is higher than the instance allows
		var mapDiff = Global.DB2Mgr.GetDownscaledMapDifficultyData(mapid, ref targetDifficulty);

		if (mapDiff == null)
			return new TransferAbortParams(TransferAbortReason.Difficulty);

		//Bypass checks for GMs
		if (player.IsGameMaster)
			return null;

		//Other requirements
		{
			TransferAbortParams abortParams = new();

			if (!player.Satisfy(Global.ObjectMgr.GetAccessRequirement(mapid, targetDifficulty), mapid, abortParams, true))
				return abortParams;
		}

		var group = player.Group;

		if (entry.IsRaid() && (int)entry.Expansion() >= WorldConfig.GetIntValue(WorldCfg.Expansion)) // can only enter in a raid group but raids from old expansion don't need a group
			if ((!group || !group.IsRaidGroup) && !WorldConfig.GetBoolValue(WorldCfg.InstanceIgnoreRaid))
				return new TransferAbortParams(TransferAbortReason.NeedGroup);

		if (entry.Instanceable())
		{
			//Get instance where player's group is bound & its map
			var instanceIdToCheck = Global.MapMgr.FindInstanceIdForPlayer(mapid, player);
			var boundMap = Global.MapMgr.FindMap(mapid, instanceIdToCheck);

			if (boundMap != null)
			{
				var denyReason = boundMap.CannotEnter(player);

				if (denyReason != null)
					return denyReason;
			}

			// players are only allowed to enter 10 instances per hour
			if (!entry.GetFlags2().HasFlag(MapFlags2.IgnoreInstanceFarmLimit) && entry.IsDungeon() && !player.CheckInstanceCount(instanceIdToCheck) && !player.IsDead)
				return new TransferAbortParams(TransferAbortReason.TooManyInstances);
		}

		return null;
	}

	public void SendInitSelf(Player player)
	{
		var data = new UpdateData(player.Location.MapId);

		// attach to player data current transport data
		var transport = player.GetTransport<Transport>();

		if (transport != null)
		{
			transport.BuildCreateUpdateBlockForPlayer(data, player);
			player.VisibleTransports.Add(transport.GUID);
		}

		player.BuildCreateUpdateBlockForPlayer(data, player);

		// build other passengers at transport also (they always visible and marked as visible and will not send at visibility update at add to map
		if (transport != null)
			foreach (var passenger in transport.GetPassengers())
				if (player != passenger && player.HaveAtClient(passenger))
					passenger.BuildCreateUpdateBlockForPlayer(data, player);

		data.BuildPacket(out var packet);
		player.SendPacket(packet);
	}

	public void SendUpdateTransportVisibility(Player player)
	{
		// Hack to send out transports
		UpdateData transData = new(player.Location.MapId);

		foreach (var transport in _transports)
		{
			if (!transport.IsInWorld)
				continue;

			var hasTransport = player.VisibleTransports.Contains(transport.GUID);

			if (player.InSamePhase(transport))
			{
				if (!hasTransport)
				{
					transport.BuildCreateUpdateBlockForPlayer(transData, player);
					player.VisibleTransports.Add(transport.GUID);
				}
			}
			else
			{
				transport.BuildOutOfRangeUpdateBlock(transData);
				player.VisibleTransports.Remove(transport.GUID);
			}
		}

		transData.BuildPacket(out var packet);
		player.SendPacket(packet);
	}

	public void Respawn(SpawnObjectType type, ulong spawnId, SQLTransaction dbTrans = null)
	{
		var info = GetRespawnInfo(type, spawnId);

		if (info != null)
			Respawn(info, dbTrans);
	}

	public void Respawn(RespawnInfo info, SQLTransaction dbTrans = null)
	{
		if (info.RespawnTime <= GameTime.GetGameTime())
			return;

		info.RespawnTime = GameTime.GetGameTime();
		SaveRespawnInfoDB(info, dbTrans);
	}

	public void RemoveRespawnTime(SpawnObjectType type, ulong spawnId, SQLTransaction dbTrans = null, bool alwaysDeleteFromDB = false)
	{
		var info = GetRespawnInfo(type, spawnId);

		if (info != null)
			DeleteRespawnInfo(info, dbTrans);
		// Some callers might need to make sure the database doesn't contain any respawn time
		else if (alwaysDeleteFromDB)
			DeleteRespawnInfoFromDB(type, spawnId, dbTrans);
	}

	public void GetRespawnInfo(List<RespawnInfo> respawnData, SpawnObjectTypeMask types)
	{
		if ((types & SpawnObjectTypeMask.Creature) != 0)
			PushRespawnInfoFrom(respawnData, _creatureRespawnTimesBySpawnId);

		if ((types & SpawnObjectTypeMask.GameObject) != 0)
			PushRespawnInfoFrom(respawnData, _gameObjectRespawnTimesBySpawnId);
	}

	public RespawnInfo GetRespawnInfo(SpawnObjectType type, ulong spawnId)
	{
		var map = GetRespawnMapForType(type);

		if (map == null)
			return null;

		var respawnInfo = map.LookupByKey(spawnId);

		if (respawnInfo == null)
			return null;

		return respawnInfo;
	}

	public void ApplyDynamicModeRespawnScaling(WorldObject obj, ulong spawnId, ref uint respawnDelay, uint mode)
	{
		if (IsBattlegroundOrArena)
			return;

		SpawnObjectType type;

		switch (obj.TypeId)
		{
			case TypeId.Unit:
				type = SpawnObjectType.Creature;

				break;
			case TypeId.GameObject:
				type = SpawnObjectType.GameObject;

				break;
			default:
				return;
		}

		var data = Global.ObjectMgr.GetSpawnMetadata(type, spawnId);

		if (data == null)
			return;

		if (!data.SpawnGroupData.Flags.HasFlag(SpawnGroupFlags.DynamicSpawnRate))
			return;

		if (!_zonePlayerCountMap.ContainsKey(obj.Zone))
			return;

		var playerCount = _zonePlayerCountMap[obj.Zone];

		if (playerCount == 0)
			return;

		double adjustFactor = WorldConfig.GetFloatValue(type == SpawnObjectType.GameObject ? WorldCfg.RespawnDynamicRateGameobject : WorldCfg.RespawnDynamicRateCreature) / playerCount;

		if (adjustFactor >= 1.0) // nothing to do here
			return;

		var timeMinimum = WorldConfig.GetUIntValue(type == SpawnObjectType.GameObject ? WorldCfg.RespawnDynamicMinimumGameObject : WorldCfg.RespawnDynamicMinimumCreature);

		if (respawnDelay <= timeMinimum)
			return;

		respawnDelay = (uint)Math.Max(Math.Ceiling(respawnDelay * adjustFactor), timeMinimum);
	}

	public bool ShouldBeSpawnedOnGridLoad<T>(ulong spawnId)
	{
		return ShouldBeSpawnedOnGridLoad(SpawnData.TypeFor<T>(), spawnId);
	}

	public bool SpawnGroupSpawn(uint groupId, bool ignoreRespawn = false, bool force = false, List<WorldObject> spawnedObjects = null)
	{
		var groupData = GetSpawnGroupData(groupId);

		if (groupData == null || groupData.Flags.HasAnyFlag(SpawnGroupFlags.System))
		{
			Log.outError(LogFilter.Maps, $"Tried to spawn non-existing (or system) spawn group {groupId}. on map {Id} Blocked.");

			return false;
		}

		SetSpawnGroupActive(groupId, true); // start processing respawns for the group

		List<SpawnData> toSpawn = new();

		foreach (var data in Global.ObjectMgr.GetSpawnMetadataForGroup(groupId))
		{
			var respawnMap = GetRespawnMapForType(data.Type);

			if (respawnMap == null)
				continue;

			if (force || ignoreRespawn)
				RemoveRespawnTime(data.Type, data.SpawnId);

			var hasRespawnTimer = respawnMap.ContainsKey(data.SpawnId);

			if (SpawnMetadata.TypeHasData(data.Type))
			{
				// has a respawn timer
				if (hasRespawnTimer)
					continue;

				// has a spawn already active
				if (!force)
				{
					var obj = GetWorldObjectBySpawnId(data.Type, data.SpawnId);

					if (obj != null)
						if ((data.Type != SpawnObjectType.Creature) || obj.AsCreature.IsAlive)
							continue;
				}

				toSpawn.Add(data.ToSpawnData());
			}
		}

		foreach (var data in toSpawn)
		{
			// don't spawn if the current map difficulty is not used by the spawn
			if (!data.SpawnDifficulties.Contains(DifficultyID))
				continue;

			// don't spawn if the grid isn't loaded (will be handled in grid loader)
			if (!IsGridLoaded(data.SpawnPoint))
				continue;

			// now do the actual (re)spawn
			switch (data.Type)
			{
				case SpawnObjectType.Creature:
				{
					Creature creature = new();

					if (!creature.LoadFromDB(data.SpawnId, this, true, force))
						creature.Dispose();
					else spawnedObjects?.Add(creature);

					break;
				}
				case SpawnObjectType.GameObject:
				{
					GameObject gameobject = new();

					if (!gameobject.LoadFromDB(data.SpawnId, this, true))
						gameobject.Dispose();
					else spawnedObjects?.Add(gameobject);

					break;
				}
				case SpawnObjectType.AreaTrigger:
				{
					var areaTrigger = new AreaTrigger();

					if (!areaTrigger.LoadFromDB(data.SpawnId, this, true, false))
						areaTrigger.Dispose();
					else spawnedObjects?.Add(areaTrigger);

					break;
				}
				default:
					return false;
			}
		}

		return true;
	}

	public bool SpawnGroupDespawn(uint groupId, bool deleteRespawnTimes = false)
	{
		return SpawnGroupDespawn(groupId, deleteRespawnTimes, out _);
	}

	public bool SpawnGroupDespawn(uint groupId, bool deleteRespawnTimes, out int count)
	{
		count = 0;
		var groupData = GetSpawnGroupData(groupId);

		if (groupData == null || groupData.Flags.HasAnyFlag(SpawnGroupFlags.System))
		{
			Log.outError(LogFilter.Maps, $"Tried to despawn non-existing (or system) spawn group {groupId} on map {Id}. Blocked.");

			return false;
		}

		foreach (var data in Global.ObjectMgr.GetSpawnMetadataForGroup(groupId))
		{
			if (deleteRespawnTimes)
				RemoveRespawnTime(data.Type, data.SpawnId);

			count += DespawnAll(data.Type, data.SpawnId);
		}

		SetSpawnGroupActive(groupId, false); // stop processing respawns for the group, too

		return true;
	}

	public void SetSpawnGroupActive(uint groupId, bool state)
	{
		var data = GetSpawnGroupData(groupId);

		if (data == null || data.Flags.HasAnyFlag(SpawnGroupFlags.System))
		{
			Log.outError(LogFilter.Maps, $"Tried to set non-existing (or system) spawn group {groupId} to {(state ? "active" : "inactive")} on map {Id}. Blocked.");

			return;
		}

		if (state != !data.Flags.HasAnyFlag(SpawnGroupFlags.ManualSpawn)) // toggled
			_toggledSpawnGroupIds.Add(groupId);
		else
			_toggledSpawnGroupIds.Remove(groupId);
	}

	// Disable the spawn group, which prevents any creatures in the group from respawning until re-enabled
	// This will not affect any already-present creatures in the group
	public void SetSpawnGroupInactive(uint groupId)
	{
		SetSpawnGroupActive(groupId, false);
	}

	public bool IsSpawnGroupActive(uint groupId)
	{
		var data = GetSpawnGroupData(groupId);

		if (data == null)
		{
			Log.outError(LogFilter.Maps, $"Tried to query state of non-existing spawn group {groupId} on map {Id}.");

			return false;
		}

		if (data.Flags.HasAnyFlag(SpawnGroupFlags.System))
			return true;

		// either manual spawn group and toggled, or not manual spawn group and not toggled...
		return _toggledSpawnGroupIds.Contains(groupId) != !data.Flags.HasAnyFlag(SpawnGroupFlags.ManualSpawn);
	}

	public void UpdateSpawnGroupConditions()
	{
		var spawnGroups = Global.ObjectMgr.GetSpawnGroupsForMap(Id);

		foreach (var spawnGroupId in spawnGroups)
		{
			var spawnGroupTemplate = GetSpawnGroupData(spawnGroupId);

			var isActive = IsSpawnGroupActive(spawnGroupId);
			var shouldBeActive = Global.ConditionMgr.IsMapMeetingNotGroupedConditions(ConditionSourceType.SpawnGroup, spawnGroupId, this);

			if (spawnGroupTemplate.Flags.HasFlag(SpawnGroupFlags.ManualSpawn))
			{
				// Only despawn the group if it isn't meeting conditions
				if (isActive && !shouldBeActive && spawnGroupTemplate.Flags.HasFlag(SpawnGroupFlags.DespawnOnConditionFailure))
					SpawnGroupDespawn(spawnGroupId, true);

				continue;
			}

			if (isActive == shouldBeActive)
				continue;

			if (shouldBeActive)
				SpawnGroupSpawn(spawnGroupId);
			else if (spawnGroupTemplate.Flags.HasFlag(SpawnGroupFlags.DespawnOnConditionFailure))
				SpawnGroupDespawn(spawnGroupId, true);
			else
				SetSpawnGroupInactive(spawnGroupId);
		}
	}

	public void AddFarSpellCallback(Action<Map> callback)
	{
		_farSpellCallbacks.Enqueue(callback);
	}

	public virtual void DelayedUpdate(uint diff)
	{
#if DEBUGMETRIC
        _metricFactory.Meter("_farSpellCallbacks").StartMark();
#endif
		while (_farSpellCallbacks.TryDequeue(out var callback))
			_threadManager.Schedule(() => callback(this));

		_threadManager.Wait();
#if DEBUGMETRIC
        _metricFactory.Meter("_farSpellCallbacks").StopMark();
        _metricFactory.Meter("RemoveAllObjectsInRemoveList").StartMark();
#endif

		RemoveAllObjectsInRemoveList();

#if DEBUGMETRIC
        _metricFactory.Meter("RemoveAllObjectsInRemoveList").StopMark();
        _metricFactory.Meter("grid?.Update").StartMark();
#endif
		// Don't unload grids if it's Battleground, since we may have manually added GOs, creatures, those doesn't load from DB at grid re-load !
		// This isn't really bother us, since as soon as we have instanced BG-s, the whole map unloads as the BG gets ended
		if (!IsBattlegroundOrArena)
			foreach (var xkvp in Grids)
			{
				foreach (var ykvp in xkvp.Value)
				{
					var grid = ykvp.Value;

                    _threadManager.Stage(() => grid?.Update(this, diff));
				}
			}

		_threadManager.Wait();
#if DEBUGMETRIC
        _metricFactory.Meter("grid?.Update").StopMark();
#endif
    }

	public void AddObjectToRemoveList(WorldObject obj)
	{
		obj.SetDestroyedObject(true);
		obj.CleanupsBeforeDelete(false); // remove or simplify at least cross referenced links

		_objectsToRemove.Add(obj);
	}

	public void AddObjectToSwitchList(WorldObject obj, bool on)
	{
		// i_objectsToSwitch is iterated only in Map::RemoveAllObjectsInRemoveList() and it uses
		// the contained objects only if GetTypeId() == TYPEID_UNIT , so we can return in all other cases
		if (!obj.IsTypeId(TypeId.Unit))
			return;

		if (!_objectsToSwitch.ContainsKey(obj))
			_objectsToSwitch.Add(obj, on);
		else if (_objectsToSwitch[obj] != on)
			_objectsToSwitch.Remove(obj);
	}

	public uint GetPlayersCountExceptGMs()
	{
		uint count = 0;

		foreach (var pl in ActivePlayers)
			if (!pl.IsGameMaster)
				++count;

		return count;
	}

	public void SendToPlayers(ServerPacket data)
	{
		foreach (var pl in ActivePlayers)
			pl.SendPacket(data);
	}

	public bool ActiveObjectsNearGrid(Grid grid)
	{
		var cell_min = new CellCoord(grid.GetX() * MapConst.MaxCells,
									grid.GetY() * MapConst.MaxCells);

		var cell_max = new CellCoord(cell_min.X_Coord + MapConst.MaxCells,
									cell_min.Y_Coord + MapConst.MaxCells);

		//we must find visible range in cells so we unload only non-visible cells...
		var viewDist = VisibilityRange;
		var cell_range = (uint)Math.Ceiling(viewDist / MapConst.SizeofCells) + 1;

		cell_min.Dec_x(cell_range);
		cell_min.Dec_y(cell_range);
		cell_max.Inc_x(cell_range);
		cell_max.Inc_y(cell_range);

		foreach (var pl in ActivePlayers)
		{
			var p = GridDefines.ComputeCellCoord(pl.Location.X, pl.Location.Y);

			if ((cell_min.X_Coord <= p.X_Coord && p.X_Coord <= cell_max.X_Coord) &&
				(cell_min.Y_Coord <= p.Y_Coord && p.Y_Coord <= cell_max.Y_Coord))
				return true;
		}

		foreach (var obj in _activeNonPlayers)
		{
			var p = GridDefines.ComputeCellCoord(obj.Location.X, obj.Location.Y);

			if ((cell_min.X_Coord <= p.X_Coord && p.X_Coord <= cell_max.X_Coord) &&
				(cell_min.Y_Coord <= p.Y_Coord && p.Y_Coord <= cell_max.Y_Coord))
				return true;
		}

		return false;
	}

	public void AddToActive(WorldObject obj)
	{
		AddToActiveHelper(obj);

		Position respawnLocation = null;

		switch (obj.TypeId)
		{
			case TypeId.Unit:
				var creature = obj.AsCreature;

				if (creature != null && !creature.IsPet && creature.SpawnId != 0)
					respawnLocation = creature.RespawnPosition;

				break;
			case TypeId.GameObject:
				var gameObject = obj.AsGameObject;

				if (gameObject != null && gameObject.SpawnId != 0)
					respawnLocation = gameObject.GetRespawnPosition();

				break;
			default:
				break;
		}

		if (respawnLocation != null)
		{
			var p = GridDefines.ComputeGridCoord(respawnLocation.X, respawnLocation.Y);

			if (GetGrid(p.X_Coord, p.Y_Coord) != null)
			{
				GetGrid(p.X_Coord, p.Y_Coord).IncUnloadActiveLock();
			}
			else
			{
				var p2 = GridDefines.ComputeGridCoord(obj.Location.X, obj.Location.Y);
				Log.outError(LogFilter.Maps, $"Active object {obj.GUID} added to grid[{p.X_Coord}, {p.Y_Coord}] but spawn grid[{p2.X_Coord}, {p2.Y_Coord}] was not loaded.");
			}
		}
	}

	public void RemoveFromActive(WorldObject obj)
	{
		RemoveFromActiveHelper(obj);

		Position respawnLocation = null;

		switch (obj.TypeId)
		{
			case TypeId.Unit:
				var creature = obj.AsCreature;

				if (creature != null && !creature.IsPet && creature.SpawnId != 0)
					respawnLocation = creature.RespawnPosition;

				break;
			case TypeId.GameObject:
				var gameObject = obj.AsGameObject;

				if (gameObject != null && gameObject.SpawnId != 0)
					respawnLocation = gameObject.GetRespawnPosition();

				break;
			default:
				break;
		}

		if (respawnLocation != null)
		{
			var p = GridDefines.ComputeGridCoord(respawnLocation.X, respawnLocation.Y);

			if (GetGrid(p.X_Coord, p.Y_Coord) != null)
			{
				GetGrid(p.X_Coord, p.Y_Coord).DecUnloadActiveLock();
			}
			else
			{
				var p2 = GridDefines.ComputeGridCoord(obj.Location.X, obj.Location.Y);
				Log.outDebug(LogFilter.Maps, $"Active object {obj.GUID} removed from grid[{p.X_Coord}, {p.Y_Coord}] but spawn grid[{p2.X_Coord}, {p2.Y_Coord}] was not loaded.");
			}
		}
	}

	public void SaveRespawnTime(SpawnObjectType type, ulong spawnId, uint entry, long respawnTime, uint gridId = 0, SQLTransaction dbTrans = null, bool startup = false)
	{
		var data = Global.ObjectMgr.GetSpawnMetadata(type, spawnId);

		if (data == null)
		{
			Log.outError(LogFilter.Maps, $"Map {Id} attempt to save respawn time for nonexistant spawnid ({type},{spawnId}).");

			return;
		}

		if (respawnTime == 0)
		{
			// Delete only
			RemoveRespawnTime(data.Type, data.SpawnId, dbTrans);

			return;
		}

		RespawnInfo ri = new();
		ri.ObjectType = data.Type;
		ri.SpawnId = data.SpawnId;
		ri.Entry = entry;
		ri.RespawnTime = respawnTime;
		ri.GridId = gridId;
		var success = AddRespawnInfo(ri);

		if (startup)
		{
			if (!success)
				Log.outError(LogFilter.Maps, $"Attempt to load saved respawn {respawnTime} for ({type},{spawnId}) failed - duplicate respawn? Skipped.");
		}
		else if (success)
		{
			SaveRespawnInfoDB(ri, dbTrans);
		}
	}

	public void SaveRespawnInfoDB(RespawnInfo info, SQLTransaction dbTrans = null)
	{
		if (Instanceable)
			return;

		var stmt = DB.Characters.GetPreparedStatement(CharStatements.REP_RESPAWN);
		stmt.AddValue(0, (ushort)info.ObjectType);
		stmt.AddValue(1, info.SpawnId);
		stmt.AddValue(2, info.RespawnTime);
		stmt.AddValue(3, Id);
		stmt.AddValue(4, InstanceId);
		DB.Characters.ExecuteOrAppend(dbTrans, stmt);
	}

	public void LoadRespawnTimes()
	{
		if (Instanceable)
			return;

		var stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_RESPAWNS);
		stmt.AddValue(0, Id);
		stmt.AddValue(1, InstanceId);
		var result = DB.Characters.Query(stmt);

		if (!result.IsEmpty())
			do
			{
				var type = (SpawnObjectType)result.Read<ushort>(0);
				var spawnId = result.Read<ulong>(1);
				var respawnTime = result.Read<long>(2);

				if (SpawnMetadata.TypeHasData(type))
				{
					var data = Global.ObjectMgr.GetSpawnData(type, spawnId);

					if (data != null)
						SaveRespawnTime(type, spawnId, data.Id, respawnTime, GridDefines.ComputeGridCoord(data.SpawnPoint.X, data.SpawnPoint.Y).GetId(), null, true);
					else
						Log.outError(LogFilter.Maps, $"Loading saved respawn time of {respawnTime} for spawnid ({type},{spawnId}) - spawn does not exist, ignoring");
				}
				else
				{
					Log.outError(LogFilter.Maps, $"Loading saved respawn time of {respawnTime} for spawnid ({type},{spawnId}) - invalid spawn type, ignoring");
				}
			} while (result.NextRow());
	}

	public void DeleteRespawnTimes()
	{
		UnloadAllRespawnInfos();
		DeleteRespawnTimesInDB();
	}

	public void DeleteRespawnTimesInDB()
	{
		if (Instanceable)
			return;

		var stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_ALL_RESPAWNS);
		stmt.AddValue(0, Id);
		stmt.AddValue(1, InstanceId);
		DB.Characters.Execute(stmt);
	}

	public long GetLinkedRespawnTime(ObjectGuid guid)
	{
		var linkedGuid = Global.ObjectMgr.GetLinkedRespawnGuid(guid);

		switch (linkedGuid.High)
		{
			case HighGuid.Creature:
				return GetCreatureRespawnTime(linkedGuid.Counter);
			case HighGuid.GameObject:
				return GetGORespawnTime(linkedGuid.Counter);
			default:
				break;
		}

		return 0L;
	}

	public void LoadCorpseData()
	{
		var stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CORPSES);
		stmt.AddValue(0, Id);
		stmt.AddValue(1, InstanceId);

		//        0     1     2     3            4      5          6          7     8      9       10     11        12    13          14          15
		// SELECT posX, posY, posZ, orientation, mapId, displayId, itemCache, race, class, gender, flags, dynFlags, time, corpseType, instanceId, guid FROM corpse WHERE mapId = ? AND instanceId = ?
		var result = DB.Characters.Query(stmt);

		if (result.IsEmpty())
			return;

		MultiMap<ulong, uint> phases = new();
		MultiMap<ulong, ChrCustomizationChoice> customizations = new();

		stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CORPSE_PHASES);
		stmt.AddValue(0, Id);
		stmt.AddValue(1, InstanceId);

		//        0          1
		// SELECT OwnerGuid, PhaseId FROM corpse_phases cp LEFT JOIN corpse c ON cp.OwnerGuid = c.guid WHERE c.mapId = ? AND c.instanceId = ?
		var phaseResult = DB.Characters.Query(stmt);

		if (!phaseResult.IsEmpty())
			do
			{
				var guid = phaseResult.Read<ulong>(0);
				var phaseId = phaseResult.Read<uint>(1);

				phases.Add(guid, phaseId);
			} while (phaseResult.NextRow());

		stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CORPSE_CUSTOMIZATIONS);
		stmt.AddValue(0, Id);
		stmt.AddValue(1, InstanceId);

		//        0             1                            2
		// SELECT cc.ownerGuid, cc.chrCustomizationOptionID, cc.chrCustomizationChoiceID FROM corpse_customizations cc LEFT JOIN corpse c ON cc.ownerGuid = c.guid WHERE c.mapId = ? AND c.instanceId = ?
		var customizationResult = DB.Characters.Query(stmt);

		if (!customizationResult.IsEmpty())
			do
			{
				var guid = customizationResult.Read<ulong>(0);

				ChrCustomizationChoice choice = new();
				choice.ChrCustomizationOptionID = customizationResult.Read<uint>(1);
				choice.ChrCustomizationChoiceID = customizationResult.Read<uint>(2);
				customizations.Add(guid, choice);
			} while (customizationResult.NextRow());

		do
		{
			var type = (CorpseType)result.Read<byte>(13);
			var guid = result.Read<ulong>(15);

			if (type >= CorpseType.Max || type == CorpseType.Bones)
			{
				Log.outError(LogFilter.Maps, "Corpse (guid: {0}) have wrong corpse type ({1}), not loading.", guid, type);

				continue;
			}

			Corpse corpse = new(type);

			if (!corpse.LoadCorpseFromDB(GenerateLowGuid(HighGuid.Corpse), result.GetFields()))
				continue;

			foreach (var phaseId in phases[guid])
				PhasingHandler.AddPhase(corpse, phaseId, false);

			corpse.SetCustomizations(customizations[guid]);

			AddCorpse(corpse);
		} while (result.NextRow());
	}

	public void DeleteCorpseData()
	{
		// DELETE cp, c FROM corpse_phases cp INNER JOIN corpse c ON cp.OwnerGuid = c.guid WHERE c.mapId = ? AND c.instanceId = ?
		var stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CORPSES_FROM_MAP);
		stmt.AddValue(0, Id);
		stmt.AddValue(1, InstanceId);
		DB.Characters.Execute(stmt);
	}

	public void AddCorpse(Corpse corpse)
	{
		corpse.Map = this;

		_corpsesByCell.Add(corpse.GetCellCoord().GetId(), corpse);

		if (corpse.GetCorpseType() != CorpseType.Bones)
			_corpsesByPlayer[corpse.OwnerGUID] = corpse;
		else
			_corpseBones.Add(corpse);
	}

	public Corpse ConvertCorpseToBones(ObjectGuid ownerGuid, bool insignia = false)
	{
		var corpse = GetCorpseByPlayer(ownerGuid);

		if (!corpse)
			return null;

		RemoveCorpse(corpse);

		// remove corpse from DB
		SQLTransaction trans = new();
		corpse.DeleteFromDB(trans);
		DB.Characters.CommitTransaction(trans);

		Corpse bones = null;

		// create the bones only if the map and the grid is loaded at the corpse's location
		// ignore bones creating option in case insignia
		if ((insignia ||
			(IsBattlegroundOrArena ? WorldConfig.GetBoolValue(WorldCfg.DeathBonesBgOrArena) : WorldConfig.GetBoolValue(WorldCfg.DeathBonesWorld))) &&
			!IsRemovalGrid(corpse.Location.X, corpse.Location.Y))
		{
			// Create bones, don't change Corpse
			bones = new Corpse();
			bones.Create(corpse.GUID.Counter, this);

			bones.ReplaceAllCorpseDynamicFlags((CorpseDynFlags)(byte)corpse.CorpseData.DynamicFlags);
			bones.SetOwnerGUID(corpse.CorpseData.Owner);
			bones.SetPartyGUID(corpse.CorpseData.PartyGUID);
			bones.SetGuildGUID(corpse.CorpseData.GuildGUID);
			bones.SetDisplayId(corpse.CorpseData.DisplayID);
			bones.SetRace(corpse.CorpseData.RaceID);
			bones.SetSex(corpse.CorpseData.Sex);
			bones.SetClass(corpse.CorpseData.Class);
			bones.SetCustomizations(corpse.CorpseData.Customizations);
			bones.ReplaceAllFlags((CorpseFlags)(corpse.CorpseData.Flags | (uint)CorpseFlags.Bones));
			bones.SetFactionTemplate(corpse.CorpseData.FactionTemplate);

			for (var i = 0; i < EquipmentSlot.End; ++i)
				bones.SetItem((uint)i, corpse.CorpseData.Items[i]);

			bones.SetCellCoord(corpse.GetCellCoord());
			bones.Location.Relocate(corpse.Location.X, corpse.Location.Y, corpse.Location.Z, corpse.Location.Orientation);

			PhasingHandler.InheritPhaseShift(bones, corpse);

			AddCorpse(bones);

			bones.UpdatePositionData();
			bones.SetZoneScript();

			// add bones in grid store if grid loaded where corpse placed
			AddToMap(bones);
		}

		// all references to the corpse should be removed at this point
		corpse.Dispose();

		return bones;
	}

	public void RemoveOldCorpses()
	{
		var now = GameTime.GetGameTime();

		List<ObjectGuid> corpses = new();

		foreach (var p in _corpsesByPlayer)
			if (p.Value.IsExpired(now))
				corpses.Add(p.Key);

		foreach (var ownerGuid in corpses)
			ConvertCorpseToBones(ownerGuid);

		List<Corpse> expiredBones = new();

		foreach (var bones in _corpseBones)
			if (bones.IsExpired(now))
				expiredBones.Add(bones);

		foreach (var bones in expiredBones)
		{
			RemoveCorpse(bones);
			bones.Dispose();
		}
	}

	public void SendZoneDynamicInfo(uint zoneId, Player player)
	{
		var zoneInfo = _zoneDynamicInfo.LookupByKey(zoneId);

		if (zoneInfo == null)
			return;

		var music = zoneInfo.MusicId;

		if (music != 0)
			player.SendPacket(new PlayMusic(music));

		SendZoneWeather(zoneInfo, player);

		foreach (var lightOverride in zoneInfo.LightOverrides)
		{
			OverrideLight overrideLight = new();
			overrideLight.AreaLightID = lightOverride.AreaLightId;
			overrideLight.OverrideLightID = lightOverride.OverrideLightId;
			overrideLight.TransitionMilliseconds = lightOverride.TransitionMilliseconds;
			player.SendPacket(overrideLight);
		}
	}

	public void SendZoneWeather(uint zoneId, Player player)
	{
		if (!player.HasAuraType(AuraType.ForceWeather))
		{
			var zoneInfo = _zoneDynamicInfo.LookupByKey(zoneId);

			if (zoneInfo == null)
				return;

			SendZoneWeather(zoneInfo, player);
		}
	}

	public void SetZoneMusic(uint zoneId, uint musicId)
	{
		if (!_zoneDynamicInfo.ContainsKey(zoneId))
			_zoneDynamicInfo[zoneId] = new ZoneDynamicInfo();

		_zoneDynamicInfo[zoneId].MusicId = musicId;

		var players = Players;

		if (!players.Empty())
		{
			PlayMusic playMusic = new(musicId);

			foreach (var player in players)
				if (player.Zone == zoneId && !player.HasAuraType(AuraType.ForceWeather))
					player.SendPacket(playMusic);
		}
	}

	public Weather GetOrGenerateZoneDefaultWeather(uint zoneId)
	{
		var weatherData = Global.WeatherMgr.GetWeatherData(zoneId);

		if (weatherData == null)
			return null;

		if (!_zoneDynamicInfo.ContainsKey(zoneId))
			_zoneDynamicInfo[zoneId] = new ZoneDynamicInfo();

		var info = _zoneDynamicInfo[zoneId];

		if (info.DefaultWeather == null)
		{
			info.DefaultWeather = new Weather(zoneId, weatherData);
			info.DefaultWeather.ReGenerate();
			info.DefaultWeather.UpdateWeather();
		}

		return info.DefaultWeather;
	}

	public WeatherState GetZoneWeather(uint zoneId)
	{
		var zoneDynamicInfo = _zoneDynamicInfo.LookupByKey(zoneId);

		if (zoneDynamicInfo != null)
		{
			if (zoneDynamicInfo.WeatherId != 0)
				return zoneDynamicInfo.WeatherId;

			if (zoneDynamicInfo.DefaultWeather != null)
				return zoneDynamicInfo.DefaultWeather.GetWeatherState();
		}

		return WeatherState.Fine;
	}

	public void SetZoneWeather(uint zoneId, WeatherState weatherId, float intensity)
	{
		if (!_zoneDynamicInfo.ContainsKey(zoneId))
			_zoneDynamicInfo[zoneId] = new ZoneDynamicInfo();

		var info = _zoneDynamicInfo[zoneId];
		info.WeatherId = weatherId;
		info.Intensity = intensity;

		var players = Players;

		if (!players.Empty())
		{
			WeatherPkt weather = new(weatherId, intensity);

			foreach (var player in players)
				if (player.Zone == zoneId)
					player.SendPacket(weather);
		}
	}

	public void SetZoneOverrideLight(uint zoneId, uint areaLightId, uint overrideLightId, TimeSpan transitionTime)
	{
		if (!_zoneDynamicInfo.ContainsKey(zoneId))
			_zoneDynamicInfo[zoneId] = new ZoneDynamicInfo();

		var info = _zoneDynamicInfo[zoneId];
		// client can support only one override for each light (zone independent)
		info.LightOverrides.RemoveAll(lightOverride => lightOverride.AreaLightId == areaLightId);

		// set new override (if any)
		if (overrideLightId != 0)
		{
			ZoneDynamicInfo.LightOverride lightOverride = new();
			lightOverride.AreaLightId = areaLightId;
			lightOverride.OverrideLightId = overrideLightId;
			lightOverride.TransitionMilliseconds = (uint)transitionTime.TotalMilliseconds;
			info.LightOverrides.Add(lightOverride);
		}

		var players = Players;

		if (!players.Empty())
		{
			OverrideLight overrideLight = new();
			overrideLight.AreaLightID = areaLightId;
			overrideLight.OverrideLightID = overrideLightId;
			overrideLight.TransitionMilliseconds = (uint)transitionTime.TotalMilliseconds;

			foreach (var player in players)
				if (player.Zone == zoneId)
					player.SendPacket(overrideLight);
		}
	}

	public void UpdateAreaDependentAuras()
	{
		var players = Players;

		foreach (var player in players)
			if (player)
				if (player.IsInWorld)
				{
					player.UpdateAreaDependentAuras(player.Area);
					player.UpdateZoneDependentAuras(player.Zone);
				}
	}

	public virtual string GetDebugInfo()
	{
		return $"Id: {Id} InstanceId: {InstanceId} Difficulty: {DifficultyID} HasPlayers: {HavePlayers}";
	}

	public bool CanUnload(uint diff)
	{
		if (UnloadTimer == 0)
			return false;

		if (UnloadTimer <= diff)
			return true;

		UnloadTimer -= diff;

		return false;
	}

	public bool IsRemovalGrid(float x, float y)
	{
		var p = GridDefines.ComputeGridCoord(x, y);

		return GetGrid(p.X_Coord, p.Y_Coord) == null ||
				GetGrid(p.X_Coord, p.Y_Coord).GetGridState() == GridState.Removal;
	}

	public void ResetGridExpiry(Grid grid, float factor = 1)
	{
		grid.ResetTimeTracker((long)(_gridExpiry * factor));
	}

	public virtual TransferAbortParams CannotEnter(Player player)
	{
		return null;
	}

	public ItemContext GetDifficultyLootItemContext()
	{
		var mapDifficulty = MapDifficulty;

		if (mapDifficulty != null && mapDifficulty.ItemContext != 0)
			return (ItemContext)mapDifficulty.ItemContext;

		var difficulty = CliDB.DifficultyStorage.LookupByKey(DifficultyID);

		if (difficulty != null)
			return (ItemContext)difficulty.ItemContext;

		return ItemContext.None;
	}

	public void AddWorldObject(WorldObject obj)
	{
		_worldObjects.Add(obj);
	}

	public void RemoveWorldObject(WorldObject obj)
	{
		_worldObjects.Remove(obj);
	}

	public void DoOnPlayers(Action<Player> action)
	{
		foreach (var player in Players)
			action(player);
	}

	public List<Corpse> GetCorpsesInCell(uint cellId)
	{
		return _corpsesByCell.LookupByKey(cellId);
	}

	public Corpse GetCorpseByPlayer(ObjectGuid ownerGuid)
	{
		return _corpsesByPlayer.LookupByKey(ownerGuid);
	}

	public void Balance()
	{
		_dynamicTree.Balance();
	}

	public void RemoveGameObjectModel(GameObjectModel model)
	{
		_dynamicTree.Remove(model);
	}

	public void InsertGameObjectModel(GameObjectModel model)
	{
		_dynamicTree.Insert(model);
	}

	public bool ContainsGameObjectModel(GameObjectModel model)
	{
		return _dynamicTree.Contains(model);
	}

	public float GetGameObjectFloor(PhaseShift phaseShift, float x, float y, float z, float maxSearchDist = MapConst.DefaultHeightSearch)
	{
		return _dynamicTree.GetHeight(x, y, z, maxSearchDist, phaseShift);
	}

	public virtual uint GetOwnerGuildId(TeamFaction team = TeamFaction.Other)
	{
		return 0;
	}

	public long GetRespawnTime(SpawnObjectType type, ulong spawnId)
	{
		var map = GetRespawnMapForType(type);

		if (map != null)
		{
			var respawnInfo = map.LookupByKey(spawnId);

			return (respawnInfo == null) ? 0 : respawnInfo.RespawnTime;
		}

		return 0;
	}

	public long GetCreatureRespawnTime(ulong spawnId)
	{
		return GetRespawnTime(SpawnObjectType.Creature, spawnId);
	}

	public long GetGORespawnTime(ulong spawnId)
	{
		return GetRespawnTime(SpawnObjectType.GameObject, spawnId);
	}

	public AreaTrigger GetAreaTrigger(ObjectGuid guid)
	{
		if (!guid.IsAreaTrigger)
			return null;

		return (AreaTrigger)_objectsStore.LookupByKey(guid);
	}

	public SceneObject GetSceneObject(ObjectGuid guid)
	{
		return _objectsStore.LookupByKey(guid) as SceneObject;
	}

	public Conversation GetConversation(ObjectGuid guid)
	{
		return (Conversation)_objectsStore.LookupByKey(guid);
	}

	public Player GetPlayer(ObjectGuid guid)
	{
		return Global.ObjAccessor.GetPlayer(this, guid);
	}

	public Corpse GetCorpse(ObjectGuid guid)
	{
		if (!guid.IsCorpse)
			return null;

		return (Corpse)_objectsStore.LookupByKey(guid);
	}

	public Creature GetCreature(ObjectGuid guid)
	{
		if (!guid.IsCreatureOrVehicle)
			return null;

		return (Creature)_objectsStore.LookupByKey(guid);
	}

	public DynamicObject GetDynamicObject(ObjectGuid guid)
	{
		if (!guid.IsDynamicObject)
			return null;

		return (DynamicObject)_objectsStore.LookupByKey(guid);
	}

	public GameObject GetGameObject(ObjectGuid guid)
	{
		if (!guid.IsAnyTypeGameObject)
			return null;

		return (GameObject)_objectsStore.LookupByKey(guid);
	}

	public Pet GetPet(ObjectGuid guid)
	{
		if (!guid.IsPet)
			return null;

		return (Pet)_objectsStore.LookupByKey(guid);
	}

	public Transport GetTransport(ObjectGuid guid)
	{
		if (!guid.IsMOTransport)
			return null;

		var go = GetGameObject(guid);

		return go ? go.AsTransport : null;
	}

	public Creature GetCreatureBySpawnId(ulong spawnId)
	{
		var bounds = CreatureBySpawnIdStore.LookupByKey(spawnId);

		if (bounds.Empty())
			return null;

		var foundCreature = bounds.Find(creature => creature.IsAlive);

		return foundCreature != null ? foundCreature : bounds[0];
	}

	public GameObject GetGameObjectBySpawnId(ulong spawnId)
	{
		var bounds = GameObjectBySpawnIdStore.LookupByKey(spawnId);

		if (bounds.Empty())
			return null;

		var foundGameObject = bounds.Find(gameobject => gameobject.IsSpawned);

		return foundGameObject != null ? foundGameObject : bounds[0];
	}

	public AreaTrigger GetAreaTriggerBySpawnId(ulong spawnId)
	{
		var bounds = AreaTriggerBySpawnIdStore.LookupByKey(spawnId);

		if (bounds.Empty())
			return null;

		return bounds.FirstOrDefault();
	}

	public WorldObject GetWorldObjectBySpawnId(SpawnObjectType type, ulong spawnId)
	{
		switch (type)
		{
			case SpawnObjectType.Creature:
				return GetCreatureBySpawnId(spawnId);
			case SpawnObjectType.GameObject:
				return GetGameObjectBySpawnId(spawnId);
			case SpawnObjectType.AreaTrigger:
				return GetAreaTriggerBySpawnId(spawnId);
			default:
				return null;
		}
	}

	public void Visit(Cell cell, IGridNotifier visitor)
	{
		var x = cell.GetGridX();
		var y = cell.GetGridY();
		var cell_x = cell.GetCellX();
		var cell_y = cell.GetCellY();

		if (!cell.NoCreate() || IsGridLoaded(x, y))
		{
			EnsureGridLoaded(cell);
			GetGrid(x, y).VisitGrid(cell_x, cell_y, visitor);
		}
	}

	public TempSummon SummonCreature(uint entry, Position pos, SummonPropertiesRecord properties = null, uint duration = 0, WorldObject summoner = null, uint spellId = 0, uint vehId = 0, ObjectGuid privateObjectOwner = default, SmoothPhasingInfo smoothPhasingInfo = null)
	{
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

		var summonerUnit = summoner?.AsUnit;

		TempSummon summon;

		switch (mask)
		{
			case UnitTypeMask.Summon:
				summon = new TempSummon(properties, summonerUnit, false);

				break;
			case UnitTypeMask.Guardian:
				summon = new Guardian(properties, summonerUnit, false);

				break;
			case UnitTypeMask.Puppet:
				summon = new Puppet(properties, summonerUnit);

				break;
			case UnitTypeMask.Totem:
				summon = new Totem(properties, summonerUnit);

				break;
			case UnitTypeMask.Minion:
				summon = new Minion(properties, summonerUnit, false);

				break;
			default:
				return null;
		}

		if (!summon.Create(GenerateLowGuid(HighGuid.Creature), this, entry, pos, null, vehId, true))
			return null;

		var transport = summoner?.Transport;

		if (transport != null)
		{
			var relocatePos = pos.Copy();
			transport.CalculatePassengerOffset(relocatePos);
			summon.MovementInfo.Transport.Pos.Relocate(relocatePos);

			// This object must be added to transport before adding to map for the client to properly display it
			transport.AddPassenger(summon);
		}

		// Set the summon to the summoner's phase
		if (summoner != null && !(properties != null && properties.GetFlags().HasFlag(SummonPropertiesFlags.IgnoreSummonerPhase)))
			PhasingHandler.InheritPhaseShift(summon, summoner);

		summon.SetCreatedBySpell(spellId);
		summon.UpdateAllowedPositionZ(pos);
		summon.HomePosition = pos;
		summon.InitStats(duration);
		summon.PrivateObjectOwner = privateObjectOwner;

		if (smoothPhasingInfo != null)
		{
			if (summoner != null && smoothPhasingInfo.ReplaceObject.HasValue)
			{
				var replacedObject = Global.ObjAccessor.GetWorldObject(summoner, smoothPhasingInfo.ReplaceObject.Value);

				if (replacedObject != null)
				{
					var originalSmoothPhasingInfo = smoothPhasingInfo;
					originalSmoothPhasingInfo.ReplaceObject = summon.GUID;
					replacedObject.GetOrCreateSmoothPhasing().SetViewerDependentInfo(privateObjectOwner, originalSmoothPhasingInfo);

					summon.DemonCreatorGUID = privateObjectOwner;
				}
			}

			summon.GetOrCreateSmoothPhasing().SetSingleInfo(smoothPhasingInfo);
		}

		if (!AddToMap(summon.AsCreature))
		{
			// Returning false will cause the object to be deleted - remove from transport
			transport?.RemovePassenger(summon);

			summon.Dispose();

			return null;
		}

		summon.InitSummon();

		// call MoveInLineOfSight for nearby creatures
		AIRelocationNotifier notifier = new(summon, GridType.All);
		Cell.VisitGrid(summon, notifier, VisibilityRange);

		return summon;
	}

	public ulong GenerateLowGuid(HighGuid high)
	{
		return GetGuidSequenceGenerator(high).Generate();
	}

	public ulong GetMaxLowGuid(HighGuid high)
	{
		return GetGuidSequenceGenerator(high).GetNextAfterMaxUsed();
	}

	public void AddUpdateObject(WorldObject obj)
	{
		lock (_updateObjects)
		{
			if (obj != null)
				_updateObjects.Add(obj);
		}
	}

	public void RemoveUpdateObject(WorldObject obj)
	{
		lock (_updateObjects)
		{
			_updateObjects.Remove(obj);
		}
	}

	public static implicit operator bool(Map map)
	{
		return map != null;
	}

	private void SwitchGridContainers(WorldObject obj, bool on)
	{
		if (obj.IsPermanentWorldObject)
			return;

		var p = GridDefines.ComputeCellCoord(obj.Location.X, obj.Location.Y);

		if (!p.IsCoordValid())
		{
			Log.outError(LogFilter.Maps,
						"Map.SwitchGridContainers: Object {0} has invalid coordinates X:{1} Y:{2} grid cell [{3}:{4}]",
						obj.GUID,
						obj.Location.X,
						obj.Location.Y,
						p.X_Coord,
						p.Y_Coord);

			return;
		}

		var cell = new Cell(p);

		if (!IsGridLoaded(cell.GetGridX(), cell.GetGridY()))
			return;

		Log.outDebug(LogFilter.Maps, "Switch object {0} from grid[{1}, {2}] {3}", obj.GUID, cell.GetGridX(), cell.GetGridY(), on);
		var ngrid = GetGrid(cell.GetGridX(), cell.GetGridY());

		RemoveFromGrid(obj, cell);

		var gridCell = ngrid.GetGridCell(cell.GetCellX(), cell.GetCellY());

		if (on)
		{
			gridCell.AddWorldObject(obj);
			AddWorldObject(obj);
		}
		else
		{
			gridCell.AddGridObject(obj);
			RemoveWorldObject(obj);
		}

		obj.Location.SetCurrentCell(cell);
		obj.AsCreature.IsTempWorldObject = on;
	}

	private void DeleteFromWorld(Player player)
	{
		Global.ObjAccessor.RemoveObject(player);
		RemoveUpdateObject(player); // @todo I do not know why we need this, it should be removed in ~Object anyway
		player.Dispose();
	}

	private void DeleteFromWorld(WorldObject obj)
	{
		obj.Dispose();
	}

	private void EnsureGridCreated(GridCoord p)
	{
		object lockobj = null;

		lock (_locks)
		{
			lockobj = _locks.GetOrAdd(p.X_Coord, p.Y_Coord, () => new object());
		}

		lock (lockobj)
		{
			if (GetGrid(p.X_Coord, p.Y_Coord) == null)
			{
				Log.outDebug(LogFilter.Maps, "Creating grid[{0}, {1}] for map {2} instance {3}", p.X_Coord, p.Y_Coord, Id, InstanceIdInternal);

				var grid = new Grid(p.X_Coord * MapConst.MaxGrids + p.Y_Coord, p.X_Coord, p.Y_Coord, _gridExpiry, WorldConfig.GetBoolValue(WorldCfg.GridUnload));
				grid.SetGridState(GridState.Idle);
				SetGrid(grid, p.X_Coord, p.Y_Coord);

				//z coord
				var gx = (int)((MapConst.MaxGrids - 1) - p.X_Coord);
				var gy = (int)((MapConst.MaxGrids - 1) - p.Y_Coord);

				if (gx > -1 && gy > -1)
					_terrain.LoadMapAndVMap(gx, gy);
			}
		}
	}

	private void EnsureGridLoadedForActiveObject(Cell cell, WorldObject obj)
	{
		EnsureGridLoaded(cell);
		var grid = GetGrid(cell.GetGridX(), cell.GetGridY());

		if (obj.IsPlayer)
			MultiPersonalPhaseTracker.LoadGrid(obj.PhaseShift, grid, this, cell);

		// refresh grid state & timer
		if (grid.GetGridState() != GridState.Active)
		{
			Log.outDebug(LogFilter.Maps,
						"Active object {0} triggers loading of grid [{1}, {2}] on map {3}",
						obj.GUID,
						cell.GetGridX(),
						cell.GetGridY(),
						Id);

			ResetGridExpiry(grid, 0.1f);
			grid.SetGridState(GridState.Active);
		}
	}

	private bool EnsureGridLoaded(Cell cell)
	{
		EnsureGridCreated(new GridCoord(cell.GetGridX(), cell.GetGridY()));
		var grid = GetGrid(cell.GetGridX(), cell.GetGridY());

		if (grid != null && !IsGridObjectDataLoaded(cell.GetGridX(), cell.GetGridY()))
		{
			Log.outDebug(LogFilter.Maps,
						"Loading grid[{0}, {1}] for map {2} instance {3}",
						cell.GetGridX(),
						cell.GetGridY(),
						Id,
						InstanceIdInternal);

			SetGridObjectDataLoaded(true, cell.GetGridX(), cell.GetGridY());

			LoadGridObjects(grid, cell);

			Balance();

			return true;
		}

		return false;
	}

	private void GridMarkNoUnload(uint x, uint y)
	{
		// First make sure this grid is loaded
		var gX = (((float)x - 0.5f - MapConst.CenterGridId) * MapConst.SizeofGrids) + (MapConst.CenterGridOffset * 2);
		var gY = (((float)y - 0.5f - MapConst.CenterGridId) * MapConst.SizeofGrids) + (MapConst.CenterGridOffset * 2);
		Cell cell = new(gX, gY);
		EnsureGridLoaded(cell);

		// Mark as don't unload
		var grid = GetGrid(x, y);
		grid.SetUnloadExplicitLock(true);
	}

	private void GridUnmarkNoUnload(uint x, uint y)
	{
		// If grid is loaded, clear unload lock
		if (IsGridLoaded(x, y))
		{
			var grid = GetGrid(x, y);
			grid.SetUnloadExplicitLock(false);
		}
	}

	private void InitializeObject(WorldObject obj)
	{
		if (!obj.IsTypeId(TypeId.Unit) || !obj.IsTypeId(TypeId.GameObject))
			return;

		obj.Location.MoveState = ObjectCellMoveState.None;
	}

	private void VisitNearbyCellsOf(WorldObject obj, IGridNotifier gridVisitor)
	{
		// Check for valid position
		if (!obj.Location.IsPositionValid)
			return;

		// Update mobs/objects in ALL visible cells around object!
		var area = Cell.CalculateCellArea(obj.Location.X, obj.Location.Y, obj.GridActivationRange);

		for (var x = area.LowBound.X_Coord; x <= area.HighBound.X_Coord; ++x)
		{
			for (var y = area.LowBound.Y_Coord; y <= area.HighBound.Y_Coord; ++y)
			{
				// marked cells are those that have been visited
				// don't visit the same cell twice
				var cell_id = (y * MapConst.TotalCellsPerMap) + x;

				if (IsCellMarked(cell_id))
					continue;

				MarkCell(cell_id);
				var pair = new CellCoord(x, y);
				var cell = new Cell(pair);
				cell.SetNoCreate();
				Visit(cell, gridVisitor);
			}
		}
	}

	private void ProcessRelocationNotifies(uint diff)
	{
		var xKeys = GridXKeys();

		foreach (var x in xKeys)
		{
			foreach (var y in GridYKeys(x))
			{
				var grid = GetGrid(x, y);

				if (grid == null)
					continue;

				if (grid.GetGridState() != GridState.Active)
					continue;

				grid.GetGridInfoRef().GetRelocationTimer().TUpdate((int)diff);

				if (!grid.GetGridInfoRef().GetRelocationTimer().TPassed())
					continue;

				var gx = grid.GetX();
				var gy = grid.GetY();

				var cell_min = new CellCoord(gx * MapConst.MaxCells, gy * MapConst.MaxCells);
				var cell_max = new CellCoord(cell_min.X_Coord + MapConst.MaxCells, cell_min.Y_Coord + MapConst.MaxCells);


				for (var xx = cell_min.X_Coord; xx < cell_max.X_Coord; ++xx)
				{
					for (var yy = cell_min.Y_Coord; yy < cell_max.Y_Coord; ++yy)
					{
						var cell_id = (yy * MapConst.TotalCellsPerMap) + xx;

						if (!IsCellMarked(cell_id))
							continue;

						var pair = new CellCoord(xx, yy);
						var cell = new Cell(pair);
						cell.SetNoCreate();

						var cell_relocation = new DelayedUnitRelocation(cell, pair, this, SharedConst.MaxVisibilityDistance, GridType.All);

						Visit(cell, cell_relocation);
					}
				}
			}
		}

		var reset = new ResetNotifier(GridType.All);

		foreach (var x in xKeys)
		{
			foreach (var y in GridYKeys(x))
			{
				var grid = GetGrid(x, y);

				if (grid == null)
					continue;

				if (grid.GetGridState() != GridState.Active)
					continue;

				if (!grid.GetGridInfoRef().GetRelocationTimer().TPassed())
					continue;

				grid.GetGridInfoRef().GetRelocationTimer().TReset((int)diff, VisibilityNotifyPeriod);

				var gx = grid.GetX();
				var gy = grid.GetY();

				var cell_min = new CellCoord(gx * MapConst.MaxCells, gy * MapConst.MaxCells);

				var cell_max = new CellCoord(cell_min.X_Coord + MapConst.MaxCells,
											cell_min.Y_Coord + MapConst.MaxCells);

				for (var xx = cell_min.X_Coord; xx < cell_max.X_Coord; ++xx)
				{
					for (var yy = cell_min.Y_Coord; yy < cell_max.Y_Coord; ++yy)
					{
						var cell_id = (yy * MapConst.TotalCellsPerMap) + xx;

						if (!IsCellMarked(cell_id))
							continue;

						var pair = new CellCoord(xx, yy);
						var cell = new Cell(pair);
						cell.SetNoCreate();
						Visit(cell, reset);
					}
				}
			}
		}
	}

	private bool CheckGridIntegrity<T>(T obj, bool moved) where T : WorldObject
	{
		var cur_cell = obj.Location.GetCurrentCell();
		Cell xy_cell = new(obj.Location.X, obj.Location.Y);

		if (xy_cell != cur_cell)
		{
			//$"grid[{GetGridX()}, {GetGridY()}]cell[{GetCellX()}, {GetCellY()}]";
			Log.outDebug(LogFilter.Maps, $"{obj.TypeId} ({obj.GUID}) X: {obj.Location.X} Y: {obj.Location.Y} ({(moved ? "final" : "original")}) is in {cur_cell} instead of {xy_cell}");

			return true; // not crash at error, just output error in debug mode
		}

		return true;
	}

	private void AddCreatureToMoveList(Creature c, float x, float y, float z, float ang)
	{
		lock (_creaturesToMove)
		{
			if (c.Location.MoveState == ObjectCellMoveState.None)
				_creaturesToMove.Add(c);

			c.Location.SetNewCellPosition(x, y, z, ang);
		}
	}

	private void AddGameObjectToMoveList(GameObject go, float x, float y, float z, float ang)
	{
		lock (_gameObjectsToMove)
		{
			if (go.Location.MoveState == ObjectCellMoveState.None)
				_gameObjectsToMove.Add(go);

			go.Location.SetNewCellPosition(x, y, z, ang);
		}
	}

	private void RemoveGameObjectFromMoveList(GameObject go)
	{
		lock (_gameObjectsToMove)
		{
			if (go.Location.MoveState == ObjectCellMoveState.Active)
				go.Location.MoveState = ObjectCellMoveState.Inactive;
		}
	}

	private void RemoveCreatureFromMoveList(Creature c)
	{
		lock (_creaturesToMove)
		{
			if (c.Location.MoveState == ObjectCellMoveState.Active)
				c.Location.MoveState = ObjectCellMoveState.Inactive;
		}
	}

	private void AddDynamicObjectToMoveList(DynamicObject dynObj, float x, float y, float z, float ang)
	{
		lock (_dynamicObjectsToMove)
		{
			if (dynObj.Location.MoveState == ObjectCellMoveState.None)
				_dynamicObjectsToMove.Add(dynObj);

			dynObj.Location.SetNewCellPosition(x, y, z, ang);
		}
	}

	private void RemoveDynamicObjectFromMoveList(DynamicObject dynObj)
	{
		lock (_dynamicObjectsToMove)
		{
			if (dynObj.Location.MoveState == ObjectCellMoveState.Active)
				dynObj.Location.MoveState = ObjectCellMoveState.Inactive;
		}
	}

	private void AddAreaTriggerToMoveList(AreaTrigger at, float x, float y, float z, float ang)
	{
		lock (_areaTriggersToMove)
		{
			if (at.Location.MoveState == ObjectCellMoveState.None)
				_areaTriggersToMove.Add(at);

			at.Location.SetNewCellPosition(x, y, z, ang);
		}
	}

	private void RemoveAreaTriggerFromMoveList(AreaTrigger at)
	{
		lock (_areaTriggersToMove)
		{
			if (at.Location.MoveState == ObjectCellMoveState.Active)
				at.Location.MoveState = ObjectCellMoveState.Inactive;
		}
	}

	private void MoveAllCreaturesInMoveList()
	{
		lock (_creaturesToMove)
		{
			for (var i = 0; i < _creaturesToMove.Count; ++i)
			{
				var creature = _creaturesToMove[i];

				if (creature.Map != this) //pet is teleported to another map
					continue;

				if (creature.Location.MoveState != ObjectCellMoveState.Active)
				{
					creature.Location.MoveState = ObjectCellMoveState.None;

					continue;
				}

				creature.Location.MoveState = ObjectCellMoveState.None;

				if (!creature.IsInWorld)
					continue;

				// do move or do move to respawn or remove creature if previous all fail
				if (CreatureCellRelocation(creature, new Cell(creature.Location.NewPosition.X, creature.Location.NewPosition.Y)))
				{
					// update pos
					creature.Location.Relocate(creature.Location.NewPosition);

					if (creature.IsVehicle)
						creature.VehicleKit1.RelocatePassengers();

					creature.UpdatePositionData();
					creature.UpdateObjectVisibility(false);
				}
				else
				{
					// if creature can't be move in new cell/grid (not loaded) move it to repawn cell/grid
					// creature coordinates will be updated and notifiers send
					if (!CreatureRespawnRelocation(creature, false))
					{
						// ... or unload (if respawn grid also not loaded)
						//This may happen when a player just logs in and a pet moves to a nearby unloaded cell
						//To avoid this, we can load nearby cells when player log in
						//But this check is always needed to ensure safety
						// @todo pets will disappear if this is outside CreatureRespawnRelocation
						//need to check why pet is frequently relocated to an unloaded cell
						if (creature.IsPet)
							((Pet)creature).Remove(PetSaveMode.NotInSlot, true);
						else
							AddObjectToRemoveList(creature);
					}
				}
			}
		}
	}

	private void MoveAllGameObjectsInMoveList()
	{
		lock (_gameObjectsToMove)
		{
			for (var i = 0; i < _gameObjectsToMove.Count; ++i)
			{
				var go = _gameObjectsToMove[i];

				if (go.Map != this) //transport is teleported to another map
					continue;

				if (go.Location.MoveState != ObjectCellMoveState.Active)
				{
					go.Location.MoveState = ObjectCellMoveState.None;

					continue;
				}

				go.Location.MoveState = ObjectCellMoveState.None;

				if (!go.IsInWorld)
					continue;

				// do move or do move to respawn or remove creature if previous all fail
				if (GameObjectCellRelocation(go, new Cell(go.Location.NewPosition.X, go.Location.NewPosition.Y)))
				{
					// update pos
					go.Location.Relocate(go.Location.NewPosition);
					go.AfterRelocation();
				}
				else
				{
					// if GameObject can't be move in new cell/grid (not loaded) move it to repawn cell/grid
					// GameObject coordinates will be updated and notifiers send
					if (!GameObjectRespawnRelocation(go, false))
					{
						// ... or unload (if respawn grid also not loaded)
						Log.outDebug(LogFilter.Maps,
									"GameObject (GUID: {0} Entry: {1}) cannot be move to unloaded respawn grid.",
									go.GUID.ToString(),
									go.Entry);

						AddObjectToRemoveList(go);
					}
				}
			}
		}
	}

	private void MoveAllDynamicObjectsInMoveList()
	{
		lock (_dynamicObjectsToMove)
		{
			for (var i = 0; i < _dynamicObjectsToMove.Count; ++i)
			{
				var dynObj = _dynamicObjectsToMove[i];

				if (dynObj.Map != this) //transport is teleported to another map
					continue;

				if (dynObj.Location.MoveState != ObjectCellMoveState.Active)
				{
					dynObj.Location.MoveState = ObjectCellMoveState.None;

					continue;
				}

				dynObj.Location.MoveState = ObjectCellMoveState.None;

				if (!dynObj.IsInWorld)
					continue;

				// do move or do move to respawn or remove creature if previous all fail
				if (DynamicObjectCellRelocation(dynObj, new Cell(dynObj.Location.NewPosition.X, dynObj.Location.NewPosition.Y)))
				{
					// update pos
					dynObj.Location.Relocate(dynObj.Location.NewPosition);
					dynObj.UpdatePositionData();
					dynObj.UpdateObjectVisibility(false);
				}
				else
				{
					Log.outDebug(LogFilter.Maps, "DynamicObject (GUID: {0}) cannot be moved to unloaded grid.", dynObj.GUID.ToString());
				}
			}
		}
	}

	private void MoveAllAreaTriggersInMoveList()
	{
		lock (_areaTriggersToMove)
		{
			for (var i = 0; i < _areaTriggersToMove.Count; ++i)
			{
				var at = _areaTriggersToMove[i];

				if (at.Map != this) //transport is teleported to another map
					continue;

				if (at.Location.MoveState != ObjectCellMoveState.Active)
				{
					at.Location.MoveState = ObjectCellMoveState.None;

					continue;
				}

				at.Location.MoveState = ObjectCellMoveState.None;

				if (!at.IsInWorld)
					continue;

				// do move or do move to respawn or remove creature if previous all fail
				if (AreaTriggerCellRelocation(at, new Cell(at.Location.NewPosition.X, at.Location.NewPosition.Y)))
				{
					// update pos
					at.Location.Relocate(at.Location.NewPosition);
					at.UpdateShape();
					at.UpdateObjectVisibility(false);
				}
				else
				{
					Log.outDebug(LogFilter.Maps, "AreaTrigger ({0}) cannot be moved to unloaded grid.", at.GUID.ToString());
				}
			}
		}
	}

	private bool MapObjectCellRelocation<T>(T obj, Cell new_cell) where T : WorldObject
	{
		var old_cell = obj.Location.GetCurrentCell();

		if (!old_cell.DiffGrid(new_cell)) // in same grid
		{
			// if in same cell then none do
			if (old_cell.DiffCell(new_cell))
			{
				RemoveFromGrid(obj, old_cell);
				AddToGrid(obj, new_cell);
			}

			return true;
		}

		// in diff. grids but active creature
		if (obj.IsActiveObject)
		{
			EnsureGridLoadedForActiveObject(new_cell, obj);

			Log.outDebug(LogFilter.Maps,
						"Active creature (GUID: {0} Entry: {1}) moved from grid[{2}, {3}] to grid[{4}, {5}].",
						obj.GUID.ToString(),
						obj.Entry,
						old_cell.GetGridX(),
						old_cell.GetGridY(),
						new_cell.GetGridX(),
						new_cell.GetGridY());

			RemoveFromGrid(obj, old_cell);
			AddToGrid(obj, new_cell);

			return true;
		}

		var c = obj.AsCreature;

		if (c != null && c.CharmerOrOwnerGUID.IsPlayer)
			EnsureGridLoaded(new_cell);

		// in diff. loaded grid normal creature
		var grid = new GridCoord(new_cell.GetGridX(), new_cell.GetGridY());

		if (IsGridLoaded(grid))
		{
			RemoveFromGrid(obj, old_cell);
			EnsureGridCreated(grid);
			AddToGrid(obj, new_cell);

			return true;
		}

		// fail to move: normal creature attempt move to unloaded grid
		return false;
	}

	private bool CreatureCellRelocation(Creature c, Cell new_cell)
	{
		return MapObjectCellRelocation(c, new_cell);
	}

	private bool GameObjectCellRelocation(GameObject go, Cell new_cell)
	{
		return MapObjectCellRelocation(go, new_cell);
	}

	private bool DynamicObjectCellRelocation(DynamicObject go, Cell new_cell)
	{
		return MapObjectCellRelocation(go, new_cell);
	}

	private bool AreaTriggerCellRelocation(AreaTrigger at, Cell new_cell)
	{
		return MapObjectCellRelocation(at, new_cell);
	}

	private bool GetAreaInfo(PhaseShift phaseShift, float x, float y, float z, out uint mogpflags, out int adtId, out int rootId, out int groupId)
	{
		return _terrain.GetAreaInfo(phaseShift, Id, x, y, z, out mogpflags, out adtId, out rootId, out groupId, _dynamicTree);
	}

	private void SendInitTransports(Player player)
	{
		var transData = new UpdateData(Id);

		foreach (var transport in _transports)
			if (transport.IsInWorld && transport != player.Transport && player.InSamePhase(transport))
			{
				transport.BuildCreateUpdateBlockForPlayer(transData, player);
				player.VisibleTransports.Add(transport.GUID);
			}

		transData.BuildPacket(out var packet);
		player.SendPacket(packet);
	}

	private void SendRemoveTransports(Player player)
	{
		var transData = new UpdateData(player.Location.MapId);

		foreach (var transport in _transports)
			if (player.VisibleTransports.Contains(transport.GUID) && transport != player.Transport)
			{
				transport.BuildOutOfRangeUpdateBlock(transData);
				player.VisibleTransports.Remove(transport.GUID);
			}

		transData.BuildPacket(out var packet);
		player.SendPacket(packet);
	}

	private void SetGrid(Grid grid, uint x, uint y)
	{
		if (x >= MapConst.MaxGrids || y >= MapConst.MaxGrids)
		{
			Log.outError(LogFilter.Maps, "Map.setNGrid Invalid grid coordinates found: {0}, {1}!", x, y);

			return;
		}

		lock (Grids)
		{
			Grids.Add(x, y, grid);
		}
	}

	private void SendObjectUpdates()
	{
		Dictionary<Player, UpdateData> update_players = new();

		lock (_updateObjects)
		{
			while (!_updateObjects.Empty())
			{
				var obj = _updateObjects[0];
				_updateObjects.RemoveAt(0);
				obj.BuildUpdate(update_players);
			}
		}

		foreach (var iter in update_players)
		{
			iter.Value.BuildPacket(out var packet);
			iter.Key.SendPacket(packet);
		}
	}

	private bool CheckRespawn(RespawnInfo info)
	{
		var data = Global.ObjectMgr.GetSpawnData(info.ObjectType, info.SpawnId);

		// First, check if this creature's spawn group is inactive
		if (!IsSpawnGroupActive(data.SpawnGroupData.GroupId))
		{
			info.RespawnTime = 0;

			return false;
		}

		// Next, check if there's already an instance of this object that would block the respawn
		// Only do this for unpooled spawns
		var alreadyExists = false;

		switch (info.ObjectType)
		{
			case SpawnObjectType.Creature:
			{
				// escort check for creatures only (if the world config boolean is set)
				var isEscort = WorldConfig.GetBoolValue(WorldCfg.RespawnDynamicEscortNpc) && data.SpawnGroupData.Flags.HasFlag(SpawnGroupFlags.EscortQuestNpc);

				var range = _creatureBySpawnIdStore.LookupByKey(info.SpawnId);

				foreach (var creature in range)
				{
					if (!creature.IsAlive)
						continue;

					// escort NPCs are allowed to respawn as long as all other instances are already escorting
					if (isEscort && creature.IsEscorted)
						continue;

					alreadyExists = true;

					break;
				}

				break;
			}
			case SpawnObjectType.GameObject:
				// gameobject check is simpler - they cannot be dead or escorting
				if (_gameobjectBySpawnIdStore.ContainsKey(info.SpawnId))
					alreadyExists = true;

				break;
			default:
				return true;
		}

		if (alreadyExists)
		{
			info.RespawnTime = 0;

			return false;
		}

		// next, check linked respawn time
		var thisGUID = info.ObjectType == SpawnObjectType.GameObject ? ObjectGuid.Create(HighGuid.GameObject, Id, info.Entry, info.SpawnId) : ObjectGuid.Create(HighGuid.Creature, Id, info.Entry, info.SpawnId);
		var linkedTime = GetLinkedRespawnTime(thisGUID);

		if (linkedTime != 0)
		{
			var now = GameTime.GetGameTime();
			long respawnTime;

			if (linkedTime == long.MaxValue)
				respawnTime = linkedTime;
			else if (Global.ObjectMgr.GetLinkedRespawnGuid(thisGUID) == thisGUID) // never respawn, save "something" in DB
				respawnTime = now + Time.Week;
			else // set us to check again shortly after linked unit
				respawnTime = Math.Max(now, linkedTime) + RandomHelper.URand(5, 15);

			info.RespawnTime = respawnTime;

			return false;
		}

		// everything ok, let's spawn
		return true;
	}

	private int DespawnAll(SpawnObjectType type, ulong spawnId)
	{
		List<WorldObject> toUnload = new();

		switch (type)
		{
			case SpawnObjectType.Creature:
				foreach (var creature in CreatureBySpawnIdStore.LookupByKey(spawnId))
					toUnload.Add(creature);

				break;
			case SpawnObjectType.GameObject:
				foreach (var obj in GameObjectBySpawnIdStore.LookupByKey(spawnId))
					toUnload.Add(obj);

				break;
			default:
				break;
		}

		foreach (var o in toUnload)
			AddObjectToRemoveList(o);

		return toUnload.Count;
	}

	private bool AddRespawnInfo(RespawnInfo info)
	{
		if (info.SpawnId == 0)
		{
			Log.outError(LogFilter.Maps, $"Attempt to insert respawn info for zero spawn id (type {info.ObjectType})");

			return false;
		}

		var bySpawnIdMap = GetRespawnMapForType(info.ObjectType);

		if (bySpawnIdMap == null)
			return false;

		// check if we already have the maximum possible number of respawns scheduled
		if (SpawnMetadata.TypeHasData(info.ObjectType))
		{
			var existing = bySpawnIdMap.LookupByKey(info.SpawnId);

			if (existing != null) // spawnid already has a respawn scheduled
			{
				if (info.RespawnTime <= existing.RespawnTime) // delete existing in this case
					DeleteRespawnInfo(existing);
				else
					return false;
			}
		}
		else
		{
			return false;
		}

		RespawnInfo ri = new(info);
		_respawnTimes.Add(ri);
		bySpawnIdMap.Add(ri.SpawnId, ri);

		return true;
	}

	private static void PushRespawnInfoFrom(List<RespawnInfo> data, Dictionary<ulong, RespawnInfo> map)
	{
		foreach (var pair in map)
			data.Add(pair.Value);
	}

	private Dictionary<ulong, RespawnInfo> GetRespawnMapForType(SpawnObjectType type)
	{
		switch (type)
		{
			case SpawnObjectType.Creature:
				return _creatureRespawnTimesBySpawnId;
			case SpawnObjectType.GameObject:
				return _gameObjectRespawnTimesBySpawnId;
			case SpawnObjectType.AreaTrigger:
				return null;
			default:
				return null;
		}
	}

	private void UnloadAllRespawnInfos() // delete everything from memory
	{
		_respawnTimes.Clear();
		_creatureRespawnTimesBySpawnId.Clear();
		_gameObjectRespawnTimesBySpawnId.Clear();
	}

	private void DeleteRespawnInfo(RespawnInfo info, SQLTransaction dbTrans = null)
	{
		// spawnid store
		var spawnMap = GetRespawnMapForType(info.ObjectType);

		if (spawnMap == null)
			return;

		var respawnInfo = spawnMap.LookupByKey(info.SpawnId);
		spawnMap.Remove(info.SpawnId);

		// respawn heap
		_respawnTimes.Remove(info);

		// database
		DeleteRespawnInfoFromDB(info.ObjectType, info.SpawnId, dbTrans);
	}

	private void DeleteRespawnInfoFromDB(SpawnObjectType type, ulong spawnId, SQLTransaction dbTrans = null)
	{
		if (Instanceable)
			return;

		var stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_RESPAWN);
		stmt.AddValue(0, (ushort)type);
		stmt.AddValue(1, spawnId);
		stmt.AddValue(2, Id);
		stmt.AddValue(3, InstanceId);
		DB.Characters.ExecuteOrAppend(dbTrans, stmt);
	}

	private void DoRespawn(SpawnObjectType type, ulong spawnId, uint gridId)
	{
		if (!IsGridLoaded(gridId)) // if grid isn't loaded, this will be processed in grid load handler
			return;

		switch (type)
		{
			case SpawnObjectType.Creature:
			{
				Creature obj = new();

				if (!obj.LoadFromDB(spawnId, this, true, true))
					obj.Dispose();

				break;
			}
			case SpawnObjectType.GameObject:
			{
				GameObject obj = new();

				if (!obj.LoadFromDB(spawnId, this, true))
					obj.Dispose();

				break;
			}
		}
	}

	private void ProcessRespawns()
	{
		var now = GameTime.GetGameTime();

		while (!_respawnTimes.Empty())
		{
			var next = _respawnTimes.First();

			if (now < next.RespawnTime) // done for this tick
				break;

			var poolId = Global.PoolMgr.IsPartOfAPool(next.ObjectType, next.SpawnId);

			if (poolId != 0) // is this part of a pool?
			{
				// if yes, respawn will be handled by (external) pooling logic, just delete the respawn time
				// step 1: remove entry from maps to avoid it being reachable by outside logic
				_respawnTimes.Remove(next);
				GetRespawnMapForType(next.ObjectType).Remove(next.SpawnId);

				// step 2: tell pooling logic to do its thing
				Global.PoolMgr.UpdatePool(PoolData, poolId, next.ObjectType, next.SpawnId);

				// step 3: get rid of the actual entry
				RemoveRespawnTime(next.ObjectType, next.SpawnId, null, true);
				GetRespawnMapForType(next.ObjectType).Remove(next.SpawnId);
			}
			else if (CheckRespawn(next)) // see if we're allowed to respawn
			{
				// ok, respawn
				// step 1: remove entry from maps to avoid it being reachable by outside logic
				_respawnTimes.Remove(next);
				GetRespawnMapForType(next.ObjectType).Remove(next.SpawnId);

				// step 2: do the respawn, which involves external logic
				DoRespawn(next.ObjectType, next.SpawnId, next.GridId);

				// step 3: get rid of the actual entry
				RemoveRespawnTime(next.ObjectType, next.SpawnId, null, true);
				GetRespawnMapForType(next.ObjectType).Remove(next.SpawnId);
			}
			else if (next.RespawnTime == 0)
			{
				// just remove this respawn entry without rescheduling
				_respawnTimes.Remove(next);
				GetRespawnMapForType(next.ObjectType).Remove(next.SpawnId);
				RemoveRespawnTime(next.ObjectType, next.SpawnId, null, true);
			}
			else
			{
				// new respawn time, update heap position
				SaveRespawnInfoDB(next);
			}
		}
	}

	private bool ShouldBeSpawnedOnGridLoad(SpawnObjectType type, ulong spawnId)
	{
		// check if the object is on its respawn timer
		if (GetRespawnTime(type, spawnId) != 0)
			return false;

		var spawnData = Global.ObjectMgr.GetSpawnMetadata(type, spawnId);
		// check if the object is part of a spawn group
		var spawnGroup = spawnData.SpawnGroupData;

		if (!spawnGroup.Flags.HasFlag(SpawnGroupFlags.System))
			if (!IsSpawnGroupActive(spawnGroup.GroupId))
				return false;

		if (spawnData.ToSpawnData().poolId != 0)
			if (!PoolData.IsSpawnedObject(type, spawnId))
				return false;

		return true;
	}

	private SpawnGroupTemplateData GetSpawnGroupData(uint groupId)
	{
		var data = Global.ObjectMgr.GetSpawnGroupData(groupId);

		if (data != null && (data.Flags.HasAnyFlag(SpawnGroupFlags.System) || data.MapId == Id))
			return data;

		return null;
	}

	private void RemoveAllObjectsInRemoveList()
	{
		while (!_objectsToSwitch.Empty())
		{
			var pair = _objectsToSwitch.First();
			var obj = pair.Key;
			var on = pair.Value;
			_objectsToSwitch.Remove(pair.Key);

			if (!obj.IsPermanentWorldObject)
				switch (obj.TypeId)
				{
					case TypeId.Unit:
						SwitchGridContainers(obj.AsCreature, on);

						break;
					default:
						break;
				}
		}

		while (!_objectsToRemove.Empty())
		{
			var obj = _objectsToRemove.First();

			switch (obj.TypeId)
			{
				case TypeId.Corpse:
				{
					var corpse = ObjectAccessor.GetCorpse(obj, obj.GUID);

					if (corpse == null)
						Log.outError(LogFilter.Maps, "Tried to delete corpse/bones {0} that is not in map.", obj.GUID.ToString());
					else
						RemoveFromMap(corpse, true);

					break;
				}
				case TypeId.DynamicObject:
					RemoveFromMap(obj, true);

					break;
				case TypeId.AreaTrigger:
					RemoveFromMap(obj, true);

					break;
				case TypeId.Conversation:
					RemoveFromMap(obj, true);

					break;
				case TypeId.GameObject:
					var go = obj.AsGameObject;
					var transport = go.AsTransport;

					if (transport)
						RemoveFromMap(transport, true);
					else
						RemoveFromMap(go, true);

					break;
				case TypeId.Unit:
					// in case triggered sequence some spell can continue casting after prev CleanupsBeforeDelete call
					// make sure that like sources auras/etc removed before destructor start
					obj. // in case triggered sequence some spell can continue casting after prev CleanupsBeforeDelete call
						// make sure that like sources auras/etc removed before destructor start
						AsCreature.CleanupsBeforeDelete();

					RemoveFromMap(obj.AsCreature, true);

					break;
				default:
					Log.outError(LogFilter.Maps, "Non-grid object (TypeId: {0}) is in grid object remove list, ignored.", obj.TypeId);

					break;
			}

			_objectsToRemove.Remove(obj);
		}
	}

	private void AddToActiveHelper(WorldObject obj)
	{
		_activeNonPlayers.Add(obj);
	}

	private void RemoveFromActiveHelper(WorldObject obj)
	{
		_activeNonPlayers.Remove(obj);
	}

	private void RemoveCorpse(Corpse corpse)
	{
		corpse.UpdateObjectVisibilityOnDestroy();

		if (corpse.Location.GetCurrentCell() != null)
		{
			RemoveFromMap(corpse, false);
		}
		else
		{
			corpse.RemoveFromWorld();
			corpse.ResetMap();
		}

		_corpsesByCell.Remove(corpse.GetCellCoord().GetId(), corpse);

		if (corpse.GetCorpseType() != CorpseType.Bones)
			_corpsesByPlayer.Remove(corpse.OwnerGUID);
		else
			_corpseBones.Remove(corpse);
	}

	private void SendZoneWeather(ZoneDynamicInfo zoneDynamicInfo, Player player)
	{
		var weatherId = zoneDynamicInfo.WeatherId;

		if (weatherId != 0)
		{
			WeatherPkt weather = new(weatherId, zoneDynamicInfo.Intensity);
			player.SendPacket(weather);
		}
		else if (zoneDynamicInfo.DefaultWeather != null)
		{
			zoneDynamicInfo.DefaultWeather.SendWeatherUpdateToPlayer(player);
		}
		else
		{
			Weather.SendFineWeatherUpdateToPlayer(player);
		}
	}

	private bool GetEntrancePos(out uint mapid, out float x, out float y)
	{
		mapid = 0;
		x = 0;
		y = 0;

		if (_mapRecord == null)
			return false;

		return _mapRecord.GetEntrancePos(out mapid, out x, out y);
	}

	private void ResetMarkedCells()
	{
		_markedCells.SetAll(false);
	}

	private bool IsCellMarked(uint pCellId)
	{
		return _markedCells.Get((int)pCellId);
	}

	private void MarkCell(uint pCellId)
	{
		_markedCells.Set((int)pCellId, true);
	}

	private void SetTimer(uint t)
	{
		_gridExpiry = t < MapConst.MinGridDelay ? MapConst.MinGridDelay : t;
	}

	private Grid GetGrid(uint x, uint y)
	{
		if (x > MapConst.MaxGrids || y > MapConst.MaxGrids)
			return null;

		lock (Grids)
		{
			if (Grids.TryGetValue(x, out var ygrid) && ygrid.TryGetValue(y, out var grid))
				return grid;
		}

		return null;
	}

	private bool IsGridObjectDataLoaded(uint x, uint y)
	{
		var grid = GetGrid(x, y);

		if (grid == null)
			return false;

		return grid.IsGridObjectDataLoaded();
	}

	private void SetGridObjectDataLoaded(bool pLoaded, uint x, uint y)
	{
		var grid = GetGrid(x, y);

		grid?.SetGridObjectDataLoaded(pLoaded);
	}

	private ObjectGuidGenerator GetGuidSequenceGenerator(HighGuid high)
	{
		if (!_guidGenerators.ContainsKey(high))
			_guidGenerators[high] = new ObjectGuidGenerator(high);

		return _guidGenerators[high];
	}

	#region Script Updates

	//MapScript
	public static void OnCreateMap(Map map)
	{
		var record = map.Entry;

		if (record != null && record.IsWorldMap())
			Global.ScriptMgr.ForEach<IMapOnCreate<Map>>(p => p.OnCreate(map));

		if (record != null && record.IsDungeon())
			Global.ScriptMgr.ForEach<IMapOnCreate<InstanceMap>>(p => p.OnCreate(map.ToInstanceMap));

		if (record != null && record.IsBattleground())
			Global.ScriptMgr.ForEach<IMapOnCreate<BattlegroundMap>>(p => p.OnCreate(map.ToBattlegroundMap));
	}

	public static void OnDestroyMap(Map map)
	{
		var record = map.Entry;

		if (record != null && record.IsWorldMap())
			Global.ScriptMgr.ForEach<IMapOnDestroy<Map>>(p => p.OnDestroy(map));

		if (record != null && record.IsDungeon())
			Global.ScriptMgr.ForEach<IMapOnDestroy<InstanceMap>>(p => p.OnDestroy(map.ToInstanceMap));

		if (record != null && record.IsBattleground())
			Global.ScriptMgr.ForEach<IMapOnDestroy<BattlegroundMap>>(p => p.OnDestroy(map.ToBattlegroundMap));
	}

	public static void OnPlayerEnterMap(Map map, Player player)
	{
		Global.ScriptMgr.ForEach<IPlayerOnMapChanged>(p => p.OnMapChanged(player));

		var record = map.Entry;

		if (record != null && record.IsWorldMap())
			Global.ScriptMgr.ForEach<IMapOnPlayerEnter<Map>>(p => p.OnPlayerEnter(map, player));

		if (record != null && record.IsDungeon())
			Global.ScriptMgr.ForEach<IMapOnPlayerEnter<InstanceMap>>(p => p.OnPlayerEnter(map.ToInstanceMap, player));

		if (record != null && record.IsBattleground())
			Global.ScriptMgr.ForEach<IMapOnPlayerEnter<BattlegroundMap>>(p => p.OnPlayerEnter(map.ToBattlegroundMap, player));
	}

	public static void OnPlayerLeaveMap(Map map, Player player)
	{
		var record = map.Entry;

		if (record != null && record.IsWorldMap())
			Global.ScriptMgr.ForEach<IMapOnPlayerLeave<Map>>(p => p.OnPlayerLeave(map, player));

		if (record != null && record.IsDungeon())
			Global.ScriptMgr.ForEach<IMapOnPlayerLeave<InstanceMap>>(p => p.OnPlayerLeave(map.ToInstanceMap, player));

		if (record != null && record.IsBattleground())
			Global.ScriptMgr.ForEach<IMapOnPlayerLeave<BattlegroundMap>>(p => p.OnPlayerLeave(map.ToBattlegroundMap, player));
	}

	public static void OnMapUpdate(Map map, uint diff)
	{
		var record = map.Entry;

		if (record != null && record.IsWorldMap())
			Global.ScriptMgr.ForEach<IMapOnUpdate<Map>>(p => p.OnUpdate(map, diff));

		if (record != null && record.IsDungeon())
			Global.ScriptMgr.ForEach<IMapOnUpdate<InstanceMap>>(p => p.OnUpdate(map.ToInstanceMap, diff));

		if (record != null && record.IsBattleground())
			Global.ScriptMgr.ForEach<IMapOnUpdate<BattlegroundMap>>(p => p.OnUpdate(map.ToBattlegroundMap, diff));
	}

	#endregion

	#region Scripts

	// Put scripts in the execution queue
	public void ScriptsStart(ScriptsType scriptsType, uint id, WorldObject source, WorldObject target)
	{
		var scripts = Global.ObjectMgr.GetScriptsMapByType(scriptsType);

		// Find the script map
		var list = scripts.LookupByKey(id);

		if (list == null)
			return;

		// prepare static data
		var sourceGUID = source != null ? source.GUID : ObjectGuid.Empty; //some script commands doesn't have source
		var targetGUID = target != null ? target.GUID : ObjectGuid.Empty;
		var ownerGUID = (source != null && source.IsTypeMask(TypeMask.Item)) ? ((Item)source).OwnerGUID : ObjectGuid.Empty;

		// Schedule script execution for all scripts in the script map
		var immedScript = false;

		foreach (var script in list.KeyValueList)
		{
			ScriptAction sa;
			sa.SourceGUID = sourceGUID;
			sa.TargetGUID = targetGUID;
			sa.OwnerGUID = ownerGUID;

			sa.Script = script.Value;
			_scriptSchedule.Add(GameTime.GetGameTime() + script.Key, sa);

			if (script.Key == 0)
				immedScript = true;

			Global.MapMgr.IncreaseScheduledScriptsCount();
		}

		// If one of the effects should be immediate, launch the script execution
		if (immedScript)
			lock (_scriptLock)
			{
				ScriptsProcess();
			}
	}

	public void ScriptCommandStart(ScriptInfo script, uint delay, WorldObject source, WorldObject target)
	{
		// NOTE: script record _must_ exist until command executed

		// prepare static data
		var sourceGUID = source != null ? source.GUID : ObjectGuid.Empty;
		var targetGUID = target != null ? target.GUID : ObjectGuid.Empty;
		var ownerGUID = (source != null && source.IsTypeMask(TypeMask.Item)) ? ((Item)source).OwnerGUID : ObjectGuid.Empty;

		var sa = new ScriptAction();
		sa.SourceGUID = sourceGUID;
		sa.TargetGUID = targetGUID;
		sa.OwnerGUID = ownerGUID;

		sa.Script = script;
		_scriptSchedule.Add(GameTime.GetGameTime() + delay, sa);

		Global.MapMgr.IncreaseScheduledScriptsCount();

		// If effects should be immediate, launch the script execution
		if (delay == 0)
			lock (_scriptLock)
			{
				ScriptsProcess();
			}
	}

	// Helpers for ScriptProcess method.
	private Player GetScriptPlayerSourceOrTarget(WorldObject source, WorldObject target, ScriptInfo scriptInfo)
	{
		Player player = null;

		if (source == null && target == null)
		{
			Log.outError(LogFilter.Scripts, "{0} source and target objects are NULL.", scriptInfo.GetDebugInfo());
		}
		else
		{
			// Check target first, then source.
			if (target != null)
				player = target.AsPlayer;

			if (player == null && source != null)
				player = source.AsPlayer;

			if (player == null)
				Log.outError(LogFilter.Scripts,
							"{0} neither source nor target object is player (source: TypeId: {1}, Entry: {2}, {3}; target: TypeId: {4}, Entry: {5}, {6}), skipping.",
							scriptInfo.GetDebugInfo(),
							source ? source.TypeId : 0,
							source ? source.Entry : 0,
							source ? source.GUID.ToString() : "",
							target ? target.TypeId : 0,
							target ? target.Entry : 0,
							target ? target.GUID.ToString() : "");
		}

		return player;
	}

	private Creature GetScriptCreatureSourceOrTarget(WorldObject source, WorldObject target, ScriptInfo scriptInfo, bool bReverse = false)
	{
		Creature creature = null;

		if (source == null && target == null)
		{
			Log.outError(LogFilter.Scripts, "{0} source and target objects are NULL.", scriptInfo.GetDebugInfo());
		}
		else
		{
			if (bReverse)
			{
				// Check target first, then source.
				if (target != null)
					creature = target.AsCreature;

				if (creature == null && source != null)
					creature = source.AsCreature;
			}
			else
			{
				// Check source first, then target.
				if (source != null)
					creature = source.AsCreature;

				if (creature == null && target != null)
					creature = target.AsCreature;
			}

			if (creature == null)
				Log.outError(LogFilter.Scripts,
							"{0} neither source nor target are creatures (source: TypeId: {1}, Entry: {2}, {3}; target: TypeId: {4}, Entry: {5}, {6}), skipping.",
							scriptInfo.GetDebugInfo(),
							source ? source.TypeId : 0,
							source ? source.Entry : 0,
							source ? source.GUID.ToString() : "",
							target ? target.TypeId : 0,
							target ? target.Entry : 0,
							target ? target.GUID.ToString() : "");
		}

		return creature;
	}

	private GameObject GetScriptGameObjectSourceOrTarget(WorldObject source, WorldObject target, ScriptInfo scriptInfo, bool bReverse)
	{
		GameObject gameobject = null;

		if (source == null && target == null)
		{
			Log.outError(LogFilter.MapsScript, $"{scriptInfo.GetDebugInfo()} source and target objects are NULL.");
		}
		else
		{
			if (bReverse)
			{
				// Check target first, then source.
				if (target != null)
					gameobject = target.AsGameObject;

				if (gameobject == null && source != null)
					gameobject = source.AsGameObject;
			}
			else
			{
				// Check source first, then target.
				if (source != null)
					gameobject = source.AsGameObject;

				if (gameobject == null && target != null)
					gameobject = target.AsGameObject;
			}

			if (gameobject == null)
				Log.outError(LogFilter.MapsScript,
							$"{scriptInfo.GetDebugInfo()} neither source nor target are gameobjects " +
							$"(source: TypeId: {(source != null ? source.TypeId : 0)}, Entry: {(source != null ? source.Entry : 0)}, {(source != null ? source.GUID : ObjectGuid.Empty)}; " +
							$"target: TypeId: {(target != null ? target.TypeId : 0)}, Entry: {(target != null ? target.Entry : 0)}, {(target != null ? target.GUID : ObjectGuid.Empty)}), skipping.");
		}

		return gameobject;
	}

	private Unit GetScriptUnit(WorldObject obj, bool isSource, ScriptInfo scriptInfo)
	{
		Unit unit = null;

		if (obj == null)
		{
			Log.outError(LogFilter.Scripts,
						"{0} {1} object is NULL.",
						scriptInfo.GetDebugInfo(),
						isSource ? "source" : "target");
		}
		else if (!obj.IsTypeMask(TypeMask.Unit))
		{
			Log.outError(LogFilter.Scripts,
						"{0} {1} object is not unit (TypeId: {2}, Entry: {3}, GUID: {4}), skipping.",
						scriptInfo.GetDebugInfo(),
						isSource ? "source" : "target",
						obj.TypeId,
						obj.Entry,
						obj.GUID.ToString());
		}
		else
		{
			unit = obj.AsUnit;

			if (unit == null)
				Log.outError(LogFilter.Scripts, "{0} {1} object could not be casted to unit.", scriptInfo.GetDebugInfo(), isSource ? "source" : "target");
		}

		return unit;
	}

	private Player GetScriptPlayer(WorldObject obj, bool isSource, ScriptInfo scriptInfo)
	{
		Player player = null;

		if (obj == null)
		{
			Log.outError(LogFilter.Scripts,
						"{0} {1} object is NULL.",
						scriptInfo.GetDebugInfo(),
						isSource ? "source" : "target");
		}
		else
		{
			player = obj.AsPlayer;

			if (player == null)
				Log.outError(LogFilter.Scripts,
							"{0} {1} object is not a player (TypeId: {2}, Entry: {3}, GUID: {4}).",
							scriptInfo.GetDebugInfo(),
							isSource ? "source" : "target",
							obj.TypeId,
							obj.Entry,
							obj.GUID.ToString());
		}

		return player;
	}

	private Creature GetScriptCreature(WorldObject obj, bool isSource, ScriptInfo scriptInfo)
	{
		Creature creature = null;

		if (obj == null)
		{
			Log.outError(LogFilter.Scripts, "{0} {1} object is NULL.", scriptInfo.GetDebugInfo(), isSource ? "source" : "target");
		}
		else
		{
			creature = obj.AsCreature;

			if (creature == null)
				Log.outError(LogFilter.Scripts,
							"{0} {1} object is not a creature (TypeId: {2}, Entry: {3}, GUID: {4}).",
							scriptInfo.GetDebugInfo(),
							isSource ? "source" : "target",
							obj.TypeId,
							obj.Entry,
							obj.GUID.ToString());
		}

		return creature;
	}

	private WorldObject GetScriptWorldObject(WorldObject obj, bool isSource, ScriptInfo scriptInfo)
	{
		WorldObject pWorldObject = null;

		if (obj == null)
		{
			Log.outError(LogFilter.Scripts, "{0} {1} object is NULL.", scriptInfo.GetDebugInfo(), isSource ? "source" : "target");
		}
		else
		{
			pWorldObject = obj;

			if (pWorldObject == null)
				Log.outError(LogFilter.Scripts,
							"{0} {1} object is not a world object (TypeId: {2}, Entry: {3}, GUID: {4}).",
							scriptInfo.GetDebugInfo(),
							isSource ? "source" : "target",
							obj.TypeId,
							obj.Entry,
							obj.GUID.ToString());
		}

		return pWorldObject;
	}

	private void ScriptProcessDoor(WorldObject source, WorldObject target, ScriptInfo scriptInfo)
	{
		var bOpen = false;
		ulong guid = scriptInfo.ToggleDoor.GOGuid;
		var nTimeToToggle = Math.Max(15, (int)scriptInfo.ToggleDoor.ResetDelay);

		switch (scriptInfo.command)
		{
			case ScriptCommands.OpenDoor:
				bOpen = true;

				break;
			case ScriptCommands.CloseDoor:
				break;
			default:
				Log.outError(LogFilter.Scripts, "{0} unknown command for _ScriptProcessDoor.", scriptInfo.GetDebugInfo());

				return;
		}

		if (guid == 0)
		{
			Log.outError(LogFilter.Scripts, "{0} door guid is not specified.", scriptInfo.GetDebugInfo());
		}
		else if (source == null)
		{
			Log.outError(LogFilter.Scripts, "{0} source object is NULL.", scriptInfo.GetDebugInfo());
		}
		else if (!source.IsTypeMask(TypeMask.Unit))
		{
			Log.outError(LogFilter.Scripts,
						"{0} source object is not unit (TypeId: {1}, Entry: {2}, GUID: {3}), skipping.",
						scriptInfo.GetDebugInfo(),
						source.TypeId,
						source.Entry,
						source.GUID.ToString());
		}
		else
		{
			if (source == null)
			{
				Log.outError(LogFilter.Scripts,
							"{0} source object could not be casted to world object (TypeId: {1}, Entry: {2}, GUID: {3}), skipping.",
							scriptInfo.GetDebugInfo(),
							source.TypeId,
							source.Entry,
							source.GUID.ToString());
			}
			else
			{
				var pDoor = FindGameObject(source, guid);

				if (pDoor == null)
				{
					Log.outError(LogFilter.Scripts, "{0} gameobject was not found (guid: {1}).", scriptInfo.GetDebugInfo(), guid);
				}
				else if (pDoor.GoType != GameObjectTypes.Door)
				{
					Log.outError(LogFilter.Scripts, "{0} gameobject is not a door (GoType: {1}, Entry: {2}, GUID: {3}).", scriptInfo.GetDebugInfo(), pDoor.GoType, pDoor.Entry, pDoor.GUID.ToString());
				}
				else if (bOpen == (pDoor.GoState == GameObjectState.Ready))
				{
					pDoor.UseDoorOrButton((uint)nTimeToToggle);

					if (target != null && target.IsTypeMask(TypeMask.GameObject))
					{
						var goTarget = target.AsGameObject;

						if (goTarget != null && goTarget.GoType == GameObjectTypes.Button)
							goTarget.UseDoorOrButton((uint)nTimeToToggle);
					}
				}
			}
		}
	}

	private GameObject FindGameObject(WorldObject searchObject, ulong guid)
	{
		var bounds = searchObject.Map.GameObjectBySpawnIdStore.LookupByKey(guid);

		if (bounds.Empty())
			return null;

		return bounds[0];
	}

	// Process queued scripts
	private void ScriptsProcess()
	{
		if (_scriptSchedule.Empty())
			return;

		// Process overdue queued scripts
		var iter = _scriptSchedule.FirstOrDefault();

		while (!_scriptSchedule.Empty())
		{
			if (iter.Key > GameTime.GetGameTime())
				break; // we are a sorted dictionary, once we hit this value we can break all other are going to be greater.

			if (iter.Value == default && iter.Key == default)
				break; // we have a default on get first or defalt. stack is empty

			foreach (var step in iter.Value)
			{
				WorldObject source = null;

				if (!step.SourceGUID.IsEmpty)
					switch (step.SourceGUID.High)
					{
						case HighGuid.Item: // as well as HIGHGUID_CONTAINER
							var player = GetPlayer(step.OwnerGUID);

							if (player != null)
								source = player.GetItemByGuid(step.SourceGUID);

							break;
						case HighGuid.Creature:
						case HighGuid.Vehicle:
							source = GetCreature(step.SourceGUID);

							break;
						case HighGuid.Pet:
							source = GetPet(step.SourceGUID);

							break;
						case HighGuid.Player:
							source = GetPlayer(step.SourceGUID);

							break;
						case HighGuid.GameObject:
						case HighGuid.Transport:
							source = GetGameObject(step.SourceGUID);

							break;
						case HighGuid.Corpse:
							source = GetCorpse(step.SourceGUID);

							break;
						default:
							Log.outError(LogFilter.Scripts,
										"{0} source with unsupported high guid (GUID: {1}, high guid: {2}).",
										step.Script.GetDebugInfo(),
										step.SourceGUID,
										step.SourceGUID.ToString());

							break;
					}

				WorldObject target = null;

				if (!step.TargetGUID.IsEmpty)
					switch (step.TargetGUID.High)
					{
						case HighGuid.Creature:
						case HighGuid.Vehicle:
							target = GetCreature(step.TargetGUID);

							break;
						case HighGuid.Pet:
							target = GetPet(step.TargetGUID);

							break;
						case HighGuid.Player:
							target = GetPlayer(step.TargetGUID);

							break;
						case HighGuid.GameObject:
						case HighGuid.Transport:
							target = GetGameObject(step.TargetGUID);

							break;
						case HighGuid.Corpse:
							target = GetCorpse(step.TargetGUID);

							break;
						default:
							Log.outError(LogFilter.Scripts, "{0} target with unsupported high guid {1}.", step.Script.GetDebugInfo(), step.TargetGUID.ToString());

							break;
					}

				switch (step.Script.command)
				{
					case ScriptCommands.Talk:
					{
						if (step.Script.Talk.ChatType > ChatMsg.Whisper && step.Script.Talk.ChatType != ChatMsg.RaidBossWhisper)
						{
							Log.outError(LogFilter.Scripts,
										"{0} invalid chat type ({1}) specified, skipping.",
										step.Script.GetDebugInfo(),
										step.Script.Talk.ChatType);

							break;
						}

						if (step.Script.Talk.Flags.HasAnyFlag(eScriptFlags.TalkUsePlayer))
							source = GetScriptPlayerSourceOrTarget(source, target, step.Script);
						else
							source = GetScriptCreatureSourceOrTarget(source, target, step.Script);

						if (source)
						{
							var sourceUnit = source.AsUnit;

							if (!sourceUnit)
							{
								Log.outError(LogFilter.Scripts, "{0} source object ({1}) is not an unit, skipping.", step.Script.GetDebugInfo(), source.GUID.ToString());

								break;
							}

							switch (step.Script.Talk.ChatType)
							{
								case ChatMsg.Say:
									sourceUnit.Say((uint)step.Script.Talk.TextID, target);

									break;
								case ChatMsg.Yell:
									sourceUnit.Yell((uint)step.Script.Talk.TextID, target);

									break;
								case ChatMsg.Emote:
								case ChatMsg.RaidBossEmote:
									sourceUnit.TextEmote((uint)step.Script.Talk.TextID, target, step.Script.Talk.ChatType == ChatMsg.RaidBossEmote);

									break;
								case ChatMsg.Whisper:
								case ChatMsg.RaidBossWhisper:
								{
									var receiver = target ? target.AsPlayer : null;

									if (!receiver)
										Log.outError(LogFilter.Scripts, "{0} attempt to whisper to non-player unit, skipping.", step.Script.GetDebugInfo());
									else
										sourceUnit.Whisper((uint)step.Script.Talk.TextID, receiver, step.Script.Talk.ChatType == ChatMsg.RaidBossWhisper);

									break;
								}
								default:
									break; // must be already checked at load
							}
						}

						break;
					}
					case ScriptCommands.Emote:
					{
						// Source or target must be Creature.
						var cSource = GetScriptCreatureSourceOrTarget(source, target, step.Script);

						if (cSource)
						{
							if (step.Script.Emote.Flags.HasAnyFlag(eScriptFlags.EmoteUseState))
								cSource.EmoteState = (Emote)step.Script.Emote.EmoteID;
							else
								cSource.HandleEmoteCommand((Emote)step.Script.Emote.EmoteID);
						}

						break;
					}
					case ScriptCommands.MoveTo:
					{
						// Source or target must be Creature.
						var cSource = GetScriptCreatureSourceOrTarget(source, target, step.Script);

						if (cSource)
						{
							var unit = cSource.AsUnit;

							if (step.Script.MoveTo.TravelTime != 0)
							{
								var speed =
									unit.GetDistance(step.Script.MoveTo.DestX,
													step.Script.MoveTo.DestY,
													step.Script.MoveTo.DestZ) /
									(step.Script.MoveTo.TravelTime * 0.001f);

								unit.MonsterMoveWithSpeed(step.Script.MoveTo.DestX,
														step.Script.MoveTo.DestY,
														step.Script.MoveTo.DestZ,
														speed);
							}
							else
							{
								unit.NearTeleportTo(step.Script.MoveTo.DestX,
													step.Script.MoveTo.DestY,
													step.Script.MoveTo.DestZ,
													unit.Location.Orientation);
							}
						}

						break;
					}
					case ScriptCommands.TeleportTo:
					{
						if (step.Script.TeleportTo.Flags.HasAnyFlag(eScriptFlags.TeleportUseCreature))
						{
							// Source or target must be Creature.
							var cSource = GetScriptCreatureSourceOrTarget(source, target, step.Script);

							if (cSource)
								cSource.NearTeleportTo(step.Script.TeleportTo.DestX,
														step.Script.TeleportTo.DestY,
														step.Script.TeleportTo.DestZ,
														step.Script.TeleportTo.Orientation);
						}
						else
						{
							// Source or target must be Player.
							var player = GetScriptPlayerSourceOrTarget(source, target, step.Script);

							if (player)
								player.TeleportTo(step.Script.TeleportTo.MapID,
												step.Script.TeleportTo.DestX,
												step.Script.TeleportTo.DestY,
												step.Script.TeleportTo.DestZ,
												step.Script.TeleportTo.Orientation);
						}

						break;
					}
					case ScriptCommands.QuestExplored:
					{
						if (!source)
						{
							Log.outError(LogFilter.Scripts, "{0} source object is NULL.", step.Script.GetDebugInfo());

							break;
						}

						if (!target)
						{
							Log.outError(LogFilter.Scripts, "{0} target object is NULL.", step.Script.GetDebugInfo());

							break;
						}

						// when script called for item spell casting then target == (unit or GO) and source is player
						WorldObject worldObject;
						var player = target.AsPlayer;

						if (player != null)
						{
							if (!source.IsTypeId(TypeId.Unit) && !source.IsTypeId(TypeId.GameObject) && !source.IsTypeId(TypeId.Player))
							{
								Log.outError(LogFilter.Scripts,
											"{0} source is not unit, gameobject or player (TypeId: {1}, Entry: {2}, GUID: {3}), skipping.",
											step.Script.GetDebugInfo(),
											source.TypeId,
											source.Entry,
											source.GUID.ToString());

								break;
							}

							worldObject = source;
						}
						else
						{
							player = source.AsPlayer;

							if (player != null)
							{
								if (!target.IsTypeId(TypeId.Unit) && !target.IsTypeId(TypeId.GameObject) && !target.IsTypeId(TypeId.Player))
								{
									Log.outError(LogFilter.Scripts,
												"{0} target is not unit, gameobject or player (TypeId: {1}, Entry: {2}, GUID: {3}), skipping.",
												step.Script.GetDebugInfo(),
												target.TypeId,
												target.Entry,
												target.GUID.ToString());

									break;
								}

								worldObject = target;
							}
							else
							{
								Log.outError(LogFilter.Scripts,
											"{0} neither source nor target is player (Entry: {0}, GUID: {1}; target: Entry: {2}, GUID: {3}), skipping.",
											step.Script.GetDebugInfo(),
											source.Entry,
											source.GUID.ToString(),
											target.Entry,
											target.GUID.ToString());

								break;
							}
						}

						// quest id and flags checked at script loading
						if ((!worldObject.IsTypeId(TypeId.Unit) || worldObject.AsUnit.IsAlive) &&
							(step.Script.QuestExplored.Distance == 0 ||
							worldObject.IsWithinDistInMap(player, step.Script.QuestExplored.Distance)))
							player.AreaExploredOrEventHappens(step.Script.QuestExplored.QuestID);
						else
							player.FailQuest(step.Script.QuestExplored.QuestID);

						break;
					}

					case ScriptCommands.KillCredit:
					{
						// Source or target must be Player.
						var player = GetScriptPlayerSourceOrTarget(source, target, step.Script);

						if (player)
						{
							if (step.Script.KillCredit.Flags.HasAnyFlag(eScriptFlags.KillcreditRewardGroup))
								player.RewardPlayerAndGroupAtEvent(step.Script.KillCredit.CreatureEntry, player);
							else
								player.KilledMonsterCredit(step.Script.KillCredit.CreatureEntry, ObjectGuid.Empty);
						}

						break;
					}
					case ScriptCommands.RespawnGameobject:
					{
						if (step.Script.RespawnGameObject.GOGuid == 0)
						{
							Log.outError(LogFilter.Scripts, "{0} gameobject guid (datalong) is not specified.", step.Script.GetDebugInfo());

							break;
						}

						// Source or target must be WorldObject.
						var pSummoner = GetScriptWorldObject(source, true, step.Script);

						if (pSummoner)
						{
							var pGO = FindGameObject(pSummoner, step.Script.RespawnGameObject.GOGuid);

							if (pGO == null)
							{
								Log.outError(LogFilter.Scripts, "{0} gameobject was not found (guid: {1}).", step.Script.GetDebugInfo(), step.Script.RespawnGameObject.GOGuid);

								break;
							}

							if (pGO.GoType == GameObjectTypes.FishingNode ||
								pGO.GoType == GameObjectTypes.Door ||
								pGO.GoType == GameObjectTypes.Button ||
								pGO.GoType == GameObjectTypes.Trap)
							{
								Log.outError(LogFilter.Scripts,
											"{0} can not be used with gameobject of type {1} (guid: {2}).",
											step.Script.GetDebugInfo(),
											pGO.GoType,
											step.Script.RespawnGameObject.GOGuid);

								break;
							}

							// Check that GO is not spawned
							if (!pGO.IsSpawned)
							{
								var nTimeToDespawn = Math.Max(5, (int)step.Script.RespawnGameObject.DespawnDelay);
								pGO.SetLootState(LootState.Ready);
								pGO.SetRespawnTime(nTimeToDespawn);

								pGO.Map.AddToMap(pGO);
							}
						}

						break;
					}
					case ScriptCommands.TempSummonCreature:
					{
						// Source must be WorldObject.
						var pSummoner = GetScriptWorldObject(source, true, step.Script);

						if (pSummoner)
						{
							if (step.Script.TempSummonCreature.CreatureEntry == 0)
							{
								Log.outError(LogFilter.Scripts, "{0} creature entry (datalong) is not specified.", step.Script.GetDebugInfo());
							}
							else
							{
								var x = step.Script.TempSummonCreature.PosX;
								var y = step.Script.TempSummonCreature.PosY;
								var z = step.Script.TempSummonCreature.PosZ;
								var o = step.Script.TempSummonCreature.Orientation;

								if (pSummoner.SummonCreature(step.Script.TempSummonCreature.CreatureEntry, new Position(x, y, z, o), TempSummonType.TimedOrDeadDespawn, TimeSpan.FromMilliseconds(step.Script.TempSummonCreature.DespawnDelay)) == null)
									Log.outError(LogFilter.Scripts, "{0} creature was not spawned (entry: {1}).", step.Script.GetDebugInfo(), step.Script.TempSummonCreature.CreatureEntry);
							}
						}

						break;
					}

					case ScriptCommands.OpenDoor:
					case ScriptCommands.CloseDoor:
						ScriptProcessDoor(source, target, step.Script);

						break;
					case ScriptCommands.ActivateObject:
					{
						// Source must be Unit.
						var unit = GetScriptUnit(source, true, step.Script);

						if (unit)
						{
							// Target must be GameObject.
							if (target == null)
							{
								Log.outError(LogFilter.Scripts, "{0} target object is NULL.", step.Script.GetDebugInfo());

								break;
							}

							if (!target.IsTypeId(TypeId.GameObject))
							{
								Log.outError(LogFilter.Scripts,
											"{0} target object is not gameobject (TypeId: {1}, Entry: {2}, GUID: {3}), skipping.",
											step.Script.GetDebugInfo(),
											target.TypeId,
											target.Entry,
											target.GUID.ToString());

								break;
							}

							var pGO = target.AsGameObject;

							if (pGO)
								pGO.Use(unit);
						}

						break;
					}
					case ScriptCommands.RemoveAura:
					{
						// Source (datalong2 != 0) or target (datalong2 == 0) must be Unit.
						var bReverse = step.Script.RemoveAura.Flags.HasAnyFlag(eScriptFlags.RemoveauraReverse);
						var unit = GetScriptUnit(bReverse ? source : target, bReverse, step.Script);

						if (unit)
							unit.RemoveAura(step.Script.RemoveAura.SpellID);

						break;
					}
					case ScriptCommands.CastSpell:
					{
						if (source == null && target == null)
						{
							Log.outError(LogFilter.Scripts, "{0} source and target objects are NULL.", step.Script.GetDebugInfo());

							break;
						}

						WorldObject uSource = null;
						WorldObject uTarget = null;

						// source/target cast spell at target/source (script.datalong2: 0: s.t 1: s.s 2: t.t 3: t.s
						switch (step.Script.CastSpell.Flags)
						{
							case eScriptFlags.CastspellSourceToTarget: // source . target
								uSource = source;
								uTarget = target;

								break;
							case eScriptFlags.CastspellSourceToSource: // source . source
								uSource = source;
								uTarget = uSource;

								break;
							case eScriptFlags.CastspellTargetToTarget: // target . target
								uSource = target;
								uTarget = uSource;

								break;
							case eScriptFlags.CastspellTargetToSource: // target . source
								uSource = target;
								uTarget = source;

								break;
							case eScriptFlags.CastspellSearchCreature: // source . creature with entry
								uSource = source;
								uTarget = uSource?.FindNearestCreature((uint)Math.Abs(step.Script.CastSpell.CreatureEntry), step.Script.CastSpell.SearchRadius);

								break;
						}

						if (uSource == null)
						{
							Log.outError(LogFilter.Scripts, "{0} no source worldobject found for spell {1}", step.Script.GetDebugInfo(), step.Script.CastSpell.SpellID);

							break;
						}

						if (uTarget == null)
						{
							Log.outError(LogFilter.Scripts, "{0} no target worldobject found for spell {1}", step.Script.GetDebugInfo(), step.Script.CastSpell.SpellID);

							break;
						}

						var triggered = ((int)step.Script.CastSpell.Flags != 4)
											? step.Script.CastSpell.CreatureEntry.HasAnyFlag((int)eScriptFlags.CastspellTriggered)
											: step.Script.CastSpell.CreatureEntry < 0;

						uSource.CastSpell(uTarget, step.Script.CastSpell.SpellID, triggered);

						break;
					}

					case ScriptCommands.PlaySound:
						// Source must be WorldObject.
						var obj = GetScriptWorldObject(source, true, step.Script);

						if (obj)
						{
							// PlaySound.Flags bitmask: 0/1=anyone/target
							Player player2 = null;

							if (step.Script.PlaySound.Flags.HasAnyFlag(eScriptFlags.PlaysoundTargetPlayer))
							{
								// Target must be Player.
								player2 = GetScriptPlayer(target, false, step.Script);

								if (target == null)
									break;
							}

							// PlaySound.Flags bitmask: 0/2=without/with distance dependent
							if (step.Script.PlaySound.Flags.HasAnyFlag(eScriptFlags.PlaysoundDistanceSound))
								obj.PlayDistanceSound(step.Script.PlaySound.SoundID, player2);
							else
								obj.PlayDirectSound(step.Script.PlaySound.SoundID, player2);
						}

						break;

					case ScriptCommands.CreateItem:
						// Target or source must be Player.
						var pReceiver = GetScriptPlayerSourceOrTarget(source, target, step.Script);

						if (pReceiver)
						{
							var dest = new List<ItemPosCount>();
							var msg = pReceiver.CanStoreNewItem(ItemConst.NullBag, ItemConst.NullSlot, dest, step.Script.CreateItem.ItemEntry, step.Script.CreateItem.Amount);

							if (msg == InventoryResult.Ok)
							{
								var item = pReceiver.StoreNewItem(dest, step.Script.CreateItem.ItemEntry, true);

								if (item != null)
									pReceiver.SendNewItem(item, step.Script.CreateItem.Amount, false, true);
							}
							else
							{
								pReceiver.SendEquipError(msg, null, null, step.Script.CreateItem.ItemEntry);
							}
						}

						break;

					case ScriptCommands.DespawnSelf:
					{
						// First try with target or source creature, then with target or source gameobject
						var cSource = GetScriptCreatureSourceOrTarget(source, target, step.Script, true);

						if (cSource != null)
						{
							cSource.DespawnOrUnsummon(TimeSpan.FromMilliseconds(step.Script.DespawnSelf.DespawnDelay));
						}
						else
						{
							var goSource = GetScriptGameObjectSourceOrTarget(source, target, step.Script, true);

							goSource?.DespawnOrUnsummon(TimeSpan.FromMilliseconds(step.Script.DespawnSelf.DespawnDelay));
						}

						break;
					}
					case ScriptCommands.LoadPath:
					{
						// Source must be Unit.
						var unit = GetScriptUnit(source, true, step.Script);

						if (unit)
						{
							if (Global.WaypointMgr.GetPath(step.Script.LoadPath.PathID) == null)
								Log.outError(LogFilter.Scripts, "{0} source object has an invalid path ({1}), skipping.", step.Script.GetDebugInfo(), step.Script.LoadPath.PathID);
							else
								unit.MotionMaster.MovePath(step.Script.LoadPath.PathID, step.Script.LoadPath.IsRepeatable != 0);
						}

						break;
					}
					case ScriptCommands.CallscriptToUnit:
					{
						if (step.Script.CallScript.CreatureEntry == 0)
						{
							Log.outError(LogFilter.Scripts, "{0} creature entry is not specified, skipping.", step.Script.GetDebugInfo());

							break;
						}

						if (step.Script.CallScript.ScriptID == 0)
						{
							Log.outError(LogFilter.Scripts, "{0} script id is not specified, skipping.", step.Script.GetDebugInfo());

							break;
						}

						Creature cTarget = null;
						var creatureBounds = _creatureBySpawnIdStore.LookupByKey(step.Script.CallScript.CreatureEntry);

						if (!creatureBounds.Empty())
						{
							// Prefer alive (last respawned) creature
							var foundCreature = creatureBounds.Find(creature => creature.IsAlive);

							cTarget = foundCreature ?? creatureBounds[0];
						}

						if (cTarget == null)
						{
							Log.outError(LogFilter.Scripts, "{0} target was not found (entry: {1})", step.Script.GetDebugInfo(), step.Script.CallScript.CreatureEntry);

							break;
						}

						// Insert script into schedule but do not start it
						ScriptsStart((ScriptsType)step.Script.CallScript.ScriptType, step.Script.CallScript.ScriptID, cTarget, null);

						break;
					}

					case ScriptCommands.Kill:
					{
						// Source or target must be Creature.
						var cSource = GetScriptCreatureSourceOrTarget(source, target, step.Script);

						if (cSource)
						{
							if (cSource.IsDead)
							{
								Log.outError(LogFilter.Scripts, "{0} creature is already dead (Entry: {1}, GUID: {2})", step.Script.GetDebugInfo(), cSource.Entry, cSource.GUID.ToString());
							}
							else
							{
								cSource.SetDeathState(DeathState.JustDied);

								if (step.Script.Kill.RemoveCorpse == 1)
									cSource.RemoveCorpse();
							}
						}

						break;
					}
					case ScriptCommands.Orientation:
					{
						// Source must be Unit.
						var sourceUnit = GetScriptUnit(source, true, step.Script);

						if (sourceUnit)
						{
							if (step.Script.Orientation.Flags.HasAnyFlag(eScriptFlags.OrientationFaceTarget))
							{
								// Target must be Unit.
								var targetUnit = GetScriptUnit(target, false, step.Script);

								if (targetUnit == null)
									break;

								sourceUnit.SetFacingToObject(targetUnit);
							}
							else
							{
								sourceUnit.SetFacingTo(step.Script.Orientation._Orientation);
							}
						}

						break;
					}
					case ScriptCommands.Equip:
					{
						// Source must be Creature.
						var cSource = GetScriptCreature(source, target, step.Script);

						if (cSource)
							cSource.LoadEquipment((int)step.Script.Equip.EquipmentID);

						break;
					}
					case ScriptCommands.Model:
					{
						// Source must be Creature.
						var cSource = GetScriptCreature(source, target, step.Script);

						if (cSource)
							cSource.SetDisplayId(step.Script.Model.ModelID);

						break;
					}
					case ScriptCommands.CloseGossip:
					{
						// Source must be Player.
						var player = GetScriptPlayer(source, true, step.Script);

						player?.PlayerTalkClass.SendCloseGossip();

						break;
					}
					case ScriptCommands.Playmovie:
					{
						// Source must be Player.
						var player = GetScriptPlayer(source, true, step.Script);

						if (player)
							player.SendMovieStart(step.Script.PlayMovie.MovieID);

						break;
					}
					case ScriptCommands.Movement:
					{
						// Source must be Creature.
						var cSource = GetScriptCreature(source, true, step.Script);

						if (cSource)
						{
							if (!cSource.IsAlive)
								return;

							cSource.MotionMaster.MoveIdle();

							switch ((MovementGeneratorType)step.Script.Movement.MovementType)
							{
								case MovementGeneratorType.Random:
									cSource.MotionMaster.MoveRandom(step.Script.Movement.MovementDistance);

									break;
								case MovementGeneratorType.Waypoint:
									cSource.MotionMaster.MovePath((uint)step.Script.Movement.Path, false);

									break;
							}
						}

						break;
					}
					case ScriptCommands.PlayAnimkit:
					{
						// Source must be Creature.
						var cSource = GetScriptCreature(source, true, step.Script);

						if (cSource)
							cSource.PlayOneShotAnimKitId((ushort)step.Script.PlayAnimKit.AnimKitID);

						break;
					}
					default:
						Log.outError(LogFilter.Scripts, "Unknown script command {0}.", step.Script.GetDebugInfo());

						break;
				}

				Global.MapMgr.DecreaseScheduledScriptCount();
			}

			_scriptSchedule.Remove(iter.Key);
			iter = _scriptSchedule.FirstOrDefault();
		}
	}

	#endregion
}