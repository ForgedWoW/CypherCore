// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Numerics;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.GameObjects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Maps;
using Forged.MapServer.Maps.Checks;
using Forged.MapServer.Maps.GridNotifiers;
using Forged.MapServer.Maps.Grids;
using Forged.MapServer.Maps.Instances;
using Forged.MapServer.Phasing;
using Framework.Constants;
using Framework.Util;
using Serilog;

namespace Forged.MapServer.Entities.Objects;

public class WorldLocation : Position
{
    private readonly CellCalculator _cellCalculator;
    private readonly WorldObject _worldObject;
    private readonly PhasingHandler _phasingHandler;
    private Cell _currentCell;
    private uint _instanceId;
    private uint _mapId;
    private PhaseShift _phaseShift = new();
    private float _staticFloorZ = MapConst.VMAPInvalidHeightValue;
    private PhaseShift _suppressedPhaseShift = new(); // contains phases for current area but not applied due to conditions

    public WorldLocation(WorldObject obj)
    {
        _worldObject = obj;
        _cellCalculator = obj.ClassFactory.Resolve<CellCalculator>();
        GridDefines = obj.ClassFactory.Resolve<GridDefines>();
        _phasingHandler = obj.ClassFactory.Resolve<PhasingHandler>();
    }

    public WorldLocation(WorldObject obj, Map map, Position pos)
    {
        _worldObject = obj;
        _cellCalculator = obj.ClassFactory.Resolve<CellCalculator>();
        GridDefines = obj.ClassFactory.Resolve<GridDefines>();
        _phasingHandler = obj.ClassFactory.Resolve<PhasingHandler>();
        Map = map;
        Relocate(pos);
    }

    public WorldLocation(uint mapId = 0xFFFFFFFF, float x = 0, float y = 0, float z = 0, float o = 0)
    {
        _mapId = mapId;
        Relocate(x, y, z, o);
    }

    public WorldLocation(uint mapId, Position pos)
    {
        _mapId = mapId;
        Relocate(pos);
    }

    public WorldLocation(WorldLocation loc)
    {
        _mapId = loc.MapId;
        Map = loc.Map;
        Relocate(loc);
    }

    public WorldLocation(Position pos)
    {
        _mapId = 0xFFFFFFFF;
        Relocate(pos);
    }

    public uint Area { get; set; }
    public float CollisionHeight => 0.0f;
    public int DBPhase { get; set; }
    public float FloorZ => !IsInWorld ? _staticFloorZ : Math.Max(_staticFloorZ, Map.GetGameObjectFloor(PhaseShift, X, Y, Z + MapConst.ZOffsetFindHeight));
    public GridDefines GridDefines { get; }

    public uint InstanceId
    {
        get => Map?.InstanceId ?? _instanceId;
        set => _instanceId = value;
    }

    public InstanceScript InstanceScript => Map is { IsDungeon: true } ? ((InstanceMap)Map).InstanceScript : null;
    public bool IsInWater => LiquidStatus.HasAnyFlag(ZLiquidStatus.InWater | ZLiquidStatus.UnderWater);
    public bool IsInWorld { get; set; }
    public bool IsOutdoors { get; private set; }
    public bool IsUnderWater => LiquidStatus.HasFlag(ZLiquidStatus.UnderWater);
    public ZLiquidStatus LiquidStatus { get; private set; }
    public Map Map { get; set; }
    public uint MapId => Map?.Id ?? _mapId;
    public ObjectCellMoveState MoveState { get; set; }

    public Position NewPosition { get; set; } = new();

    public PhaseShift PhaseShift
    {
        get => _phaseShift;
        set => _phaseShift = new PhaseShift(value);
    }

    public PhaseShift SuppressedPhaseShift
    {
        get => _suppressedPhaseShift;
        set => _suppressedPhaseShift = new PhaseShift(value);
    }

    public uint Zone { get; set; }

    public static bool InSamePhase(WorldObject a, WorldObject b)
    {
        return a != null && b != null && a.Location.InSamePhase(b);
    }

    public void AddObjectToRemoveList()
    {
        var map = Map;

        if (map == null)
        {
            Log.Logger.Error("Object (TypeId: {0} Entry: {1} GUID: {2}) at attempt add to move list not have valid map (Id: {3}).", _worldObject.TypeId, _worldObject.Entry, _worldObject.GUID.ToString(), MapId);

            return;
        }

        map.AddObjectToRemoveList(_worldObject);
    }

