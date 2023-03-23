// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Framework.Constants;
using Game.Maps;
using Game.Movement;
using Game.Scripting;
using Game.Scripting.Interfaces.IAreaTrigger;
using Game.Spells;
using Game.Common.Networking;
using Game.Common.Networking.Packets.AreaTrigger;

namespace Game.Entities;

public class AreaTrigger : WorldObject
{
	static readonly List<IAreaTriggerScript> Dummy = new();
	readonly AreaTriggerFieldData _areaTriggerData;
	readonly Spline<int> _spline;
	readonly HashSet<ObjectGuid> _insideUnits = new();
	readonly Dictionary<Type, List<IAreaTriggerScript>> _scriptsByType = new();

	uint _areaTriggerId;
	ulong _spawnId;

	ObjectGuid _targetGuid;

	AuraEffect _aurEff;

	AreaTriggerShapeInfo _shape;
	float _maxSearchRadius;
	int _duration;
	int _totalDuration;
	uint _timeSinceCreated;
	float _previousCheckOrientation;
	bool _isRemoved;

	Vector3 _rollPitchYaw;
	Vector3 _targetRollPitchYaw;
	List<Vector2> _polygonVertices;

	bool _reachedDestination;
	int _lastSplineIndex;
	uint _movementTime;

	AreaTriggerOrbitInfo _orbitInfo;

	AreaTriggerCreateProperties _areaTriggerCreateProperties;
	AreaTriggerTemplate _areaTriggerTemplate;

	uint _periodicProcTimer;
	uint _basePeriodicProcTimer;
	List<AreaTriggerScript> _loadedScripts = new();

	public override uint Faction
	{
		get
		{
			var caster = GetCaster();

			if (caster)
				return caster.Faction;

			return 0;
		}
	}

	public override ObjectGuid OwnerGUID => CasterGuid;

	public bool IsServerSide => _areaTriggerTemplate.Id.IsServerSide;

	public bool IsRemoved => _isRemoved;

	public uint SpellId => _areaTriggerData.SpellID;

	public AuraEffect AuraEff => _aurEff;

	public uint TimeSinceCreated => _timeSinceCreated;

	public uint TimeToTarget => _areaTriggerData.TimeToTarget;

	public uint TimeToTargetScale => _areaTriggerData.TimeToTargetScale;

	public int Duration => _duration;

	public int TotalDuration => _totalDuration;

	public HashSet<ObjectGuid> InsideUnits => _insideUnits;

	public AreaTriggerCreateProperties CreateProperties => _areaTriggerCreateProperties;

	public ObjectGuid CasterGuid => _areaTriggerData.Caster;

	public AreaTriggerShapeInfo Shape => _shape;

	public Vector3 RollPitchYaw => _rollPitchYaw;

	public Vector3 TargetRollPitchYaw => _targetRollPitchYaw;

	public bool HasSplines => !_spline.Empty();

	public Spline<int> Spline => _spline;

	public uint ElapsedTimeForMovement => TimeSinceCreated;
	// @todo: research the right value, in sniffs both timers are nearly identical

	public AreaTriggerOrbitInfo CircularMovementInfo => _orbitInfo;

	private float Progress => TimeSinceCreated < TimeToTargetScale ? (float)TimeSinceCreated / TimeToTargetScale : 1.0f;

	private Unit Target => Global.ObjAccessor.GetUnit(this, _targetGuid);

	private float MaxSearchRadius => _maxSearchRadius;

	public AreaTrigger() : base(false)
	{
		_previousCheckOrientation = float.PositiveInfinity;
		_reachedDestination = true;

		ObjectTypeMask |= TypeMask.AreaTrigger;
		ObjectTypeId = TypeId.AreaTrigger;

		_updateFlag.Stationary = true;
		_updateFlag.AreaTrigger = true;

		_areaTriggerData = new AreaTriggerFieldData();

		_spline = new Spline<int>();
	}

	public override void AddToWorld()
	{
		// Register the AreaTrigger for guid lookup and for caster
		if (!IsInWorld)
		{
			Map.ObjectsStore.TryAdd(GUID, this);

			if (_spawnId != 0)
				Map.AreaTriggerBySpawnIdStore.Add(_spawnId, this);

			base.AddToWorld();
		}
	}

