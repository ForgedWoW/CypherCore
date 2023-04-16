// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Forged.MapServer.DataStorage;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Objects.Update;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Maps;
using Forged.MapServer.Maps.Checks;
using Forged.MapServer.Maps.GridNotifiers;
using Forged.MapServer.Movement;
using Forged.MapServer.Movement.Generators;
using Forged.MapServer.Networking;
using Forged.MapServer.Networking.Packets.AreaTrigger;
using Forged.MapServer.Phasing;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.IAreaTrigger;
using Forged.MapServer.Spells;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;
using Game.Common;
using Serilog;

namespace Forged.MapServer.Entities.AreaTriggers;

public class AreaTrigger : WorldObject
{
    private static readonly List<IAreaTriggerScript> Dummy = new();
    private readonly AreaTriggerFieldData _areaTriggerData;
    private readonly AreaTriggerDataStorage _dataStorage;
    private readonly DB2Manager _db2Manager;
    private readonly Dictionary<Type, List<IAreaTriggerScript>> _scriptsByType = new();

    private uint _areaTriggerId;
    private AreaTriggerTemplate _areaTriggerTemplate;
    private uint _basePeriodicProcTimer;
    private int _lastSplineIndex;
    private List<AreaTriggerScript> _loadedScripts = new();
    private uint _movementTime;
    private uint _periodicProcTimer;
    private List<Vector2> _polygonVertices;
    private float _previousCheckOrientation;
    private bool _reachedDestination;
    private ulong _spawnId;

    private ObjectGuid _targetGuid;
    public AreaTrigger(ClassFactory classFactory, AreaTriggerDataStorage dataStorage, DB2Manager db2Manager) : base(false, classFactory)
    {
        _dataStorage = dataStorage;
        _db2Manager = db2Manager;
        _previousCheckOrientation = float.PositiveInfinity;
        _reachedDestination = true;

        ObjectTypeMask |= TypeMask.AreaTrigger;
        ObjectTypeId = TypeId.AreaTrigger;

        UpdateFlag.Stationary = true;
        UpdateFlag.AreaTrigger = true;

        _areaTriggerData = new AreaTriggerFieldData();

        Spline = new Spline<int>();
    }

    public AuraEffect AuraEff { get; private set; }

    public ObjectGuid CasterGuid => _areaTriggerData.Caster;

    public AreaTriggerOrbitInfo CircularMovementInfo { get; private set; }

    public AreaTriggerCreateProperties CreateProperties { get; private set; }

    public int Duration { get; private set; }

    public uint ElapsedTimeForMovement => TimeSinceCreated;

    public override uint Faction
    {
        get
        {
            var caster = GetCaster();

            return caster ? caster.Faction : 0;
        }
    }

    public bool HasSplines => !Spline.IsEmpty;
    public HashSet<ObjectGuid> InsideUnits { get; } = new();
    public bool IsRemoved { get; private set; }
    public bool IsServerSide => _areaTriggerTemplate.Id.IsServerSide;
    public override ObjectGuid OwnerGUID => CasterGuid;
    public Vector3 RollPitchYaw { get; set; }
    public AreaTriggerShapeInfo Shape { get; private set; }
    public uint SpellId => _areaTriggerData.SpellID;
    public Spline<int> Spline { get; }
    public Vector3 TargetRollPitchYaw { get; set; }
    public uint TimeSinceCreated { get; private set; }

    public uint TimeToTarget => _areaTriggerData.TimeToTarget;

    public uint TimeToTargetScale => _areaTriggerData.TimeToTargetScale;
    public int TotalDuration { get; private set; }
    private float MaxSearchRadius { get; set; }

    // @todo: research the right value, in sniffs both timers are nearly identical
    private float Progress => TimeSinceCreated < TimeToTargetScale ? (float)TimeSinceCreated / TimeToTargetScale : 1.0f;

    private Unit Target => ObjectAccessor.GetUnit(this, _targetGuid);
    public static AreaTrigger CreateAreaTrigger(ClassFactory classFactory, uint areaTriggerCreatePropertiesId, Unit caster, Unit target, SpellInfo spell, Position pos, int duration, SpellCastVisualField spellVisual, ObjectGuid castId = default, AuraEffect aurEff = null)
    {
        var at = classFactory.Resolve<AreaTrigger>();
        return !at.Create(areaTriggerCreatePropertiesId, caster, target, spell, pos, duration, spellVisual, castId, aurEff) ? null : at;
    }

    public static ObjectGuid CreateNewMovementForceId(Map map, uint areaTriggerId)
    {
        return ObjectGuid.Create(HighGuid.AreaTrigger, map.Id, areaTriggerId, map.GenerateLowGuid(HighGuid.AreaTrigger));
    }

    public override void AddToWorld()
    {
        // Register the AreaTrigger for guid lookup and for caster
        if (Location.IsInWorld)
            return;

        Location.Map.ObjectsStore.TryAdd(GUID, this);

        if (_spawnId != 0)
            Location.Map.AreaTriggerBySpawnIdStore.Add(_spawnId, this);

        base.AddToWorld();
    }

    public override void BuildValuesCreate(WorldPacket data, Player target)
    {
        var flags = GetUpdateFieldFlagsFor(target);
        WorldPacket buffer = new();

        buffer.WriteUInt8((byte)flags);
        ObjectData.WriteCreate(buffer, flags, this, target);
        _areaTriggerData.WriteCreate(buffer, flags, this, target);

        data.WriteUInt32(buffer.GetSize());
        data.WriteBytes(buffer);
    }