    public Creature FindNearestCreature(uint entry, float range, bool alive = true)
    {
        var checker = new NearestCreatureEntryWithLiveStateInObjectRangeCheck(_worldObject, entry, alive, range);
        var searcher = new CreatureLastSearcher(_worldObject, checker, GridType.All);

        _cellCalculator.VisitGrid(_worldObject, searcher, range);

        return searcher.Target;
    }

    public Creature FindNearestCreatureWithOptions(float range, FindCreatureOptions options)
    {
        NearestCheckCustomizer checkCustomizer = new(_worldObject, range);
        CreatureWithOptionsInObjectRangeCheck<NearestCheckCustomizer> checker = new(_worldObject, checkCustomizer, options);
        CreatureLastSearcher searcher = new(_worldObject, checker, GridType.All);

        if (options.IgnorePhases)
            searcher.PhaseShift = _phasingHandler.GetAlwaysVisiblePhaseShift();

        _cellCalculator.VisitGrid(_worldObject, searcher, range);

        return searcher.Target;
    }

    public GameObject FindNearestGameObject(uint entry, float range, bool spawnedOnly = true)
    {
        var checker = new NearestGameObjectEntryInObjectRangeCheck(_worldObject, entry, range, spawnedOnly);
        var searcher = new GameObjectLastSearcher(_worldObject, checker, GridType.Grid);

        _cellCalculator.VisitGrid(_worldObject, searcher, range);

        return searcher.GetTarget();
    }

    public GameObject FindNearestGameObjectOfType(GameObjectTypes type, float range)
    {
        var checker = new NearestGameObjectTypeInObjectRangeCheck(_worldObject, type, range);
        var searcher = new GameObjectLastSearcher(_worldObject, checker, GridType.Grid);

        _cellCalculator.VisitGrid(_worldObject, searcher, range);

        return searcher.GetTarget();
    }

    public Player FindNearestPlayer(float range, bool alive = true)
    {
        var check = new AnyPlayerInObjectRangeCheck(_worldObject, _worldObject.Visibility.VisibilityRange);
        var searcher = new PlayerSearcher(_worldObject, check, GridType.Grid);
        _cellCalculator.VisitGrid(_worldObject, searcher, range);

        return searcher.GetTarget();
    }

    public GameObject FindNearestUnspawnedGameObject(uint entry, float range)
    {
        NearestUnspawnedGameObjectEntryInObjectRangeCheck checker = new(_worldObject, entry, range);
        GameObjectLastSearcher searcher = new(_worldObject, checker, GridType.Grid);

        _cellCalculator.VisitGrid(_worldObject, searcher, range);

        return searcher.GetTarget();
    }

    public ZoneScript FindZoneScript()
    {
        var map = Map;

        if (map == null)
            return null;

        var instanceMap = map.ToInstanceMap;

        if (instanceMap != null)
            return instanceMap.InstanceScript;

        var bgMap = map.ToBattlegroundMap;

        if (bgMap != null)
            return bgMap.BG;

        if (map.IsBattlegroundOrArena)
            return null;

        var bf = _worldObject.BattleFieldManager.GetBattlefieldToZoneId(map, Zone);

        if (bf != null)
            return bf;

        return _worldObject.OutdoorPvPManager.GetOutdoorPvPToZoneId(map, Zone);
    }

    public void GetAlliesWithinRange(List<Unit> unitList, float maxSearchRange, bool includeSelf = true)
    {
        var pair = new CellCoord((uint)X, (uint)Y);
        var cell = new Cell(pair, GridDefines);
        cell.Data.NoCreate = true;

        var check = new AnyFriendlyUnitInObjectRangeCheck(_worldObject, _worldObject.AsUnit, maxSearchRange);
        var searcher = new UnitListSearcher(_worldObject, unitList, check, GridType.All);

        cell.Visit(pair, searcher, Map, _worldObject, maxSearchRange);

        if (!includeSelf)
            unitList.Remove(_worldObject.AsUnit);
    }

    public void GetAlliesWithinRangeWithOwnedAura(List<Unit> unitList, float maxSearchRange, uint auraId, bool includeSelf = true)
    {
        GetAlliesWithinRange(unitList, maxSearchRange, includeSelf);

        unitList.RemoveIf(creature => !creature.HasAura(auraId, _worldObject.GUID));
    }