	public override void RemoveFromWorld()
	{
		// Remove the AreaTrigger from the accessor and from all lists of objects in world
		if (IsInWorld)
		{
			_isRemoved = true;

			var caster = GetCaster();

			if (caster)
				caster._UnregisterAreaTrigger(this);

			// Handle removal of all units, calling OnUnitExit & deleting auras if needed
			HandleUnitEnterExit(new List<Unit>());

			ForEachAreaTriggerScript<IAreaTriggerOnRemove>(a => a.OnRemove());

			base.RemoveFromWorld();

			if (_spawnId != 0)
				Map.AreaTriggerBySpawnIdStore.Remove(_spawnId, this);

			Map.ObjectsStore.TryRemove(GUID, out _);
		}
	}

	public static AreaTrigger CreateAreaTrigger(uint areaTriggerCreatePropertiesId, Unit caster, Unit target, SpellInfo spell, Position pos, int duration, SpellCastVisualField spellVisual, ObjectGuid castId = default, AuraEffect aurEff = null)
	{
		AreaTrigger at = new();

		if (!at.Create(areaTriggerCreatePropertiesId, caster, target, spell, pos, duration, spellVisual, castId, aurEff))
			return null;

		return at;
	}

	public static ObjectGuid CreateNewMovementForceId(Map map, uint areaTriggerId)
	{
		return ObjectGuid.Create(HighGuid.AreaTrigger, map.Id, areaTriggerId, map.GenerateLowGuid(HighGuid.AreaTrigger));
	}

	public override bool LoadFromDB(ulong spawnId, Map map, bool addToMap, bool allowDuplicate)
	{
		_spawnId = spawnId;

		var position = Global.AreaTriggerDataStorage.GetAreaTriggerSpawn(spawnId);

		if (position == null)
			return false;

		var areaTriggerTemplate = Global.AreaTriggerDataStorage.GetAreaTriggerTemplate(position.TriggerId);

		if (areaTriggerTemplate == null)
			return false;

		return CreateServer(map, areaTriggerTemplate, position);
	}

	public override void Update(uint diff)
	{
		base.Update(diff);
		_timeSinceCreated += diff;

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
					Map.AreaTriggerRelocation(this, target.Location.X, target.Location.Y, target.Location.Z, target.Location.Orientation);
			}
			else
			{
				UpdateSplinePosition(diff);
			}