    public override void BuildValuesUpdate(WorldPacket data, Player target)
    {
        var flags = GetUpdateFieldFlagsFor(target);
        WorldPacket buffer = new();

        buffer.WriteUInt32(Values.GetChangedObjectTypeMask());

        if (Values.HasChanged(TypeId.Object))
            ObjectData.WriteUpdate(buffer, flags, this, target);

        if (Values.HasChanged(TypeId.AreaTrigger))
            _areaTriggerData.WriteUpdate(buffer, flags, this, target);

        data.WriteUInt32(buffer.GetSize());
        data.WriteBytes(buffer);
    }

    public void BuildValuesUpdateForPlayerWithMask(UpdateData data, UpdateMask requestedObjectMask, UpdateMask requestedAreaTriggerMask, Player target)
    {
        UpdateMask valuesMask = new((int)TypeId.Max);

        if (requestedObjectMask.IsAnySet())
            valuesMask.Set((int)TypeId.Object);

        if (requestedAreaTriggerMask.IsAnySet())
            valuesMask.Set((int)TypeId.AreaTrigger);

        WorldPacket buffer = new();
        buffer.WriteUInt32(valuesMask.GetBlock(0));

        if (valuesMask[(int)TypeId.Object])
            ObjectData.WriteUpdate(buffer, requestedObjectMask, true, this, target);

        if (valuesMask[(int)TypeId.AreaTrigger])
            _areaTriggerData.WriteUpdate(buffer, requestedAreaTriggerMask, true, this, target);

        WorldPacket buffer1 = new();
        buffer1.WriteUInt8((byte)UpdateType.Values);
        buffer1.WritePackedGuid(GUID);
        buffer1.WriteUInt32(buffer.GetSize());
        buffer1.WriteBytes(buffer.GetData());

        data.AddUpdateBlock(buffer1);
    }

    public override void ClearUpdateMask(bool remove)
    {
        Values.ClearChangesMask(_areaTriggerData);
        base.ClearUpdateMask(remove);
    }

    public void Delay(int delaytime)
    {
        SetDuration(Duration - delaytime);
    }

    public void ForEachAreaTriggerScript<T>(Action<T> action) where T : IAreaTriggerScript
    {
        foreach (T script in GetAreaTriggerScripts<T>())
            action.Invoke(script);
    }

    public List<IAreaTriggerScript> GetAreaTriggerScripts<T>() where T : IAreaTriggerScript
    {
        return _scriptsByType.TryGetValue(typeof(T), out var scripts) ? scripts : Dummy;
    }

    public Unit GetCaster()
    {
        return ObjectAccessor.GetUnit(this, CasterGuid);
    }

    public AreaTriggerTemplate GetTemplate()
    {
        return _areaTriggerTemplate;
    }

    public bool HasOrbit()
    {
        return CircularMovementInfo != null;
    }

    public void InitSplines(List<Vector3> splinePoints, uint timeToTarget)
    {
        if (splinePoints.Count < 2)
            return;

        _movementTime = 0;

        Spline.InitSpline(splinePoints.ToArray(), splinePoints.Count, EvaluationMode.Linear);
        Spline.InitLengths();

        // should be sent in object create packets only
        DoWithSuppressingObjectUpdates(() =>
        {
            SetUpdateFieldValue(Values.ModifyValue(_areaTriggerData).ModifyValue(_areaTriggerData.TimeToTarget), timeToTarget);
            _areaTriggerData.ClearChanged(_areaTriggerData.TimeToTarget);
        });

        if (Location.IsInWorld)
        {
            if (_reachedDestination)
            {
                AreaTriggerRePath reshapeDest = new()
                {
                    TriggerGUID = GUID
                };

                SendMessageToSet(reshapeDest, true);
            }

            AreaTriggerRePath reshape = new()
            {
                TriggerGUID = GUID,
                AreaTriggerSpline = new AreaTriggerSplineInfo
                {
                    ElapsedTimeForMovement = ElapsedTimeForMovement,
                    TimeToTarget = timeToTarget,
                    Points = splinePoints
                }
            };

            SendMessageToSet(reshape, true);
        }

        _reachedDestination = false;
    }

    public override bool IsNeverVisibleFor(WorldObject seer)
    {
        return base.IsNeverVisibleFor(seer) || IsServerSide;
    }

    public override bool LoadFromDB(ulong spawnId, Map map, bool addToMap, bool allowDuplicate)
    {
        _spawnId = spawnId;

        var position = _dataStorage.GetAreaTriggerSpawn(spawnId);

        if (position == null)
            return false;

        var areaTriggerTemplate = _dataStorage.GetAreaTriggerTemplate(position.TriggerId);

        return areaTriggerTemplate != null && CreateServer(map, areaTriggerTemplate, position);
    }

    public void Remove()
    {
        if (Location.IsInWorld)
            Location.AddObjectToRemoveList();
    }