    public void GetClosePoint(Position pos, float size, float distance2d = 0, float relAngle = 0)
    {
        // angle calculated from current orientation
        GetNearPoint(null, pos, distance2d + size, Orientation + relAngle);
    }

    public void GetContactPoint(WorldObject obj, Position pos, float distance2d = 0.5f)
    {
        // angle to face `obj` to `this` using distance includes size of `obj`
        GetNearPoint(obj, pos, distance2d, GetAbsoluteAngle(obj.Location));
    }

    public void GetCreatureListInGrid(List<Creature> creatureList, float maxSearchRange)
    {
        var pair = new CellCoord((uint)X, (uint)Y);
        var cell = new Cell(pair, GridDefines);
        cell.Data.NoCreate = true;

        var check = new AllCreaturesWithinRange(_worldObject, maxSearchRange);
        var searcher = new CreatureListSearcher(_worldObject, creatureList, check, GridType.All);

        cell.Visit(pair, searcher, Map, _worldObject, maxSearchRange);
    }

    public List<Creature> GetCreatureListWithEntryInGrid(uint entry = 0, float maxSearchRange = 250.0f)
    {
        List<Creature> creatureList = new();
        var check = new AllCreaturesOfEntryInRange(_worldObject, entry, maxSearchRange);
        var searcher = new CreatureListSearcher(_worldObject, creatureList, check, GridType.Grid);

        _cellCalculator.VisitGrid(_worldObject, searcher, maxSearchRange);

        return creatureList;
    }

    public List<Creature> GetCreatureListWithEntryInGrid(uint[] entry, float maxSearchRange = 250.0f)
    {
        List<Creature> creatureList = new();
        var check = new AllCreaturesOfEntriesInRange(_worldObject, entry, maxSearchRange);
        var searcher = new CreatureListSearcher(_worldObject, creatureList, check, GridType.Grid);

        _cellCalculator.VisitGrid(_worldObject, searcher, maxSearchRange);

        return creatureList;
    }

    public List<Creature> GetCreatureListWithOptionsInGrid(float maxSearchRange, FindCreatureOptions options)
    {
        List<Creature> creatureList = new();
        NoopCheckCustomizer checkCustomizer = new();
        CreatureWithOptionsInObjectRangeCheck<NoopCheckCustomizer> check = new(_worldObject, checkCustomizer, options);
        CreatureListSearcher searcher = new(_worldObject, creatureList, check, GridType.Grid);

        if (options.IgnorePhases)
            searcher.PhaseShift = _phasingHandler.GetAlwaysVisiblePhaseShift();

        _cellCalculator.VisitGrid(_worldObject, searcher, maxSearchRange);

        return creatureList;
    }

    public Cell GetCurrentCell()
    {
        return _currentCell;
    }

    public virtual string GetDebugInfo()
    {
        return $"MapID: {MapId} {base.ToString()}";
    }

    public float GetDistance(WorldObject obj)
    {
        var d = GetExactDist(obj.Location) - _worldObject.CombatReach - obj.CombatReach;

        return d > 0.0f ? d : 0.0f;
    }

    public float GetDistance(Position pos)
    {
        var d = GetExactDist(pos) - _worldObject.CombatReach;

        return d > 0.0f ? d : 0.0f;
    }

    public float GetDistance(float x, float y, float z)
    {
        var d = GetExactDist(x, y, z) - _worldObject.CombatReach;

        return d > 0.0f ? d : 0.0f;
    }

    public float GetDistance2d(WorldObject obj)
    {
        var d = GetExactDist2d(obj.Location) - _worldObject.CombatReach - obj.CombatReach;

        return d > 0.0f ? d : 0.0f;
    }

    public float GetDistance2d(float x, float y)
    {
        var d = GetExactDist2d(x, y) - _worldObject.CombatReach;

        return d > 0.0f ? d : 0.0f;
    }

    public bool GetDistanceOrder(WorldObject obj1, WorldObject obj2, bool is3D = true)
    {
        var dx1 = X - obj1.Location.X;
        var dy1 = Y - obj1.Location.Y;
        var distsq1 = dx1 * dx1 + dy1 * dy1;

        if (is3D)
        {
            var dz1 = Z - obj1.Location.Z;
            distsq1 += dz1 * dz1;
        }

        var dx2 = X - obj2.Location.X;
        var dy2 = Y - obj2.Location.Y;
        var distsq2 = dx2 * dx2 + dy2 * dy2;

        if (is3D)
        {
            var dz2 = Z - obj2.Location.Z;
            distsq2 += dz2 * dz2;
        }

        return distsq1 < distsq2;
    }

