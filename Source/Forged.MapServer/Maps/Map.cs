// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks.Dataflow;
using Autofac;
using Forged.MapServer.BattleFields;
using Forged.MapServer.Chrono;
using Forged.MapServer.Collision;
using Forged.MapServer.Collision.Management;
using Forged.MapServer.Collision.Models;
using Forged.MapServer.Conditions;
using Forged.MapServer.DataStorage;
using Forged.MapServer.DataStorage.Structs.M;
using Forged.MapServer.DataStorage.Structs.S;
using Forged.MapServer.Entities;
using Forged.MapServer.Entities.AreaTriggers;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.GameObjects;
using Forged.MapServer.Entities.Items;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Objects.Update;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Globals;
using Forged.MapServer.Globals.Caching;
using Forged.MapServer.Maps.GridNotifiers;
using Forged.MapServer.Maps.Grids;
using Forged.MapServer.Maps.Interfaces;
using Forged.MapServer.MapWeather;
using Forged.MapServer.Movement;
using Forged.MapServer.Networking;
using Forged.MapServer.Networking.Packets.Misc;
using Forged.MapServer.Networking.Packets.WorldState;
using Forged.MapServer.OutdoorPVP;
using Forged.MapServer.Phasing;
using Forged.MapServer.Pools;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IMap;
using Forged.MapServer.Scripting.Interfaces.IPlayer;
using Forged.MapServer.Scripting.Interfaces.IWorldState;
using Forged.MapServer.World;
using Framework.Constants;
using Framework.Database;
using Framework.Threading;
using Framework.Util;
using Game.Common;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Forged.MapServer.Maps;

public class Map : IDisposable
{
    private readonly List<WorldObject> _activeNonPlayers = new();
    private readonly List<AreaTrigger> _areaTriggersToMove = new();
    private readonly List<Corpse> _corpseBones = new();
    private readonly MultiMap<uint, Corpse> _corpsesByCell = new();
    private readonly Dictionary<ObjectGuid, Corpse> _corpsesByPlayer = new();
    private readonly Dictionary<ulong, RespawnInfo> _creatureRespawnTimesBySpawnId = new();
    private readonly List<Creature> _creaturesToMove = new();
    private readonly List<DynamicObject> _dynamicObjectsToMove = new();
    private readonly DynamicMapTree _dynamicTree;
    private readonly ConcurrentQueue<Action<Map>> _farSpellCallbacks = new();
    private readonly Dictionary<ulong, RespawnInfo> _gameObjectRespawnTimesBySpawnId = new();
    private readonly List<GameObject> _gameObjectsToMove = new();
    private readonly long _gridExpiry;
    private readonly Dictionary<HighGuid, ObjectGuidGenerator> _guidGenerators = new();
    private readonly Dictionary<uint, Dictionary<uint, object>> _locks = new();
    private readonly BitSet _markedCells = new(MapConst.TotalCellsPerMap * MapConst.TotalCellsPerMap);
    private readonly List<WorldObject> _objectsToRemove = new();
    private readonly Dictionary<WorldObject, bool> _objectsToSwitch = new();
    private readonly ActionBlock<uint> _processRelocationQueue;
    private readonly LimitedThreadTaskManager _processTransportaionQueue = new(1);
    private readonly SortedSet<RespawnInfo> _respawnTimes = new(new CompareRespawnInfo());
    private readonly object _scriptLock = new();
    private readonly ScriptManager _scriptManager;
    private readonly SortedDictionary<long, List<ScriptAction>> _scriptSchedule = new();
    private readonly LimitedThreadTaskManager _threadManager;
    private readonly List<uint> _toggledSpawnGroupIds = new();
    private readonly List<Transport> _transports = new();
    private readonly List<WorldObject> _updateObjects = new();
    private readonly IntervalTimer _weatherUpdateTimer;
    private readonly List<WorldObject> _worldObjects = new();
    private readonly Dictionary<int, int> _worldStateValues;
    private readonly Dictionary<uint, ZoneDynamicInfo> _zoneDynamicInfo = new();
    private readonly Dictionary<uint, uint> _zonePlayerCountMap = new();
    private uint _respawnCheckTimer;