    public override void RemoveFromWorld()
    {
        // Remove the AreaTrigger from the accessor and from all lists of objects in world
        if (!Location.IsInWorld)
            return;

        IsRemoved = true;

        var caster = GetCaster();

        if (caster)
            caster._UnregisterAreaTrigger(this);

        // Handle removal of all units, calling OnUnitExit & deleting auras if needed
        HandleUnitEnterExit(new List<Unit>());

        ForEachAreaTriggerScript<IAreaTriggerOnRemove>(a => a.OnRemove());

        base.RemoveFromWorld();

        if (_spawnId != 0)
            Location.Map.AreaTriggerBySpawnIdStore.Remove(_spawnId, this);

        Location.Map.ObjectsStore.TryRemove(GUID, out _);
    }
    public bool SetDestination(uint timeToTarget, Position targetPos = null, WorldObject startingObject = null)
    {
        var path = new PathGenerator(startingObject ?? GetCaster());
        var result = path.CalculatePath(targetPos ?? Location, true);

        if (!result || (path.PathType & PathType.NoPath) != 0)
            return false;

        InitSplines(path.Path.ToList(), timeToTarget);

        return true;
    }

    public void SetDuration(int newDuration)
    {
        Duration = newDuration;
        TotalDuration = newDuration;

        // negative duration (permanent areatrigger) sent as 0
        SetUpdateFieldValue(Values.ModifyValue(_areaTriggerData).ModifyValue(_areaTriggerData.Duration), (uint)Math.Max(newDuration, 0));
    }

    public void SetPeriodicProcTimer(uint periodicProctimer)
    {
        _basePeriodicProcTimer = periodicProctimer;
        _periodicProcTimer = periodicProctimer;
    }

    public override void Update(uint diff)
    {
        base.Update(diff);
        TimeSinceCreated += diff;

        if (!IsServerSide)
        {
            // "If" order matter here, Orbit > Attached > Splines
            if (HasOrbit())
            {
                UpdateOrbitPosition();
            }
            else if (GetTemplate() != null && GetTemplate().HasFlag(AreaTriggerFlags.HasAttached))
            {
                var target = Target;

                if (target)
                    Location.Map.AreaTriggerRelocation(this, target.Location.X, target.Location.Y, target.Location.Z, target.Location.Orientation);
            }
            else
            {
                UpdateSplinePosition(diff);
            }

            if (Duration != -1)
            {
                if (Duration > diff)
                {
                    _UpdateDuration((int)(Duration - diff));
                }
                else
                {
                    Remove(); // expired

                    return;
                }
            }
        }

        ForEachAreaTriggerScript<IAreaTriggerOnUpdate>(a => a.OnUpdate(diff));

        UpdateTargetList();

        if (_basePeriodicProcTimer == 0)
            return;

        if (_periodicProcTimer <= diff)
        {
            ForEachAreaTriggerScript<IAreaTriggerOnPeriodicProc>(a => a.OnPeriodicProc());
            _periodicProcTimer = _basePeriodicProcTimer;
        }
        else
        {
            _periodicProcTimer -= diff;
        }
    }
    public void UpdateShape()
    {
        if (Shape.TriggerType == AreaTriggerTypes.Polygon)
            UpdatePolygonOrientation();
    }
    private void _UpdateDuration(int newDuration)
    {
        Duration = newDuration;

        // should be sent in object create packets only
        DoWithSuppressingObjectUpdates(() =>
        {
            SetUpdateFieldValue(Values.ModifyValue(_areaTriggerData).ModifyValue(_areaTriggerData.Duration), (uint)Duration);
            _areaTriggerData.ClearChanged(_areaTriggerData.Duration);
        });
    }

    private Position CalculateOrbitPosition()
    {
        var centerPos = GetOrbitCenterPosition();

        if (centerPos == null)
            return Location;

        var cmi = CircularMovementInfo;

        // AreaTrigger make exactly "Duration / TimeToTarget" loops during his life time
        var pathProgress = (float)cmi.ElapsedTimeForMovement / cmi.TimeToTarget;

        // We already made one circle and can't loop
        if (!cmi.CanLoop)
            pathProgress = Math.Min(1.0f, pathProgress);

        var radius = cmi.Radius;

        if (MathFunctions.fuzzyNe(cmi.BlendFromRadius, radius))
        {
            var blendCurve = (cmi.BlendFromRadius - radius) / radius;
            // 4.f Defines four quarters
            blendCurve = MathFunctions.RoundToInterval(ref blendCurve, 1.0f, 4.0f) / 4.0f;
            var blendProgress = Math.Min(1.0f, pathProgress / blendCurve);
            radius = MathFunctions.Lerp(cmi.BlendFromRadius, cmi.Radius, blendProgress);
        }

        // Adapt Path progress depending of circle direction
        if (!cmi.CounterClockwise)
            pathProgress *= -1;

        var angle = cmi.InitialAngle + 2.0f * (float)Math.PI * pathProgress;
        var x = centerPos.X + radius * (float)Math.Cos(angle);
        var y = centerPos.Y + radius * (float)Math.Sin(angle);
        var z = centerPos.Z + cmi.ZOffset;

        return new Position(x, y, z, angle);
    }