    public float GetDistanceZ(WorldObject obj)
    {
        var dz = Math.Abs(Z - obj.Location.Z);
        var sizefactor = _worldObject.CombatReach + obj.CombatReach;
        var dist = dz - sizefactor;

        return dist > 0 ? dist : 0;
    }

    public void GetEnemiesWithinRange(List<Unit> unitList, float maxSearchRange)
    {
        var uCheck = new AnyUnfriendlyUnitInObjectRangeCheck(_worldObject, _worldObject.AsUnit, maxSearchRange);
        var searcher = new UnitListSearcher(_worldObject, unitList, uCheck, GridType.All);
        _cellCalculator.VisitGrid(_worldObject, searcher, maxSearchRange);
    }

    public void GetEnemiesWithinRangeWithOwnedAura(List<Unit> unitList, float maxSearchRange, uint auraId)
    {
        GetEnemiesWithinRange(unitList, maxSearchRange);

        unitList.RemoveIf(unit => !unit.HasAura(auraId, _worldObject.GUID));
    }

    public Position GetFirstCollisionPosition(float dist, float angle)
    {
        var pos = new Position(this);
        _worldObject.MovePositionToFirstCollision(pos, dist, angle);

        return pos;
    }

    public List<GameObject> GetGameObjectListWithEntryInGrid(uint entry = 0, float maxSearchRange = 250.0f)
    {
        List<GameObject> gameobjectList = new();
        var check = new AllGameObjectsWithEntryInRange(_worldObject, entry, maxSearchRange);
        var searcher = new GameObjectListSearcher(_worldObject, gameobjectList, check, GridType.Grid);

        _cellCalculator.VisitGrid(_worldObject, searcher, maxSearchRange);

        return gameobjectList;
    }

    public Position GetHitSpherePointFor(Position dest)
    {
        Vector3 vThis = new(X, Y, Z + CollisionHeight);
        Vector3 vObj = new(dest.X, dest.Y, dest.Z);
        var contactPoint = vThis + (vObj - vThis).directionOrZero() * Math.Min(dest.GetExactDist(this), _worldObject.CombatReach);

        return new Position(contactPoint.X, contactPoint.Y, contactPoint.Z, GetAbsoluteAngle(contactPoint.X, contactPoint.Y));
    }

    public void GetHitSpherePointFor(Position dest, Position refDest)
    {
        var pos = GetHitSpherePointFor(dest);
        refDest.X = pos.X;
        refDest.Y = pos.Y;
        refDest.Z = pos.Z;
    }

    public float GetMapHeight(Position pos, bool vmap = true, float distanceToSearch = MapConst.DefaultHeightSearch)
    {
        return GetMapHeight(pos.X, pos.Y, pos.Z, vmap, distanceToSearch);
    }

    public float GetMapHeight(float x, float y, float z, bool vmap = true, float distanceToSearch = MapConst.DefaultHeightSearch)
    {
        if (z != MapConst.MaxHeight)
            z += MapConst.ZOffsetFindHeight;

        return Map.GetHeight(PhaseShift, x, y, z, vmap, distanceToSearch);
    }

    public float GetMapWaterOrGroundLevel(float x, float y, float z)
    {
        float groundLevel = 0;

        return GetMapWaterOrGroundLevel(x, y, z, ref groundLevel);
    }

    public float GetMapWaterOrGroundLevel(float x, float y, float z, ref float ground)
    {
        return Map.GetWaterOrGroundLevel(PhaseShift, x, y, z, ref ground, _worldObject.IsTypeMask(TypeMask.Unit) && !_worldObject.AsUnit.HasAuraType(AuraType.WaterWalk), CollisionHeight);
    }