    public Map(uint id, long expiry, uint instanceId, Difficulty spawnmode, ClassFactory classFactory)
    {
        ClassFactory = classFactory;
        _dynamicTree = classFactory.Resolve<DynamicMapTree>();
        Configuration = classFactory.Resolve<IConfiguration>();
        _threadManager = new LimitedThreadTaskManager(Configuration.GetDefaultValue("Map:ParellelUpdateTasks", 20));
        CliDB = classFactory.Resolve<CliDB>();
        TerrainManager = classFactory.Resolve<TerrainManager>();
        GameObjectManager = classFactory.Resolve<GameObjectManager>();
        PoolManager = classFactory.Resolve<PoolManager>();
        MMAPManager = classFactory.Resolve<MMapManager>();
        WorldManager = classFactory.Resolve<WorldManager>();
        TransportManager = classFactory.Resolve<TransportManager>();
        WorldStateManager = classFactory.Resolve<WorldStateManager>();
        OutdoorPvPManager = classFactory.Resolve<OutdoorPvPManager>();
        BattleFieldManager = classFactory.Resolve<BattleFieldManager>();
        MapManager = classFactory.Resolve<MapManager>();
        VMapManager = classFactory.Resolve<VMapManager>();
        DB2Manager = classFactory.Resolve<DB2Manager>();
        GridDefines = classFactory.Resolve<GridDefines>();
        ScriptManager = classFactory.Resolve<ScriptManager>();
        ConditionManager = classFactory.Resolve<ConditionManager>();
        CharacterDatabase = classFactory.Resolve<CharacterDatabase>();
        WeatherManager = classFactory.Resolve<WeatherManager>();
        ObjectAccessor = classFactory.Resolve<ObjectAccessor>();
        CellCalculator = classFactory.Resolve<CellCalculator>();
        WaypointManager = classFactory.Resolve<WaypointManager>();
        PhasingHandler = classFactory.Resolve<PhasingHandler>();
        MapSpawnGroupCache = classFactory.Resolve<MapSpawnGroupCache>();
        CreatureDataCache = classFactory.Resolve<CreatureDataCache>();
        _scriptManager = classFactory.Resolve<ScriptManager>();
        

        try
        {
            Entry = CliDB.MapStorage.LookupByKey(id);
            DifficultyID = spawnmode;
            InstanceIdInternal = instanceId;
            VisibleDistance = SharedConst.DefaultVisibilityDistance;
            VisibilityNotifyPeriod = SharedConst.DefaultVisibilityNotifyPeriod;
            _gridExpiry = expiry;
            Terrain = TerrainManager.LoadTerrain(id);
            _zonePlayerCountMap.Clear();

            var guidGeneratorFactory = classFactory.Resolve<ObjectGuidGeneratorFactory>();

            //lets initialize visibility distance for map
            InitVisibilityDistance();

            _weatherUpdateTimer = new IntervalTimer
            {
                Interval = 1 * Time.IN_MILLISECONDS
            };

            GetGuidSequenceGenerator(HighGuid.Transport).Set(guidGeneratorFactory.GetGenerator(HighGuid.Transport).GetNextAfterMaxUsed());

            PoolData = PoolManager.InitPoolsForMap(this);

            TransportManager.CreateTransportsForMap(this);

            MMAPManager.LoadMapInstance(WorldManager.DataPath, Id, InstanceIdInternal);

            _worldStateValues = WorldStateManager.GetInitialWorldStatesForMap(this);

            OutdoorPvPManager.CreateOutdoorPvPForMap(this);
            BattleFieldManager.CreateBattlefieldsForMap(this);

            _processRelocationQueue = new ActionBlock<uint>(ProcessRelocationNotifies,
                                                            new ExecutionDataflowBlockOptions
                                                            {
                                                                MaxDegreeOfParallelism = 1,
                                                                EnsureOrdered = true,
                                                                MaxMessagesPerTask = 1
                                                            });

            OnCreateMap(this);
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex);

            throw;
        }
    }

    public int ActiveNonPlayersCount => _activeNonPlayers.Count;
    public ConcurrentMultiMap<ulong, AreaTrigger> AreaTriggerBySpawnIdStore { get; } = new();
    public BattleFieldManager BattleFieldManager { get; }
    public CellCalculator CellCalculator { get; }
    public CharacterDatabase CharacterDatabase { get; }
    public ClassFactory ClassFactory { get; }
    public CliDB CliDB { get; }
    public ConditionManager ConditionManager { get; }
    public IConfiguration Configuration { get; }
    public CreatureDataCache CreatureDataCache { get; }
    public ConcurrentMultiMap<ulong, Creature> CreatureBySpawnIdStore { get; } = new();
    public Dictionary<ulong, CreatureGroup> CreatureGroupHolder { get; set; } = new();
    public DB2Manager DB2Manager { get; }
    public Difficulty DifficultyID { get; }
    public MapRecord Entry { get; }
    public MapSpawnGroupCache MapSpawnGroupCache { get; }
    public ConcurrentMultiMap<ulong, GameObject> GameObjectBySpawnIdStore { get; } = new();
    public GameObjectManager GameObjectManager { get; }
    public GridDefines GridDefines { get; }
    public Dictionary<uint, Dictionary<uint, Grid>> Grids { get; } = new();
    public bool HavePlayers => !ActivePlayers.Empty();
    public uint Id => Entry.Id;
    public bool Instanceable => Entry != null && Entry.Instanceable();
    public uint InstanceId => InstanceIdInternal;

    // since 25man difficulties are 1 and 3, we can check them like that
    public bool Is25ManRaid => IsRaid && DifficultyID is Difficulty.Raid25N or Difficulty.Raid25HC;

    public bool IsBattleArena => Entry != null && Entry.IsBattleArena();
    public bool IsBattleground => Entry != null && Entry.IsBattleground();
    public bool IsBattlegroundOrArena => Entry != null && Entry.IsBattlegroundOrArena();
    public bool IsDungeon => Entry != null && Entry.IsDungeon();
    public bool IsGarrison => Entry != null && Entry.IsGarrison();
    public bool IsHeroic => CliDB.DifficultyStorage.TryGetValue((uint)DifficultyID, out var difficulty) && difficulty.Flags.HasAnyFlag(DifficultyFlags.Heroic);
    public bool IsNonRaidDungeon => Entry != null && Entry.IsNonRaidDungeon();
    public bool IsRaid => Entry != null && Entry.IsRaid();
    public bool IsScenario => Entry != null && Entry.IsScenario();
    public MapDifficultyRecord MapDifficulty => DB2Manager.GetMapDifficultyData(Id, DifficultyID);
    public MapManager MapManager { get; }
    public string MapName => Entry.MapName[WorldManager.DefaultDbcLocale];
    public MMapManager MMAPManager { get; }
    public MultiPersonalPhaseTracker MultiPersonalPhaseTracker { get; } = new();
    public ObjectAccessor ObjectAccessor { get; }
    public ConcurrentDictionary<ObjectGuid, WorldObject> ObjectsStore { get; } = new();
    public OutdoorPvPManager OutdoorPvPManager { get; }
    public PhasingHandler PhasingHandler { get; }
    public List<Player> Players => ActivePlayers;

    public int PlayersCountExceptGMs
    {
        get { return ActivePlayers.Count(pl => !pl.IsGameMaster); }
    }

    public SpawnedPoolData PoolData { get; }
    public PoolManager PoolManager { get; }
    public ScriptManager ScriptManager { get; }
    public TerrainInfo Terrain { get; }
    public TerrainManager TerrainManager { get; }
    public BattlegroundMap ToBattlegroundMap => IsBattlegroundOrArena ? this as BattlegroundMap : null;
    public InstanceMap ToInstanceMap => IsDungeon ? this as InstanceMap : null;
    public TransportManager TransportManager { get; }
    public int VisibilityNotifyPeriod { get; set; }
    public float VisibilityRange => VisibleDistance;
    public float VisibleDistance { get; set; }
    public VMapManager VMapManager { get; }
    public WaypointManager WaypointManager { get; }
    public WeatherManager WeatherManager { get; }
    public WorldManager WorldManager { get; }
    public WorldStateManager WorldStateManager { get; }
    internal uint InstanceIdInternal { get; set; }
    internal object MapLock { get; set; } = new();
    internal uint UnloadTimer { get; set; }
    protected List<Player> ActivePlayers { get; } = new();

    public static bool IsInWMOInterior(uint mogpFlags)
    {
        return (mogpFlags & 0x2000) != 0;
    }

    public bool ActiveObjectsNearGrid(Grid grid)
    {
        var cellMin = new CellCoord(grid.X * MapConst.MaxCells,
                                    grid.Y * MapConst.MaxCells);

        var cellMax = new CellCoord(cellMin.X + MapConst.MaxCells,
                                    cellMin.Y + MapConst.MaxCells);

        //we must find visible range in cells so we unload only non-visible cells...
        var viewDist = VisibilityRange;
        var cellRange = (uint)Math.Ceiling(viewDist / MapConst.SizeofCells) + 1;

        cellMin.DecX(cellRange);
        cellMin.DecY(cellRange);
        cellMax.IncX(cellRange);
        cellMax.IncY(cellRange);

        foreach (var pl in ActivePlayers)
        {
            var p = GridDefines.ComputeCellCoord(pl.Location.X, pl.Location.Y);

            if (cellMin.X <= p.X &&
                p.X <= cellMax.X &&
                cellMin.Y <= p.Y &&
                p.Y <= cellMax.Y)
                return true;
        }

        foreach (var obj in _activeNonPlayers)
        {
            var p = GridDefines.ComputeCellCoord(obj.Location.X, obj.Location.Y);

            if (cellMin.X <= p.X &&
                p.X <= cellMax.X &&
                cellMin.Y <= p.Y &&
                p.Y <= cellMax.Y)
                return true;
        }

        return false;
    }

    public void AddCorpse(Corpse corpse)
    {
        corpse.Location.Map = this;
        corpse.CheckAddToMap();
        _corpsesByCell.Add(corpse.CellCoord.GetId(), corpse);

        if (corpse.CorpseType != CorpseType.Bones)
            _corpsesByPlayer[corpse.OwnerGUID] = corpse;
        else
            _corpseBones.Add(corpse);
    }

    public void AddFarSpellCallback(Action<Map> callback)
    {
        _farSpellCallbacks.Enqueue(callback);
    }

    public void AddObjectToRemoveList(WorldObject obj)
    {
        obj.SetDestroyedObject(true);
        obj.CleanupsBeforeDelete(false); // remove or simplify at least cross referenced links

        lock (_objectsToRemove)
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

    public virtual bool AddPlayerToMap(Player player, bool initPlayer = true)
    {
        var cellCoord = GridDefines.ComputeCellCoord(player.Location.X, player.Location.Y);

        if (!cellCoord.IsCoordValid())
        {
            Log.Logger.Error("Map.AddPlayer (GUID: {0}) has invalid coordinates X:{1} Y:{2}",
                             player.GUID.ToString(),
                             player.Location.X,
                             player.Location.Y);

            return false;
        }

        var cell = new Cell(cellCoord, GridDefines);
        EnsureGridLoadedForActiveObject(cell, player);
        AddToGrid(player, cell);

        player.Location.Map = this;
        player.CheckAddToMap();
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

    public void AddToActive(WorldObject obj)
    {
        AddToActiveHelper(obj);

        Position respawnLocation = null;

        switch (obj.TypeId)
        {
            case TypeId.Unit:
                var creature = obj.AsCreature;

                if (creature is { IsPet: false } && creature.SpawnId != 0)
                    respawnLocation = creature.RespawnPosition;

                break;

            case TypeId.GameObject:
                var gameObject = obj.AsGameObject;

                if (gameObject != null && gameObject.SpawnId != 0)
                    respawnLocation = gameObject.GetRespawnPosition();

                break;
        }

        if (respawnLocation == null)
            return;

        var p = GridDefines.ComputeGridCoord(respawnLocation.X, respawnLocation.Y);

        if (GetGrid(p.X, p.Y) != null)
            GetGrid(p.X, p.Y).GridInformation.IncUnloadActiveLock();
        else
        {
            var p2 = GridDefines.ComputeGridCoord(obj.Location.X, obj.Location.Y);
            Log.Logger.Error($"Active object {obj.GUID} added to grid[{p.X}, {p.Y}] but spawn grid[{p2.X}, {p2.Y}] was not loaded.");
        }
    }

    public void AddToGrid<T>(T obj, Cell cell) where T : WorldObject
    {
        var grid = GetGrid(cell.Data.GridX, cell.Data.GridY);

        switch (obj.TypeId)
        {
            case TypeId.Corpse:
                if (grid.IsGridObjectDataLoaded)
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
                        grid.GetGridCell(cell.Data.CellX, cell.Data.CellY).AddWorldObject(obj);
                    }
                    else
                        grid.GetGridCell(cell.Data.CellX, cell.Data.CellY).AddGridObject(obj);
                }

                return;

            case TypeId.GameObject:
            case TypeId.AreaTrigger:
                grid.GetGridCell(cell.Data.CellX, cell.Data.CellY).AddGridObject(obj);

                break;

            case TypeId.DynamicObject:
            default:
                if (obj.IsWorldObject())
                    grid.GetGridCell(cell.Data.CellX, cell.Data.CellY).AddWorldObject(obj);
                else
                    grid.GetGridCell(cell.Data.CellX, cell.Data.CellY).AddGridObject(obj);

                break;
        }

        obj.Location.SetCurrentCell(cell);
    }

    public bool AddToMap(WorldObject obj)
    {
        //TODO: Needs clean up. An object should not be added to map twice.
        if (obj.Location.IsInWorld)
        {
            obj.UpdateObjectVisibility();

            return true;
        }

        var cellCoord = GridDefines.ComputeCellCoord(obj.Location.X, obj.Location.Y);

        if (!cellCoord.IsCoordValid())
        {
            Log.Logger.Error("Map.Add: Object {0} has invalid coordinates X:{1} Y:{2} grid cell [{3}:{4}]",
                             obj.GUID,
                             obj.Location.X,
                             obj.Location.Y,
                             cellCoord.X,
                             cellCoord.Y);

            return false; //Should delete object
        }

        var cell = new Cell(cellCoord, GridDefines);

        if (obj.IsActive)
            EnsureGridLoadedForActiveObject(cell, obj);
        else
            EnsureGridCreated(new GridCoord(cell.Data.GridX, cell.Data.GridY));

        AddToGrid(obj, cell);
        Log.Logger.Debug("Object {0} enters grid[{1}, {2}]", obj.GUID.ToString(), cell.Data.GridX, cell.Data.GridY);

        obj.AddToWorld();

        InitializeObject(obj);

        if (obj.IsActive)
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
        if (obj.Location.IsInWorld)
            return true;

        var cellCoord = GridDefines.ComputeCellCoord(obj.Location.X, obj.Location.Y);

        if (!cellCoord.IsCoordValid())
        {
            Log.Logger.Error("Map.Add: Object {0} has invalid coordinates X:{1} Y:{2} grid cell [{3}:{4}]",
                             obj.GUID,
                             obj.Location.X,
                             obj.Location.Y,
                             cellCoord.X,
                             cellCoord.Y);

            return false; //Should delete object
        }

        _transports.Add(obj);

        if (obj.ExpectedMapId == Id)
        {
            obj.AddToWorld();

            // Broadcast creation to players
            foreach (var player in Players)
                if (player.Transport != obj && player.Location.InSamePhase(obj))
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

    public void AddUpdateObject(WorldObject obj)
    {
        lock (_updateObjects)
            if (obj != null)
                _updateObjects.Add(obj);
    }

    public void AddWorldObject(WorldObject obj)
    {
        _worldObjects.Add(obj);
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

        var data = GameObjectManager.SpawnDataCacheRouter.GetSpawnMetadata(type, spawnId);

        if (data == null)
            return;

        if (!data.SpawnGroupData.Flags.HasFlag(SpawnGroupFlags.DynamicSpawnRate))
            return;

        if (!_zonePlayerCountMap.ContainsKey(obj.Location.Zone))
            return;

        var playerCount = _zonePlayerCountMap[obj.Location.Zone];

        if (playerCount == 0)
            return;

        double adjustFactor = Configuration.GetDefaultValue(type == SpawnObjectType.GameObject ? "Respawn.DynamicRateGameObject" : "Respawn.DynamicRateCreature", 10f) / playerCount;

        if (adjustFactor >= 1.0) // nothing to do here
            return;

        var timeMinimum = Configuration.GetDefaultValue(type == SpawnObjectType.GameObject ? "Respawn.DynamicMinimumGameObject" : "Respawn.DynamicMinimumCreature", 10);

        if (respawnDelay <= timeMinimum)
            return;

        respawnDelay = (uint)Math.Max(Math.Ceiling(respawnDelay * adjustFactor), timeMinimum);
    }

    public void AreaTriggerRelocation(AreaTrigger at, Position pos)
    {
        AreaTriggerRelocation(at, pos.X, pos.Y, pos.Z, pos.Orientation);
    }

    public void AreaTriggerRelocation(AreaTrigger at, float x, float y, float z, float orientation)
    {
        Cell newCell = new(x, y, GridDefines);

        if (GetGrid(newCell.Data.GridX, newCell.Data.GridY) == null)
            return;

        var oldCell = at.Location.GetCurrentCell();

        // delay areatrigger move for grid/cell to grid/cell moves
        if (oldCell.DiffCell(newCell) || oldCell.DiffGrid(newCell))
        {
            Log.Logger.Debug("AreaTrigger ({0}) added to moving list from {1} to {2}.", at.GUID.ToString(), oldCell.ToString(), newCell.ToString());

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

    public void Balance()
    {
        _dynamicTree.Balance();
    }

    public virtual TransferAbortParams CannotEnter(Player player)
    {
        return null;
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

    public bool ContainsGameObjectModel(GameObjectModel model)
    {
        return _dynamicTree.Contains(model);
    }

    public Corpse ConvertCorpseToBones(ObjectGuid ownerGuid, bool insignia = false)
    {
        var corpse = GetCorpseByPlayer(ownerGuid);

        if (corpse == null)
            return null;

        RemoveCorpse(corpse);

        // remove corpse from DB
        SQLTransaction trans = new();
        corpse.DeleteFromDB(trans);
        CharacterDatabase.CommitTransaction(trans);

        Corpse bones = null;

        // create the bones only if the map and the grid is loaded at the corpse's location
        // ignore bones creating option in case insignia
        if ((insignia ||
             (IsBattlegroundOrArena ? Configuration.GetDefaultValue("Death:Bones:BattlegroundOrArena", true) : Configuration.GetDefaultValue("Death:Bones:World", true))) &&
            !IsRemovalGrid(corpse.Location.X, corpse.Location.Y))
        {
            // Create bones, don't change Corpse
            bones = ClassFactory.ResolveWithPositionalParameters<Corpse>(CorpseType.Bones);
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

            bones.SetCellCoord(corpse.CellCoord);
            bones.Location.Relocate(corpse.Location.X, corpse.Location.Y, corpse.Location.Z, corpse.Location.Orientation);

            PhasingHandler.InheritPhaseShift(bones, corpse);

            AddCorpse(bones);

            bones.Location.UpdatePositionData();
            bones.Location.SetZoneScript();

            // add bones in grid store if grid loaded where corpse placed
            AddToMap(bones);
        }

        // all references to the corpse should be removed at this point
        corpse.Dispose();

        return bones;
    }

    public void CreatureRelocation(Creature creature, Position p, bool respawnRelocationOnFail = true)
    {
        CreatureRelocation(creature, p.X, p.Y, p.Z, p.Orientation, respawnRelocationOnFail);
    }

    public void CreatureRelocation(Creature creature, float x, float y, float z, float ang, bool respawnRelocationOnFail = true)
    {
        var newCell = new Cell(x, y, GridDefines);

        if (!respawnRelocationOnFail && GetGrid(newCell.Data.GridX, newCell.Data.GridY) == null)
            return;

        var oldCell = creature.Location.GetCurrentCell();

        // delay creature move for grid/cell to grid/cell moves
        if (oldCell.DiffCell(newCell) || oldCell.DiffGrid(newCell))
            AddCreatureToMoveList(creature, x, y, z, ang);
        // in diffcell/diffgrid case notifiers called at finishing move creature in MoveAllCreaturesInMoveList
        else
        {
            creature.Location.Relocate(x, y, z, ang);

            if (creature.IsVehicle)
                creature.VehicleKit.RelocatePassengers();

            creature.UpdateObjectVisibility(false);
            creature.Location.UpdatePositionData();
            RemoveCreatureFromMoveList(creature);
        }
    }

    public bool CreatureRespawnRelocation(Creature c, bool diffGridOnly)
    {
        var respPos = c.RespawnPosition;
        var respCell = new Cell(respPos.X, respPos.Y, GridDefines);

        //creature will be unloaded with grid
        if (diffGridOnly && !c.Location.GetCurrentCell().DiffGrid(respCell))
            return true;

        c.CombatStop();
        c.MotionMaster.Clear();

        // teleport it to respawn point (like normal respawn if player see)
        if (!CreatureCellRelocation(c, respCell))
            return false;

        c.Location.Relocate(respPos);
        c.MotionMaster.Initialize(); // prevent possible problems with default move generators
        c.Location.UpdatePositionData();
        c.UpdateObjectVisibility(false);

        return true;
    }

    public virtual void DelayedUpdate(uint diff)
    {
        while (_farSpellCallbacks.TryDequeue(out var callback))
            callback(this);

        RemoveAllObjectsInRemoveList();

        // Don't unload grids if it's Battleground, since we may have manually added GOs, creatures, those doesn't load from DB at grid re-load !
        // This isn't really bother us, since as soon as we have instanced BG-s, the whole map unloads as the BG gets ended
        if (IsBattlegroundOrArena)
            return;

        foreach (var grid in Grids.SelectMany(kvp => kvp.Value.Select(ivp => ivp.Value)).Where(g => g != null).ToList()) // flatten and make copy as it can remove grids.
            grid?.Update(this, diff);
    }

    public void DeleteCorpseData()
    {
        // DELETE cp, c FROM corpse_phases cp INNER JOIN corpse c ON cp.OwnerGuid = c.guid WHERE c.mapId = ? AND c.instanceId = ?
        var stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_CORPSES_FROM_MAP);
        stmt.AddValue(0, Id);
        stmt.AddValue(1, InstanceId);
        CharacterDatabase.Execute(stmt);
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

        var stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_ALL_RESPAWNS);
        stmt.AddValue(0, Id);
        stmt.AddValue(1, InstanceId);
        CharacterDatabase.Execute(stmt);
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
            obj.Location.ResetMap();
        }

        if (!_scriptSchedule.Empty())
            MapManager.DecreaseScheduledScriptCount((uint)_scriptSchedule.Sum(kvp => kvp.Value.Count));

        OutdoorPvPManager.DestroyOutdoorPvPForMap(this);
        BattleFieldManager.DestroyBattlefieldsForMap(this);

        MMAPManager.UnloadMapInstance(Id, InstanceIdInternal);
    }

    public void DoOnPlayers(Action<Player> action)
    {
        foreach (var player in Players)
            action(player);
    }

    public void DynamicObjectRelocation(DynamicObject dynObj, Position pos)
    {
        DynamicObjectRelocation(dynObj, pos.X, pos.Y, pos.Z, pos.Orientation);
    }

    public void DynamicObjectRelocation(DynamicObject dynObj, float x, float y, float z, float orientation)
    {
        Cell newCell = new(x, y, GridDefines);

        if (GetGrid(newCell.Data.GridX, newCell.Data.GridY) == null)
            return;

        var oldCell = dynObj.Location.GetCurrentCell();

        // delay creature move for grid/cell to grid/cell moves
        if (oldCell.DiffCell(newCell) || oldCell.DiffGrid(newCell))
        {
            Log.Logger.Debug("DynamicObject (GUID: {0}) added to moving list from grid[{1}, {2}]cell[{3}, {4}] to grid[{5}, {6}]cell[{7}, {8}].",
                             dynObj.GUID.ToString(),
                             oldCell.Data.GridX,
                             oldCell.Data.GridY,
                             oldCell.Data.CellX,
                             oldCell.Data.CellY,
                             newCell.Data.GridX,
                             newCell.Data.GridY,
                             newCell.Data.CellX,
                             newCell.Data.CellY);

            AddDynamicObjectToMoveList(dynObj, x, y, z, orientation);
            // in diffcell/diffgrid case notifiers called at finishing move dynObj in Map.MoveAllGameObjectsInMoveList
        }
        else
        {
            dynObj.Location.Relocate(x, y, z, orientation);
            dynObj.Location.UpdatePositionData();
            dynObj.UpdateObjectVisibility(false);
            RemoveDynamicObjectFromMoveList(dynObj);
        }
    }

    public void GameObjectRelocation(GameObject go, Position pos, bool respawnRelocationOnFail = true)
    {
        GameObjectRelocation(go, pos.X, pos.Y, pos.Z, pos.Orientation, respawnRelocationOnFail);
    }

    public void GameObjectRelocation(GameObject go, float x, float y, float z, float orientation, bool respawnRelocationOnFail = true)
    {
        var newCell = new Cell(x, y, GridDefines);

        if (!respawnRelocationOnFail && GetGrid(newCell.Data.GridX, newCell.Data.GridY) == null)
            return;

        var oldCell = go.Location.GetCurrentCell();

        // delay creature move for grid/cell to grid/cell moves
        if (oldCell.DiffCell(newCell) || oldCell.DiffGrid(newCell))
        {
            Log.Logger.Debug("GameObject (GUID: {0} Entry: {1}) added to moving list from grid[{2}, {3}]cell[{4}, {5}] to grid[{6}, {7}]cell[{8}, {9}].",
                             go.GUID.ToString(),
                             go.Entry,
                             oldCell.Data.GridX,
                             oldCell.Data.GridY,
                             oldCell.Data.CellX,
                             oldCell.Data.CellY,
                             newCell.Data.GridX,
                             newCell.Data.GridY,
                             newCell.Data.CellX,
                             newCell.Data.CellY);

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

    public bool GameObjectRespawnRelocation(GameObject go, bool diffGridOnly)
    {
        var respawnPos = go.GetRespawnPosition();
        var respCell = new Cell(respawnPos.X, respawnPos.Y, GridDefines);

        //GameObject will be unloaded with grid
        if (diffGridOnly && !go.Location.GetCurrentCell().DiffGrid(respCell))
            return true;

        Log.Logger.Debug("GameObject (GUID: {0} Entry: {1}) moved from grid[{2}, {3}] to respawn grid[{4}, {5}].",
                         go.GUID.ToString(),
                         go.Entry,
                         go.Location.GetCurrentCell().Data.GridX,
                         go.Location.GetCurrentCell().Data.GridY,
                         respCell.Data.GridX,
                         respCell.Data.GridY);

        // teleport it to respawn point (like normal respawn if player see)
        if (!GameObjectCellRelocation(go, respCell))
            return false;

        go.Location.Relocate(respawnPos);
        go.Location.UpdatePositionData();
        go.UpdateObjectVisibility(false);

        return true;
    }

    public ulong GenerateLowGuid(HighGuid high)
    {
        return GetGuidSequenceGenerator(high).Generate();
    }

    public uint GetAreaId(PhaseShift phaseShift, Position pos)
    {
        return Terrain.GetAreaId(phaseShift, Id, pos.X, pos.Y, pos.Z, _dynamicTree);
    }

    public uint GetAreaId(PhaseShift phaseShift, float x, float y, float z)
    {
        return Terrain.GetAreaId(phaseShift, Id, x, y, z, _dynamicTree);
    }

    public AreaTrigger GetAreaTrigger(ObjectGuid guid)
    {
        if (!guid.IsAreaTrigger)
            return null;

        return ObjectsStore.LookupByKey(guid) as AreaTrigger;
    }

    public AreaTrigger GetAreaTriggerBySpawnId(ulong spawnId)
    {
        return AreaTriggerBySpawnIdStore.TryGetValue(spawnId, out var bounds) ? null : bounds.FirstOrDefault();
    }

    public Conversation GetConversation(ObjectGuid guid)
    {
        return ObjectsStore.LookupByKey(guid) as Conversation;
    }

    public Corpse GetCorpse(ObjectGuid guid)
    {
        if (!guid.IsCorpse)
            return null;

        return ObjectsStore.LookupByKey(guid) as Corpse;
    }

    public Corpse GetCorpseByPlayer(ObjectGuid ownerGuid)
    {
        return _corpsesByPlayer.LookupByKey(ownerGuid);
    }

    public List<Corpse> GetCorpsesInCell(uint cellId)
    {
        return _corpsesByCell.LookupByKey(cellId);
    }

    public Creature GetCreature(ObjectGuid guid)
    {
        if (!guid.IsCreatureOrVehicle)
            return null;

        return ObjectsStore.LookupByKey(guid) as Creature;
    }

    public Creature GetCreatureBySpawnId(ulong spawnId)
    {
        if (CreatureBySpawnIdStore.TryGetValue(spawnId, out var bounds))
            return null;

        var foundCreature = bounds.Find(creature => creature.IsAlive);

        return foundCreature ?? bounds[0];
    }

    public long GetCreatureRespawnTime(ulong spawnId)
    {
        return GetRespawnTime(SpawnObjectType.Creature, spawnId);
    }

    public virtual string GetDebugInfo()
    {
        return $"Id: {Id} InstanceId: {InstanceId} Difficulty: {DifficultyID} HasPlayers: {HavePlayers}";
    }

    public ItemContext GetDifficultyLootItemContext()
    {
        var mapDifficulty = MapDifficulty;

        if (mapDifficulty != null && mapDifficulty.ItemContext != 0)
            return (ItemContext)mapDifficulty.ItemContext;

        if (CliDB.DifficultyStorage.TryGetValue((uint)DifficultyID, out var difficulty))
            return (ItemContext)difficulty.ItemContext;

        return ItemContext.None;
    }

    public DynamicObject GetDynamicObject(ObjectGuid guid)
    {
        if (!guid.IsDynamicObject)
            return null;

        return ObjectsStore.LookupByKey(guid) as DynamicObject;
    }

    public void GetFullTerrainStatusForPosition(PhaseShift phaseShift, float x, float y, float z, PositionFullTerrainStatus data, LiquidHeaderTypeFlags reqLiquidType, float collisionHeight = MapConst.DefaultCollesionHeight)
    {
        Terrain.GetFullTerrainStatusForPosition(phaseShift, Id, x, y, z, data, reqLiquidType, collisionHeight, _dynamicTree);
    }

    public GameObject GetGameObject(ObjectGuid guid)
    {
        if (!guid.IsAnyTypeGameObject)
            return null;

        return ObjectsStore.LookupByKey(guid) as GameObject;
    }

    public GameObject GetGameObjectBySpawnId(ulong spawnId)
    {
        if (GameObjectBySpawnIdStore.TryGetValue(spawnId, out var bounds))
            return null;

        var foundGameObject = bounds.Find(gameobject => gameobject.IsSpawned);

        return foundGameObject ?? bounds[0];
    }

    public float GetGameObjectFloor(PhaseShift phaseShift, float x, float y, float z, float maxSearchDist = MapConst.DefaultHeightSearch)
    {
        return _dynamicTree.GetHeight(x, y, z, maxSearchDist, phaseShift);
    }

    public long GetGORespawnTime(ulong spawnId)
    {
        return GetRespawnTime(SpawnObjectType.GameObject, spawnId);
    }

    public float GetGridHeight(PhaseShift phaseShift, float x, float y)
    {
        return Terrain.GetGridHeight(phaseShift, Id, x, y);
    }

    public float GetHeight(PhaseShift phaseShift, float x, float y, float z, bool vmap = true, float maxSearchDist = MapConst.DefaultHeightSearch)
    {
        return Math.Max(GetStaticHeight(phaseShift, x, y, z, vmap, maxSearchDist), GetGameObjectFloor(phaseShift, x, y, z, maxSearchDist));
    }

    public float GetHeight(PhaseShift phaseShift, Position pos, bool vmap = true, float maxSearchDist = MapConst.DefaultHeightSearch)
    {
        return GetHeight(phaseShift, pos.X, pos.Y, pos.Z, vmap, maxSearchDist);
    }

    public long GetLinkedRespawnTime(ObjectGuid guid)
    {
        var linkedGuid = CreatureDataCache.GetLinkedRespawnGuid(guid);

        return linkedGuid.High switch
        {
            HighGuid.Creature => GetCreatureRespawnTime(linkedGuid.Counter),
            HighGuid.GameObject => GetGORespawnTime(linkedGuid.Counter),
            _ => 0L
        };
    }

    public ZLiquidStatus GetLiquidStatus(PhaseShift phaseShift, Position pos, LiquidHeaderTypeFlags reqLiquidType, float collisionHeight = MapConst.DefaultCollesionHeight)
    {
        return GetLiquidStatus(phaseShift, pos.X, pos.Y, pos.Z, reqLiquidType, collisionHeight);
    }

    public ZLiquidStatus GetLiquidStatus(PhaseShift phaseShift, float x, float y, float z, LiquidHeaderTypeFlags reqLiquidType, float collisionHeight = MapConst.DefaultCollesionHeight)
    {
        return Terrain.GetLiquidStatus(phaseShift, Id, x, y, z, reqLiquidType, out _, collisionHeight);
    }

    public ZLiquidStatus GetLiquidStatus(PhaseShift phaseShift, Position pos, LiquidHeaderTypeFlags reqLiquidType, out LiquidData data, float collisionHeight = MapConst.DefaultCollesionHeight)
    {
        return Terrain.GetLiquidStatus(phaseShift, Id, pos.X, pos.Y, pos.Z, reqLiquidType, out data, collisionHeight);
    }

    public ZLiquidStatus GetLiquidStatus(PhaseShift phaseShift, float x, float y, float z, LiquidHeaderTypeFlags reqLiquidType, out LiquidData data, float collisionHeight = MapConst.DefaultCollesionHeight)
    {
        return Terrain.GetLiquidStatus(phaseShift, Id, x, y, z, reqLiquidType, out data, collisionHeight);
    }

    public ulong GetMaxLowGuid(HighGuid high)
    {
        return GetGuidSequenceGenerator(high).GetNextAfterMaxUsed();
    }

    public float GetMinHeight(PhaseShift phaseShift, float x, float y)
    {
        return Terrain.GetMinHeight(phaseShift, Id, x, y);
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

    public Weather GetOrGenerateZoneDefaultWeather(uint zoneId)
    {
        var weatherData = WeatherManager.GetWeatherData(zoneId);

        if (weatherData == null)
            return null;

        if (!_zoneDynamicInfo.ContainsKey(zoneId))
            _zoneDynamicInfo[zoneId] = new ZoneDynamicInfo();

        var info = _zoneDynamicInfo[zoneId];

        if (info.DefaultWeather != null)
            return info.DefaultWeather;

        info.DefaultWeather = ClassFactory.ResolveWithPositionalParameters<Weather>(zoneId, weatherData);
        info.DefaultWeather.ReGenerate();
        info.DefaultWeather.UpdateWeather();

        return info.DefaultWeather;
    }

    public virtual uint GetOwnerGuildId(TeamFaction team = TeamFaction.Other)
    {
        return 0;
    }

    public Pet GetPet(ObjectGuid guid)
    {
        if (!guid.IsPet)
            return null;

        return ObjectsStore.LookupByKey(guid) as Pet;
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

        var respawnInfo = map?.LookupByKey(spawnId);

        return respawnInfo;
    }

    public long GetRespawnTime(SpawnObjectType type, ulong spawnId)
    {
        var map = GetRespawnMapForType(type);

        if (map == null)
            return 0;

        var respawnInfo = map.LookupByKey(spawnId);

        return respawnInfo?.RespawnTime ?? 0;
    }

    public SceneObject GetSceneObject(ObjectGuid guid)
    {
        return ObjectsStore.LookupByKey(guid) as SceneObject;
    }

    public float GetStaticHeight(PhaseShift phaseShift, float x, float y, float z, bool checkVMap = true, float maxSearchDist = MapConst.DefaultHeightSearch)
    {
        return Terrain.GetStaticHeight(phaseShift, Id, x, y, z, checkVMap, maxSearchDist);
    }

    public Transport GetTransport(ObjectGuid guid)
    {
        return !guid.IsMOTransport ? null : GetGameObject(guid)?.AsTransport;
    }

    public float GetWaterLevel(PhaseShift phaseShift, float x, float y)
    {
        return Terrain.GetWaterLevel(phaseShift, Id, x, y);
    }

    public float GetWaterOrGroundLevel(PhaseShift phaseShift, float x, float y, float z, float collisionHeight = MapConst.DefaultCollesionHeight)
    {
        float ground = 0;

        return Terrain.GetWaterOrGroundLevel(phaseShift, Id, x, y, z, ref ground, false, collisionHeight, _dynamicTree);
    }

    public float GetWaterOrGroundLevel(PhaseShift phaseShift, float x, float y, float z, ref float ground, bool swim = false, float collisionHeight = MapConst.DefaultCollesionHeight)
    {
        return Terrain.GetWaterOrGroundLevel(phaseShift, Id, x, y, z, ref ground, swim, collisionHeight, _dynamicTree);
    }

    public WorldObject GetWorldObjectBySpawnId(SpawnObjectType type, ulong spawnId)
    {
        return type switch
        {
            SpawnObjectType.Creature => GetCreatureBySpawnId(spawnId),
            SpawnObjectType.GameObject => GetGameObjectBySpawnId(spawnId),
            SpawnObjectType.AreaTrigger => GetAreaTriggerBySpawnId(spawnId),
            _ => null
        };
    }

    public int GetWorldStateValue(int worldStateId)
    {
        return _worldStateValues.LookupByKey(worldStateId);
    }

    public Dictionary<int, int> GetWorldStateValues()
    {
        return _worldStateValues;
    }

    public void GetZoneAndAreaId(PhaseShift phaseShift, out uint zoneid, out uint areaid, Position pos)
    {
        Terrain.GetZoneAndAreaId(phaseShift, Id, out zoneid, out areaid, pos.X, pos.Y, pos.Z, _dynamicTree);
    }

    public void GetZoneAndAreaId(PhaseShift phaseShift, out uint zoneid, out uint areaid, float x, float y, float z)
    {
        Terrain.GetZoneAndAreaId(phaseShift, Id, out zoneid, out areaid, x, y, z, _dynamicTree);
    }

    public uint GetZoneId(PhaseShift phaseShift, Position pos)
    {
        return Terrain.GetZoneId(phaseShift, Id, pos.X, pos.Y, pos.Z, _dynamicTree);
    }

    public uint GetZoneId(PhaseShift phaseShift, float x, float y, float z)
    {
        return Terrain.GetZoneId(phaseShift, Id, x, y, z, _dynamicTree);
    }

    public WeatherState GetZoneWeather(uint zoneId)
    {
        if (_zoneDynamicInfo.TryGetValue(zoneId, out var zoneDynamicInfo))
        {
            if (zoneDynamicInfo.WeatherId != 0)
                return zoneDynamicInfo.WeatherId;

            if (zoneDynamicInfo.DefaultWeather != null)
                return zoneDynamicInfo.DefaultWeather.GetWeatherState();
        }

        return WeatherState.Fine;
    }

    public IList<uint> GridXKeys()
    {
        return Grids.Keys.ToList();
    }

    public IList<uint> GridYKeys(uint x)
    {
        lock (Grids)
            if (Grids.TryGetValue(x, out var yGrid))
                return yGrid.Keys.ToList();

        return Enumerable.Empty<uint>() as IList<uint>;
    }

    public virtual void InitVisibilityDistance()
    {
        //init visibility for continents
        VisibleDistance = WorldManager.MaxVisibleDistanceOnContinents;
        VisibilityNotifyPeriod = WorldManager.VisibilityNotifyPeriodOnContinents;
    }

    public void InsertGameObjectModel(GameObjectModel model)
    {
        _dynamicTree.Insert(model);
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
        return GetGrid(x, y) != null && IsGridObjectDataLoaded(x, y);
    }

    public bool IsGridLoaded(GridCoord p)
    {
        return GetGrid(p.X, p.Y) != null && IsGridObjectDataLoaded(p.X, p.Y);
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
        if (checks.HasAnyFlag(LineOfSightChecks.Vmap) && !VMapManager.IsInLineOfSight(PhasingHandler.GetTerrainMapId(phaseShift, Id, Terrain, x1, y1), x1, y1, z1, x2, y2, z2, ignoreFlags))
            return false;

        return !Configuration.GetDefaultValue("CheckGameObjectLoS", true) || !checks.HasAnyFlag(LineOfSightChecks.Gobject) || _dynamicTree.IsInLineOfSight(new Vector3(x1, y1, z1), new Vector3(x2, y2, z2), phaseShift);
    }

    public bool IsInWater(PhaseShift phaseShift, float x, float y, float z, out LiquidData data)
    {
        return Terrain.IsInWater(phaseShift, Id, x, y, z, out data);
    }

    public bool IsRemovalGrid(float x, float y)
    {
        var p = GridDefines.ComputeGridCoord(x, y);

        return GetGrid(p.X, p.Y) == null ||
               GetGrid(p.X, p.Y).GridState == GridState.Removal;
    }

    public bool IsSpawnGroupActive(uint groupId)
    {
        var data = GetSpawnGroupData(groupId);

        if (data == null)
        {
            Log.Logger.Error($"Tried to query state of non-existing spawn group {groupId} on map {Id}.");

            return false;
        }

        if (data.Flags.HasAnyFlag(SpawnGroupFlags.System))
            return true;

        // either manual spawn group and toggled, or not manual spawn group and not toggled...
        return _toggledSpawnGroupIds.Contains(groupId) != !data.Flags.HasAnyFlag(SpawnGroupFlags.ManualSpawn);
    }

    public bool IsUnderWater(PhaseShift phaseShift, float x, float y, float z)
    {
        return Terrain.IsUnderWater(phaseShift, Id, x, y, z);
    }

    public void LoadAllCells()
    {
        var manager = new LimitedThreadTaskManager(50);

        for (uint cellX = 0; cellX < MapConst.TotalCellsPerMap; cellX++)
            for (uint cellY = 0; cellY < MapConst.TotalCellsPerMap; cellY++)
                manager.Schedule(() =>
                                     LoadGrid((cellX + 0.5f - MapConst.CenterGridCellId) * MapConst.SizeofCells, (cellY + 0.5f - MapConst.CenterGridCellId) * MapConst.SizeofCells));

        manager.Wait();
    }

    public void LoadCorpseData()
    {
        var stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_CORPSES);
        stmt.AddValue(0, Id);
        stmt.AddValue(1, InstanceId);

        //        0     1     2     3            4      5          6          7     8      9       10     11        12    13          14          15
        // SELECT posX, posY, posZ, orientation, mapId, displayId, itemCache, race, class, gender, flags, dynFlags, time, corpseType, instanceId, guid FROM corpse WHERE mapId = ? AND instanceId = ?
        var result = CharacterDatabase.Query(stmt);

        if (result.IsEmpty())
            return;

        MultiMap<ulong, uint> phases = new();
        MultiMap<ulong, ChrCustomizationChoice> customizations = new();

        stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_CORPSE_PHASES);
        stmt.AddValue(0, Id);
        stmt.AddValue(1, InstanceId);

        //        0          1
        // SELECT OwnerGuid, PhaseId FROM corpse_phases cp LEFT JOIN corpse c ON cp.OwnerGuid = c.guid WHERE c.mapId = ? AND c.instanceId = ?
        var phaseResult = CharacterDatabase.Query(stmt);

        if (!phaseResult.IsEmpty())
            do
            {
                var guid = phaseResult.Read<ulong>(0);
                var phaseId = phaseResult.Read<uint>(1);

                phases.Add(guid, phaseId);
            } while (phaseResult.NextRow());

        stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_CORPSE_CUSTOMIZATIONS);
        stmt.AddValue(0, Id);
        stmt.AddValue(1, InstanceId);

        //        0             1                            2
        // SELECT cc.ownerGuid, cc.chrCustomizationOptionID, cc.chrCustomizationChoiceID FROM corpse_customizations cc LEFT JOIN corpse c ON cc.ownerGuid = c.guid WHERE c.mapId = ? AND c.instanceId = ?
        var customizationResult = CharacterDatabase.Query(stmt);

        if (!customizationResult.IsEmpty())
            do
            {
                var guid = customizationResult.Read<ulong>(0);

                ChrCustomizationChoice choice = new()
                {
                    ChrCustomizationOptionID = customizationResult.Read<uint>(1),
                    ChrCustomizationChoiceID = customizationResult.Read<uint>(2)
                };

                customizations.Add(guid, choice);
            } while (customizationResult.NextRow());

        do
        {
            var type = (CorpseType)result.Read<byte>(13);
            var guid = result.Read<ulong>(15);

            if (type is >= CorpseType.Max or CorpseType.Bones)
            {
                Log.Logger.Error("Corpse (guid: {0}) have wrong corpse type ({1}), not loading.", guid, type);

                continue;
            }

            var corpse = ClassFactory.ResolveWithPositionalParameters<Corpse>(type);

            if (!corpse.LoadCorpseFromDB(GenerateLowGuid(HighGuid.Corpse), result.GetFields()))
                continue;

            foreach (var phaseId in phases[guid])
                PhasingHandler.AddPhase(corpse, phaseId, false);

            corpse.SetCustomizations(customizations[guid]);

            AddCorpse(corpse);
        } while (result.NextRow());
    }

    public void LoadGrid(float x, float y)
    {
        EnsureGridLoaded(new Cell(x, y, GridDefines));
    }

    public void LoadGridForActiveObject(float x, float y, WorldObject obj)
    {
        EnsureGridLoadedForActiveObject(new Cell(x, y, GridDefines), obj);
    }

    public virtual void LoadGridObjects(Grid grid, Cell cell)
    {
        if (grid == null)
            return;

        ObjectGridLoader loader = new(grid, this, cell, GridType.Grid);
        loader.LoadN();
    }

    public void LoadRespawnTimes()
    {
        if (Instanceable)
            return;

        var stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_RESPAWNS);
        stmt.AddValue(0, Id);
        stmt.AddValue(1, InstanceId);
        var result = CharacterDatabase.Query(stmt);

        if (result.IsEmpty())
            return;

        do
        {
            var type = (SpawnObjectType)result.Read<ushort>(0);
            var spawnId = result.Read<ulong>(1);
            var respawnTime = result.Read<long>(2);

            if (SpawnMetadata.TypeHasData(type))
            {
                var data = GameObjectManager.SpawnDataCacheRouter.GetSpawnData(type, spawnId);

                if (data != null)
                    SaveRespawnTime(type, spawnId, data.Id, respawnTime, GridDefines.ComputeGridCoord(data.SpawnPoint.X, data.SpawnPoint.Y).GetId(), null, true);
                else
                    Log.Logger.Error($"Loading saved respawn time of {respawnTime} for spawnid ({type},{spawnId}) - spawn does not exist, ignoring");
            }
            else
                Log.Logger.Error($"Loading saved respawn time of {respawnTime} for spawnid ({type},{spawnId}) - invalid spawn type, ignoring");
        } while (result.NextRow());
    }

    //MapScript
    public void OnCreateMap(Map map)
    {
        var record = map.Entry;

        if (record != null && record.IsWorldMap())
            ScriptManager.ForEach<IMapOnCreate<Map>>(p => p.OnCreate(map));

        if (record != null && record.IsDungeon())
            ScriptManager.ForEach<IMapOnCreate<InstanceMap>>(p => p.OnCreate(map.ToInstanceMap));

        if (record != null && record.IsBattleground())
            ScriptManager.ForEach<IMapOnCreate<BattlegroundMap>>(p => p.OnCreate(map.ToBattlegroundMap));
    }

    public void OnDestroyMap(Map map)
    {
        var record = map.Entry;

        if (record != null && record.IsWorldMap())
            ScriptManager.ForEach<IMapOnDestroy<Map>>(p => p.OnDestroy(map));

        if (record != null && record.IsDungeon())
            ScriptManager.ForEach<IMapOnDestroy<InstanceMap>>(p => p.OnDestroy(map.ToInstanceMap));

        if (record != null && record.IsBattleground())
            ScriptManager.ForEach<IMapOnDestroy<BattlegroundMap>>(p => p.OnDestroy(map.ToBattlegroundMap));
    }

    public void OnMapUpdate(Map map, uint diff)
    {
        var record = map.Entry;

        if (record != null && record.IsWorldMap())
            ScriptManager.ForEach<IMapOnUpdate<Map>>(p => p.OnUpdate(map, diff));

        if (record != null && record.IsDungeon())
            ScriptManager.ForEach<IMapOnUpdate<InstanceMap>>(p => p.OnUpdate(map.ToInstanceMap, diff));

        if (record != null && record.IsBattleground())
            ScriptManager.ForEach<IMapOnUpdate<BattlegroundMap>>(p => p.OnUpdate(map.ToBattlegroundMap, diff));
    }

    public void OnPlayerEnterMap(Map map, Player player)
    {
        ScriptManager.ForEach<IPlayerOnMapChanged>(p => p.OnMapChanged(player));

        var record = map.Entry;

        if (record != null && record.IsWorldMap())
            ScriptManager.ForEach<IMapOnPlayerEnter<Map>>(p => p.OnPlayerEnter(map, player));

        if (record != null && record.IsDungeon())
            ScriptManager.ForEach<IMapOnPlayerEnter<InstanceMap>>(p => p.OnPlayerEnter(map.ToInstanceMap, player));

        if (record != null && record.IsBattleground())
            ScriptManager.ForEach<IMapOnPlayerEnter<BattlegroundMap>>(p => p.OnPlayerEnter(map.ToBattlegroundMap, player));
    }

    public void OnPlayerLeaveMap(Map map, Player player)
    {
        var record = map.Entry;

        if (record != null && record.IsWorldMap())
            ScriptManager.ForEach<IMapOnPlayerLeave<Map>>(p => p.OnPlayerLeave(map, player));

        if (record != null && record.IsDungeon())
            ScriptManager.ForEach<IMapOnPlayerLeave<InstanceMap>>(p => p.OnPlayerLeave(map.ToInstanceMap, player));

        if (record != null && record.IsBattleground())
            ScriptManager.ForEach<IMapOnPlayerLeave<BattlegroundMap>>(p => p.OnPlayerLeave(map.ToBattlegroundMap, player));
    }

    public void PlayerRelocation(Player player, Position pos)
    {
        PlayerRelocation(player, pos.X, pos.Y, pos.Z, pos.Orientation);
    }

    public void PlayerRelocation(Player player, float x, float y, float z, float orientation)
    {
        var oldcell = player.Location.GetCurrentCell();
        var newcell = new Cell(x, y, GridDefines);

        player.Location.Relocate(x, y, z, orientation);

        if (player.IsVehicle)
            player.VehicleKit.RelocatePassengers();

        if (oldcell == null || oldcell.DiffGrid(newcell) || oldcell.DiffCell(newcell))
        {
            RemoveFromGrid(player, oldcell);

            if (oldcell != null && oldcell.DiffGrid(newcell))
                EnsureGridLoadedForActiveObject(newcell, player);

            AddToGrid(player, newcell);
        }

        player.Location.UpdatePositionData();
        player.UpdateObjectVisibility(false);
    }

    public virtual void RemoveAllPlayers()
    {
        if (!HavePlayers)
            return;

        foreach (var pl in ActivePlayers.Where(pl => !pl.IsBeingTeleportedFar))
        {
            // this is happening for bg
            Log.Logger.Error($"Map.UnloadAll: player {pl.GetName()} is still in map {Id} during unload, this should not happen!");
            pl.TeleportTo(pl.Homebind);
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

                if (creature is { IsPet: false } && creature.SpawnId != 0)
                    respawnLocation = creature.RespawnPosition;

                break;

            case TypeId.GameObject:
                var gameObject = obj.AsGameObject;

                if (gameObject != null && gameObject.SpawnId != 0)
                    respawnLocation = gameObject.GetRespawnPosition();

                break;
        }

        if (respawnLocation == null)
            return;

        var p = GridDefines.ComputeGridCoord(respawnLocation.X, respawnLocation.Y);

        if (GetGrid(p.X, p.Y) != null)
            GetGrid(p.X, p.Y).GridInformation.DecUnloadActiveLock();
        else
        {
            var p2 = GridDefines.ComputeGridCoord(obj.Location.X, obj.Location.Y);
            Log.Logger.Debug($"Active object {obj.GUID} removed from grid[{p.X}, {p.Y}] but spawn grid[{p2.X}, {p2.Y}] was not loaded.");
        }
    }

    public void RemoveFromGrid(WorldObject obj, Cell cell)
    {
        if (cell == null)
            return;

        var grid = GetGrid(cell.Data.GridX, cell.Data.GridY);

        if (grid == null)
            return;

        if (obj.IsWorldObject())
            grid.GetGridCell(cell.Data.CellX, cell.Data.CellY).RemoveWorldObject(obj);
        else
            grid.GetGridCell(cell.Data.CellX, cell.Data.CellY).RemoveGridObject(obj);

        obj.Location.SetCurrentCell(null);
    }

    public void RemoveFromMap(WorldObject obj, bool remove)
    {
        var inWorld = obj.Location.IsInWorld && obj.TypeId is >= TypeId.Unit and <= TypeId.GameObject;
        obj.RemoveFromWorld();

        if (obj.IsActive)
            RemoveFromActive(obj);

        MultiPersonalPhaseTracker.UnregisterTrackedObject(obj);

        if (!inWorld) // if was in world, RemoveFromWorld() called DestroyForNearbyPlayers()
            obj.UpdateObjectVisibilityOnDestroy();

        var cell = obj.Location.GetCurrentCell();
        RemoveFromGrid(obj, cell);

        obj.Location.ResetMap();

        if (remove)
            DeleteFromWorld(obj);
    }

    public void RemoveFromMap(Transport obj, bool remove)
    {
        if (obj.Location.IsInWorld)
        {
            obj.RemoveFromWorld();

            UpdateData data = new(Id);

            if (obj.IsDestroyedObject)
                obj.BuildDestroyUpdateBlock(data);
            else
                obj.BuildOutOfRangeUpdateBlock(data);

            data.BuildPacket(out var packet);

            foreach (var player in Players.Where(player => player.Transport != obj && player.VisibleTransports.Contains(obj.GUID)))
            {
                player.SendPacket(packet);
                player.VisibleTransports.Remove(obj.GUID);
            }
        }

        if (!_transports.Contains(obj))
            return;

        _transports.Remove(obj);

        obj.Location.ResetMap();

        if (remove)
            DeleteFromWorld(obj);
    }

    public void RemoveGameObjectModel(GameObjectModel model)
    {
        _dynamicTree.Remove(model);
    }

    public void RemoveOldCorpses()
    {
        var now = GameTime.CurrentTime;

        var corpses = (from p in _corpsesByPlayer where p.Value.IsExpired(now) select p.Key).ToList();

        foreach (var ownerGuid in corpses)
            ConvertCorpseToBones(ownerGuid);

        var expiredBones = _corpseBones.Where(bones => bones.IsExpired(now)).ToList();

        foreach (var bones in expiredBones)
        {
            RemoveCorpse(bones);
            bones.Dispose();
        }
    }

    public virtual void RemovePlayerFromMap(Player player, bool remove)
    {
        // Before leaving map, update zone/area for stats
        player.UpdateZone(MapConst.InvalidZone, 0);
        OnPlayerLeaveMap(this, player);

        MultiPersonalPhaseTracker.MarkAllPhasesForDeletion(player.GUID);

        player.CombatStop();

        var inWorld = player.Location.IsInWorld;
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

    public void RemoveRespawnTime(SpawnObjectType type, ulong spawnId, SQLTransaction dbTrans = null, bool alwaysDeleteFromDB = false)
    {
        var info = GetRespawnInfo(type, spawnId);

        if (info != null)
            DeleteRespawnInfo(info, dbTrans);
        // Some callers might need to make sure the database doesn't contain any respawn time
        else if (alwaysDeleteFromDB)
            DeleteRespawnInfoFromDB(type, spawnId, dbTrans);
    }

    public void RemoveUpdateObject(WorldObject obj)
    {
        lock (_updateObjects)
            _updateObjects.Remove(obj);
    }

    public void RemoveWorldObject(WorldObject obj)
    {
        _worldObjects.Remove(obj);
    }

    public void ResetGridExpiry(Grid grid, float factor = 1)
    {
        grid.GridInformation.TimeTracker.Reset((uint)(_gridExpiry * factor));
    }

    public void Respawn(SpawnObjectType type, ulong spawnId, SQLTransaction dbTrans = null)
    {
        var info = GetRespawnInfo(type, spawnId);

        if (info != null)
            Respawn(info, dbTrans);
    }

    public void Respawn(RespawnInfo info, SQLTransaction dbTrans = null)
    {
        if (info.RespawnTime <= GameTime.CurrentTime)
            return;

        info.RespawnTime = GameTime.CurrentTime;
        SaveRespawnInfoDB(info, dbTrans);
    }

    public void SaveRespawnInfoDB(RespawnInfo info, SQLTransaction dbTrans = null)
    {
        if (Instanceable)
            return;

        var stmt = CharacterDatabase.GetPreparedStatement(CharStatements.REP_RESPAWN);
        stmt.AddValue(0, (ushort)info.ObjectType);
        stmt.AddValue(1, info.SpawnId);
        stmt.AddValue(2, info.RespawnTime);
        stmt.AddValue(3, Id);
        stmt.AddValue(4, InstanceId);
        CharacterDatabase.ExecuteOrAppend(dbTrans, stmt);
    }

    public void SaveRespawnTime(SpawnObjectType type, ulong spawnId, uint entry, long respawnTime, uint gridId = 0, SQLTransaction dbTrans = null, bool startup = false)
    {
        var data = GameObjectManager.SpawnDataCacheRouter.GetSpawnMetadata(type, spawnId);

        if (data == null)
        {
            Log.Logger.Error($"Map {Id} attempt to save respawn time for nonexistant spawnid ({type},{spawnId}).");

            return;
        }

        if (respawnTime == 0)
        {
            // Delete only
            RemoveRespawnTime(data.Type, data.SpawnId, dbTrans);

            return;
        }

        RespawnInfo ri = new()
        {
            ObjectType = data.Type,
            SpawnId = data.SpawnId,
            Entry = entry,
            RespawnTime = respawnTime,
            GridId = gridId
        };

        var success = AddRespawnInfo(ri);

        if (startup)
        {
            if (!success)
                Log.Logger.Error($"Attempt to load saved respawn {respawnTime} for ({type},{spawnId}) failed - duplicate respawn? Skipped.");
        }
        else if (success)
            SaveRespawnInfoDB(ri, dbTrans);
    }

    public void ScriptCommandStart(ScriptInfo script, uint delay, WorldObject source, WorldObject target)
    {
        // NOTE: script record _must_ exist until command executed

        // prepare static data
        var sourceGUID = source?.GUID ?? ObjectGuid.Empty;
        var targetGUID = target?.GUID ?? ObjectGuid.Empty;
        var ownerGUID = source != null && source.IsTypeMask(TypeMask.Item) ? ((Item)source).OwnerGUID : ObjectGuid.Empty;

        var sa = new ScriptAction
        {
            SourceGUID = sourceGUID,
            TargetGUID = targetGUID,
            OwnerGUID = ownerGUID,
            Script = script
        };

        _scriptSchedule.Add(GameTime.CurrentTime + delay, sa);

        MapManager.IncreaseScheduledScriptsCount();

        // If effects should be immediate, launch the script execution
        if (delay != 0)
            return;

        lock (_scriptLock)
            ScriptsProcess();
    }

    // Put scripts in the execution queue
    public void ScriptsStart(ScriptsType scriptsType, uint id, WorldObject source, WorldObject target)
    {
        var scripts = _scriptManager.GetScriptsMapByType(scriptsType);

        // Find the script map
        if (!scripts.TryGetValue(id, out var list))
            return;

        // prepare static data
        var sourceGUID = source?.GUID ?? ObjectGuid.Empty; //some script commands doesn't have source
        var targetGUID = target?.GUID ?? ObjectGuid.Empty;
        var ownerGUID = source != null && source.IsTypeMask(TypeMask.Item) ? ((Item)source).OwnerGUID : ObjectGuid.Empty;

        // Schedule script execution for all scripts in the script map
        var immedScript = false;

        foreach (var script in list.KeyValueList)
        {
            ScriptAction sa;
            sa.SourceGUID = sourceGUID;
            sa.TargetGUID = targetGUID;
            sa.OwnerGUID = ownerGUID;

            sa.Script = script.Value;
            _scriptSchedule.Add(GameTime.CurrentTime + script.Key, sa);

            if (script.Key == 0)
                immedScript = true;

            MapManager.IncreaseScheduledScriptsCount();
        }

        // If one of the effects should be immediate, launch the script execution
        if (!immedScript)
            return;

        lock (_scriptLock)
            ScriptsProcess();
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
            foreach (var passenger in transport.Passengers.Where(passenger => player != passenger && player.HaveAtClient(passenger)))
                passenger.BuildCreateUpdateBlockForPlayer(data, player);

        data.BuildPacket(out var packet);
        player.SendPacket(packet);
    }

    public void SendToPlayers(ServerPacket data)
    {
        foreach (var pl in ActivePlayers)
            pl.SendPacket(data);
    }

    public void SendUpdateTransportVisibility(Player player)
    {
        // Hack to send out transports
        UpdateData transData = new(player.Location.MapId);

        foreach (var transport in _transports)
        {
            if (!transport.Location.IsInWorld)
                continue;

            var hasTransport = player.VisibleTransports.Contains(transport.GUID);

            if (player.Location.InSamePhase(transport))
            {
                if (hasTransport)
                    continue;

                transport.BuildCreateUpdateBlockForPlayer(transData, player);
                player.VisibleTransports.Add(transport.GUID);
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

    public void SendZoneDynamicInfo(uint zoneId, Player player)
    {
        if (!_zoneDynamicInfo.TryGetValue(zoneId, out var zoneInfo))
            return;

        var music = zoneInfo.MusicId;

        if (music != 0)
            player.SendPacket(new PlayMusic(music));

        SendZoneWeather(zoneInfo, player);

        foreach (var overrideLight in zoneInfo.LightOverrides.Select(lightOverride => new OverrideLight
        {
            AreaLightID = lightOverride.AreaLightId,
            OverrideLightID = lightOverride.OverrideLightId,
            TransitionMilliseconds = lightOverride.TransitionMilliseconds
        }))
            player.SendPacket(overrideLight);
    }

    public void SendZoneWeather(uint zoneId, Player player)
    {
        if (player.HasAuraType(AuraType.ForceWeather))
            return;

        if (!_zoneDynamicInfo.TryGetValue(zoneId, out var zoneInfo))
            return;

        SendZoneWeather(zoneInfo, player);
    }

    public void SetSpawnGroupActive(uint groupId, bool state)
    {
        var data = GetSpawnGroupData(groupId);

        if (data == null || data.Flags.HasAnyFlag(SpawnGroupFlags.System))
        {
            Log.Logger.Error($"Tried to set non-existing (or system) spawn group {groupId} to {(state ? "active" : "inactive")} on map {Id}. Blocked.");

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

        var worldStateTemplate = WorldStateManager.GetWorldStateTemplate(worldStateId);

        if (worldStateTemplate != null)
            ScriptManager.RunScript<IWorldStateOnValueChange>(script => script.OnValueChange(worldStateTemplate.Id, oldValue, value, this), worldStateTemplate.ScriptId);

        // Broadcast update to all players on the map
        UpdateWorldState updateWorldState = new()
        {
            VariableID = (uint)worldStateId,
            Value = value,
            Hidden = hidden
        };

        updateWorldState.Write();

        foreach (var player in Players)
        {
            if (worldStateTemplate != null && !worldStateTemplate.AreaIds.Empty())
            {
                var isInAllowedArea = worldStateTemplate.AreaIds.Any(requiredAreaId => DB2Manager.IsInArea(player.Location.Area, requiredAreaId));

                if (!isInAllowedArea)
                    continue;
            }

            player.SendPacket(updateWorldState);
        }
    }

    public void SetZoneMusic(uint zoneId, uint musicId)
    {
        if (!_zoneDynamicInfo.ContainsKey(zoneId))
            _zoneDynamicInfo[zoneId] = new ZoneDynamicInfo();

        _zoneDynamicInfo[zoneId].MusicId = musicId;

        var players = Players;

        if (players.Empty())
            return;

        PlayMusic playMusic = new(musicId);

        foreach (var player in players.Where(player => player.Location.Zone == zoneId && !player.HasAuraType(AuraType.ForceWeather)))
            player.SendPacket(playMusic);
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
            ZoneDynamicInfo.LightOverride lightOverride = new()
            {
                AreaLightId = areaLightId,
                OverrideLightId = overrideLightId,
                TransitionMilliseconds = (uint)transitionTime.TotalMilliseconds
            };

            info.LightOverrides.Add(lightOverride);
        }

        var players = Players;

        if (players.Empty())
            return;

        OverrideLight overrideLight = new()
        {
            AreaLightID = areaLightId,
            OverrideLightID = overrideLightId,
            TransitionMilliseconds = (uint)transitionTime.TotalMilliseconds
        };

        foreach (var player in players.Where(player => player.Location.Zone == zoneId))
            player.SendPacket(overrideLight);
    }

    public void SetZoneWeather(uint zoneId, WeatherState weatherId, float intensity)
    {
        if (!_zoneDynamicInfo.ContainsKey(zoneId))
            _zoneDynamicInfo[zoneId] = new ZoneDynamicInfo();

        var info = _zoneDynamicInfo[zoneId];
        info.WeatherId = weatherId;
        info.Intensity = intensity;

        var players = Players;

        if (players.Empty())
            return;

        WeatherPkt weather = new(weatherId, intensity);

        foreach (var player in players.Where(player => player.Location.Zone == zoneId))
            player.SendPacket(weather);
    }

    public bool ShouldBeSpawnedOnGridLoad<T>(ulong spawnId)
    {
        return ShouldBeSpawnedOnGridLoad(SpawnData.TypeFor<T>(), spawnId);
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
            Log.Logger.Error($"Tried to despawn non-existing (or system) spawn group {groupId} on map {Id}. Blocked.");

            return false;
        }

        foreach (var data in MapSpawnGroupCache.SpawnGroupMapStorage.LookupByKey(groupId))
        {
            if (deleteRespawnTimes)
                RemoveRespawnTime(data.Type, data.SpawnId);

            count += DespawnAll(data.Type, data.SpawnId);
        }

        SetSpawnGroupActive(groupId, false); // stop processing respawns for the group, too

        return true;
    }

    public bool SpawnGroupSpawn(uint groupId, bool ignoreRespawn = false, bool force = false, List<WorldObject> spawnedObjects = null)
    {
        var groupData = GetSpawnGroupData(groupId);

        if (groupData == null || groupData.Flags.HasAnyFlag(SpawnGroupFlags.System))
        {
            Log.Logger.Error($"Tried to spawn non-existing (or system) spawn group {groupId}. on map {Id} Blocked.");

            return false;
        }

        SetSpawnGroupActive(groupId, true); // start processing respawns for the group

        List<SpawnData> toSpawn = new();

        foreach (var data in MapSpawnGroupCache.SpawnGroupMapStorage.LookupByKey(groupId))
        {
            var respawnMap = GetRespawnMapForType(data.Type);

            if (respawnMap == null)
                continue;

            if (force || ignoreRespawn)
                RemoveRespawnTime(data.Type, data.SpawnId);

            var hasRespawnTimer = respawnMap.ContainsKey(data.SpawnId);

            if (!SpawnMetadata.TypeHasData(data.Type))
                continue;

            // has a respawn timer
            if (hasRespawnTimer)
                continue;

            // has a spawn already active
            if (!force)
            {
                var obj = GetWorldObjectBySpawnId(data.Type, data.SpawnId);

                if (obj != null)
                    if (data.Type != SpawnObjectType.Creature || obj.AsCreature.IsAlive)
                        continue;
            }

            toSpawn.Add(data.ToSpawnData());
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
                    var creature = ClassFactory.Resolve<Creature>();

                    if (!creature.LoadFromDB(data.SpawnId, this, true, force))
                        creature.Dispose();
                    else spawnedObjects?.Add(creature);

                    break;
                }
                case SpawnObjectType.GameObject:
                {
                    var gameobject = ClassFactory.Resolve<GameObject>();

                    if (!gameobject.LoadFromDB(data.SpawnId, this, true))
                        gameobject.Dispose();
                    else spawnedObjects?.Add(gameobject);

                    break;
                }
                case SpawnObjectType.AreaTrigger:
                {
                    var areaTrigger = ClassFactory.Resolve<AreaTrigger>();

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

        var summon = mask switch
        {
            UnitTypeMask.Summon => ClassFactory.ResolveWithPositionalParameters<TempSummon>(properties, summonerUnit, false),
            UnitTypeMask.Guardian => ClassFactory.ResolveWithPositionalParameters<Guardian>(properties, summonerUnit, false),
            UnitTypeMask.Puppet => ClassFactory.ResolveWithPositionalParameters<Puppet>(properties, summonerUnit),
            UnitTypeMask.Totem => ClassFactory.ResolveWithPositionalParameters<Totem>(properties, summonerUnit),
            UnitTypeMask.Minion => ClassFactory.ResolveWithPositionalParameters<Minion>(properties, summonerUnit, false),
            // ReSharper disable once UnreachableSwitchArmDueToIntegerAnalysis
            _ => ClassFactory.ResolveWithPositionalParameters<TempSummon>(properties, summonerUnit, false)
        };

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
        summon.Location.UpdateAllowedPositionZ(pos);
        summon.HomePosition = pos;
        summon.InitStats(duration);
        summon.PrivateObjectOwner = privateObjectOwner;

        if (smoothPhasingInfo != null)
        {
            if (summoner != null && smoothPhasingInfo.ReplaceObject.HasValue)
            {
                var replacedObject = ObjectAccessor.GetWorldObject(summoner, smoothPhasingInfo.ReplaceObject.Value);

                if (replacedObject != null)
                {
                    smoothPhasingInfo.ReplaceObject = summon.GUID;
                    replacedObject.Visibility.GetOrCreateSmoothPhasing().SetViewerDependentInfo(privateObjectOwner, smoothPhasingInfo);

                    summon.DemonCreatorGUID = privateObjectOwner;
                }
            }

            summon.Visibility.GetOrCreateSmoothPhasing().SetSingleInfo(smoothPhasingInfo);
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
        CellCalculator.VisitGrid(summon, notifier, VisibilityRange);

        return summon;
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
            corpse.Location.ResetMap();
            corpse.Dispose();
        }

        _corpsesByCell.Clear();
        _corpsesByPlayer.Clear();
        _corpseBones.Clear();
    }

    public bool UnloadGrid(Grid grid, bool unloadAll)
    {
        var x = grid.X;
        var y = grid.Y;

        if (!unloadAll)
        {
            //pets, possessed creatures (must be active), transport passengers
            if (grid.GetWorldObjectCountInNGrid<Creature>() != 0)
                return false;

            if (ActiveObjectsNearGrid(grid))
                return false;
        }

        Log.Logger.Debug("Unloading grid[{0}, {1}] for map {2}", x, y, Id);

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
            Grids.Remove(x, y);

        var gx = (int)(MapConst.MaxGrids - 1 - x);
        var gy = (int)(MapConst.MaxGrids - 1 - y);

        Terrain.UnloadMap(gx, gy);

        Log.Logger.Debug("Unloading grid[{0}, {1}] for map {2} finished", x, y, Id);

        return true;
    }

    public virtual void Update(uint diff)
    {
        _dynamicTree.Update(diff);

        // update worldsessions for existing players
        for (var i = 0; i < ActivePlayers.Count; ++i)
        {
            var player = ActivePlayers[i];

            if (!player.Location.IsInWorld)
                continue;

            var session = player.Session;
            _threadManager.Schedule(() => session.UpdateMap(diff));
        }

        // process any due respawns
        if (_respawnCheckTimer <= diff)
        {
            _threadManager.Schedule(ProcessRespawns);
            _threadManager.Schedule(UpdateSpawnGroupConditions);
            _respawnCheckTimer = Configuration.GetDefaultValue("Respawn:MinCheckIntervalMS", 5000u);
        }
        else
            _respawnCheckTimer -= diff;

        _threadManager.Wait();

        // update active cells around players and active objects
        ResetMarkedCells();

        var update = new UpdaterNotifier(diff, GridType.All);

        for (var i = 0; i < ActivePlayers.Count; ++i)
        {
            var player = ActivePlayers[i];

            if (!player.Location.IsInWorld)
                continue;

            // update players at tick
            _threadManager.Schedule(() => player.Update(diff));

            _threadManager.Schedule(() => VisitNearbyCellsOf(player, update));

            // If player is using far sight or mind vision, visit that object too

            if (player.Viewpoint != null)
                _threadManager.Schedule(() => VisitNearbyCellsOf(player.Viewpoint, update));

            List<Unit> toVisit = new();

            // Handle updates for creatures in combat with player and are more than 60 yards away
            if (player.IsInCombat)
            {
                foreach (var pair in player.CombatManager.PvECombatRefs)
                {
                    var unit = pair.Value.GetOther(player).AsCreature;

                    if (unit == null)
                        continue;

                    if (unit.Location.MapId == player.Location.MapId && !unit.Location.IsWithinDistInMap(player, VisibilityRange, false))
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

                      if (caster == null)
                          return;

                      if (!caster.Location.IsWithinDistInMap(player, VisibilityRange, false))
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

                    if (unit == null)
                        continue;

                    if (unit.Location.MapId == player.Location.MapId && !unit.Location.IsWithinDistInMap(player, VisibilityRange, false))
                        toVisit.Add(unit);
                }

            foreach (var unit in toVisit)
                _threadManager.Schedule(() => VisitNearbyCellsOf(unit, update));
        }

        for (var i = 0; i < _activeNonPlayers.Count; ++i)
        {
            var obj = _activeNonPlayers[i];

            if (!obj.Location.IsInWorld)
                continue;

            _threadManager.Schedule(() => VisitNearbyCellsOf(obj, update));
        }

        _threadManager.Wait();

        update.ExecuteUpdate();

        for (var i = 0; i < _transports.Count; ++i)
        {
            var transport = _transports[i];

            if (transport == null)
                continue;

            _processTransportaionQueue.Schedule(() => transport.Update(diff));
        }

        SendObjectUpdates();

        // Process necessary scripts
        if (!_scriptSchedule.Empty())
            lock (_scriptLock)
                ScriptsProcess();

        _weatherUpdateTimer.Update(diff);

        if (_weatherUpdateTimer.Passed)
        {
            foreach (var zoneInfo in _zoneDynamicInfo)
                if (zoneInfo.Value.DefaultWeather != null && !zoneInfo.Value.DefaultWeather.Update((uint)_weatherUpdateTimer.Interval))
                    zoneInfo.Value.DefaultWeather = null;

            _weatherUpdateTimer.Reset();
        }

        // update phase shift objects
        MultiPersonalPhaseTracker.Update(this, diff);

        MoveAllCreaturesInMoveList();
        MoveAllGameObjectsInMoveList();
        MoveAllAreaTriggersInMoveList();

        if (!ActivePlayers.Empty() || !_activeNonPlayers.Empty())
            _processRelocationQueue.Post(diff);

        OnMapUpdate(this, diff);
    }

    public void UpdateAreaDependentAuras()
    {
        var players = Players;

        foreach (var player in players.Where(player => player != null && player.Location.IsInWorld))
        {
            player.UpdateAreaDependentAuras(player.Location.Area);
            player.UpdateZoneDependentAuras(player.Location.Zone);
        }
    }

    public void UpdatePersonalPhasesForPlayer(Player player)
    {
        Cell cell = new(player.Location.X, player.Location.Y, GridDefines);
        MultiPersonalPhaseTracker.OnOwnerPhaseChanged(player, GetGrid(cell.Data.GridX, cell.Data.GridY), this, cell);
    }

    public void UpdatePlayerZoneStats(uint oldZone, uint newZone)
    {
        // Nothing to do if no change
        if (oldZone == newZone)
            return;

        if (oldZone != MapConst.InvalidZone)
            --_zonePlayerCountMap[oldZone];

        if (!_zonePlayerCountMap.ContainsKey(newZone))
            _zonePlayerCountMap[newZone] = 0;

        ++_zonePlayerCountMap[newZone];
    }

    public void UpdateSpawnGroupConditions()
    {
        var spawnGroups = GameObjectManager.MapSpawnGroupCache.GetSpawnGroupsForMap(Id);

        foreach (var spawnGroupId in spawnGroups)
        {
            var spawnGroupTemplate = GetSpawnGroupData(spawnGroupId);

            var isActive = IsSpawnGroupActive(spawnGroupId);
            var shouldBeActive = ConditionManager.IsMapMeetingNotGroupedConditions(ConditionSourceType.SpawnGroup, spawnGroupId, this);

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

    public void Visit(Cell cell, IGridNotifier visitor)
    {
        var x = cell.Data.GridX;
        var y = cell.Data.GridY;
        var cellX = cell.Data.CellX;
        var cellY = cell.Data.CellY;

        if (cell.Data.NoCreate && !IsGridLoaded(x, y))
            return;

        EnsureGridLoaded(cell);
        GetGrid(x, y).VisitGrid(cellX, cellY, visitor);
    }

    private static void PushRespawnInfoFrom(List<RespawnInfo> data, Dictionary<ulong, RespawnInfo> map)
    {
        data.AddRange(map.Select(pair => pair.Value));
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

    private void AddCreatureToMoveList(Creature c, float x, float y, float z, float ang)
    {
        lock (_creaturesToMove)
        {
            if (c.Location.MoveState == ObjectCellMoveState.None)
                _creaturesToMove.Add(c);

            c.Location.SetNewCellPosition(x, y, z, ang);
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

    private void AddGameObjectToMoveList(GameObject go, float x, float y, float z, float ang)
    {
        lock (_gameObjectsToMove)
        {
            if (go.Location.MoveState == ObjectCellMoveState.None)
                _gameObjectsToMove.Add(go);

            go.Location.SetNewCellPosition(x, y, z, ang);
        }
    }

    private bool AddRespawnInfo(RespawnInfo info)
    {
        if (info.SpawnId == 0)
        {
            Log.Logger.Error($"Attempt to insert respawn info for zero spawn id (type {info.ObjectType})");

            return false;
        }

        var bySpawnIdMap = GetRespawnMapForType(info.ObjectType);

        if (bySpawnIdMap == null)
            return false;

        // check if we already have the maximum possible number of respawns scheduled
        if (SpawnMetadata.TypeHasData(info.ObjectType))
        {
            if (bySpawnIdMap.TryGetValue(info.SpawnId, out var existing)) // spawnid already has a respawn scheduled
            {
                if (info.RespawnTime <= existing.RespawnTime) // delete existing in this case
                    DeleteRespawnInfo(existing);
                else
                    return false;
            }
        }
        else
            return false;

        RespawnInfo ri = new(info);
        _respawnTimes.Add(ri);
        bySpawnIdMap.Add(ri.SpawnId, ri);

        return true;
    }

    private void AddToActiveHelper(WorldObject obj)
    {
        _activeNonPlayers.Add(obj);
    }

    private bool AreaTriggerCellRelocation(AreaTrigger at, Cell newCell)
    {
        return MapObjectCellRelocation(at, newCell);
    }

    private bool CheckRespawn(RespawnInfo info)
    {
        var data = GameObjectManager.SpawnDataCacheRouter.GetSpawnData(info.ObjectType, info.SpawnId);

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
                var isEscort = Configuration.GetDefaultValue("Respawn:DynamicEscortNPC", false) && data.SpawnGroupData.Flags.HasFlag(SpawnGroupFlags.EscortQuestNpc);

                var range = CreatureBySpawnIdStore.LookupByKey(info.SpawnId);

                alreadyExists = range.Where(creature => creature.IsAlive).Any(creature => !isEscort || !creature.IsEscorted);

                break;
            }
            case SpawnObjectType.GameObject:
                // gameobject check is simpler - they cannot be dead or escorting
                if (GameObjectBySpawnIdStore.ContainsKey(info.SpawnId))
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

        if (linkedTime == 0)
            return true;

        var now = GameTime.CurrentTime;
        long respawnTime;

        if (linkedTime == long.MaxValue)
            respawnTime = linkedTime;
        else if (CreatureDataCache.GetLinkedRespawnGuid(thisGUID) == thisGUID) // never respawn, save "something" in DB
            respawnTime = now + Time.WEEK;
        else // set us to check again shortly after linked unit
            respawnTime = Math.Max(now, linkedTime) + RandomHelper.URand(5, 15);

        info.RespawnTime = respawnTime;

        return false;

        // everything ok, let's spawn
    }

    private bool CreatureCellRelocation(Creature c, Cell newCell)
    {
        return MapObjectCellRelocation(c, newCell);
    }

    private void DeleteFromWorld(Player player)
    {
        ObjectAccessor.RemoveObject(player);
        RemoveUpdateObject(player); // @todo I do not know why we need this, it should be removed in ~Object anyway
        player.Dispose();
    }

    private void DeleteFromWorld(WorldObject obj)
    {
        obj.Dispose();
    }

    private void DeleteRespawnInfo(RespawnInfo info, SQLTransaction dbTrans = null)
    {
        // spawnid store
        var spawnMap = GetRespawnMapForType(info.ObjectType);

        if (spawnMap == null)
            return;

        spawnMap.LookupByKey(info.SpawnId);
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

        var stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_RESPAWN);
        stmt.AddValue(0, (ushort)type);
        stmt.AddValue(1, spawnId);
        stmt.AddValue(2, Id);
        stmt.AddValue(3, InstanceId);
        CharacterDatabase.ExecuteOrAppend(dbTrans, stmt);
    }

    private int DespawnAll(SpawnObjectType type, ulong spawnId)
    {
        List<WorldObject> toUnload = new();

        switch (type)
        {
            case SpawnObjectType.Creature:
                toUnload.AddRange(CreatureBySpawnIdStore.LookupByKey(spawnId));

                break;

            case SpawnObjectType.GameObject:
                toUnload.AddRange(GameObjectBySpawnIdStore.LookupByKey(spawnId));

                break;
        }

        foreach (var o in toUnload)
            AddObjectToRemoveList(o);

        return toUnload.Count;
    }

    private void DoRespawn(SpawnObjectType type, ulong spawnId, uint gridId)
    {
        if (!IsGridLoaded(gridId)) // if grid isn't loaded, this will be processed in grid load handler
            return;

        switch (type)
        {
            case SpawnObjectType.Creature:
            {
                var obj = ClassFactory.Resolve<Creature>();

                if (!obj.LoadFromDB(spawnId, this, true, true))
                    obj.Dispose();

                break;
            }
            case SpawnObjectType.GameObject:
            {
                var obj = ClassFactory.Resolve<GameObject>();

                if (!obj.LoadFromDB(spawnId, this, true))
                    obj.Dispose();

                break;
            }
        }
    }

    private void EnsureGridCreated(GridCoord p)
    {
        object lockobj = null;

        lock (_locks)
            lockobj = _locks.GetOrAdd(p.X, p.Y, () => new object());

        lock (lockobj)
        {
            if (GetGrid(p.X, p.Y) != null)
                return;

            Log.Logger.Debug("Creating grid[{0}, {1}] for map {2} instance {3}", p.X, p.Y, Id, InstanceIdInternal);

            var grid = new Grid(p.X * MapConst.MaxGrids + p.Y, p.X, p.Y, _gridExpiry, Configuration.GetDefaultValue("GridUnload", true))
            {
                GridState = GridState.Idle
            };

            SetGrid(grid, p.X, p.Y);

            //z coord
            var gx = (int)(MapConst.MaxGrids - 1 - p.X);
            var gy = (int)(MapConst.MaxGrids - 1 - p.Y);

            if (gx > -1 && gy > -1)
                Terrain.LoadMapAndVMap(gx, gy);
        }
    }

    private void EnsureGridLoaded(Cell cell)
    {
        EnsureGridCreated(new GridCoord(cell.Data.GridX, cell.Data.GridY));
        var grid = GetGrid(cell.Data.GridX, cell.Data.GridY);

        if (grid == null || IsGridObjectDataLoaded(cell.Data.GridX, cell.Data.GridY))
            return;

        Log.Logger.Debug("Loading grid[{0}, {1}] for map {2} instance {3}",
                         cell.Data.GridX,
                         cell.Data.GridY,
                         Id,
                         InstanceIdInternal);

        SetGridObjectDataLoaded(true, cell.Data.GridX, cell.Data.GridY);

        LoadGridObjects(grid, cell);

        Balance();
    }

    private void EnsureGridLoadedForActiveObject(Cell cell, WorldObject obj)
    {
        EnsureGridLoaded(cell);
        var grid = GetGrid(cell.Data.GridX, cell.Data.GridY);

        if (obj.IsPlayer)
            MultiPersonalPhaseTracker.LoadGrid(obj.Location.PhaseShift, grid, this, cell);

        // refresh grid state & timer
        if (grid.GridState == GridState.Active)
            return;

        Log.Logger.Debug("Active object {0} triggers loading of grid [{1}, {2}] on map {3}",
                         obj.GUID,
                         cell.Data.GridX,
                         cell.Data.GridY,
                         Id);

        ResetGridExpiry(grid, 0.1f);
        grid.GridState = GridState.Active;
    }

    private GameObject FindGameObject(WorldObject searchObject, ulong guid)
    {
        return searchObject.Location.Map.GameObjectBySpawnIdStore.TryGetValue(guid, out var bounds) ? null : bounds[0];
    }

    private bool GameObjectCellRelocation(GameObject go, Cell newCell)
    {
        return MapObjectCellRelocation(go, newCell);
    }

    private Grid GetGrid(uint x, uint y)
    {
        if (x > MapConst.MaxGrids || y > MapConst.MaxGrids)
            return null;

        lock (Grids)
            if (Grids.TryGetValue(x, out var ygrid) && ygrid.TryGetValue(y, out var grid))
                return grid;

        return null;
    }

    private ObjectGuidGenerator GetGuidSequenceGenerator(HighGuid high)
    {
        if (!_guidGenerators.ContainsKey(high))
            _guidGenerators[high] = ClassFactory.Resolve<ObjectGuidGenerator>(new PositionalParameter(0, high), new PositionalParameter(1, 1));

        return _guidGenerators[high];
    }

    private Dictionary<ulong, RespawnInfo> GetRespawnMapForType(SpawnObjectType type)
    {
        return type switch
        {
            SpawnObjectType.Creature => _creatureRespawnTimesBySpawnId,
            SpawnObjectType.GameObject => _gameObjectRespawnTimesBySpawnId,
            SpawnObjectType.AreaTrigger => null,
            _ => null
        };
    }

    private Creature GetScriptCreatureSourceOrTarget(WorldObject source, WorldObject target, ScriptInfo scriptInfo, bool bReverse = false)
    {
        Creature creature = null;

        if (source == null && target == null)
            Log.Logger.Error("{0} source and target objects are NULL.", scriptInfo.GetDebugInfo());
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
                Log.Logger.Error("{0} neither source nor target are creatures (source: TypeId: {1}, Entry: {2}, {3}; target: TypeId: {4}, Entry: {5}, {6}), skipping.",
                                 scriptInfo.GetDebugInfo(),
                                 source?.TypeId ?? 0,
                                 source?.Entry ?? 0,
                                 source != null ? source.GUID.ToString() : "",
                                 target?.TypeId ?? 0,
                                 target?.Entry ?? 0,
                                 target != null ? target.GUID.ToString() : "");
        }

        return creature;
    }

    private GameObject GetScriptGameObjectSourceOrTarget(WorldObject source, WorldObject target, ScriptInfo scriptInfo, bool bReverse)
    {
        GameObject gameobject = null;

        if (source == null && target == null)
            Log.Logger.Error($"{scriptInfo.GetDebugInfo()} source and target objects are NULL.");
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
                Log.Logger.Error($"{scriptInfo.GetDebugInfo()} neither source nor target are gameobjects " +
                                 $"(source: TypeId: {source?.TypeId ?? 0}, Entry: {source?.Entry ?? 0}, {source?.GUID ?? ObjectGuid.Empty}; " +
                                 $"target: TypeId: {target?.TypeId ?? 0}, Entry: {target?.Entry ?? 0}, {target?.GUID ?? ObjectGuid.Empty}), skipping.");
        }

        return gameobject;
    }

    // Helpers for ScriptProcess method.
    private Player GetScriptPlayerSourceOrTarget(WorldObject source, WorldObject target, ScriptInfo scriptInfo)
    {
        Player player = null;

        if (source == null && target == null)
            Log.Logger.Error("{0} source and target objects are NULL.", scriptInfo.GetDebugInfo());
        else
        {
            // Check target first, then source.
            if (target != null)
                player = target.AsPlayer;

            if (player == null && source != null)
                player = source.AsPlayer;

            if (player == null)
                Log.Logger.Error("{0} neither source nor target object is player (source: TypeId: {1}, Entry: {2}, {3}; target: TypeId: {4}, Entry: {5}, {6}), skipping.",
                                 scriptInfo.GetDebugInfo(),
                                 source?.TypeId ?? 0,
                                 source?.Entry ?? 0,
                                 source != null ? source.GUID.ToString() : "",
                                 target?.TypeId ?? 0,
                                 target?.Entry ?? 0,
                                 target != null ? target.GUID.ToString() : "");
        }

        return player;
    }

    private SpawnGroupTemplateData GetSpawnGroupData(uint groupId)
    {
        var data = GameObjectManager.SpawnGroupDataCache.GetSpawnGroupData(groupId);

        if (data != null && (data.Flags.HasAnyFlag(SpawnGroupFlags.System) || data.MapId == Id))
            return data;

        return null;
    }

    private void InitializeObject(WorldObject obj)
    {
        if (!obj.IsTypeId(TypeId.Unit) || !obj.IsTypeId(TypeId.GameObject))
            return;

        obj.Location.MoveState = ObjectCellMoveState.None;
    }

    private bool IsCellMarked(uint pCellId)
    {
        return _markedCells.Get((int)pCellId);
    }

    private bool IsGridObjectDataLoaded(uint x, uint y)
    {
        var grid = GetGrid(x, y);

        return grid is { IsGridObjectDataLoaded: true };
    }

    private bool MapObjectCellRelocation<T>(T obj, Cell newCell) where T : WorldObject
    {
        var oldCell = obj.Location.GetCurrentCell();

        if (!oldCell.DiffGrid(newCell)) // in same grid
        {
            // if in same cell then none do
            if (!oldCell.DiffCell(newCell))
                return true;

            RemoveFromGrid(obj, oldCell);
            AddToGrid(obj, newCell);

            return true;
        }

        // in diff. grids but active creature
        if (obj.IsActive)
        {
            EnsureGridLoadedForActiveObject(newCell, obj);

            Log.Logger.Debug("Active creature (GUID: {0} Entry: {1}) moved from grid[{2}, {3}] to grid[{4}, {5}].",
                             obj.GUID.ToString(),
                             obj.Entry,
                             oldCell.Data.GridX,
                             oldCell.Data.GridY,
                             newCell.Data.GridX,
                             newCell.Data.GridY);

            RemoveFromGrid(obj, oldCell);
            AddToGrid(obj, newCell);

            return true;
        }

        var c = obj.AsCreature;

        if (c is { CharmerOrOwnerGUID.IsPlayer: true })
            EnsureGridLoaded(newCell);

        // in diff. loaded grid normal creature
        var grid = new GridCoord(newCell.Data.GridX, newCell.Data.GridY);

        if (!IsGridLoaded(grid))
            return false;

        RemoveFromGrid(obj, oldCell);
        EnsureGridCreated(grid);
        AddToGrid(obj, newCell);

        return true;

        // fail to move: normal creature attempt move to unloaded grid
    }

    private void MarkCell(uint pCellId)
    {
        _markedCells.Set((int)pCellId, true);
    }

    private void MoveAllAreaTriggersInMoveList()
    {
        lock (_areaTriggersToMove)
            for (var i = 0; i < _areaTriggersToMove.Count; ++i)
            {
                var at = _areaTriggersToMove[i];

                if (at.Location.Map != this) //transport is teleported to another map
                    continue;

                if (at.Location.MoveState != ObjectCellMoveState.Active)
                {
                    at.Location.MoveState = ObjectCellMoveState.None;

                    continue;
                }

                at.Location.MoveState = ObjectCellMoveState.None;

                if (!at.Location.IsInWorld)
                    continue;

                // do move or do move to respawn or remove creature if previous all fail
                if (AreaTriggerCellRelocation(at, new Cell(at.Location.NewPosition.X, at.Location.NewPosition.Y, GridDefines)))
                {
                    // update pos
                    at.Location.Relocate(at.Location.NewPosition);
                    at.UpdateShape();
                    at.UpdateObjectVisibility(false);
                }
                else
                    Log.Logger.Debug("AreaTrigger ({0}) cannot be moved to unloaded grid.", at.GUID.ToString());
            }
    }

    private void MoveAllCreaturesInMoveList()
    {
        lock (_creaturesToMove)
            for (var i = 0; i < _creaturesToMove.Count; ++i)
            {
                var creature = _creaturesToMove[i];

                if (creature.Location.Map != this) //pet is teleported to another map
                    continue;

                if (creature.Location.MoveState != ObjectCellMoveState.Active)
                {
                    creature.Location.MoveState = ObjectCellMoveState.None;

                    continue;
                }

                creature.Location.MoveState = ObjectCellMoveState.None;

                if (!creature.Location.IsInWorld)
                    continue;

                // do move or do move to respawn or remove creature if previous all fail
                if (CreatureCellRelocation(creature, new Cell(creature.Location.NewPosition.X, creature.Location.NewPosition.Y, GridDefines)))
                {
                    // update pos
                    creature.Location.Relocate(creature.Location.NewPosition);

                    if (creature.IsVehicle)
                        creature.VehicleKit.RelocatePassengers();

                    creature.Location.UpdatePositionData();
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

    private void MoveAllGameObjectsInMoveList()
    {
        lock (_gameObjectsToMove)
            for (var i = 0; i < _gameObjectsToMove.Count; ++i)
            {
                var go = _gameObjectsToMove[i];

                if (go.Location.Map != this) //transport is teleported to another map
                    continue;

                if (go.Location.MoveState != ObjectCellMoveState.Active)
                {
                    go.Location.MoveState = ObjectCellMoveState.None;

                    continue;
                }

                go.Location.MoveState = ObjectCellMoveState.None;

                if (!go.Location.IsInWorld)
                    continue;

                // do move or do move to respawn or remove creature if previous all fail
                if (GameObjectCellRelocation(go, new Cell(go.Location.NewPosition.X, go.Location.NewPosition.Y, GridDefines)))
                {
                    // update pos
                    go.Location.Relocate(go.Location.NewPosition);
                    go.AfterRelocation();
                }
                else
                {
                    // if GameObject can't be move in new cell/grid (not loaded) move it to repawn cell/grid
                    // GameObject coordinates will be updated and notifiers send
                    if (GameObjectRespawnRelocation(go, false))
                        continue;

                    // ... or unload (if respawn grid also not loaded)
                    Log.Logger.Debug("GameObject (GUID: {0} Entry: {1}) cannot be move to unloaded respawn grid.",
                                     go.GUID.ToString(),
                                     go.Entry);

                    AddObjectToRemoveList(go);
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

                if (grid is not { GridState: GridState.Active })
                    continue;

                grid.GridInformation.RelocationTimer.Modify((int)diff);

                if (!grid.GridInformation.RelocationTimer.Passed())
                    continue;

                var gx = grid.X;
                var gy = grid.Y;

                var cellMin = new CellCoord(gx * MapConst.MaxCells, gy * MapConst.MaxCells);
                var cellMax = new CellCoord(cellMin.X + MapConst.MaxCells, cellMin.Y + MapConst.MaxCells);

                for (var xx = cellMin.X; xx < cellMax.X; ++xx)
                {
                    for (var yy = cellMin.Y; yy < cellMax.Y; ++yy)
                    {
                        var cellID = yy * MapConst.TotalCellsPerMap + xx;

                        if (!IsCellMarked(cellID))
                            continue;

                        var pair = new CellCoord(xx, yy);
                        var cell = new Cell(pair, GridDefines);
                        cell.Data.NoCreate = true;

                        var cellRelocation = new DelayedUnitRelocation(cell, pair, this, SharedConst.MaxVisibilityDistance, GridType.All, ObjectAccessor);

                        Visit(cell, cellRelocation);
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

                if (grid is not { GridState: GridState.Active })
                    continue;

                if (!grid.GridInformation.RelocationTimer.Passed())
                    continue;

                grid.GridInformation.RelocationTimer.Reset((int)diff, VisibilityNotifyPeriod);

                var gx = grid.X;
                var gy = grid.Y;

                var cellMin = new CellCoord(gx * MapConst.MaxCells, gy * MapConst.MaxCells);

                var cellMax = new CellCoord(cellMin.X + MapConst.MaxCells,
                                            cellMin.Y + MapConst.MaxCells);

                for (var xx = cellMin.X; xx < cellMax.X; ++xx)
                {
                    for (var yy = cellMin.Y; yy < cellMax.Y; ++yy)
                    {
                        var cellID = yy * MapConst.TotalCellsPerMap + xx;

                        if (!IsCellMarked(cellID))
                            continue;

                        var pair = new CellCoord(xx, yy);
                        var cell = new Cell(pair, GridDefines);
                        cell.Data.NoCreate = true;
                        Visit(cell, reset);
                    }
                }
            }
        }
    }

    private void ProcessRespawns()
    {
        var now = GameTime.CurrentTime;

        while (!_respawnTimes.Empty())
        {
            var next = _respawnTimes.First();

            if (now < next.RespawnTime) // done for this tick
                break;

            var poolId = PoolManager.IsPartOfAPool(next.ObjectType, next.SpawnId);

            if (poolId != 0) // is this part of a pool?
            {
                // if yes, respawn will be handled by (external) pooling logic, just delete the respawn time
                // step 1: remove entry from maps to avoid it being reachable by outside logic
                _respawnTimes.Remove(next);
                GetRespawnMapForType(next.ObjectType).Remove(next.SpawnId);

                // step 2: tell pooling logic to do its thing
                PoolManager.UpdatePool(PoolData, poolId, next.ObjectType, next.SpawnId);

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
                // new respawn time, update heap position
                SaveRespawnInfoDB(next);
        }
    }

    private void RemoveAllObjectsInRemoveList()
    {
        while (!_objectsToSwitch.Empty())
        {
            var (obj, on) = _objectsToSwitch.First();
            _objectsToSwitch.Remove(obj);

            if (!obj.IsPermanentWorldObject)
                switch (obj.TypeId)
                {
                    case TypeId.Unit:
                        SwitchGridContainers(obj.AsCreature, on);

                        break;
                }
        }

        lock (_objectsToRemove)
            while (!_objectsToRemove.Empty())
            {
                var obj = _objectsToRemove.First();

                switch (obj.TypeId)
                {
                    case TypeId.Corpse:
                    {
                        var corpse = ObjectAccessor.GetCorpse(obj, obj.GUID);

                        if (corpse == null)
                            Log.Logger.Error("Tried to delete corpse/bones {0} that is not in map.", obj.GUID.ToString());
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

                        if (transport != null)
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
                        Log.Logger.Error("Non-grid object (TypeId: {0}) is in grid object remove list, ignored.", obj.TypeId);

                        break;
                }

                _objectsToRemove.Remove(obj);
            }
    }

    private void RemoveAreaTriggerFromMoveList(AreaTrigger at)
    {
        lock (_areaTriggersToMove)
        {
            if (at.Location.MoveState == ObjectCellMoveState.Active)
                at.Location.MoveState = ObjectCellMoveState.Inactive;

            _areaTriggersToMove.Remove(at);
        }
    }

    private void RemoveCorpse(Corpse corpse)
    {
        corpse.UpdateObjectVisibilityOnDestroy();

        if (corpse.Location.GetCurrentCell() != null)
            RemoveFromMap(corpse, false);
        else
        {
            corpse.RemoveFromWorld();
            corpse.Location.ResetMap();
        }

        _corpsesByCell.Remove(corpse.CellCoord.GetId(), corpse);

        if (corpse.CorpseType != CorpseType.Bones)
            _corpsesByPlayer.Remove(corpse.OwnerGUID);
        else
            _corpseBones.Remove(corpse);
    }

    private void RemoveCreatureFromMoveList(Creature c)
    {
        lock (_creaturesToMove)
            if (c.Location.MoveState == ObjectCellMoveState.Active)
                c.Location.MoveState = ObjectCellMoveState.Inactive;
    }

    private void RemoveDynamicObjectFromMoveList(DynamicObject dynObj)
    {
        lock (_dynamicObjectsToMove)
            if (dynObj.Location.MoveState == ObjectCellMoveState.Active)
                dynObj.Location.MoveState = ObjectCellMoveState.Inactive;
    }

    private void RemoveFromActiveHelper(WorldObject obj)
    {
        _activeNonPlayers.Remove(obj);
    }

    private void RemoveGameObjectFromMoveList(GameObject go)
    {
        lock (_gameObjectsToMove)
            if (go.Location.MoveState == ObjectCellMoveState.Active)
                go.Location.MoveState = ObjectCellMoveState.Inactive;
    }

    private void ResetMarkedCells()
    {
        _markedCells.SetAll(false);
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
                Log.Logger.Error("{0} unknown command for _ScriptProcessDoor.", scriptInfo.GetDebugInfo());

                return;
        }

        if (guid == 0)
            Log.Logger.Error("{0} door guid is not specified.", scriptInfo.GetDebugInfo());
        else if (source == null)
            Log.Logger.Error("{0} source object is NULL.", scriptInfo.GetDebugInfo());
        else if (!source.IsTypeMask(TypeMask.Unit))
            Log.Logger.Error("{0} source object is not unit (TypeId: {1}, Entry: {2}, GUID: {3}), skipping.",
                             scriptInfo.GetDebugInfo(),
                             source.TypeId,
                             source.Entry,
                             source.GUID.ToString());
        else
        {
            var pDoor = FindGameObject(source, guid);

            if (pDoor == null)
                Log.Logger.Error("{0} gameobject was not found (guid: {1}).", scriptInfo.GetDebugInfo(), guid);
            else if (pDoor.GoType != GameObjectTypes.Door)
                Log.Logger.Error("{0} gameobject is not a door (GoType: {1}, Entry: {2}, GUID: {3}).", scriptInfo.GetDebugInfo(), pDoor.GoType, pDoor.Entry, pDoor.GUID.ToString());
            else if (bOpen == (pDoor.GoState == GameObjectState.Ready))
            {
                pDoor.UseDoorOrButton((uint)nTimeToToggle);

                if (target != null && target.IsTypeMask(TypeMask.GameObject))
                {
                    var goTarget = target.AsGameObject;

                    if (goTarget is { GoType: GameObjectTypes.Button })
                        goTarget.UseDoorOrButton((uint)nTimeToToggle);
                }
            }
        }
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
            if (iter.Key > GameTime.CurrentTime)
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
                            var player = ObjectAccessor.GetPlayer(this, step.OwnerGUID);

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
                            source = ObjectAccessor.GetPlayer(this, step.SourceGUID);

                            break;

                        case HighGuid.GameObject:
                        case HighGuid.Transport:
                            source = GetGameObject(step.SourceGUID);

                            break;

                        case HighGuid.Corpse:
                            source = GetCorpse(step.SourceGUID);

                            break;

                        default:
                            Log.Logger.Error("{0} source with unsupported high guid (GUID: {1}, high guid: {2}).",
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
                            target = ObjectAccessor.GetPlayer(this, step.TargetGUID);

                            break;

                        case HighGuid.GameObject:
                        case HighGuid.Transport:
                            target = GetGameObject(step.TargetGUID);

                            break;

                        case HighGuid.Corpse:
                            target = GetCorpse(step.TargetGUID);

                            break;

                        default:
                            Log.Logger.Error("{0} target with unsupported high guid {1}.", step.Script.GetDebugInfo(), step.TargetGUID.ToString());

                            break;
                    }

                switch (step.Script.command)
                {
                    case ScriptCommands.Talk:
                    {
                        if (step.Script.Talk.ChatType > ChatMsg.Whisper && step.Script.Talk.ChatType != ChatMsg.RaidBossWhisper)
                        {
                            Log.Logger.Error("{0} invalid chat type ({1}) specified, skipping.",
                                             step.Script.GetDebugInfo(),
                                             step.Script.Talk.ChatType);

                            break;
                        }

                        if (step.Script.Talk.Flags.HasAnyFlag(eScriptFlags.TalkUsePlayer))
                            source = GetScriptPlayerSourceOrTarget(source, target, step.Script);
                        else
                            source = GetScriptCreatureSourceOrTarget(source, target, step.Script);

                        if (source != null)
                        {
                            var sourceUnit = source.AsUnit;

                            if (sourceUnit == null)
                            {
                                Log.Logger.Error("{0} source object ({1}) is not an unit, skipping.", step.Script.GetDebugInfo(), source.GUID.ToString());

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
                                    var receiver = target?.AsPlayer;

                                    if (receiver == null)
                                        Log.Logger.Error("{0} attempt to whisper to non-player unit, skipping.", step.Script.GetDebugInfo());
                                    else
                                        sourceUnit.Whisper((uint)step.Script.Talk.TextID, receiver, step.Script.Talk.ChatType == ChatMsg.RaidBossWhisper);

                                    break;
                                }
                                // must be already checked at load
                            }
                        }

                        break;
                    }
                    case ScriptCommands.Emote:
                    {
                        // Source or target must be Creature.
                        var cSource = GetScriptCreatureSourceOrTarget(source, target, step.Script);

                        if (cSource != null)
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

                        if (cSource != null)
                        {
                            var unit = cSource.AsUnit;

                            if (step.Script.MoveTo.TravelTime != 0)
                            {
                                var speed =
                                    unit.Location.GetDistance(step.Script.MoveTo.DestX,
                                                              step.Script.MoveTo.DestY,
                                                              step.Script.MoveTo.DestZ) /
                                    (step.Script.MoveTo.TravelTime * 0.001f);

                                unit.MonsterMoveWithSpeed(step.Script.MoveTo.DestX,
                                                          step.Script.MoveTo.DestY,
                                                          step.Script.MoveTo.DestZ,
                                                          speed);
                            }
                            else
                                unit.NearTeleportTo(step.Script.MoveTo.DestX,
                                                    step.Script.MoveTo.DestY,
                                                    step.Script.MoveTo.DestZ,
                                                    unit.Location.Orientation);
                        }

                        break;
                    }
                    case ScriptCommands.TeleportTo:
                    {
                        if (step.Script.TeleportTo.Flags.HasAnyFlag(eScriptFlags.TeleportUseCreature))
                        {
                            // Source or target must be Creature.
                            var cSource = GetScriptCreatureSourceOrTarget(source, target, step.Script);

                            if (cSource != null)
                                cSource.NearTeleportTo(step.Script.TeleportTo.DestX,
                                                       step.Script.TeleportTo.DestY,
                                                       step.Script.TeleportTo.DestZ,
                                                       step.Script.TeleportTo.Orientation);
                        }
                        else
                        {
                            // Source or target must be Player.
                            var player = GetScriptPlayerSourceOrTarget(source, target, step.Script);

                            if (player != null)
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
                        if (source == null)
                        {
                            Log.Logger.Error("{0} source object is NULL.", step.Script.GetDebugInfo());

                            break;
                        }

                        if (target == null)
                        {
                            Log.Logger.Error("{0} target object is NULL.", step.Script.GetDebugInfo());

                            break;
                        }

                        // when script called for item spell casting then target == (unit or GO) and source is player
                        WorldObject worldObject;
                        var player = target.AsPlayer;

                        if (player != null)
                        {
                            if (!source.IsTypeId(TypeId.Unit) && !source.IsTypeId(TypeId.GameObject) && !source.IsTypeId(TypeId.Player))
                            {
                                Log.Logger.Error("{0} source is not unit, gameobject or player (TypeId: {1}, Entry: {2}, GUID: {3}), skipping.",
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
                                    Log.Logger.Error("{0} target is not unit, gameobject or player (TypeId: {1}, Entry: {2}, GUID: {3}), skipping.",
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
                                Log.Logger.Error("{0} neither source nor target is player (Entry: {1}, GUID: {2}; target: Entry: {3}, GUID: {4}), skipping.",
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
                             worldObject.Location.IsWithinDistInMap(player, step.Script.QuestExplored.Distance)))
                            player.AreaExploredOrEventHappens(step.Script.QuestExplored.QuestID);
                        else
                            player.FailQuest(step.Script.QuestExplored.QuestID);

                        break;
                    }

                    case ScriptCommands.KillCredit:
                    {
                        // Source or target must be Player.
                        var player = GetScriptPlayerSourceOrTarget(source, target, step.Script);

                        if (player != null)
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
                            Log.Logger.Error("{0} gameobject guid (datalong) is not specified.", step.Script.GetDebugInfo());

                            break;
                        }

                        // Source or target must be WorldObject.

                        if (source != null)
                        {
                            var pGO = FindGameObject(source, step.Script.RespawnGameObject.GOGuid);

                            if (pGO == null)
                            {
                                Log.Logger.Error("{0} gameobject was not found (guid: {1}).", step.Script.GetDebugInfo(), step.Script.RespawnGameObject.GOGuid);

                                break;
                            }

                            if (pGO.GoType is GameObjectTypes.FishingNode or GameObjectTypes.Door or GameObjectTypes.Button or GameObjectTypes.Trap)
                            {
                                Log.Logger.Error("{0} can not be used with gameobject of type {1} (guid: {2}).",
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

                                pGO.Location.Map.AddToMap(pGO);
                            }
                        }

                        break;
                    }
                    case ScriptCommands.TempSummonCreature:
                    {
                        // Source must be WorldObject.
                        if (source != null)
                        {
                            if (step.Script.TempSummonCreature.CreatureEntry == 0)
                                Log.Logger.Error("{0} creature entry (datalong) is not specified.", step.Script.GetDebugInfo());
                            else
                            {
                                var x = step.Script.TempSummonCreature.PosX;
                                var y = step.Script.TempSummonCreature.PosY;
                                var z = step.Script.TempSummonCreature.PosZ;
                                var o = step.Script.TempSummonCreature.Orientation;

                                if (source.SummonCreature(step.Script.TempSummonCreature.CreatureEntry, new Position(x, y, z, o), TempSummonType.TimedOrDeadDespawn, TimeSpan.FromMilliseconds(step.Script.TempSummonCreature.DespawnDelay)) == null)
                                    Log.Logger.Error("{0} creature was not spawned (entry: {1}).", step.Script.GetDebugInfo(), step.Script.TempSummonCreature.CreatureEntry);
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
                        var unit = source.AsUnit;

                        if (unit != null)
                        {
                            // Target must be GameObject.
                            if (target == null)
                            {
                                Log.Logger.Error("{0} target object is NULL.", step.Script.GetDebugInfo());

                                break;
                            }

                            if (!target.IsTypeId(TypeId.GameObject))
                            {
                                Log.Logger.Error("{0} target object is not gameobject (TypeId: {1}, Entry: {2}, GUID: {3}), skipping.",
                                                 step.Script.GetDebugInfo(),
                                                 target.TypeId,
                                                 target.Entry,
                                                 target.GUID.ToString());

                                break;
                            }

                            var pGO = target.AsGameObject;

                            pGO?.Use(unit);
                        }

                        break;
                    }
                    case ScriptCommands.RemoveAura:
                    {
                        // Source (datalong2 != 0) or target (datalong2 == 0) must be Unit.
                        var bReverse = step.Script.RemoveAura.Flags.HasAnyFlag(eScriptFlags.RemoveauraReverse);
                        var unit = bReverse ? source.AsUnit : target.AsUnit;
                        unit?.RemoveAura(step.Script.RemoveAura.SpellID);

                        break;
                    }
                    case ScriptCommands.CastSpell:
                    {
                        if (source == null && target == null)
                        {
                            Log.Logger.Error("{0} source and target objects are NULL.", step.Script.GetDebugInfo());

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
                                uTarget = uSource.Location?.FindNearestCreature((uint)Math.Abs(step.Script.CastSpell.CreatureEntry), step.Script.CastSpell.SearchRadius);

                                break;
                        }

                        if (uSource == null)
                        {
                            Log.Logger.Error("{0} no source worldobject found for spell {1}", step.Script.GetDebugInfo(), step.Script.CastSpell.SpellID);

                            break;
                        }

                        if (uTarget == null)
                        {
                            Log.Logger.Error("{0} no target worldobject found for spell {1}", step.Script.GetDebugInfo(), step.Script.CastSpell.SpellID);

                            break;
                        }

                        var triggered = (int)step.Script.CastSpell.Flags != 4
                                            ? step.Script.CastSpell.CreatureEntry.HasAnyFlag((int)eScriptFlags.CastspellTriggered)
                                            : step.Script.CastSpell.CreatureEntry < 0;

                        uSource.SpellFactory.CastSpell(uTarget, step.Script.CastSpell.SpellID, triggered);

                        break;
                    }

                    case ScriptCommands.PlaySound:
                        // Source must be WorldObject.

                        if (source != null)
                        {
                            // PlaySound.Flags bitmask: 0/1=anyone/target
                            Player player2 = null;

                            if (step.Script.PlaySound.Flags.HasAnyFlag(eScriptFlags.PlaysoundTargetPlayer))
                            {
                                // Target must be Player.
                                player2 = target.AsPlayer;

                                if (target == null)
                                    break;
                            }

                            // PlaySound.Flags bitmask: 0/2=without/with distance dependent
                            if (step.Script.PlaySound.Flags.HasAnyFlag(eScriptFlags.PlaysoundDistanceSound))
                                source.PlayDistanceSound(step.Script.PlaySound.SoundID, player2);
                            else
                                source.PlayDirectSound(step.Script.PlaySound.SoundID, player2);
                        }

                        break;

                    case ScriptCommands.CreateItem:
                        // Target or source must be Player.
                        var pReceiver = GetScriptPlayerSourceOrTarget(source, target, step.Script);

                        if (pReceiver != null)
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
                                pReceiver.SendEquipError(msg, null, null, step.Script.CreateItem.ItemEntry);
                        }

                        break;

                    case ScriptCommands.DespawnSelf:
                    {
                        // First try with target or source creature, then with target or source gameobject
                        var cSource = GetScriptCreatureSourceOrTarget(source, target, step.Script, true);

                        if (cSource != null)
                            cSource.DespawnOrUnsummon(TimeSpan.FromMilliseconds(step.Script.DespawnSelf.DespawnDelay));
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
                        var unit = source.AsUnit;

                        if (unit != null)
                        {
                            if (WaypointManager.GetPath(step.Script.LoadPath.PathID) == null)
                                Log.Logger.Error("{0} source object has an invalid path ({1}), skipping.", step.Script.GetDebugInfo(), step.Script.LoadPath.PathID);
                            else
                                unit.MotionMaster.MovePath(step.Script.LoadPath.PathID, step.Script.LoadPath.IsRepeatable != 0);
                        }

                        break;
                    }
                    case ScriptCommands.CallscriptToUnit:
                    {
                        if (step.Script.CallScript.CreatureEntry == 0)
                        {
                            Log.Logger.Error("{0} creature entry is not specified, skipping.", step.Script.GetDebugInfo());

                            break;
                        }

                        if (step.Script.CallScript.ScriptID == 0)
                        {
                            Log.Logger.Error("{0} script id is not specified, skipping.", step.Script.GetDebugInfo());

                            break;
                        }

                        Creature cTarget = null;

                        if (CreatureBySpawnIdStore.TryGetValue(step.Script.CallScript.CreatureEntry, out var creatureBounds))
                        {
                            // Prefer alive (last respawned) creature
                            var foundCreature = creatureBounds.Find(creature => creature.IsAlive);

                            cTarget = foundCreature ?? creatureBounds[0];
                        }

                        if (cTarget == null)
                        {
                            Log.Logger.Error("{0} target was not found (entry: {1})", step.Script.GetDebugInfo(), step.Script.CallScript.CreatureEntry);

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

                        if (cSource != null)
                        {
                            if (cSource.IsDead)
                                Log.Logger.Error("{0} creature is already dead (Entry: {1}, GUID: {2})", step.Script.GetDebugInfo(), cSource.Entry, cSource.GUID.ToString());
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
                        var sourceUnit = source.AsUnit;

                        if (sourceUnit != null)
                        {
                            if (step.Script.Orientation.Flags.HasAnyFlag(eScriptFlags.OrientationFaceTarget))
                            {
                                // Target must be Unit.
                                var targetUnit = target.AsUnit;

                                if (targetUnit == null)
                                    break;

                                sourceUnit.SetFacingToObject(targetUnit);
                            }
                            else
                                sourceUnit.SetFacingTo(step.Script.Orientation._Orientation);
                        }

                        break;
                    }
                    case ScriptCommands.Equip:
                    {
                        // Source must be Creature.
                        source.AsCreature?.LoadEquipment((int)step.Script.Equip.EquipmentID);

                        break;
                    }
                    case ScriptCommands.Model:
                    {
                        // Source must be Creature.
                        source.AsCreature?.SetDisplayId(step.Script.Model.ModelID);

                        break;
                    }
                    case ScriptCommands.CloseGossip:
                    {
                        // Source must be Player.
                        source.AsPlayer?.PlayerTalkClass.SendCloseGossip();

                        break;
                    }
                    case ScriptCommands.Playmovie:
                    {
                        // Source must be Player.
                        source.AsPlayer?.SendMovieStart(step.Script.PlayMovie.MovieID);

                        break;
                    }
                    case ScriptCommands.Movement:
                    {
                        // Source must be Creature.
                        var cSource = source.AsCreature;

                        if (cSource != null)
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
                        source.AsCreature?.PlayOneShotAnimKitId((ushort)step.Script.PlayAnimKit.AnimKitID);

                        break;
                    }
                    default:
                        Log.Logger.Error("Unknown script command {0}.", step.Script.GetDebugInfo());

                        break;
                }

                MapManager.DecreaseScheduledScriptCount();
            }

            _scriptSchedule.Remove(iter.Key);
            iter = _scriptSchedule.FirstOrDefault();
        }
    }

    private void SendInitTransports(Player player)
    {
        var transData = new UpdateData(Id);

        foreach (var transport in _transports.Where(transport => transport.Location.IsInWorld && transport != player.Transport && player.Location.InSamePhase(transport)))
        {
            transport.BuildCreateUpdateBlockForPlayer(transData, player);
            player.VisibleTransports.Add(transport.GUID);
        }

        transData.BuildPacket(out var packet);
        player.SendPacket(packet);
    }

    private void SendObjectUpdates()
    {
        Dictionary<Player, UpdateData> updatePlayers = new();

        lock (_updateObjects)
            while (!_updateObjects.Empty())
            {
                var obj = _updateObjects[0];
                _updateObjects.RemoveAt(0);
                obj.BuildUpdate(updatePlayers);
            }

        foreach (var iter in updatePlayers)
        {
            iter.Value.BuildPacket(out var packet);
            iter.Key.SendPacket(packet);
        }
    }

    private void SendRemoveTransports(Player player)
    {
        var transData = new UpdateData(player.Location.MapId);

        foreach (var transport in _transports.Where(transport => player.VisibleTransports.Contains(transport.GUID) && transport != player.Transport))
        {
            transport.BuildOutOfRangeUpdateBlock(transData);
            player.VisibleTransports.Remove(transport.GUID);
        }

        transData.BuildPacket(out var packet);
        player.SendPacket(packet);
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
            zoneDynamicInfo.DefaultWeather.SendWeatherUpdateToPlayer(player);
        else
            Weather.SendFineWeatherUpdateToPlayer(player);
    }

    private void SetGrid(Grid grid, uint x, uint y)
    {
        if (x >= MapConst.MaxGrids || y >= MapConst.MaxGrids)
        {
            Log.Logger.Error("Map.setNGrid Invalid grid coordinates found: {0}, {1}!", x, y);

            return;
        }

        lock (Grids)
            Grids.Add(x, y, grid);
    }

    private void SetGridObjectDataLoaded(bool pLoaded, uint x, uint y)
    {
        var grid = GetGrid(x, y);

        if (grid != null)
            grid.IsGridObjectDataLoaded = pLoaded;
    }

    private bool ShouldBeSpawnedOnGridLoad(SpawnObjectType type, ulong spawnId)
    {
        // check if the object is on its respawn timer
        if (GetRespawnTime(type, spawnId) != 0)
            return false;

        var spawnData = GameObjectManager.SpawnDataCacheRouter.GetSpawnMetadata(type, spawnId);
        // check if the object is part of a spawn group
        var spawnGroup = spawnData.SpawnGroupData;

        if (spawnGroup.Flags.HasFlag(SpawnGroupFlags.System))
            return spawnData.ToSpawnData().PoolId == 0 || PoolData.IsSpawnedObject(type, spawnId);

        if (!IsSpawnGroupActive(spawnGroup.GroupId))
            return false;

        return spawnData.ToSpawnData().PoolId == 0 || PoolData.IsSpawnedObject(type, spawnId);
    }

    private void SwitchGridContainers(WorldObject obj, bool on)
    {
        if (obj.IsPermanentWorldObject)
            return;

        var p = GridDefines.ComputeCellCoord(obj.Location.X, obj.Location.Y);

        if (!p.IsCoordValid())
        {
            Log.Logger.Error("Map.SwitchGridContainers: Object {0} has invalid coordinates X:{1} Y:{2} grid cell [{3}:{4}]",
                             obj.GUID,
                             obj.Location.X,
                             obj.Location.Y,
                             p.X,
                             p.Y);

            return;
        }

        var cell = new Cell(p, GridDefines);

        if (!IsGridLoaded(cell.Data.GridX, cell.Data.GridY))
            return;

        Log.Logger.Debug("Switch object {0} from grid[{1}, {2}] {3}", obj.GUID, cell.Data.GridX, cell.Data.GridY, on);
        var ngrid = GetGrid(cell.Data.GridX, cell.Data.GridY);

        RemoveFromGrid(obj, cell);

        var gridCell = ngrid.GetGridCell(cell.Data.CellX, cell.Data.CellY);

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

    private void UnloadAllRespawnInfos() // delete everything from memory
    {
        _respawnTimes.Clear();
        _creatureRespawnTimesBySpawnId.Clear();
        _gameObjectRespawnTimesBySpawnId.Clear();
    }

    private void VisitNearbyCellsOf(WorldObject obj, IGridNotifier gridVisitor)
    {
        // Check for valid position
        if (!obj.GridDefines.IsValidMapCoord(obj.Location))
            return;

        // Update mobs/objects in ALL visible cells around object!
        var area = CellCalculator.CalculateCellArea(obj.Location.X, obj.Location.Y, obj.GridActivationRange);

        for (var x = area.LowBound.X; x <= area.HighBound.X; ++x)
        {
            for (var y = area.LowBound.Y; y <= area.HighBound.Y; ++y)
            {
                // marked cells are those that have been visited
                // don't visit the same cell twice
                var cellID = y * MapConst.TotalCellsPerMap + x;

                if (IsCellMarked(cellID))
                    continue;

                MarkCell(cellID);
                var pair = new CellCoord(x, y);
                var cell = new Cell(pair, GridDefines);
                cell.Data.NoCreate = true;
                Visit(cell, gridVisitor);
            }
        }
    }
}