    private bool CheckIsInPolygon2D(Position pos)
    {
        var testX = pos.X;
        var testY = pos.Y;

        //this method uses the ray tracing algorithm to determine if the point is in the polygon
        var locatedInPolygon = false;

        for (var vertex = 0; vertex < _polygonVertices.Count; ++vertex)
        {
            int nextVertex;

            //repeat loop for all sets of points
            if (vertex == _polygonVertices.Count - 1)
                //if i is the last vertex, let j be the first vertex
                nextVertex = 0;
            else
                //for all-else, let j=(i+1)th vertex
                nextVertex = vertex + 1;

            var vertXi = Location.X + _polygonVertices[vertex].X;
            var vertYi = Location.Y + _polygonVertices[vertex].Y;
            var vertXj = Location.X + _polygonVertices[nextVertex].X;
            var vertYj = Location.Y + _polygonVertices[nextVertex].Y;

            // following statement checks if testPoint.Y is below Y-coord of i-th vertex
            var belowLowY = vertYi > testY;
            // following statement checks if testPoint.Y is below Y-coord of i+1-th vertex
            var belowHighY = vertYj > testY;

            /* following statement is true if testPoint.Y satisfies either (only one is possible)
            -.(i).Y < testPoint.Y < (i+1).Y        OR
            -.(i).Y > testPoint.Y > (i+1).Y
         
            (Note)
            Both of the conditions indicate that a point is located within the edges of the Y-th coordinate
            of the (i)-th and the (i+1)- th vertices of the polygon. If neither of the above
            conditions is satisfied, then it is assured that a semi-infinite horizontal line draw
            to the right from the testpoint will NOT cross the line that connects vertices i and i+1
            of the polygon
            */
            var withinYsEdges = belowLowY != belowHighY;

            if (withinYsEdges)
            {
                // this is the slope of the line that connects vertices i and i+1 of the polygon
                var slopeOfLine = (vertXj - vertXi) / (vertYj - vertYi);

                // this looks up the x-coord of a point lying on the above line, given its y-coord
                var pointOnLine = slopeOfLine * (testY - vertYi) + vertXi;

                //checks to see if x-coord of testPoint is smaller than the point on the line with the same y-coord
                var isLeftToLine = testX < pointOnLine;

                if (isLeftToLine)
                    //this statement changes true to false (and vice-versa)
                    locatedInPolygon = !locatedInPolygon; //end if (isLeftToLine)
            }                                             //end if (withinYsEdges
        }

        return locatedInPolygon;
    }