    public float GetNearPoint(WorldObject searcher, Position pos, float distance2d, float absAngle)
    {
        float floor = 0;
        GetNearPoint2D(searcher, out var x, out var y, distance2d, absAngle);
        pos.Z = Z;
        pos.Z = (searcher ?? _worldObject).Location.UpdateAllowedPositionZ(x, y, pos.Z, ref floor);
        pos.X = x;
        pos.Y = y;

        // if detection disabled, return first point
        if (!_worldObject.Configuration.GetDefaultValue("DetectPosCollision", true))
            return floor;

        // return if the point is already in LoS
        if (IsWithinLOS(pos.X, pos.Y, pos.Z))
            return floor;

        // remember first point
        var firstX = pos.X;
        var firstY = pos.Y;

        // loop in a circle to look for a point in LoS using small steps
        for (var angle = MathFunctions.PI / 8; angle < Math.PI * 2; angle += MathFunctions.PI / 8)
        {
            GetNearPoint2D(searcher, out x, out y, distance2d, absAngle + angle);
            pos.Z = Z;
            pos.Z = (searcher ?? _worldObject).Location.UpdateAllowedPositionZ(x, y, pos.Z);
            pos.X = x;
            pos.Y = y;

            if (IsWithinLOS(pos.X, pos.Y, pos.Z))
                return floor;
        }

        // still not in LoS, give up and return first position found
        pos.X = firstX;
        pos.Y = firstY;

        return floor;
    }

    public void GetNearPoint2D(WorldObject searcher, out float x, out float y, float distance2d, float absAngle)
    {
        var effectiveReach = _worldObject.CombatReach;

        if (searcher != null)
        {
            effectiveReach += searcher.CombatReach;

            if (_worldObject != searcher)
            {
                var myHover = 0.0f;
                var searcherHover = 0.0f;

                var unit = _worldObject.AsUnit;

                if (unit != null)
                    myHover = unit.HoverOffset;

                var searchUnit = searcher.AsUnit;

                if (searchUnit != null)
                    searcherHover = searchUnit.HoverOffset;

                var hoverDelta = myHover - searcherHover;

                if (hoverDelta != 0.0f)
                    effectiveReach = MathF.Sqrt(Math.Max(effectiveReach * effectiveReach - hoverDelta * hoverDelta, 0.0f));
            }
        }

        x = X + (effectiveReach + distance2d) * MathF.Cos(absAngle);
        y = Y + (effectiveReach + distance2d) * MathF.Sin(absAngle);

        x = GridDefines.NormalizeMapCoord(x);
        y = GridDefines.NormalizeMapCoord(y);
    }

    public Position GetNearPosition(float dist, float angle)
    {
        var pos = this;
        _worldObject.MovePosition(pos, dist, angle);

        return pos;
    }

    public List<Unit> GetPlayerListInGrid(float maxSearchRange, bool alive = true)
    {
        List<Unit> playerList = new();
        var checker = new AnyPlayerInObjectRangeCheck(_worldObject, maxSearchRange, alive);
        var searcher = new PlayerListSearcher(_worldObject, playerList, checker);

        _cellCalculator.VisitGrid(_worldObject, searcher, maxSearchRange);

        return playerList;
    }

    public Position GetRandomNearPosition(float radius)
    {
        _worldObject.MovePosition(this, radius * (float)RandomHelper.NextDouble(), (float)RandomHelper.NextDouble() * MathFunctions.PI * 2);

        return this;
    }

    public void GetRandomPoint(Position pos, float distance, out float randX, out float randY, out float randZ)
    {
        if (distance == 0)
        {
            randX = pos.X;
            randY = pos.Y;
            randZ = pos.Z;

            return;
        }

        // angle to face `obj` to `this`
        var angle = (float)RandomHelper.NextDouble() * (2 * MathFunctions.PI);
        var newDist = (float)RandomHelper.NextDouble() + (float)RandomHelper.NextDouble();
        newDist = distance * (newDist > 1 ? newDist - 2 : newDist);

        randX = (float)(pos.X + newDist * Math.Cos(angle));
        randY = (float)(pos.Y + newDist * Math.Sin(angle));
        randZ = pos.Z;

        randX = GridDefines.NormalizeMapCoord(randX);
        randY = GridDefines.NormalizeMapCoord(randY);
        randZ = UpdateGroundPositionZ(randX, randY, randZ); // update to LOS height if available
    }

    public Position GetRandomPoint(Position srcPos, float distance)
    {
        GetRandomPoint(srcPos, distance, out var x, out var y, out var z);

        return new Position(x, y, z, Orientation);
    }

    public bool InSamePhase(PhaseShift phaseShift)
    {
        return PhaseShift.CanSee(phaseShift);
    }

    public bool InSamePhase(WorldObject obj)
    {
        return PhaseShift.CanSee(obj.Location.PhaseShift);
    }

    public bool IsInBack(WorldObject target, float arc = MathFunctions.PI)
    {
        return !HasInArc(2 * MathFunctions.PI - arc, target.Location);
    }