			if (Duration != -1)
			{
				if (Duration > diff)
				{
					_UpdateDuration((int)(_duration - diff));
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

		if (_basePeriodicProcTimer != 0)
		{
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
	}

	public void Remove()
	{
		if (IsInWorld)
			AddObjectToRemoveList();
	}

	public void SetDuration(int newDuration)
	{
		_duration = newDuration;
		_totalDuration = newDuration;

		// negative duration (permanent areatrigger) sent as 0
		SetUpdateFieldValue(Values.ModifyValue(_areaTriggerData).ModifyValue(_areaTriggerData.Duration), (uint)Math.Max(newDuration, 0));
	}

	public AreaTriggerTemplate GetTemplate()
	{
		return _areaTriggerTemplate;
	}

	public Unit GetCaster()
	{
		return Global.ObjAccessor.GetUnit(this, CasterGuid);
	}

	public void UpdateShape()
	{
		if (_shape.IsPolygon())
			UpdatePolygonOrientation();
	}

	public void InitSplines(List<Vector3> splinePoints, uint timeToTarget)
	{
		if (splinePoints.Count < 2)
			return;

		_movementTime = 0;

		_spline.InitSpline(splinePoints.ToArray(), splinePoints.Count, EvaluationMode.Linear);
		_spline.InitLengths();

		// should be sent in object create packets only
		DoWithSuppressingObjectUpdates(() =>
		{
			SetUpdateFieldValue(Values.ModifyValue(_areaTriggerData).ModifyValue(_areaTriggerData.TimeToTarget), timeToTarget);
			_areaTriggerData.ClearChanged(_areaTriggerData.TimeToTarget);
		});

		if (IsInWorld)
		{
			if (_reachedDestination)
			{
				AreaTriggerRePath reshapeDest = new();
				reshapeDest.TriggerGUID = GUID;
				SendMessageToSet(reshapeDest, true);
			}

			AreaTriggerRePath reshape = new();
			reshape.TriggerGUID = GUID;
			reshape.AreaTriggerSpline = new AreaTriggerSplineInfo();
			reshape.AreaTriggerSpline.ElapsedTimeForMovement = ElapsedTimeForMovement;
			reshape.AreaTriggerSpline.TimeToTarget = timeToTarget;
			reshape.AreaTriggerSpline.Points = splinePoints;
			SendMessageToSet(reshape, true);
		}

		_reachedDestination = false;
	}

	public bool HasOrbit()
	{
		return _orbitInfo != null;
	}

	public void SetPeriodicProcTimer(uint periodicProctimer)
	{
		_basePeriodicProcTimer = periodicProctimer;
		_periodicProcTimer = periodicProctimer;
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

	public override bool IsNeverVisibleFor(WorldObject seer)
	{
		return base.IsNeverVisibleFor(seer) || IsServerSide;
    }

    public bool SetDestination(uint timeToTarget, Position targetPos = null, WorldObject startingObject = null)
    {
        var path = new PathGenerator(startingObject != null ? startingObject : GetCaster());
        var result = path.CalculatePath(targetPos != null ? targetPos : Location, true);

        if (!result || (path.GetPathType() & PathType.NoPath) != 0)
            return false;

        InitSplines(path.GetPath().ToList(), timeToTarget);

        return true;
    }

    public void Delay(int delaytime)
	{
		SetDuration(Duration - delaytime);
	}

	public List<IAreaTriggerScript> GetAreaTriggerScripts<T>() where T : IAreaTriggerScript
	{
		if (_scriptsByType.TryGetValue(typeof(T), out var scripts))
			return scripts;

		return Dummy;
	}

	public void ForEachAreaTriggerScript<T>(Action<T> action) where T : IAreaTriggerScript
	{
		foreach (T script in GetAreaTriggerScripts<T>())
			action.Invoke(script);
	}

	bool Create(uint areaTriggerCreatePropertiesId, Unit caster, Unit target, SpellInfo spell, Position pos, int duration, SpellCastVisualField spellVisual, ObjectGuid castId, AuraEffect aurEff)
	{
		_areaTriggerId = areaTriggerCreatePropertiesId;
		LoadScripts();
		ForEachAreaTriggerScript<IAreaTriggerOnInitialize>(a => a.OnInitialize());

		_targetGuid = target ? target.GUID : ObjectGuid.Empty;
		_aurEff = aurEff;

		Map = caster.Map;
		Location.Relocate(pos);

		if (!Location.IsPositionValid)
		{
			Log.outError(LogFilter.AreaTrigger, $"AreaTrigger (areaTriggerCreatePropertiesId: {areaTriggerCreatePropertiesId}) not created. Invalid coordinates (X: {Location.X} Y: {Location.Y})");

			return false;
		}

		ForEachAreaTriggerScript<IAreaTriggerOverrideCreateProperties>(a => _areaTriggerCreateProperties = a.AreaTriggerCreateProperties);

		if (_areaTriggerCreateProperties == null)
		{
			_areaTriggerCreateProperties = Global.AreaTriggerDataStorage.GetAreaTriggerCreateProperties(areaTriggerCreatePropertiesId);

			if (_areaTriggerCreateProperties == null)
			{
				Log.outError(LogFilter.AreaTrigger, $"AreaTrigger (areaTriggerCreatePropertiesId {areaTriggerCreatePropertiesId}) not created. Invalid areatrigger create properties id ({areaTriggerCreatePropertiesId})");

				return false;
			}
		}

		_areaTriggerTemplate = _areaTriggerCreateProperties.Template;

		Create(ObjectGuid.Create(HighGuid.AreaTrigger, Location.MapId, GetTemplate() != null ? GetTemplate().Id.Id : 0, caster.Map.GenerateLowGuid(HighGuid.AreaTrigger)));

		if (GetTemplate() != null)
			Entry = GetTemplate().Id.Id;

		SetDuration(duration);

		ObjectScale = 1.0f;

		_shape = CreateProperties.Shape;
		_maxSearchRadius = CreateProperties.GetMaxSearchRadius();

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

		UpdatePositionData();
		SetZoneScript();

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
		else if (CreateProperties.HasSplines())
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

		if (!Map.AddToMap(this))
		{
			// Returning false will cause the object to be deleted - remove from transport
			if (transport != null)
				transport.RemovePassenger(this);

			return false;
		}

		caster._RegisterAreaTrigger(this);

		ForEachAreaTriggerScript<IAreaTriggerOnCreate>(a => a.OnCreate());

		return true;
	}

	bool CreateServer(Map map, AreaTriggerTemplate areaTriggerTemplate, AreaTriggerSpawn position)
	{
		Map = map;
		Location.Relocate(position.SpawnPoint);

		if (!Location.IsPositionValid)
		{
			Log.outError(LogFilter.AreaTrigger, $"AreaTriggerServer (id {areaTriggerTemplate.Id}) not created. Invalid coordinates (X: {Location.X} Y: {Location.Y})");

			return false;
		}

		_areaTriggerTemplate = areaTriggerTemplate;

		Create(ObjectGuid.Create(HighGuid.AreaTrigger, Location.MapId, areaTriggerTemplate.Id.Id, Map.GenerateLowGuid(HighGuid.AreaTrigger)));

		Entry = areaTriggerTemplate.Id.Id;

		ObjectScale = 1.0f;

		_shape = position.Shape;
		_maxSearchRadius = _shape.GetMaxSearchRadius();

		if (position.PhaseUseFlags != 0 || position.PhaseId != 0 || position.PhaseGroup != 0)
			PhasingHandler.InitDbPhaseShift(PhaseShift, position.PhaseUseFlags, position.PhaseId, position.PhaseGroup);

		UpdateShape();

		ForEachAreaTriggerScript<IAreaTriggerOnCreate>(a => a.OnCreate());

		return true;
	}

	void _UpdateDuration(int newDuration)
	{
		_duration = newDuration;

		// should be sent in object create packets only
		DoWithSuppressingObjectUpdates(() =>
		{
			SetUpdateFieldValue(Values.ModifyValue(_areaTriggerData).ModifyValue(_areaTriggerData.Duration), (uint)_duration);
			_areaTriggerData.ClearChanged(_areaTriggerData.Duration);
		});
	}

	void UpdateTargetList()
	{
		List<Unit> targetList = new();

		switch (_shape.TriggerType)
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
			var conditions = Global.ConditionMgr.GetConditionsForAreaTrigger(GetTemplate().Id.Id, GetTemplate().Id.IsServerSide);

			if (!conditions.Empty())
				targetList.RemoveAll(target => !Global.ConditionMgr.IsObjectMeetToConditions(target, conditions));
		}

		HandleUnitEnterExit(targetList);
	}

	void SearchUnits(List<Unit> targetList, float radius, bool check3D)
	{
		var check = new AnyUnitInObjectRangeCheck(this, radius, check3D);

		if (IsServerSide)
		{
			var searcher = new PlayerListSearcher(this, targetList, check);
			Cell.VisitGrid(this, searcher, MaxSearchRadius);
		}
		else
		{
			var searcher = new UnitListSearcher(this, targetList, check, GridType.All);
			Cell.VisitGrid(this, searcher, MaxSearchRadius);
		}
	}

	void SearchUnitInSphere(List<Unit> targetList)
	{
		var radius = _shape.SphereDatas.Radius;

		if (GetTemplate() != null && GetTemplate().HasFlag(AreaTriggerFlags.HasDynamicShape))
			if (CreateProperties.MorphCurveId != 0)
				radius = MathFunctions.lerp(_shape.SphereDatas.Radius, _shape.SphereDatas.RadiusTarget, Global.DB2Mgr.GetCurveValueAt(CreateProperties.MorphCurveId, Progress));

		SearchUnits(targetList, radius, true);
	}

	void SearchUnitInBox(List<Unit> targetList)
	{
		SearchUnits(targetList, MaxSearchRadius, false);

		Position boxCenter = Location;
		float extentsX, extentsY, extentsZ;

		unsafe
		{
			extentsX = _shape.BoxDatas.Extents[0];
			extentsY = _shape.BoxDatas.Extents[1];
			extentsZ = _shape.BoxDatas.Extents[2];
		}

		targetList.RemoveAll(unit => !unit.Location.IsWithinBox(boxCenter, extentsX, extentsY, extentsZ));
	}

	void SearchUnitInPolygon(List<Unit> targetList)
	{
		SearchUnits(targetList, MaxSearchRadius, false);

		var height = _shape.PolygonDatas.Height;
		var minZ = Location.Z - height;
		var maxZ = Location.Z + height;

		targetList.RemoveAll(unit => !CheckIsInPolygon2D(unit.Location) || unit.Location.Z < minZ || unit.Location.Z > maxZ);
	}

	void SearchUnitInCylinder(List<Unit> targetList)
	{
		SearchUnits(targetList, MaxSearchRadius, false);

		var height = _shape.CylinderDatas.Height;
		var minZ = Location.Z - height;
		var maxZ = Location.Z + height;

		targetList.RemoveAll(unit => unit.Location.Z < minZ || unit.Location.Z > maxZ);
	}

	void SearchUnitInDisk(List<Unit> targetList)
	{
		SearchUnits(targetList, MaxSearchRadius, false);

		var innerRadius = _shape.DiskDatas.InnerRadius;
		var height = _shape.DiskDatas.Height;
		var minZ = Location.Z - height;
		var maxZ = Location.Z + height;

		targetList.RemoveAll(unit => unit.Location.IsInDist2d(Location, innerRadius) || unit.Location.Z < minZ || unit.Location.Z > maxZ);
	}

	void SearchUnitInBoundedPlane(List<Unit> targetList)
	{
		SearchUnits(targetList, MaxSearchRadius, false);

		Position boxCenter = Location;
		float extentsX, extentsY;

		unsafe
		{
			extentsX = _shape.BoxDatas.Extents[0];
			extentsY = _shape.BoxDatas.Extents[1];
		}

		targetList.RemoveAll(unit => { return !unit.Location.IsWithinBox(boxCenter, extentsX, extentsY, MapConst.MapSize); });
	}

	void HandleUnitEnterExit(List<Unit> newTargetList)
	{
		var exitUnits = _insideUnits.ToHashSet();
		_insideUnits.Clear();

		List<Unit> enteringUnits = new();

		foreach (var unit in newTargetList)
		{
			if (!exitUnits.Remove(unit.GUID)) // erase(key_type) returns number of elements erased
				enteringUnits.Add(unit);

			_insideUnits.Add(unit.GUID); // if the unit is in the new target list we need to add it. This broke rain of fire.
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
			var leavingUnit = Global.ObjAccessor.GetUnit(this, exitUnitGuid);

			if (leavingUnit)
			{
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
	}

	void UpdatePolygonOrientation()
	{
		var newOrientation = Location.Orientation;

		// No need to recalculate, orientation didn't change
		if (MathFunctions.fuzzyEq(_previousCheckOrientation, newOrientation))
			return;

		_polygonVertices = CreateProperties.PolygonVertices;

		var angleSin = (float)Math.Sin(newOrientation);
		var angleCos = (float)Math.Cos(newOrientation);

		// This is needed to rotate the vertices, following orientation
		for (var i = 0; i < _polygonVertices.Count; ++i)
		{
			var vertice = _polygonVertices[i];

			vertice.X = vertice.X * angleCos - vertice.Y * angleSin;
			vertice.Y = vertice.Y * angleCos + vertice.X * angleSin;
		}

		_previousCheckOrientation = newOrientation;
	}

	bool CheckIsInPolygon2D(Position pos)
	{
		var testX = pos.X;
		var testY = pos.Y;

		//this method uses the ray tracing algorithm to determine if the point is in the polygon
		var locatedInPolygon = false;

		for (var vertex = 0; vertex < _polygonVertices.Count; ++vertex)
		{
			int nextVertex;

			//repeat loop for all sets of points
			if (vertex == (_polygonVertices.Count - 1))
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
				var pointOnLine = (slopeOfLine * (testY - vertYi)) + vertXi;

				//checks to see if x-coord of testPoint is smaller than the point on the line with the same y-coord
				var isLeftToLine = testX < pointOnLine;

				if (isLeftToLine)
					//this statement changes true to false (and vice-versa)
					locatedInPolygon = !locatedInPolygon; //end if (isLeftToLine)
			}                                             //end if (withinYsEdges
		}

		return locatedInPolygon;
	}

	bool UnitFitToActionRequirement(Unit unit, Unit caster, AreaTriggerAction action)
	{
		switch (action.TargetType)
		{
			case AreaTriggerActionUserTypes.Friend:
				return caster.IsValidAssistTarget(unit, Global.SpellMgr.GetSpellInfo(action.Param, caster.Map.DifficultyID));
			case AreaTriggerActionUserTypes.Enemy:
				return caster.IsValidAttackTarget(unit, Global.SpellMgr.GetSpellInfo(action.Param, caster.Map.DifficultyID));
			case AreaTriggerActionUserTypes.Raid:
				return caster.IsInRaidWith(unit);
			case AreaTriggerActionUserTypes.Party:
				return caster.IsInPartyWith(unit);
			case AreaTriggerActionUserTypes.Caster:
				return unit.GUID == caster.GUID;
			case AreaTriggerActionUserTypes.Any:
			default:
				break;
		}

		return true;
	}

	void DoActions(Unit unit)
	{
		var caster = IsServerSide ? unit : GetCaster();

		if (caster != null && GetTemplate() != null)
			foreach (var action in GetTemplate().Actions)
				if (IsServerSide || UnitFitToActionRequirement(unit, caster, action))
					switch (action.ActionType)
					{
						case AreaTriggerActionTypes.Cast:
							caster.CastSpell(unit,
											action.Param,
											new CastSpellExtraArgs(TriggerCastFlags.FullMask)
												.SetOriginalCastId(_areaTriggerData.CreatingEffectGUID.Value.IsCast ? _areaTriggerData.CreatingEffectGUID : ObjectGuid.Empty));

							break;
						case AreaTriggerActionTypes.AddAura:
							caster.AddAura(action.Param, unit);

							break;
						case AreaTriggerActionTypes.Teleport:
							var safeLoc = Global.ObjectMgr.GetWorldSafeLoc(action.Param);

							if (safeLoc != null && caster.TryGetAsPlayer(out var player))
								player.TeleportTo(safeLoc.Loc);

							break;
					}
	}

	void UndoActions(Unit unit)
	{
		if (GetTemplate() != null)
			foreach (var action in GetTemplate().Actions)
				if (action.ActionType == AreaTriggerActionTypes.Cast || action.ActionType == AreaTriggerActionTypes.AddAura)
					unit.RemoveAurasDueToSpell(action.Param, CasterGuid);
	}

	void InitSplineOffsets(List<Vector3> offsets, uint timeToTarget)
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

			z = UpdateAllowedPositionZ(x, y, z);
			z += offset.Z;

			rotatedPoints.Add(new Vector3(x, y, z));
		}

		InitSplines(rotatedPoints, timeToTarget);
	}

	void InitOrbit(AreaTriggerOrbitInfo orbit, uint timeToTarget)
	{
		// should be sent in object create packets only
		DoWithSuppressingObjectUpdates(() =>
		{
			SetUpdateFieldValue(Values.ModifyValue(_areaTriggerData).ModifyValue(_areaTriggerData.TimeToTarget), timeToTarget);
			_areaTriggerData.ClearChanged(_areaTriggerData.TimeToTarget);
		});

		_orbitInfo = orbit;

		_orbitInfo.TimeToTarget = timeToTarget;
		_orbitInfo.ElapsedTimeForMovement = 0;

		if (IsInWorld)
		{
			AreaTriggerRePath reshape = new();
			reshape.TriggerGUID = GUID;
			reshape.AreaTriggerOrbit = _orbitInfo;

			SendMessageToSet(reshape, true);
		}
	}

	Position GetOrbitCenterPosition()
	{
		if (_orbitInfo == null)
			return null;

		if (_orbitInfo.PathTarget.HasValue)
		{
			var center = Global.ObjAccessor.GetWorldObject(this, _orbitInfo.PathTarget.Value);

			if (center)
				return center.Location;
		}

		if (_orbitInfo.Center.HasValue)
			return new Position(_orbitInfo.Center.Value);

		return null;
	}

	Position CalculateOrbitPosition()
	{
		var centerPos = GetOrbitCenterPosition();

		if (centerPos == null)
			return Location;

		var cmi = _orbitInfo;

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
			radius = MathFunctions.lerp(cmi.BlendFromRadius, cmi.Radius, blendProgress);
		}

		// Adapt Path progress depending of circle direction
		if (!cmi.CounterClockwise)
			pathProgress *= -1;

		var angle = cmi.InitialAngle + 2.0f * (float)Math.PI * pathProgress;
		var x = centerPos.X + (radius * (float)Math.Cos(angle));
		var y = centerPos.Y + (radius * (float)Math.Sin(angle));
		var z = centerPos.Z + cmi.ZOffset;

		return new Position(x, y, z, angle);
	}