    private bool Create(uint areaTriggerCreatePropertiesId, Unit caster, Unit target, SpellInfo spell, Position pos, int duration, SpellCastVisualField spellVisual, ObjectGuid castId, AuraEffect aurEff)
    {
        _areaTriggerId = areaTriggerCreatePropertiesId;
        LoadScripts();
        ForEachAreaTriggerScript<IAreaTriggerOnInitialize>(a => a.OnInitialize());

        _targetGuid = target ? target.GUID : ObjectGuid.Empty;
        AuraEff = aurEff;

        Location.WorldRelocate(caster.Location.Map, pos);
        CheckAddToMap();

        if (!Location.IsPositionValid)
        {
            Log.Logger.Error($"AreaTrigger (areaTriggerCreatePropertiesId: {areaTriggerCreatePropertiesId}) not created. Invalid coordinates (X: {Location.X} Y: {Location.Y})");

            return false;
        }

        ForEachAreaTriggerScript<IAreaTriggerOverrideCreateProperties>(a => CreateProperties = a.AreaTriggerCreateProperties);

        if (CreateProperties == null)
        {
            CreateProperties = _dataStorage.GetAreaTriggerCreateProperties(areaTriggerCreatePropertiesId);

            if (CreateProperties == null)
            {
                Log.Logger.Error($"AreaTrigger (areaTriggerCreatePropertiesId {areaTriggerCreatePropertiesId}) not created. Invalid areatrigger create properties id ({areaTriggerCreatePropertiesId})");

                return false;
            }
        }

        _areaTriggerTemplate = CreateProperties.Template;

        Create(ObjectGuid.Create(HighGuid.AreaTrigger, Location.MapId, GetTemplate() != null ? GetTemplate().Id.Id : 0, caster.Location.Map.GenerateLowGuid(HighGuid.AreaTrigger)));

        if (GetTemplate() != null)
            Entry = GetTemplate().Id.Id;

        SetDuration(duration);

        ObjectScale = 1.0f;

        Shape = CreateProperties.Shape;
        MaxSearchRadius = CreateProperties.MaxSearchRadius;

        var areaTriggerData = Values.ModifyValue(_areaTriggerData);
        SetUpdateFieldValue(areaTriggerData.ModifyValue(_areaTriggerData.Caster), caster.GUID);
        SetUpdateFieldValue(areaTriggerData.ModifyValue(_areaTriggerData.CreatingEffectGUID), castId);

        SetUpdateFieldValue(areaTriggerData.ModifyValue(_areaTriggerData.SpellID), spell.Id);
        SetUpdateFieldValue(areaTriggerData.ModifyValue(_areaTriggerData.SpellForVisuals), spell.Id);

        SpellCastVisualField spellCastVisual = areaTriggerData.ModifyValue(_areaTriggerData.SpellVisual);
        SetUpdateFieldValue(ref spellCastVisual.SpellXSpellVisualID, spellVisual.SpellXSpellVisualID);
        SetUpdateFieldValue(ref spellCastVisual.ScriptVisualID, spellVisual.ScriptVisualID);

        SetUpdateFieldValue(areaTriggerData.ModifyValue(_areaTriggerData.TimeToTargetScale), CreateProperties.TimeToTargetScale != 0 ? CreateProperties.TimeToTargetScale : _areaTriggerData.Duration);
        SetUpdateFieldValue(areaTriggerData.ModifyValue(_areaTriggerData.BoundsRadius2D), MaxSearchRadius);
        SetUpdateFieldValue(areaTriggerData.ModifyValue(_areaTriggerData.DecalPropertiesID), CreateProperties.DecalPropertiesId);

        ScaleCurve extraScaleCurve = areaTriggerData.ModifyValue(_areaTriggerData.ExtraScaleCurve);

        if (CreateProperties.ExtraScale.Structured.StartTimeOffset != 0)
            SetUpdateFieldValue(extraScaleCurve.ModifyValue(extraScaleCurve.StartTimeOffset), CreateProperties.ExtraScale.Structured.StartTimeOffset);

        if (CreateProperties.ExtraScale.Structured.X != 0 || CreateProperties.ExtraScale.Structured.Y != 0)
        {
            Vector2 point = new(CreateProperties.ExtraScale.Structured.X, CreateProperties.ExtraScale.Structured.Y);
            SetUpdateFieldValue(ref extraScaleCurve.ModifyValue(extraScaleCurve.Points, 0), point);
        }

        if (CreateProperties.ExtraScale.Structured.Z != 0 || CreateProperties.ExtraScale.Structured.W != 0)
        {
            Vector2 point = new(CreateProperties.ExtraScale.Structured.Z, CreateProperties.ExtraScale.Structured.W);
            SetUpdateFieldValue(ref extraScaleCurve.ModifyValue(extraScaleCurve.Points, 1), point);
        }

        unsafe
        {
            if (CreateProperties.ExtraScale.Raw.Data[5] != 0)
                SetUpdateFieldValue(extraScaleCurve.ModifyValue(extraScaleCurve.ParameterCurve), CreateProperties.ExtraScale.Raw.Data[5]);

            if (CreateProperties.ExtraScale.Structured.OverrideActive != 0)
                SetUpdateFieldValue(extraScaleCurve.ModifyValue(extraScaleCurve.OverrideActive), CreateProperties.ExtraScale.Structured.OverrideActive != 0);
        }

        VisualAnim visualAnim = areaTriggerData.ModifyValue(_areaTriggerData.VisualAnim);
        SetUpdateFieldValue(visualAnim.ModifyValue(visualAnim.AnimationDataID), CreateProperties.AnimId);
        SetUpdateFieldValue(visualAnim.ModifyValue(visualAnim.AnimKitID), CreateProperties.AnimKitId);

        if (GetTemplate() != null && GetTemplate().HasFlag(AreaTriggerFlags.Unk3))
            SetUpdateFieldValue(visualAnim.ModifyValue(visualAnim.Field_C), true);

        PhasingHandler.InheritPhaseShift(this, caster);

        if (target && GetTemplate() != null && GetTemplate().HasFlag(AreaTriggerFlags.HasAttached))
            MovementInfo.Transport.Guid = target.GUID;

        Location.UpdatePositionData();
        Location.SetZoneScript();

        UpdateShape();

        var timeToTarget = CreateProperties.TimeToTarget != 0 ? CreateProperties.TimeToTarget : _areaTriggerData.Duration;

        if (CreateProperties.OrbitInfo != null)
        {
            var orbit = CreateProperties.OrbitInfo;

            if (target && GetTemplate() != null && GetTemplate().HasFlag(AreaTriggerFlags.HasAttached))
                orbit.PathTarget = target.GUID;
            else
                orbit.Center = new Vector3(pos.X, pos.Y, pos.Z);

            InitOrbit(orbit, timeToTarget);
        }
        else if (CreateProperties.HasSplines)
        {
            InitSplineOffsets(CreateProperties.SplinePoints, timeToTarget);
        }

        // movement on transport of areatriggers on unit is handled by themself
        var transport = MovementInfo.Transport.Guid.IsEmpty ? caster.Transport : null;

        if (transport != null)
        {
            var newPos = pos.Copy();
            transport.CalculatePassengerOffset(newPos);
            MovementInfo.Transport.Pos.Relocate(newPos);

            // This object must be added to transport before adding to map for the client to properly display it
            transport.AddPassenger(this);
        }

        // Relocate areatriggers with circular movement again
        if (HasOrbit())
            Location.Relocate(CalculateOrbitPosition());

        if (!Location.Map.AddToMap(this))
        {
            // Returning false will cause the object to be deleted - remove from transport
            transport?.RemovePassenger(this);

            return false;
        }

        caster._RegisterAreaTrigger(this);

        ForEachAreaTriggerScript<IAreaTriggerOnCreate>(a => a.OnCreate());

        return true;
    }

    private bool CreateServer(Map map, AreaTriggerTemplate areaTriggerTemplate, AreaTriggerSpawn position)
    {
        Location.WorldRelocate(map, position.SpawnPoint);
        CheckAddToMap();

        if (!Location.IsPositionValid)
        {
            Log.Logger.Error($"AreaTriggerServer (id {areaTriggerTemplate.Id}) not created. Invalid coordinates (X: {Location.X} Y: {Location.Y})");

            return false;
        }

        _areaTriggerTemplate = areaTriggerTemplate;

        Create(ObjectGuid.Create(HighGuid.AreaTrigger, Location.MapId, areaTriggerTemplate.Id.Id, Location.Map.GenerateLowGuid(HighGuid.AreaTrigger)));

        Entry = areaTriggerTemplate.Id.Id;

        ObjectScale = 1.0f;

        Shape = position.Shape;
        MaxSearchRadius = Shape.GetMaxSearchRadius();

        if (position.PhaseUseFlags != 0 || position.PhaseId != 0 || position.PhaseGroup != 0)
            PhasingHandler.InitDbPhaseShift(Location.PhaseShift, position.PhaseUseFlags, position.PhaseId, position.PhaseGroup);

        UpdateShape();

        ForEachAreaTriggerScript<IAreaTriggerOnCreate>(a => a.OnCreate());

        return true;
    }
    [System.Diagnostics.Conditional("DEBUG")]
    private void DebugVisualizePosition()
    {
        var caster = GetCaster();

        if (caster)
        {
            var player = caster.AsPlayer;

            if (player)
                if (player.IsDebugAreaTriggers)
                    player.SummonCreature(1, Location, TempSummonType.TimedDespawn, TimeSpan.FromMilliseconds(TimeToTarget));
        }
    }