    public bool IsInBetween(WorldObject obj1, WorldObject obj2, float size = 0)
    {
        return obj1 != null && obj2 != null && IsInBetween(obj1.Location, obj2.Location, size);
    }

    public bool IsInFront(WorldObject target, float arc = MathFunctions.PI)
    {
        return HasInArc(arc, target.Location);
    }

    public bool IsInMap(WorldObject obj)
    {
        if (obj != null)
            return IsInWorld && obj.Location.IsInWorld && Map.Id == obj.Location.Map.Id;

        return false;
    }

    public bool IsInRange(WorldObject obj, float minRange, float maxRange, bool is3D = true)
    {
        var dx = X - obj.Location.X;
        var dy = Y - obj.Location.Y;
        var distsq = dx * dx + dy * dy;

        if (is3D)
        {
            var dz = Z - obj.Location.Z;
            distsq += dz * dz;
        }

        var sizefactor = _worldObject.CombatReach + obj.CombatReach;

        // check only for real range
        if (minRange > 0.0f)
        {
            var mindist = minRange + sizefactor;

            if (distsq < mindist * mindist)
                return false;
        }

        var maxdist = maxRange + sizefactor;

        return distsq < maxdist * maxdist;
    }

    public bool IsSelfOrInSameMap(WorldObject obj)
    {
        if (_worldObject == obj)
            return true;

        return IsInMap(obj);
    }

    public virtual bool IsWithinDist(WorldObject obj, float dist2Compare, bool is3D = true, bool incOwnRadius = true, bool incTargetRadius = true)
    {
        float sizefactor = 0;
        sizefactor += incOwnRadius ? _worldObject.CombatReach : 0.0f;
        sizefactor += incTargetRadius ? obj.CombatReach : 0.0f;
        var maxdist = dist2Compare + sizefactor;

        Position thisOrTransport = this;
        Position objOrObjTransport = obj.Location;

        if (_worldObject.Transport == null || obj.Transport == null || obj.Transport.GetTransportGUID() != _worldObject.Transport.GetTransportGUID())
            return is3D ? thisOrTransport.IsInDist(objOrObjTransport, maxdist) : thisOrTransport.IsInDist2d(objOrObjTransport, maxdist);

        thisOrTransport = _worldObject.MovementInfo.Transport.Pos;
        objOrObjTransport = obj.MovementInfo.Transport.Pos;

        return is3D ? thisOrTransport.IsInDist(objOrObjTransport, maxdist) : thisOrTransport.IsInDist2d(objOrObjTransport, maxdist);
    }

    public bool IsWithinDist2d(float x, float y, float dist)
    {
        return IsInDist2d(x, y, dist + _worldObject.CombatReach);
    }

    public bool IsWithinDist2d(Position pos, float dist)
    {
        return IsInDist2d(pos, dist + _worldObject.CombatReach);
    }

    public bool IsWithinDist3d(float x, float y, float z, float dist)
    {
        return IsInDist(x, y, z, dist + _worldObject.CombatReach);
    }

    public bool IsWithinDist3d(Position pos, float dist)
    {
        return IsInDist(pos, dist + _worldObject.CombatReach);
    }

    public bool IsWithinDistInMap(WorldObject obj, float dist2Compare, bool is3D = true, bool incOwnRadius = true, bool incTargetRadius = true)
    {
        return obj != null && IsInMap(obj) && InSamePhase(obj) && IsWithinDist(obj, dist2Compare, is3D, incOwnRadius, incTargetRadius);
    }

    public bool IsWithinLOS(Position pos, LineOfSightChecks checks = LineOfSightChecks.All, ModelIgnoreFlags ignoreFlags = ModelIgnoreFlags.Nothing)
    {
        return IsWithinLOS(pos.X, pos.Y, pos.Z, checks, ignoreFlags);
    }

    public bool IsWithinLOS(float ox, float oy, float oz, LineOfSightChecks checks = LineOfSightChecks.All, ModelIgnoreFlags ignoreFlags = ModelIgnoreFlags.Nothing)
    {
        if (IsInWorld)
        {
            oz += CollisionHeight;
            var pos = new Position();

            if (_worldObject.IsTypeId(TypeId.Player))
            {
                pos = Copy();
                pos.Z += CollisionHeight;
            }
            else
                GetHitSpherePointFor(new Position(ox, oy, oz), pos);

            return Map.IsInLineOfSight(PhaseShift, pos, ox, oy, oz, checks, ignoreFlags);
        }

        return true;
    }