	void UpdateOrbitPosition()
	{
		if (_orbitInfo.StartDelay > ElapsedTimeForMovement)
			return;

		_orbitInfo.ElapsedTimeForMovement = (int)(ElapsedTimeForMovement - _orbitInfo.StartDelay);

		var pos = CalculateOrbitPosition();

		Map.AreaTriggerRelocation(this, pos.X, pos.Y, pos.Z, pos.Orientation);

		DebugVisualizePosition();
	}

	void UpdateSplinePosition(uint diff)
	{
		if (_reachedDestination)
			return;

		if (!HasSplines)
			return;

		_movementTime += diff;

		if (_movementTime >= TimeToTarget)
		{
			_reachedDestination = true;
			_lastSplineIndex = _spline.Last();

			var lastSplinePosition = _spline.GetPoint(_lastSplineIndex);
			Map.AreaTriggerRelocation(this, lastSplinePosition.X, lastSplinePosition.Y, lastSplinePosition.Z, Location.Orientation);

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
			var progress = Global.DB2Mgr.GetCurveValueAt(CreateProperties.MoveCurveId, currentTimePercent);

			if (progress < 0.0f || progress > 1.0f)
				Log.outError(LogFilter.AreaTrigger, $"AreaTrigger (Id: {Entry}, AreaTriggerCreatePropertiesId: {CreateProperties.Id}) has wrong progress ({progress}) caused by curve calculation (MoveCurveId: {CreateProperties.MorphCurveId})");
			else
				currentTimePercent = progress;
		}

		var lastPositionIndex = 0;
		float percentFromLastPoint = 0;
		_spline.ComputeIndex(currentTimePercent, ref lastPositionIndex, ref percentFromLastPoint);

		_spline.Evaluate_Percent(lastPositionIndex, percentFromLastPoint, out var currentPosition);

		var orientation = Location.Orientation;

		if (GetTemplate() != null && GetTemplate().HasFlag(AreaTriggerFlags.HasFaceMovementDir))
		{
			var nextPoint = _spline.GetPoint(lastPositionIndex + 1);
			orientation = Location.GetAbsoluteAngle(nextPoint.X, nextPoint.Y);
		}

		Map.AreaTriggerRelocation(this, currentPosition.X, currentPosition.Y, currentPosition.Z, orientation);

		DebugVisualizePosition();

		if (_lastSplineIndex != lastPositionIndex)
		{
			_lastSplineIndex = lastPositionIndex;
			ForEachAreaTriggerScript<IAreaTriggerOnSplineIndexReached>(a => a.OnSplineIndexReached(_lastSplineIndex));
		}
	}