    private void DoActions(Unit unit)
    {
        var caster = IsServerSide ? unit : GetCaster();

        if (caster == null || GetTemplate() == null)
            return;

        foreach (var action in GetTemplate().Actions.Where(action => IsServerSide || UnitFitToActionRequirement(unit, caster, action)))
            switch (action.ActionType)
            {
                case AreaTriggerActionTypes.Cast:
                    caster.SpellFactory.CastSpell(unit,
                                                  action.Param,
                                                  new CastSpellExtraArgs(TriggerCastFlags.FullMask)
                                                      .SetOriginalCastId(_areaTriggerData.CreatingEffectGUID.Value.IsCast ? _areaTriggerData.CreatingEffectGUID : ObjectGuid.Empty));

                    break;
                case AreaTriggerActionTypes.AddAura:
                    caster.AddAura(action.Param, unit);

                    break;
                case AreaTriggerActionTypes.Teleport:
                    var safeLoc = ObjectManager.GetWorldSafeLoc(action.Param);

                    if (safeLoc != null && caster.TryGetAsPlayer(out var player))
                        player.TeleportTo(safeLoc.Location);

                    break;
            }
    }

    private Position GetOrbitCenterPosition()
    {
        if (CircularMovementInfo == null)
            return null;

        if (CircularMovementInfo.PathTarget.HasValue)
        {
            var center = ObjectAccessor.GetWorldObject(this, CircularMovementInfo.PathTarget.Value);

            if (center)
                return center.Location;
        }

        return CircularMovementInfo.Center.HasValue ? new Position(CircularMovementInfo.Center.Value) : null;
    }

    private void HandleUnitEnterExit(List<Unit> newTargetList)
    {
        var exitUnits = InsideUnits.ToHashSet();
        InsideUnits.Clear();

        List<Unit> enteringUnits = new();

        foreach (var unit in newTargetList)
        {
            if (!exitUnits.Remove(unit.GUID)) // erase(key_type) returns number of elements erased
                enteringUnits.Add(unit);

            InsideUnits.Add(unit.GUID); // if the unit is in the new target list we need to add it. This broke rain of fire.
        }

        // Handle after _insideUnits have been reinserted so we can use GetInsideUnits() in hooks
        foreach (var unit in enteringUnits)
        {
            var player = unit.AsPlayer;

            if (player)
            {
                if (player.IsDebugAreaTriggers)
                    player.SendSysMessage(CypherStrings.DebugAreatriggerEntered, Entry);

                player.UpdateQuestObjectiveProgress(QuestObjectiveType.AreaTriggerEnter, (int)Entry, 1);
            }

            DoActions(unit);
            ForEachAreaTriggerScript<IAreaTriggerOnUnitEnter>(a => a.OnUnitEnter(unit));
        }

        foreach (var exitUnitGuid in exitUnits)
        {
            var leavingUnit = ObjectAccessor.GetUnit(this, exitUnitGuid);

            if (!leavingUnit)
                continue;

            var player = leavingUnit.AsPlayer;

            if (player)
            {
                if (player.IsDebugAreaTriggers)
                    player.SendSysMessage(CypherStrings.DebugAreatriggerLeft, Entry);

                player.UpdateQuestObjectiveProgress(QuestObjectiveType.AreaTriggerExit, (int)Entry, 1);
            }

            UndoActions(leavingUnit);

            ForEachAreaTriggerScript<IAreaTriggerOnUnitExit>(a => a.OnUnitExit(leavingUnit));
        }
    }

    private void InitOrbit(AreaTriggerOrbitInfo orbit, uint timeToTarget)
    {
        // should be sent in object create packets only
        DoWithSuppressingObjectUpdates(() =>
        {
            SetUpdateFieldValue(Values.ModifyValue(_areaTriggerData).ModifyValue(_areaTriggerData.TimeToTarget), timeToTarget);
            _areaTriggerData.ClearChanged(_areaTriggerData.TimeToTarget);
        });

        CircularMovementInfo = orbit;

        CircularMovementInfo.TimeToTarget = timeToTarget;
        CircularMovementInfo.ElapsedTimeForMovement = 0;

        if (!Location.IsInWorld)
            return;

        AreaTriggerRePath reshape = new()
        {
            TriggerGUID = GUID,
            AreaTriggerOrbit = CircularMovementInfo
        };

        SendMessageToSet(reshape, true);
    }

    private void InitSplineOffsets(List<Vector3> offsets, uint timeToTarget)
    {
        var angleSin = (float)Math.Sin(Location.Orientation);
        var angleCos = (float)Math.Cos(Location.Orientation);

        // This is needed to rotate the spline, following caster orientation
        List<Vector3> rotatedPoints = new();

        foreach (var offset in offsets)
        {
            var x = Location.X + (offset.X * angleCos - offset.Y * angleSin);
            var y = Location.Y + (offset.Y * angleCos + offset.X * angleSin);
            var z = Location.Z;

            z = Location.UpdateAllowedPositionZ(x, y, z);
            z += offset.Z;

            rotatedPoints.Add(new Vector3(x, y, z));
        }

        InitSplines(rotatedPoints, timeToTarget);
    }