    public bool IsWithinLOSInMap(WorldObject obj, LineOfSightChecks checks = LineOfSightChecks.All, ModelIgnoreFlags ignoreFlags = ModelIgnoreFlags.Nothing)
    {
        if (!IsInMap(obj))
            return false;

        var pos = new Position();

        if (obj.IsTypeId(TypeId.Player))
        {
            pos = obj.Location.Copy();
            pos.Z += CollisionHeight;
        }
        else
            obj.Location.GetHitSpherePointFor(new Position(X, Y, Z + CollisionHeight), pos);

        var pos2 = new Position();

        if (_worldObject.IsPlayer)
        {
            pos2 = Copy();
            pos2.Z += CollisionHeight;
        }
        else
            GetHitSpherePointFor(new Position(obj.Location.X, obj.Location.Y, obj.Location.Z + obj.Location.CollisionHeight), pos2);

        return Map.IsInLineOfSight(PhaseShift, pos2, pos, checks, ignoreFlags);
    }

    public TransferAbortParams PlayerCannotEnter(uint mapid, Player player)
    {
        if (!player.CliDB.MapStorage.TryGetValue(mapid, out var entry))
            return new TransferAbortParams(TransferAbortReason.MapNotAllowed);

        if (!entry.IsDungeon())
            return null;

        var targetDifficulty = player.GetDifficultyId(entry);
        // Get the highest available difficulty if current setting is higher than the instance allows
        var mapDiff = player.DB2Manager.GetDownscaledMapDifficultyData(mapid, ref targetDifficulty);

        if (mapDiff == null)
            return new TransferAbortParams(TransferAbortReason.Difficulty);

        //Bypass checks for GMs
        if (player.IsGameMaster)
            return null;

        //Other requirements
        {
            TransferAbortParams abortParams = new();

            if (!player.Satisfy(player.GameObjectManager.GetAccessRequirement(mapid, targetDifficulty), mapid, abortParams, true))
                return abortParams;
        }

        var group = player.Group;

        if (entry.IsRaid() && (int)entry.Expansion() >= player.Configuration.GetDefaultValue("Expansion", (int)Expansion.Dragonflight)) // can only enter in a raid group but raids from old expansion don't need a group
            if ((group == null || !group.IsRaidGroup) && !player.Configuration.GetDefaultValue("Instance:IgnoreRaid", false))
                return new TransferAbortParams(TransferAbortReason.NeedGroup);

        if (!entry.Instanceable())
            return null;

        //Get instance where player's group is bound & its map
        var instanceIdToCheck = player.MapManager.FindInstanceIdForPlayer(mapid, player);
        var boundMap = player.MapManager.FindMap(mapid, instanceIdToCheck);

        var denyReason = boundMap?.CannotEnter(player);

        if (denyReason != null)
            return denyReason;

        // players are only allowed to enter 10 instances per hour
        if (!entry.GetFlags2().HasFlag(MapFlags2.IgnoreInstanceFarmLimit) && entry.IsDungeon() && !player.CheckInstanceCount(instanceIdToCheck) && !player.IsDead)
            return new TransferAbortParams(TransferAbortReason.TooManyInstances);

        return null;
    }

    public void ProcessPositionDataChanged(PositionFullTerrainStatus data)
    {
        Zone = Area = data.AreaId;

        if (_worldObject.CliDB.AreaTableStorage.TryGetValue(Area, out var area))
            if (area.ParentAreaID != 0)
                Zone = area.ParentAreaID;

        IsOutdoors = data.Outdoors;
        _staticFloorZ = data.FloorZ;
        LiquidStatus = data.LiquidStatus;
    }

    public virtual void ResetMap()
    {
        if (Map == null)
            return;

        if (_worldObject.IsWorldObject())
            Map.RemoveWorldObject(_worldObject);

        Map = null;
    }

    public Player SelectNearestPlayer(float distance)
    {
        var checker = new NearestPlayerInObjectRangeCheck(_worldObject, distance);
        var searcher = new PlayerLastSearcher(_worldObject, checker, GridType.All);
        _cellCalculator.VisitGrid(_worldObject, searcher, distance);

        return searcher.GetTarget();
    }

    public void SetCurrentCell(Cell cell)
    {
        _currentCell = cell;
    }