	[System.Diagnostics.Conditional("DEBUG")]
	void DebugVisualizePosition()
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

	void LoadScripts()
	{
		_loadedScripts = Global.ScriptMgr.CreateAreaTriggerScripts(_areaTriggerId, this);

		foreach (var script in _loadedScripts)
		{
			Log.outDebug(LogFilter.Spells, "AreaTrigger.LoadScripts: Script `{0}` for AreaTrigger `{1}` is loaded now", script._GetScriptName(), _areaTriggerId);
			script.Register();

			if (script is IAreaTriggerScript)
				foreach (var iFace in script.GetType().GetInterfaces())
				{
					if (iFace.Name == nameof(IAreaTriggerScript))
						continue;

					if (!_scriptsByType.TryGetValue(iFace, out var scripts))
					{
						scripts = new List<IAreaTriggerScript>();
						_scriptsByType[iFace] = scripts;
					}

					scripts.Add((IAreaTriggerScript)script);
				}
		}
	}

	class ValuesUpdateForPlayerWithMaskSender : IDoWork<Player>
	{
		readonly AreaTrigger _owner;
		readonly ObjectFieldData _objectMask = new();
		readonly AreaTriggerFieldData _areaTriggerMask = new();

		public ValuesUpdateForPlayerWithMaskSender(AreaTrigger owner)
		{
			_owner = owner;
		}

		public void Invoke(Player player)
		{
			UpdateData udata = new(_owner.Location.MapId);

			_owner.BuildValuesUpdateForPlayerWithMask(udata, _objectMask.GetUpdateMask(), _areaTriggerMask.GetUpdateMask(), player);

			udata.BuildPacket(out var updateObject);
			player.SendPacket(updateObject);
		}
	}
}