    private void LoadScripts()
    {
        _loadedScripts = ScriptManager.CreateAreaTriggerScripts(_areaTriggerId, this);

        foreach (var script in _loadedScripts)
        {
            Log.Logger.Debug("AreaTrigger.LoadScripts: Script `{0}` for AreaTrigger `{1}` is loaded now", script._GetScriptName(), _areaTriggerId);
            script.Register();

            foreach (var iFace in script.GetType().GetInterfaces())
            {
                if (iFace.Name == nameof(IAreaTriggerScript))
                    continue;

                if (!_scriptsByType.TryGetValue(iFace, out var scripts))
                {
                    scripts = new List<IAreaTriggerScript>();
                    _scriptsByType[iFace] = scripts;
                }

                scripts.Add(script);
            }
        }
    }

    private void SearchUnitInBoundedPlane(List<Unit> targetList)
    {
        SearchUnits(targetList, MaxSearchRadius, false);

        Position boxCenter = Location;
        float extentsX, extentsY;

        unsafe
        {
            extentsX = Shape.BoxDatas.Extents[0];
            extentsY = Shape.BoxDatas.Extents[1];
        }

        targetList.RemoveAll(unit => !unit.Location.IsWithinBox(boxCenter, extentsX, extentsY, MapConst.MapSize));
    }

    private void SearchUnitInBox(List<Unit> targetList)
    {
        SearchUnits(targetList, MaxSearchRadius, false);

        Position boxCenter = Location;
        float extentsX, extentsY, extentsZ;

        unsafe
        {
            extentsX = Shape.BoxDatas.Extents[0];
            extentsY = Shape.BoxDatas.Extents[1];
            extentsZ = Shape.BoxDatas.Extents[2];
        }

        targetList.RemoveAll(unit => !unit.Location.IsWithinBox(boxCenter, extentsX, extentsY, extentsZ));
    }

    private void SearchUnitInCylinder(List<Unit> targetList)
    {
        SearchUnits(targetList, MaxSearchRadius, false);

        var height = Shape.CylinderDatas.Height;
        var minZ = Location.Z - height;
        var maxZ = Location.Z + height;

        targetList.RemoveAll(unit => unit.Location.Z < minZ || unit.Location.Z > maxZ);
    }

    private void SearchUnitInDisk(List<Unit> targetList)
    {
        SearchUnits(targetList, MaxSearchRadius, false);

        var innerRadius = Shape.DiskDatas.InnerRadius;
        var height = Shape.DiskDatas.Height;
        var minZ = Location.Z - height;
        var maxZ = Location.Z + height;

        targetList.RemoveAll(unit => unit.Location.IsInDist2d(Location, innerRadius) || unit.Location.Z < minZ || unit.Location.Z > maxZ);
    }

    private void SearchUnitInPolygon(List<Unit> targetList)
    {
        SearchUnits(targetList, MaxSearchRadius, false);

        var height = Shape.PolygonDatas.Height;
        var minZ = Location.Z - height;
        var maxZ = Location.Z + height;

        targetList.RemoveAll(unit => !CheckIsInPolygon2D(unit.Location) || unit.Location.Z < minZ || unit.Location.Z > maxZ);
    }

    private void SearchUnitInSphere(List<Unit> targetList)
    {
        var radius = Shape.SphereDatas.Radius;

        if (GetTemplate() != null && GetTemplate().HasFlag(AreaTriggerFlags.HasDynamicShape))
            if (CreateProperties.MorphCurveId != 0)
                radius = MathFunctions.Lerp(Shape.SphereDatas.Radius, Shape.SphereDatas.RadiusTarget, _db2Manager.GetCurveValueAt(CreateProperties.MorphCurveId, Progress));

        SearchUnits(targetList, radius, true);
    }

    private void SearchUnits(List<Unit> targetList, float radius, bool check3D)
    {
        var check = new AnyUnitInObjectRangeCheck(this, radius, check3D);

        if (IsServerSide)
        {
            var searcher = new PlayerListSearcher(this, targetList, check);
            CellCalculator.VisitGrid(this, searcher, MaxSearchRadius);
        }
        else
        {
            var searcher = new UnitListSearcher(this, targetList, check, GridType.All);
            CellCalculator.VisitGrid(this, searcher, MaxSearchRadius);
        }
    }

    private void UndoActions(Unit unit)
    {
        if (GetTemplate() == null)
            return;

        foreach (var action in GetTemplate().Actions.Where(action => action.ActionType is AreaTriggerActionTypes.Cast or AreaTriggerActionTypes.AddAura))
            unit.RemoveAurasDueToSpell(action.Param, CasterGuid);
    }

    private bool UnitFitToActionRequirement(Unit unit, Unit caster, AreaTriggerAction action)
    {
        return action.TargetType switch
        {
            AreaTriggerActionUserTypes.Friend => caster.WorldObjectCombat.IsValidAssistTarget(unit, SpellManager.GetSpellInfo(action.Param, caster.Location.Map.DifficultyID)),
            AreaTriggerActionUserTypes.Enemy  => caster.WorldObjectCombat.IsValidAttackTarget(unit, SpellManager.GetSpellInfo(action.Param, caster.Location.Map.DifficultyID)),
            AreaTriggerActionUserTypes.Raid   => caster.IsInRaidWith(unit),
            AreaTriggerActionUserTypes.Party  => caster.IsInPartyWith(unit),
            AreaTriggerActionUserTypes.Caster => unit.GUID == caster.GUID,
            _                                 => true
        };
    }