    public void SetLocationInstanceId(uint instanceId)
    {
        _worldObject.InstanceId = instanceId;
    }

    public void SetNewCellPosition(float x, float y, float z, float o)
    {
        MoveState = ObjectCellMoveState.Active;
        NewPosition.Relocate(x, y, z, o);
    }

    public void SetZoneScript()
    {
        _worldObject.ZoneScript = FindZoneScript();
    }

    public override string ToString()
    {
        return $"X: {X} Y: {Y} Z: {Z} O: {Orientation} MapId: {MapId}";
    }

    public void UpdateAllowedPositionZ(Position pos, ref float groundZ)
    {
        pos.Z = UpdateAllowedPositionZ(pos.X, pos.Y, pos.Z, ref groundZ);
    }

    public void UpdateAllowedPositionZ(Position pos)
    {
        pos.Z = UpdateAllowedPositionZ(pos.X, pos.Y, pos.Z);
    }

    public float UpdateAllowedPositionZ(float x, float y, float z)
    {
        var unused = 0f;

        return UpdateAllowedPositionZ(x, y, z, ref unused);
    }

    public float UpdateAllowedPositionZ(float x, float y, float z, ref float groundZ)
    {
        // TODO: Allow transports to be part of dynamic vmap tree
        if (_worldObject.Transport != null)
        {
            groundZ = z;

            return z;
        }

        var unit = _worldObject.AsUnit;

        if (unit != null)
        {
            if (!unit.CanFly)
            {
                var canSwim = unit.CanSwim;
                var getMapHeight = z;
                float maxZ;

                if (canSwim)
                    maxZ = GetMapWaterOrGroundLevel(x, y, z, ref getMapHeight);
                else
                    maxZ = getMapHeight = GetMapHeight(x, y, z);

                if (maxZ > MapConst.InvalidHeight)
                {
                    // hovering units cannot go below their hover height
                    var hoverOffset = unit.HoverOffset;
                    maxZ += hoverOffset;
                    getMapHeight += hoverOffset;

                    if (z > maxZ)
                        z = maxZ;
                    else if (z < getMapHeight)
                        z = getMapHeight;
                }

                groundZ = getMapHeight;
            }
            else
            {
                var mapHeight = GetMapHeight(x, y, z) + unit.HoverOffset;

                if (z < mapHeight)
                    z = mapHeight;

                groundZ = mapHeight;
            }
        }
        else
        {
            var mapHeight = GetMapHeight(x, y, z);

            if (mapHeight > MapConst.InvalidHeight)
                z = mapHeight;

            groundZ = mapHeight;
        }

        return z;
    }

    public float UpdateGroundPositionZ(float x, float y, float z)
    {
        var newZ = GetMapHeight(x, y, z);

        if (newZ > MapConst.InvalidHeight)
            z = newZ + (_worldObject.IsUnit ? _worldObject.AsUnit.HoverOffset : 0.0f);

        return z;
    }

    public void UpdatePositionData()
    {
        PositionFullTerrainStatus data = new();
        Map.GetFullTerrainStatusForPosition(PhaseShift, X, Y, Z, data, LiquidHeaderTypeFlags.AllLiquids, CollisionHeight);
        ProcessPositionDataChanged(data);
    }

    public void WorldRelocate(uint mapId, Position pos)
    {
        _mapId = mapId;
        Relocate(pos);
    }

    public void WorldRelocate(Map map, Position pos)
    {
        Map = map;
        Relocate(pos);
    }

    public void WorldRelocate(WorldLocation loc)
    {
        _mapId = loc.MapId;
        Map = loc.Map;
        Relocate(loc);
    }

    public void WorldRelocate(uint mapId = 0xFFFFFFFF, float x = 0.0f, float y = 0.0f, float z = 0.0f, float o = 0.0f)
    {
        _mapId = mapId;
        Relocate(x, y, z, o);
    }

    private bool IsInBetween(Position pos1, Position pos2, float size)
    {
        var dist = GetExactDist2d(pos1);

        // not using sqrt() for performance
        if (dist * dist >= pos1.GetExactDist2dSq(pos2))
            return false;

        if (size == 0)
            size = _worldObject.CombatReach / 2;

        var angle = pos1.GetAbsoluteAngle(pos2);

        // not using sqrt() for performance
        return size * size >= GetExactDist2dSq(pos1.X + (float)Math.Cos(angle) * dist, pos1.Y + (float)Math.Sin(angle) * dist);
    }
}