    private void UpdateOrbitPosition()
    {
        if (CircularMovementInfo.StartDelay > ElapsedTimeForMovement)
            return;

        CircularMovementInfo.ElapsedTimeForMovement = (int)(ElapsedTimeForMovement - CircularMovementInfo.StartDelay);

        var pos = CalculateOrbitPosition();

        Location.Map.AreaTriggerRelocation(this, pos.X, pos.Y, pos.Z, pos.Orientation);

        DebugVisualizePosition();
    }

    private void UpdatePolygonOrientation()
    {
        var newOrientation = Location.Orientation;

        // No need to recalculate, orientation didn't change
        if (MathFunctions.fuzzyEq(_previousCheckOrientation, newOrientation))
            return;

        var angleSin = (float)Math.Sin(newOrientation);
        var angleCos = (float)Math.Cos(newOrientation);
        _polygonVertices = CreateProperties.PolygonVertices.ToList();

        // This is needed to rotate the vertices, following orientation
        for (var i = 0; i < _polygonVertices.Count; ++i)
        {
            var vertice = _polygonVertices[i];

            _polygonVertices[i] = new Vector2(vertice.X * angleCos - vertice.Y * angleSin, vertice.Y * angleCos + vertice.X * angleSin);
        }

        _previousCheckOrientation = newOrientation;
    }

    private void UpdateSplinePosition(uint diff)
    {
        if (_reachedDestination)
            return;

        if (!HasSplines)
            return;

        _movementTime += diff;

        if (_movementTime >= TimeToTarget)
        {
            _reachedDestination = true;
            _lastSplineIndex = Spline.Last;

            var lastSplinePosition = Spline.GetPoint(_lastSplineIndex);
            Location.Map.AreaTriggerRelocation(this, lastSplinePosition.X, lastSplinePosition.Y, lastSplinePosition.Z, Location.Orientation);

            DebugVisualizePosition();

            ForEachAreaTriggerScript<IAreaTriggerOnSplineIndexReached>(a => a.OnSplineIndexReached(_lastSplineIndex));
            ForEachAreaTriggerScript<IAreaTriggerOnDestinationReached>(a => a.OnDestinationReached());

            return;
        }

        var currentTimePercent = (float)_movementTime / TimeToTarget;

        if (currentTimePercent <= 0.0f)
            return;

        if (CreateProperties.MoveCurveId != 0)
        {
            var progress = _db2Manager.GetCurveValueAt(CreateProperties.MoveCurveId, currentTimePercent);

            if (progress is < 0.0f or > 1.0f)
                Log.Logger.Error($"AreaTrigger (Id: {Entry}, AreaTriggerCreatePropertiesId: {CreateProperties.Id}) has wrong progress ({progress}) caused by curve calculation (MoveCurveId: {CreateProperties.MorphCurveId})");
            else
                currentTimePercent = progress;
        }

        var lastPositionIndex = 0;
        float percentFromLastPoint = 0;
        Spline.ComputeIndex(currentTimePercent, ref lastPositionIndex, ref percentFromLastPoint);

        Spline.Evaluate_Percent(lastPositionIndex, percentFromLastPoint, out var currentPosition);

        var orientation = Location.Orientation;

        if (GetTemplate() != null && GetTemplate().HasFlag(AreaTriggerFlags.HasFaceMovementDir))
        {
            var nextPoint = Spline.GetPoint(lastPositionIndex + 1);
            orientation = Location.GetAbsoluteAngle(nextPoint.X, nextPoint.Y);
        }

        Location.Map.AreaTriggerRelocation(this, currentPosition.X, currentPosition.Y, currentPosition.Z, orientation);

        DebugVisualizePosition();

        if (_lastSplineIndex != lastPositionIndex)
        {
            _lastSplineIndex = lastPositionIndex;
            ForEachAreaTriggerScript<IAreaTriggerOnSplineIndexReached>(a => a.OnSplineIndexReached(_lastSplineIndex));
        }
    }

    private void UpdateTargetList()
    {
        List<Unit> targetList = new();

        switch (Shape.TriggerType)
        {
            case AreaTriggerTypes.Sphere:
                SearchUnitInSphere(targetList);

                break;
            case AreaTriggerTypes.Box:
                SearchUnitInBox(targetList);

                break;
            case AreaTriggerTypes.Polygon:
                SearchUnitInPolygon(targetList);

                break;
            case AreaTriggerTypes.Cylinder:
                SearchUnitInCylinder(targetList);

                break;
            case AreaTriggerTypes.Disk:
                SearchUnitInDisk(targetList);

                break;
            case AreaTriggerTypes.BoundedPlane:
                SearchUnitInBoundedPlane(targetList);

                break;
        }

        if (GetTemplate() != null)
        {
            var conditions = ConditionManager.GetConditionsForAreaTrigger(GetTemplate().Id.Id, GetTemplate().Id.IsServerSide);

            if (!conditions.Empty())
                targetList.RemoveAll(target => !ConditionManager.IsObjectMeetToConditions(target, conditions));
        }

        HandleUnitEnterExit(targetList);
    }
}