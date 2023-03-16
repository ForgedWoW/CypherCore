// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Numerics;
using Framework.Constants;
using Framework.Dynamic;
using Framework.Util;
using Game.AI;
using Game.DataStorage;
using Game.Loots;
using Game.Maps;
using Game.Maps.Grids;
using Game.Movement;
using Game.Networking;
using Game.Networking.Packets;
using Game.Scenarios;
using Game.Spells;

namespace Game.Entities;

public abstract class WorldObject : IDisposable
{
	public uint LastUsedScriptID;
	protected CreateObjectBits _updateFlag;
	protected bool m_isActive;
	readonly bool _isWorldObject;
	ObjectGuid _guid;
	bool _isNewObject;
	bool _isDestroyedObject;

	bool _objectUpdated;

	uint _zoneId;
	uint _areaId;
	float _staticFloorZ;
	bool _outdoors;
	ZLiquidStatus _liquidStatus;
	string _name;
	bool _isFarVisible;
	float? _visibilityDistanceOverride;

	ITransport _transport;
	Map _currMap;
	PhaseShift _phaseShift = new();
	PhaseShift _suppressedPhaseShift = new(); // contains phases for current area but not applied due to conditions
	int _dbPhase;

	NotifyFlags _notifyflags;

	ObjectGuid _privateObjectOwner;

	SmoothPhasing _smoothPhasing;

	public VariableStore VariableStorage { get; } = new();

	public TypeMask ObjectTypeMask { get; set; }
	protected TypeId ObjectTypeId { get; set; }

	public UpdateFieldHolder Values { get; set; }
	public ObjectFieldData ObjectData { get; set; }

	// Event handler
	public EventSystem Events { get; set; } = new();

	public MovementInfo MovementInfo { get; set; }
	public ZoneScript ZoneScript { get; set; }
	public uint InstanceId { get; set; }
	public bool IsInWorld { get; set; }

	public FlaggedArray32<StealthType> Stealth { get; set; } = new(2);
	public FlaggedArray32<StealthType> StealthDetect { get; set; } = new(2);

	public FlaggedArray64<InvisibilityType> Invisibility { get; set; } = new((int)InvisibilityType.Max);
	public FlaggedArray64<InvisibilityType> InvisibilityDetect { get; set; } = new((int)InvisibilityType.Max);

	public FlaggedArray32<ServerSideVisibilityType> ServerSideVisibility { get; set; } = new(2);
	public FlaggedArray32<ServerSideVisibilityType> ServerSideVisibilityDetect { get; set; } = new(2);

	public WorldLocation Location { get; set; }

	public Scenario Scenario
	{
		get
		{
			if (IsInWorld)
			{
				var instanceMap = Map.ToInstanceMap;

				if (instanceMap != null)
					return instanceMap.InstanceScenario;
			}

			return null;
		}
	}

	public ObjectGuid CharmerOrOwnerOrOwnGUID
	{
		get
		{
			var guid = CharmerOrOwnerGUID;

			if (!guid.IsEmpty)
				return guid;

			return GUID;
		}
	}

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

	// if negative it is used as PhaseGroupId
	public int DBPhase
	{
		get => _dbPhase;
		set => _dbPhase = value;
	}

	public virtual float CombatReach => SharedConst.DefaultPlayerCombatReach;

	public virtual ushort AIAnimKitId => 0;

	public virtual ushort MovementAnimKitId => 0;

	public virtual ushort MeleeAnimKitId => 0;

	// Watcher
	public bool IsPrivateObject => !_privateObjectOwner.IsEmpty;

	public ObjectGuid PrivateObjectOwner
	{
		get => _privateObjectOwner;
		set => _privateObjectOwner = value;
	}

	public ObjectGuid GUID => _guid;

	public uint Entry
	{
		get => ObjectData.EntryId;
		set => SetUpdateFieldValue(Values.ModifyValue(ObjectData).ModifyValue(ObjectData.EntryId), value);
	}

	public virtual float ObjectScale
	{
		get => ObjectData.Scale;
		set => SetUpdateFieldValue(Values.ModifyValue(ObjectData).ModifyValue(ObjectData.Scale), value);
	}

	public TypeId TypeId => ObjectTypeId;

	public bool IsDestroyedObject => _isDestroyedObject;

	public bool IsCreature => ObjectTypeId == TypeId.Unit;

	public bool IsPlayer => ObjectTypeId == TypeId.Player;

	public bool IsGameObject => ObjectTypeId == TypeId.GameObject;

	public bool IsItem => ObjectTypeId == TypeId.Item;

	public bool IsUnit => IsTypeMask(TypeMask.Unit);

	public bool IsCorpse => ObjectTypeId == TypeId.Corpse;

	public bool IsDynObject => ObjectTypeId == TypeId.DynamicObject;

	public bool IsAreaTrigger => ObjectTypeId == TypeId.AreaTrigger;

	public bool IsConversation => ObjectTypeId == TypeId.Conversation;

	public bool IsSceneObject => ObjectTypeId == TypeId.SceneObject;

	public ZoneScript ZoneScript1 => ZoneScript;

	public bool IsActiveObject => m_isActive;

	public bool IsPermanentWorldObject => _isWorldObject;

	public ITransport Transport => _transport;

	public float TransOffsetX => MovementInfo.Transport.Pos.X;

	public float TransOffsetY => MovementInfo.Transport.Pos.Y;

	public float TransOffsetZ => MovementInfo.Transport.Pos.Z;

	public float TransOffsetO => MovementInfo.Transport.Pos.Orientation;

	public uint TransTime => MovementInfo.Transport.Time;

	public sbyte TransSeat => MovementInfo.Transport.Seat;

	public virtual float StationaryX => Location.X;

	public virtual float StationaryY => Location.Y;

	public virtual float StationaryZ => Location.Z;

	public virtual float StationaryO => Location.Orientation;

	public virtual float CollisionHeight => 0.0f;

	public float MidsectionHeight => CollisionHeight / 2.0f;

	public virtual ObjectGuid OwnerGUID => default;

	public virtual ObjectGuid CharmerOrOwnerGUID => OwnerGUID;

	public virtual uint Faction { get; set; }

	NotifyFlags NotifyFlags => _notifyflags;

	Position TransOffset => MovementInfo.Transport.Pos;

	public virtual Unit CharmerOrOwner
	{
		get
		{
			var unit = AsUnit;

			if (unit != null)
			{
				return unit.CharmerOrOwner;
			}
			else
			{
				var go = AsGameObject;

				if (go != null)
					return go.OwnerUnit;
			}

			return null;
		}
	}

	public Creature AsCreature => this as Creature;

	public Player AsPlayer => this as Player;

	public GameObject AsGameObject => this as GameObject;

	public Item AsItem => this as Item;

	public Unit AsUnit => this as Unit;

	public Corpse AsCorpse => this as Corpse;

	public DynamicObject AsDynamicObject => this as DynamicObject;

	public AreaTrigger AsAreaTrigger => this as AreaTrigger;

	public Conversation AsConversation => this as Conversation;

	public SceneObject AsSceneObject => this as SceneObject;

	public virtual Unit OwnerUnit => Global.ObjAccessor.GetUnit(this, OwnerGUID);

	public Unit CharmerOrOwnerOrSelf
	{
		get
		{
			var u = CharmerOrOwner;

			if (u != null)
				return u;

			return AsUnit;
		}
	}

	public Player CharmerOrOwnerPlayerOrPlayerItself
	{
		get
		{
			var guid = CharmerOrOwnerGUID;

			if (guid.IsPlayer)
				return Global.ObjAccessor.GetPlayer(this, guid);

			return AsPlayer;
		}
	}

	public Player AffectingPlayer
	{
		get
		{
			if (CharmerOrOwnerGUID.IsEmpty)
				return AsPlayer;

			var owner = CharmerOrOwner;

			if (owner != null)
				return owner.CharmerOrOwnerPlayerOrPlayerItself;

			return null;
		}
	}

	public uint Zone => _zoneId;

	public uint Area => _areaId;

	public bool IsOutdoors => _outdoors;

	public ZLiquidStatus LiquidStatus => _liquidStatus;

	public bool IsInWorldPvpZone
	{
		get
		{
			switch (Zone)
			{
				case 4197: // Wintergrasp
				case 5095: // Tol Barad
				case 6941: // Ashran
					return true;
				default:
					return false;
			}
		}
	}

	public InstanceScript InstanceScript
	{
		get
		{
			var map = Map;

			return map.IsDungeon ? ((InstanceMap)map).InstanceScript : null;
		}
	}

	public float GridActivationRange
	{
		get
		{
			if (IsActiveObject)
			{
				if (TypeId == TypeId.Player && AsPlayer.CinematicMgr.IsOnCinematic())
					return Math.Max(SharedConst.DefaultVisibilityInstance, Map.VisibilityRange);

				return Map.VisibilityRange;
			}

			var thisCreature = AsCreature;

			if (thisCreature != null)
				return thisCreature.SightDistance;

			return 0.0f;
		}
	}

	public float VisibilityRange
	{
		get
		{
			if (IsVisibilityOverridden && !IsPlayer)
				return _visibilityDistanceOverride.Value;
			else if (IsFarVisible && !IsPlayer)
				return SharedConst.MaxVisibilityDistance;
			else
				return Map.VisibilityRange;
		}
	}

	public Map Map
	{
		get => _currMap;
		set
		{
			if (_currMap == value)
				return;

			_currMap = value;
			Location.MapId = value.Id;
			InstanceId = value.InstanceId;

			if (IsWorldObject())
				_currMap.AddWorldObject(this);
		}
	}

	public Player SpellModOwner
	{
		get
		{
			var player = AsPlayer;

			if (player != null)
				return player;

			if (IsCreature)
			{
				var creature = AsCreature;

				if (creature.IsPet || creature.IsTotem)
				{
					var owner = creature.OwnerUnit;

					if (owner != null)
						return owner.AsPlayer;
				}
			}
			else if (IsGameObject)
			{
				var go = AsGameObject;
				var owner = go.OwnerUnit;

				if (owner != null)
					return owner.AsPlayer;
			}

			return null;
		}
	}

	public float FloorZ
	{
		get
		{
			if (!IsInWorld)
				return _staticFloorZ;

			return Math.Max(_staticFloorZ, Map.GetGameObjectFloor(PhaseShift, Location.X, Location.Y, Location.Z + MapConst.ZOffsetFindHeight));
		}
	}

	bool IsFarVisible => _isFarVisible;

	bool IsVisibilityOverridden => _visibilityDistanceOverride.HasValue;

	public WorldObject(bool isWorldObject)
	{
		_name = "";
		_isWorldObject = isWorldObject;

		ServerSideVisibility.SetValue(ServerSideVisibilityType.Ghost, GhostVisibilityType.Alive | GhostVisibilityType.Ghost);
		ServerSideVisibilityDetect.SetValue(ServerSideVisibilityType.Ghost, GhostVisibilityType.Alive);

		ObjectTypeId = TypeId.Object;
		ObjectTypeMask = TypeMask.Object;

		Values = new UpdateFieldHolder(this);

		MovementInfo = new MovementInfo();
		_updateFlag.Clear();

		ObjectData = new ObjectFieldData();

		_staticFloorZ = MapConst.VMAPInvalidHeightValue;
		Location = new WorldLocation();
	}

	public virtual void Dispose()
	{
		// this may happen because there are many !create/delete
		if (IsWorldObject() && _currMap)
		{
			if (IsTypeId(TypeId.Corpse))
				Log.outFatal(LogFilter.Misc, "WorldObject.Dispose() Corpse Type: {0} ({1}) deleted but still in map!!", AsCorpse.GetCorpseType(), GUID.ToString());
			else
				ResetMap();
		}

		if (IsInWorld)
		{
			Log.outFatal(LogFilter.Misc, "WorldObject.Dispose() {0} deleted but still in world!!", GUID.ToString());

			if (IsTypeMask(TypeMask.Item))
				Log.outFatal(LogFilter.Misc, "Item slot {0}", ((Item)this).Slot);
		}

		if (_objectUpdated)
		{
			Log.outFatal(LogFilter.Misc, "WorldObject.Dispose() {0} deleted but still in update list!!", GUID.ToString());
		}
	}

	public void Create(ObjectGuid guid)
	{
		_objectUpdated = false;
		_guid = guid;
	}

	public virtual void AddToWorld()
	{
		if (IsInWorld)
			return;

		IsInWorld = true;
		ClearUpdateMask(true);

		if (Map != null)
			Map.GetZoneAndAreaId(_phaseShift, out _zoneId, out _areaId, Location.X, Location.Y, Location.Z);
	}

	public virtual void RemoveFromWorld()
	{
		if (!IsInWorld)
			return;

		if (!ObjectTypeMask.HasAnyFlag(TypeMask.Item | TypeMask.Container))
			UpdateObjectVisibilityOnDestroy();

		IsInWorld = false;
		ClearUpdateMask(true);
	}

	public void UpdatePositionData()
	{
		PositionFullTerrainStatus data = new();
		Map.GetFullTerrainStatusForPosition(_phaseShift, Location.X, Location.Y, Location.Z, data, LiquidHeaderTypeFlags.AllLiquids, CollisionHeight);
		ProcessPositionDataChanged(data);
	}

	public virtual void ProcessPositionDataChanged(PositionFullTerrainStatus data)
	{
		_zoneId = _areaId = data.AreaId;

		var area = CliDB.AreaTableStorage.LookupByKey(_areaId);

		if (area != null)
			if (area.ParentAreaID != 0)
				_zoneId = area.ParentAreaID;

		_outdoors = data.Outdoors;
		_staticFloorZ = data.FloorZ;
		_liquidStatus = data.LiquidStatus;
	}

	public virtual void BuildCreateUpdateBlockForPlayer(UpdateData data, Player target)
	{
		if (!target)
			return;

		var updateType = _isNewObject ? UpdateType.CreateObject2 : UpdateType.CreateObject;
		var tempObjectType = ObjectTypeId;
		var flags = _updateFlag;

		if (target == this)
		{
			flags.ThisIsYou = true;
			flags.ActivePlayer = true;
			tempObjectType = TypeId.ActivePlayer;
		}

		if (!flags.MovementUpdate && !MovementInfo.Transport.Guid.IsEmpty)
			flags.MovementTransport = true;

		if (AIAnimKitId != 0 || MovementAnimKitId != 0 || MeleeAnimKitId != 0)
			flags.AnimKit = true;

		if (GetSmoothPhasing()?.GetInfoForSeer(target.GUID) != null)
			flags.SmoothPhasing = true;

		var unit = AsUnit;

		if (unit)
		{
			flags.PlayHoverAnim = unit.IsPlayingHoverAnim;

			if (unit.Victim)
				flags.CombatVictim = true;
		}

		WorldPacket buffer = new();
		buffer.WriteUInt8((byte)updateType);
		buffer.WritePackedGuid(GUID);
		buffer.WriteUInt8((byte)tempObjectType);

		BuildMovementUpdate(buffer, flags, target);
		BuildValuesCreate(buffer, target);
		data.AddUpdateBlock(buffer);
	}

	public void SendUpdateToPlayer(Player player)
	{
		// send create update to player
		UpdateData upd = new(player.Location.MapId);

		if (player.HaveAtClient(this))
			BuildValuesUpdateBlockForPlayer(upd, player);
		else
			BuildCreateUpdateBlockForPlayer(upd, player);

		upd.BuildPacket(out var packet);
		player.SendPacket(packet);
	}

	public void BuildValuesUpdateBlockForPlayer(UpdateData data, Player target)
	{
		WorldPacket buffer = new();
		buffer.WriteUInt8((byte)UpdateType.Values);
		buffer.WritePackedGuid(GUID);

		BuildValuesUpdate(buffer, target);

		data.AddUpdateBlock(buffer);
	}

	public void BuildValuesUpdateBlockForPlayerWithFlag(UpdateData data, UpdateFieldFlag flags, Player target)
	{
		WorldPacket buffer = new();
		buffer.WriteUInt8((byte)UpdateType.Values);
		buffer.WritePackedGuid(GUID);

		BuildValuesUpdateWithFlag(buffer, flags, target);

		data.AddUpdateBlock(buffer);
	}

	public void BuildDestroyUpdateBlock(UpdateData data)
	{
		data.AddDestroyObject(GUID);
	}

	public void BuildOutOfRangeUpdateBlock(UpdateData data)
	{
		data.AddOutOfRangeGUID(GUID);
	}

	public virtual void DestroyForPlayer(Player target)
	{
		UpdateData updateData = new(target.Location.MapId);
		BuildDestroyUpdateBlock(updateData);
		updateData.BuildPacket(out var packet);
		target.SendPacket(packet);
	}

	public void SendOutOfRangeForPlayer(Player target)
	{
		UpdateData updateData = new(target.Location.MapId);
		BuildOutOfRangeUpdateBlock(updateData);
		updateData.BuildPacket(out var packet);
		target.SendPacket(packet);
	}

	public void BuildMovementUpdate(WorldPacket data, CreateObjectBits flags, Player target)
	{
		List<uint> PauseTimes = null;
		var go = AsGameObject;

		if (go)
			PauseTimes = go.GetPauseTimes();

		data.WriteBit(flags.NoBirthAnim);
		data.WriteBit(flags.EnablePortals);
		data.WriteBit(flags.PlayHoverAnim);
		data.WriteBit(flags.MovementUpdate);
		data.WriteBit(flags.MovementTransport);
		data.WriteBit(flags.Stationary);
		data.WriteBit(flags.CombatVictim);
		data.WriteBit(flags.ServerTime);
		data.WriteBit(flags.Vehicle);
		data.WriteBit(flags.AnimKit);
		data.WriteBit(flags.Rotation);
		data.WriteBit(flags.AreaTrigger);
		data.WriteBit(flags.GameObject);
		data.WriteBit(flags.SmoothPhasing);
		data.WriteBit(flags.ThisIsYou);
		data.WriteBit(flags.SceneObject);
		data.WriteBit(flags.ActivePlayer);
		data.WriteBit(flags.Conversation);
		data.FlushBits();

		if (flags.MovementUpdate)
		{
			var unit = AsUnit;
			var HasFallDirection = unit.HasUnitMovementFlag(MovementFlag.Falling);
			var HasFall = HasFallDirection || unit.MovementInfo.Jump.FallTime != 0;
			var HasSpline = unit.IsSplineEnabled;
			var HasInertia = unit.MovementInfo.Inertia.HasValue;
			var HasAdvFlying = unit.MovementInfo.AdvFlying.HasValue;

			data.WritePackedGuid(GUID); // MoverGUID

			data.WriteUInt32((uint)unit.GetUnitMovementFlags());
			data.WriteUInt32((uint)unit.GetUnitMovementFlags2());
			data.WriteUInt32((uint)unit.GetExtraUnitMovementFlags2());

			data.WriteUInt32(unit.MovementInfo.Time); // MoveTime
			data.WriteFloat(unit.Location.X);
			data.WriteFloat(unit.Location.Y);
			data.WriteFloat(unit.Location.Z);
			data.WriteFloat(unit.Location.Orientation);

			data.WriteFloat(unit.MovementInfo.Pitch);                // Pitch
			data.WriteFloat(unit.MovementInfo.StepUpStartElevation); // StepUpStartElevation

			data.WriteUInt32(0); // RemoveForcesIDs.size()
			data.WriteUInt32(0); // MoveIndex

			//for (public uint i = 0; i < RemoveForcesIDs.Count; ++i)
			//    *data << ObjectGuid(RemoveForcesIDs);

			data.WriteBit(!unit.MovementInfo.Transport.Guid.IsEmpty); // HasTransport
			data.WriteBit(HasFall);                                   // HasFall
			data.WriteBit(HasSpline);                                 // HasSpline - marks that the unit uses spline movement
			data.WriteBit(false);                                     // HeightChangeFailed
			data.WriteBit(false);                                     // RemoteTimeValid
			data.WriteBit(HasInertia);                                // HasInertia

			if (!unit.MovementInfo.Transport.Guid.IsEmpty)
				MovementExtensions.WriteTransportInfo(data, unit.MovementInfo.Transport);

			if (HasInertia)
			{
				data.WriteInt32(unit.MovementInfo.Inertia.Value.Id);
				data.WriteXYZ(unit.MovementInfo.Inertia.Value.Force);
				data.WriteUInt32(unit.MovementInfo.Inertia.Value.Lifetime);
			}

			if (HasAdvFlying)
			{
				data.WriteFloat(unit.MovementInfo.AdvFlying.Value.ForwardVelocity);
				data.WriteFloat(unit.MovementInfo.AdvFlying.Value.UpVelocity);
			}

			if (HasFall)
			{
				data.WriteUInt32(unit.MovementInfo.Jump.FallTime); // Time
				data.WriteFloat(unit.MovementInfo.Jump.Zspeed);    // JumpVelocity

				if (data.WriteBit(HasFallDirection))
				{
					data.WriteFloat(unit.MovementInfo.Jump.SinAngle); // Direction
					data.WriteFloat(unit.MovementInfo.Jump.CosAngle);
					data.WriteFloat(unit.MovementInfo.Jump.Xyspeed); // Speed
				}
			}

			data.WriteFloat(unit.GetSpeed(UnitMoveType.Walk));
			data.WriteFloat(unit.GetSpeed(UnitMoveType.Run));
			data.WriteFloat(unit.GetSpeed(UnitMoveType.RunBack));
			data.WriteFloat(unit.GetSpeed(UnitMoveType.Swim));
			data.WriteFloat(unit.GetSpeed(UnitMoveType.SwimBack));
			data.WriteFloat(unit.GetSpeed(UnitMoveType.Flight));
			data.WriteFloat(unit.GetSpeed(UnitMoveType.FlightBack));
			data.WriteFloat(unit.GetSpeed(UnitMoveType.TurnRate));
			data.WriteFloat(unit.GetSpeed(UnitMoveType.PitchRate));

			var movementForces = unit.MovementForces;

			if (movementForces != null)
			{
				data.WriteInt32(movementForces.GetForces().Count);
				data.WriteFloat(movementForces.ModMagnitude); // MovementForcesModMagnitude
			}
			else
			{
				data.WriteUInt32(0);
				data.WriteFloat(1.0f); // MovementForcesModMagnitude
			}

			data.WriteFloat(2.0f);   // advFlyingAirFriction
			data.WriteFloat(65.0f);  // advFlyingMaxVel
			data.WriteFloat(1.0f);   // advFlyingLiftCoefficient
			data.WriteFloat(3.0f);   // advFlyingDoubleJumpVelMod
			data.WriteFloat(10.0f);  // advFlyingGlideStartMinHeight
			data.WriteFloat(100.0f); // advFlyingAddImpulseMaxSpeed
			data.WriteFloat(90.0f);  // advFlyingMinBankingRate
			data.WriteFloat(140.0f); // advFlyingMaxBankingRate
			data.WriteFloat(180.0f); // advFlyingMinPitchingRateDown
			data.WriteFloat(360.0f); // advFlyingMaxPitchingRateDown
			data.WriteFloat(90.0f);  // advFlyingMinPitchingRateUp
			data.WriteFloat(270.0f); // advFlyingMaxPitchingRateUp
			data.WriteFloat(30.0f);  // advFlyingMinTurnVelocityThreshold
			data.WriteFloat(80.0f);  // advFlyingMaxTurnVelocityThreshold
			data.WriteFloat(2.75f);  // advFlyingSurfaceFriction
			data.WriteFloat(7.0f);   // advFlyingOverMaxDeceleration
			data.WriteFloat(0.4f);   // advFlyingLaunchSpeedCoefficient

			data.WriteBit(HasSpline);
			data.FlushBits();

			if (movementForces != null)
				foreach (var force in movementForces.GetForces())
					MovementExtensions.WriteMovementForceWithDirection(force, data, unit.Location);

			// HasMovementSpline - marks that spline data is present in packet
			if (HasSpline)
				MovementExtensions.WriteCreateObjectSplineDataBlock(unit.MoveSpline, data);
		}

		data.WriteInt32(PauseTimes != null ? PauseTimes.Count : 0);

		if (flags.Stationary)
		{
			var self = this;
			data.WriteFloat(self.StationaryX);
			data.WriteFloat(self.StationaryY);
			data.WriteFloat(self.StationaryZ);
			data.WriteFloat(self.StationaryO);
		}

		if (flags.CombatVictim)
			data.WritePackedGuid(AsUnit.Victim.GUID); // CombatVictim

		if (flags.ServerTime)
			data.WriteUInt32(GameTime.GetGameTimeMS());

		if (flags.Vehicle)
		{
			var unit = AsUnit;
			data.WriteUInt32(unit.VehicleKit1.GetVehicleInfo().Id); // RecID
			data.WriteFloat(unit.Location.Orientation);             // InitialRawFacing
		}

		if (flags.AnimKit)
		{
			data.WriteUInt16(AIAnimKitId);       // AiID
			data.WriteUInt16(MovementAnimKitId); // MovementID
			data.WriteUInt16(MeleeAnimKitId);    // MeleeID
		}

		if (flags.Rotation)
			data.WriteInt64(AsGameObject.PackedLocalRotation); // Rotation

		if (PauseTimes != null && !PauseTimes.Empty())
			foreach (var stopFrame in PauseTimes)
				data.WriteUInt32(stopFrame);

		if (flags.MovementTransport)
		{
			var self = this;
			MovementExtensions.WriteTransportInfo(data, self.MovementInfo.Transport);
		}

		if (flags.AreaTrigger)
		{
			var areaTrigger = AsAreaTrigger;
			var createProperties = areaTrigger.CreateProperties;
			var areaTriggerTemplate = areaTrigger.GetTemplate();
			var shape = areaTrigger.Shape;

			data.WriteUInt32(areaTrigger.TimeSinceCreated);

			data.WriteVector3(areaTrigger.RollPitchYaw);

			var hasAbsoluteOrientation = areaTriggerTemplate != null && areaTriggerTemplate.HasFlag(AreaTriggerFlags.HasAbsoluteOrientation);
			var hasDynamicShape = areaTriggerTemplate != null && areaTriggerTemplate.HasFlag(AreaTriggerFlags.HasDynamicShape);
			var hasAttached = areaTriggerTemplate != null && areaTriggerTemplate.HasFlag(AreaTriggerFlags.HasAttached);
			var hasFaceMovementDir = areaTriggerTemplate != null && areaTriggerTemplate.HasFlag(AreaTriggerFlags.HasFaceMovementDir);
			var hasFollowsTerrain = areaTriggerTemplate != null && areaTriggerTemplate.HasFlag(AreaTriggerFlags.HasFollowsTerrain);
			var hasUnk1 = areaTriggerTemplate != null && areaTriggerTemplate.HasFlag(AreaTriggerFlags.Unk1);
			var hasUnk2 = false;
			var hasTargetRollPitchYaw = areaTriggerTemplate != null && areaTriggerTemplate.HasFlag(AreaTriggerFlags.HasTargetRollPitchYaw);
			var hasScaleCurveID = createProperties != null && createProperties.ScaleCurveId != 0;
			var hasMorphCurveID = createProperties != null && createProperties.MorphCurveId != 0;
			var hasFacingCurveID = createProperties != null && createProperties.FacingCurveId != 0;
			var hasMoveCurveID = createProperties != null && createProperties.MoveCurveId != 0;
			var hasAreaTriggerSphere = shape.IsSphere();
			var hasAreaTriggerBox = shape.IsBox();
			var hasAreaTriggerPolygon = createProperties != null && shape.IsPolygon();
			var hasAreaTriggerCylinder = shape.IsCylinder();
			var hasDisk = shape.IsDisk();
			var hasBoundedPlane = shape.IsBoudedPlane();
			var hasAreaTriggerSpline = areaTrigger.HasSplines;
			var hasOrbit = areaTrigger.HasOrbit();
			var hasMovementScript = false;

			data.WriteBit(hasAbsoluteOrientation);
			data.WriteBit(hasDynamicShape);
			data.WriteBit(hasAttached);
			data.WriteBit(hasFaceMovementDir);
			data.WriteBit(hasFollowsTerrain);
			data.WriteBit(hasUnk1);
			data.WriteBit(hasUnk2);
			data.WriteBit(hasTargetRollPitchYaw);
			data.WriteBit(hasScaleCurveID);
			data.WriteBit(hasMorphCurveID);
			data.WriteBit(hasFacingCurveID);
			data.WriteBit(hasMoveCurveID);
			data.WriteBit(hasAreaTriggerSphere);
			data.WriteBit(hasAreaTriggerBox);
			data.WriteBit(hasAreaTriggerPolygon);
			data.WriteBit(hasAreaTriggerCylinder);
			data.WriteBit(hasDisk);
			data.WriteBit(hasBoundedPlane);
			data.WriteBit(hasAreaTriggerSpline);
			data.WriteBit(hasOrbit);
			data.WriteBit(hasMovementScript);

			data.FlushBits();

			if (hasAreaTriggerSpline)
			{
				data.WriteUInt32(areaTrigger.TimeToTarget);
				data.WriteUInt32(areaTrigger.ElapsedTimeForMovement);

				MovementExtensions.WriteCreateObjectAreaTriggerSpline(areaTrigger.Spline, data);
			}

			if (hasTargetRollPitchYaw)
				data.WriteVector3(areaTrigger.TargetRollPitchYaw);

			if (hasScaleCurveID)
				data.WriteUInt32(createProperties.ScaleCurveId);

			if (hasMorphCurveID)
				data.WriteUInt32(createProperties.MorphCurveId);

			if (hasFacingCurveID)
				data.WriteUInt32(createProperties.FacingCurveId);

			if (hasMoveCurveID)
				data.WriteUInt32(createProperties.MoveCurveId);

			if (hasAreaTriggerSphere)
			{
				data.WriteFloat(shape.SphereDatas.Radius);
				data.WriteFloat(shape.SphereDatas.RadiusTarget);
			}

			if (hasAreaTriggerBox)
				unsafe
				{
					data.WriteFloat(shape.BoxDatas.Extents[0]);
					data.WriteFloat(shape.BoxDatas.Extents[1]);
					data.WriteFloat(shape.BoxDatas.Extents[2]);

					data.WriteFloat(shape.BoxDatas.ExtentsTarget[0]);
					data.WriteFloat(shape.BoxDatas.ExtentsTarget[1]);
					data.WriteFloat(shape.BoxDatas.ExtentsTarget[2]);
				}

			if (hasAreaTriggerPolygon)
			{
				data.WriteInt32(createProperties.PolygonVertices.Count);
				data.WriteInt32(createProperties.PolygonVerticesTarget.Count);
				data.WriteFloat(shape.PolygonDatas.Height);
				data.WriteFloat(shape.PolygonDatas.HeightTarget);

				foreach (var vertice in createProperties.PolygonVertices)
					data.WriteVector2(vertice);

				foreach (var vertice in createProperties.PolygonVerticesTarget)
					data.WriteVector2(vertice);
			}

			if (hasAreaTriggerCylinder)
			{
				data.WriteFloat(shape.CylinderDatas.Radius);
				data.WriteFloat(shape.CylinderDatas.RadiusTarget);
				data.WriteFloat(shape.CylinderDatas.Height);
				data.WriteFloat(shape.CylinderDatas.HeightTarget);
				data.WriteFloat(shape.CylinderDatas.LocationZOffset);
				data.WriteFloat(shape.CylinderDatas.LocationZOffsetTarget);
			}

			if (hasDisk)
			{
				data.WriteFloat(shape.DiskDatas.InnerRadius);
				data.WriteFloat(shape.DiskDatas.InnerRadiusTarget);
				data.WriteFloat(shape.DiskDatas.OuterRadius);
				data.WriteFloat(shape.DiskDatas.OuterRadiusTarget);
				data.WriteFloat(shape.DiskDatas.Height);
				data.WriteFloat(shape.DiskDatas.HeightTarget);
				data.WriteFloat(shape.DiskDatas.LocationZOffset);
				data.WriteFloat(shape.DiskDatas.LocationZOffsetTarget);
			}

			if (hasBoundedPlane)
				unsafe
				{
					data.WriteFloat(shape.BoundedPlaneDatas.Extents[0]);
					data.WriteFloat(shape.BoundedPlaneDatas.Extents[1]);
					data.WriteFloat(shape.BoundedPlaneDatas.ExtentsTarget[0]);
					data.WriteFloat(shape.BoundedPlaneDatas.ExtentsTarget[1]);
				}

			//if (hasMovementScript)
			//    *data << *areaTrigger.GetMovementScript(); // AreaTriggerMovementScriptInfo

			if (hasOrbit)
				areaTrigger.CircularMovementInfo.Write(data);
		}

		if (flags.GameObject)
		{
			var bit8 = false;
			uint Int1 = 0;

			var gameObject = AsGameObject;

			data.WriteUInt32(gameObject.WorldEffectID);

			data.WriteBit(bit8);
			data.FlushBits();

			if (bit8)
				data.WriteUInt32(Int1);
		}

		if (flags.SmoothPhasing)
		{
			var smoothPhasingInfo = GetSmoothPhasing().GetInfoForSeer(target.GUID);

			data.WriteBit(smoothPhasingInfo.ReplaceActive);
			data.WriteBit(smoothPhasingInfo.StopAnimKits);
			data.WriteBit(smoothPhasingInfo.ReplaceObject.HasValue);
			data.FlushBits();

			if (smoothPhasingInfo.ReplaceObject.HasValue)
				data.WritePackedGuid(smoothPhasingInfo.ReplaceObject.Value);
		}

		if (flags.SceneObject)
		{
			data.WriteBit(false); // HasLocalScriptData
			data.WriteBit(false); // HasPetBattleFullUpdate
			data.FlushBits();

			//    if (HasLocalScriptData)
			//    {
			//        data.WriteBits(Data.length(), 7);
			//        data.FlushBits();
			//        data.WriteString(Data);
			//    }

			//    if (HasPetBattleFullUpdate)
			//    {
			//        for (std::size_t i = 0; i < 2; ++i)
			//        {
			//            *data << ObjectGuid(Players[i].CharacterID);
			//            *data << int32(Players[i].TrapAbilityID);
			//            *data << int32(Players[i].TrapStatus);
			//            *data << uint16(Players[i].RoundTimeSecs);
			//            *data << int8(Players[i].FrontPet);
			//            *data << uint8(Players[i].InputFlags);

			//            data.WriteBits(Players[i].Pets.size(), 2);
			//            data.FlushBits();
			//            for (std::size_t j = 0; j < Players[i].Pets.size(); ++j)
			//            {
			//                *data << ObjectGuid(Players[i].Pets[j].BattlePetGUID);
			//                *data << int32(Players[i].Pets[j].SpeciesID);
			//                *data << int32(Players[i].Pets[j].CreatureID);
			//                *data << int32(Players[i].Pets[j].DisplayID);
			//                *data << int16(Players[i].Pets[j].Level);
			//                *data << int16(Players[i].Pets[j].Xp);
			//                *data << int32(Players[i].Pets[j].CurHealth);
			//                *data << int32(Players[i].Pets[j].MaxHealth);
			//                *data << int32(Players[i].Pets[j].Power);
			//                *data << int32(Players[i].Pets[j].Speed);
			//                *data << int32(Players[i].Pets[j].NpcTeamMemberID);
			//                *data << uint16(Players[i].Pets[j].BreedQuality);
			//                *data << uint16(Players[i].Pets[j].StatusFlags);
			//                *data << int8(Players[i].Pets[j].Slot);

			//                *data << uint(Players[i].Pets[j].Abilities.size());
			//                *data << uint(Players[i].Pets[j].Auras.size());
			//                *data << uint(Players[i].Pets[j].States.size());
			//                for (std::size_t k = 0; k < Players[i].Pets[j].Abilities.size(); ++k)
			//                {
			//                    *data << int32(Players[i].Pets[j].Abilities[k].AbilityID);
			//                    *data << int16(Players[i].Pets[j].Abilities[k].CooldownRemaining);
			//                    *data << int16(Players[i].Pets[j].Abilities[k].LockdownRemaining);
			//                    *data << int8(Players[i].Pets[j].Abilities[k].AbilityIndex);
			//                    *data << uint8(Players[i].Pets[j].Abilities[k].Pboid);
			//                }

			//                for (std::size_t k = 0; k < Players[i].Pets[j].Auras.size(); ++k)
			//                {
			//                    *data << int32(Players[i].Pets[j].Auras[k].AbilityID);
			//                    *data << uint(Players[i].Pets[j].Auras[k].InstanceID);
			//                    *data << int32(Players[i].Pets[j].Auras[k].RoundsRemaining);
			//                    *data << int32(Players[i].Pets[j].Auras[k].CurrentRound);
			//                    *data << uint8(Players[i].Pets[j].Auras[k].CasterPBOID);
			//                }

			//                for (std::size_t k = 0; k < Players[i].Pets[j].States.size(); ++k)
			//                {
			//                    *data << uint(Players[i].Pets[j].States[k].StateID);
			//                    *data << int32(Players[i].Pets[j].States[k].StateValue);
			//                }

			//                data.WriteBits(Players[i].Pets[j].CustomName.length(), 7);
			//                data.FlushBits();
			//                data.WriteString(Players[i].Pets[j].CustomName);
			//            }
			//        }

			//        for (std::size_t i = 0; i < 3; ++i)
			//        {
			//            *data << uint(Enviros[j].Auras.size());
			//            *data << uint(Enviros[j].States.size());
			//            for (std::size_t j = 0; j < Enviros[j].Auras.size(); ++j)
			//            {
			//                *data << int32(Enviros[j].Auras[j].AbilityID);
			//                *data << uint(Enviros[j].Auras[j].InstanceID);
			//                *data << int32(Enviros[j].Auras[j].RoundsRemaining);
			//                *data << int32(Enviros[j].Auras[j].CurrentRound);
			//                *data << uint8(Enviros[j].Auras[j].CasterPBOID);
			//            }

			//            for (std::size_t j = 0; j < Enviros[j].States.size(); ++j)
			//            {
			//                *data << uint(Enviros[i].States[j].StateID);
			//                *data << int32(Enviros[i].States[j].StateValue);
			//            }
			//        }

			//        *data << uint16(WaitingForFrontPetsMaxSecs);
			//        *data << uint16(PvpMaxRoundTime);
			//        *data << int32(CurRound);
			//        *data << uint(NpcCreatureID);
			//        *data << uint(NpcDisplayID);
			//        *data << int8(CurPetBattleState);
			//        *data << uint8(ForfeitPenalty);
			//        *data << ObjectGuid(InitialWildPetGUID);
			//        data.WriteBit(IsPVP);
			//        data.WriteBit(CanAwardXP);
			//        data.FlushBits();
			//    }
		}

		if (flags.ActivePlayer)
		{
			var player = AsPlayer;

			var HasSceneInstanceIDs = !player.SceneMgr.GetSceneTemplateByInstanceMap().Empty();
			var HasRuneState = AsUnit.GetPowerIndex(PowerType.Runes) != (int)PowerType.Max;

			data.WriteBit(HasSceneInstanceIDs);
			data.WriteBit(HasRuneState);
			data.FlushBits();

			if (HasSceneInstanceIDs)
			{
				data.WriteInt32(player.SceneMgr.GetSceneTemplateByInstanceMap().Count);

				foreach (var pair in player.SceneMgr.GetSceneTemplateByInstanceMap())
					data.WriteUInt32(pair.Key);
			}

			if (HasRuneState)
			{
				float baseCd = player.GetRuneBaseCooldown();
				var maxRunes = (uint)player.GetMaxPower(PowerType.Runes);

				data.WriteUInt8((byte)((1 << (int)maxRunes) - 1u));
				data.WriteUInt8(player.GetRunesState());
				data.WriteUInt32(maxRunes);

				for (byte i = 0; i < maxRunes; ++i)
					data.WriteUInt8((byte)((baseCd - (float)player.GetRuneCooldown(i)) / baseCd * 255));
			}
		}

		if (flags.Conversation)
		{
			var self = AsConversation;

			if (data.WriteBit(self.GetTextureKitId() != 0))
				data.WriteUInt32(self.GetTextureKitId());

			data.FlushBits();
		}
	}

	public void DoWithSuppressingObjectUpdates(Action action)
	{
		var wasUpdatedBeforeAction = _objectUpdated;
		action();

		if (_objectUpdated && !wasUpdatedBeforeAction)
		{
			RemoveFromObjectUpdate();
			_objectUpdated = false;
		}
	}

	public virtual UpdateFieldFlag GetUpdateFieldFlagsFor(Player target)
	{
		return UpdateFieldFlag.None;
	}

	public virtual void BuildValuesUpdateWithFlag(WorldPacket data, UpdateFieldFlag flags, Player target)
	{
		data.WriteUInt32(0);
		data.WriteUInt32(0);
	}

	public void AddToObjectUpdateIfNeeded()
	{
		if (IsInWorld && !_objectUpdated)
			_objectUpdated = AddToObjectUpdate();
	}

	public virtual void ClearUpdateMask(bool remove)
	{
		Values.ClearChangesMask(ObjectData);

		if (_objectUpdated)
		{
			if (remove)
				RemoveFromObjectUpdate();

			_objectUpdated = false;
		}
	}

	public void BuildFieldsUpdate(Player player, Dictionary<Player, UpdateData> data_map)
	{
		if (!data_map.ContainsKey(player))
			data_map.Add(player, new UpdateData(player.Location.MapId));

		BuildValuesUpdateBlockForPlayer(data_map[player], player);
	}

	public virtual string GetDebugInfo()
	{
		return $"{Location.GetDebugInfo()}\n{GUID} Entry: {Entry}\nName: {GetName()}";
	}

	public virtual Loot GetLootForPlayer(Player player)
	{
		return null;
	}

	public abstract void BuildValuesCreate(WorldPacket data, Player target);
	public abstract void BuildValuesUpdate(WorldPacket data, Player target);

	public void SetUpdateFieldValue<T>(IUpdateField<T> updateField, T newValue)
	{
		if (!newValue.Equals(updateField.GetValue()))
		{
			updateField.SetValue(newValue);
			AddToObjectUpdateIfNeeded();
		}
	}

	public void SetUpdateFieldValue<T>(ref T value, T newValue) where T : new()
	{
		if (!newValue.Equals(value))
		{
			value = newValue;
			AddToObjectUpdateIfNeeded();
		}
	}

	public void SetUpdateFieldValue<T>(DynamicUpdateField<T> updateField, int index, T newValue) where T : new()
	{
		if (!newValue.Equals(updateField[index]))
		{
			updateField[index] = newValue;
			AddToObjectUpdateIfNeeded();
		}
	}

	public void SetUpdateFieldFlagValue<T>(IUpdateField<T> updateField, T flag) where T : new()
	{
		//static_assert(std::is_integral < T >::value, "SetUpdateFieldFlagValue must be used with integral types");
		SetUpdateFieldValue(updateField, (T)(updateField.GetValue() | (dynamic)flag));
	}

	public void SetUpdateFieldFlagValue<T>(ref T value, T flag) where T : new()
	{
		//static_assert(std::is_integral < T >::value, "SetUpdateFieldFlagValue must be used with integral types");
		SetUpdateFieldValue(ref value, (T)(value | (dynamic)flag));
	}

	public void RemoveUpdateFieldFlagValue<T>(IUpdateField<T> updateField, T flag)
	{
		//static_assert(std::is_integral < T >::value, "SetUpdateFieldFlagValue must be used with integral types");
		SetUpdateFieldValue(updateField, (T)(updateField.GetValue() & ~(dynamic)flag));
	}

	public void RemoveUpdateFieldFlagValue<T>(ref T value, T flag) where T : new()
	{
		//static_assert(std::is_integral < T >::value, "RemoveUpdateFieldFlagValue must be used with integral types");
		SetUpdateFieldValue(ref value, (T)(value & ~(dynamic)flag));
	}

	public void AddDynamicUpdateFieldValue<T>(DynamicUpdateField<T> updateField, T value) where T : new()
	{
		AddToObjectUpdateIfNeeded();
		updateField.AddValue(value);
	}

	public void InsertDynamicUpdateFieldValue<T>(DynamicUpdateField<T> updateField, int index, T value) where T : new()
	{
		AddToObjectUpdateIfNeeded();
		updateField.InsertValue(index, value);
	}

	public void RemoveDynamicUpdateFieldValue<T>(DynamicUpdateField<T> updateField, int index) where T : new()
	{
		AddToObjectUpdateIfNeeded();
		updateField.RemoveValue(index);
	}

	public void ClearDynamicUpdateFieldValues<T>(DynamicUpdateField<T> updateField) where T : new()
	{
		AddToObjectUpdateIfNeeded();
		updateField.Clear();
	}

	// stat system helpers
	public void SetUpdateFieldStatValue<T>(IUpdateField<T> updateField, T value) where T : new()
	{
		SetUpdateFieldValue(updateField, (T)Math.Max((dynamic)value, 0));
	}

	public void SetUpdateFieldStatValue<T>(ref T oldValue, T value) where T : new()
	{
		SetUpdateFieldValue(ref oldValue, (T)Math.Max((dynamic)value, 0));
	}

	public void ApplyModUpdateFieldValue<T>(IUpdateField<T> updateField, T mod, bool apply) where T : new()
	{
		dynamic value = updateField.GetValue();

		if (apply)
			value += mod;
		else
			value -= mod;

		SetUpdateFieldValue(updateField, (T)value);
	}

	public void ApplyModUpdateFieldValue<T>(ref T oldvalue, T mod, bool apply) where T : new()
	{
		dynamic value = oldvalue;

		if (apply)
			value += mod;
		else
			value -= mod;

		SetUpdateFieldValue(ref oldvalue, (T)value);
	}

	public void ApplyPercentModUpdateFieldValue<T>(IUpdateField<T> updateField, float percent, bool apply) where T : new()
	{
		dynamic value = updateField.GetValue();

		if (percent == -100.0f)
			percent = -99.99f;

		value *= (apply ? (100.0f + percent) / 100.0f : 100.0f / (100.0f + percent));

		SetUpdateFieldValue(updateField, (T)value);
	}

	public void ApplyPercentModUpdateFieldValue<T>(ref T oldValue, float percent, bool apply) where T : new()
	{
		dynamic value = oldValue;

		if (percent == -100.0f)
			percent = -99.99f;

		value *= (apply ? (100.0f + percent) / 100.0f : 100.0f / (100.0f + percent));

		SetUpdateFieldValue(ref oldValue, (T)value);
	}

	public void ForceUpdateFieldChange()
	{
		AddToObjectUpdateIfNeeded();
	}

	public bool IsWorldObject()
	{
		if (_isWorldObject)
			return true;

		if (IsTypeId(TypeId.Unit) && AsCreature.IsTempWorldObject)
			return true;

		return false;
	}

	public virtual void Update(uint diff)
	{
		Events.Update(diff);
	}

	public void SetWorldObject(bool on)
	{
		if (!IsInWorld)
			return;

		Map.AddObjectToSwitchList(this, on);
	}

	public void SetActive(bool on)
	{
		if (m_isActive == on)
			return;

		if (IsTypeId(TypeId.Player))
			return;

		m_isActive = on;

		if (on && !IsInWorld)
			return;

		var map = Map;

		if (map == null)
			return;

		if (on)
			map.AddToActive(this);
		else
			map.RemoveFromActive(this);
	}

	public void SetFarVisible(bool on)
	{
		if (IsPlayer)
			return;

		_isFarVisible = on;
	}

	public void SetVisibilityDistanceOverride(VisibilityDistanceType type)
	{
		if (TypeId == TypeId.Player)
			return;

		var creature = AsCreature;

		if (creature != null)
		{
			creature.RemoveUnitFlag2(UnitFlags2.LargeAoi | UnitFlags2.GiganticAoi | UnitFlags2.InfiniteAoi);

			switch (type)
			{
				case VisibilityDistanceType.Large:
					creature.SetUnitFlag2(UnitFlags2.LargeAoi);

					break;
				case VisibilityDistanceType.Gigantic:
					creature.SetUnitFlag2(UnitFlags2.GiganticAoi);

					break;
				case VisibilityDistanceType.Infinite:
					creature.SetUnitFlag2(UnitFlags2.InfiniteAoi);

					break;
				default:
					break;
			}
		}

		_visibilityDistanceOverride = SharedConst.VisibilityDistances[(int)type];
	}

	public virtual void CleanupsBeforeDelete(bool finalCleanup = true)
	{
		if (IsInWorld)
			RemoveFromWorld();

		var transport = Transport;

		if (transport != null)
			transport.RemovePassenger(this);

		Events.KillAllEvents(false); // non-delatable (currently cast spells) will not deleted now but it will deleted at call in Map::RemoveAllObjectsInRemoveList
	}

	public void GetZoneAndAreaId(out uint zoneid, out uint areaid)
	{
		zoneid = _zoneId;
		areaid = _areaId;
	}

	public float GetSightRange(WorldObject target = null)
	{
		if (IsPlayer || IsCreature)
		{
			if (IsPlayer)
			{
				if (target != null && target.IsVisibilityOverridden && !target.IsPlayer)
					return target._visibilityDistanceOverride.Value;
				else if (target != null && target.IsFarVisible && !target.IsPlayer)
					return SharedConst.MaxVisibilityDistance;
				else if (AsPlayer.CinematicMgr.IsOnCinematic())
					return SharedConst.DefaultVisibilityInstance;
				else
					return Map.VisibilityRange;
			}
			else if (IsCreature)
			{
				return AsCreature.SightDistance;
			}
			else
			{
				return SharedConst.SightRangeUnit;
			}
		}

		if (IsDynObject && IsActiveObject)
			return Map.VisibilityRange;

		return 0.0f;
	}

	public bool CheckPrivateObjectOwnerVisibility(WorldObject seer)
	{
		if (!IsPrivateObject)
			return true;

		// Owner of this private object
		if (_privateObjectOwner == seer.GUID)
			return true;

		// Another private object of the same owner
		if (_privateObjectOwner == seer.PrivateObjectOwner)
			return true;

		var playerSeer = seer.AsPlayer;

		if (playerSeer != null)
			if (playerSeer.IsInGroup(_privateObjectOwner))
				return true;

		return false;
	}

	public SmoothPhasing GetOrCreateSmoothPhasing()
	{
		if (_smoothPhasing == null)
			_smoothPhasing = new SmoothPhasing();

		return _smoothPhasing;
	}

	public SmoothPhasing GetSmoothPhasing()
	{
		return _smoothPhasing;
	}

	public bool CanSeeOrDetect(WorldObject obj, bool ignoreStealth = false, bool distanceCheck = false, bool checkAlert = false)
	{
		if (this == obj)
			return true;

		if (obj.IsNeverVisibleFor(this) || CanNeverSee(obj))
			return false;

		if (obj.IsAlwaysVisibleFor(this) || CanAlwaysSee(obj))
			return true;

		if (!obj.CheckPrivateObjectOwnerVisibility(this))
			return false;

		var smoothPhasing = obj.GetSmoothPhasing();

		if (smoothPhasing != null && smoothPhasing.IsBeingReplacedForSeer(GUID))
			return false;

		if (!obj.IsPrivateObject && !Global.ConditionMgr.IsObjectMeetingVisibilityByObjectIdConditions((uint)obj.TypeId, obj.Entry, this))
			return false;

		var corpseVisibility = false;

		if (distanceCheck)
		{
			var corpseCheck = false;
			var thisPlayer = AsPlayer;

			if (thisPlayer != null)
			{
				if (thisPlayer.IsDead &&
					thisPlayer.Health > 0 && // Cheap way to check for ghost state
					!Convert.ToBoolean(obj.ServerSideVisibility.GetValue(ServerSideVisibilityType.Ghost) & ServerSideVisibility.GetValue(ServerSideVisibilityType.Ghost) & (uint)GhostVisibilityType.Ghost))
				{
					var corpse = thisPlayer.GetCorpse();

					if (corpse != null)
					{
						corpseCheck = true;

						if (corpse.IsWithinDist(thisPlayer, GetSightRange(obj), false))
							if (corpse.IsWithinDist(obj, GetSightRange(obj), false))
								corpseVisibility = true;
					}
				}

				var target = obj.AsUnit;

				if (target)
				{
					// Don't allow to detect vehicle accessories if you can't see vehicle
					var vehicle = target.VehicleBase;

					if (vehicle)
						if (!thisPlayer.HaveAtClient(vehicle))
							return false;
				}
			}

			var viewpoint = this;
			var player = AsPlayer;

			if (player != null)
				viewpoint = player.Viewpoint;

			if (viewpoint == null)
				viewpoint = this;

			if (!corpseCheck && !viewpoint.IsWithinDist(obj, GetSightRange(obj), false))
				return false;
		}

		// GM visibility off or hidden NPC
		if (obj.ServerSideVisibility.GetValue(ServerSideVisibilityType.GM) == 0)
		{
			// Stop checking other things for GMs
			if (ServerSideVisibilityDetect.GetValue(ServerSideVisibilityType.GM) != 0)
				return true;
		}
		else
		{
			return ServerSideVisibilityDetect.GetValue(ServerSideVisibilityType.GM) >= obj.ServerSideVisibility.GetValue(ServerSideVisibilityType.GM);
		}

		// Ghost players, Spirit Healers, and some other NPCs
		if (!corpseVisibility && !Convert.ToBoolean(obj.ServerSideVisibility.GetValue(ServerSideVisibilityType.Ghost) & ServerSideVisibilityDetect.GetValue(ServerSideVisibilityType.Ghost)))
		{
			// Alive players can see dead players in some cases, but other objects can't do that
			var thisPlayer = AsPlayer;

			if (thisPlayer != null)
			{
				var objPlayer = obj.AsPlayer;

				if (objPlayer != null)
				{
					if (!thisPlayer.IsGroupVisibleFor(objPlayer))
						return false;
				}
				else
				{
					return false;
				}
			}
			else
			{
				return false;
			}
		}

		if (obj.IsInvisibleDueToDespawn(this))
			return false;

		if (!CanDetect(obj, ignoreStealth, checkAlert))
			return false;

		return true;
	}

	public virtual bool CanNeverSee(WorldObject obj)
	{
		return Map != obj.Map || !InSamePhase(obj);
	}

	public virtual bool CanAlwaysSee(WorldObject obj)
	{
		return false;
	}

	public virtual void SendMessageToSet(ServerPacket packet, bool self)
	{
		if (IsInWorld)
			SendMessageToSetInRange(packet, VisibilityRange, self);
	}

	public virtual void SendMessageToSetInRange(ServerPacket data, float dist, bool self)
	{
		PacketSenderRef sender = new(data);
		MessageDistDeliverer<PacketSenderRef> notifier = new(this, sender, dist);
		Cell.VisitGrid(this, notifier, dist);
	}

	public virtual void SendMessageToSet(ServerPacket data, Player skip)
	{
		PacketSenderRef sender = new(data);
		var notifier = new MessageDistDeliverer<PacketSenderRef>(this, sender, VisibilityRange, false, skip);
		Cell.VisitGrid(this, notifier, VisibilityRange);
	}

	public void SendCombatLogMessage(CombatLogServerPacket combatLog)
	{
		CombatLogSender combatLogSender = new(combatLog);

		var self = AsPlayer;

		if (self != null)
			combatLogSender.Invoke(self);

		MessageDistDeliverer<CombatLogSender> notifier = new(this, combatLogSender, VisibilityRange);
		Cell.VisitGrid(this, notifier, VisibilityRange);
	}

	public virtual void ResetMap()
	{
		if (_currMap == null)
			return;

		if (IsWorldObject())
			_currMap.RemoveWorldObject(this);

		_currMap = null;
	}

	public void AddObjectToRemoveList()
	{
		var map = Map;

		if (map == null)
		{
			Log.outError(LogFilter.Server, "Object (TypeId: {0} Entry: {1} GUID: {2}) at attempt add to move list not have valid map (Id: {3}).", TypeId, Entry, GUID.ToString(), Location.MapId);

			return;
		}

		map.AddObjectToRemoveList(this);
	}

	public ZoneScript FindZoneScript()
	{
		var map = Map;

		if (map != null)
		{
			var instanceMap = map.ToInstanceMap;

			if (instanceMap != null)
				return (ZoneScript)instanceMap.InstanceScript;

			var bgMap = map.ToBattlegroundMap;

			if (bgMap != null)
				return (ZoneScript)bgMap.GetBG();

			if (!map.IsBattlegroundOrArena)
			{
				var bf = Global.BattleFieldMgr.GetBattlefieldToZoneId(map, Zone);

				if (bf != null)
					return bf;

				return Global.OutdoorPvPMgr.GetOutdoorPvPToZoneId(map, Zone);
			}
		}

		return null;
	}

	public void SetZoneScript()
	{
		ZoneScript = FindZoneScript();
	}

	public TempSummon SummonCreature(uint entry, float x, float y, float z, float o = 0, TempSummonType despawnType = TempSummonType.ManualDespawn, TimeSpan despawnTime = default, uint vehId = 0, uint spellId = 0, ObjectGuid privateObjectOwner = default)
	{
		return SummonCreature(entry, new Position(x, y, z, o), despawnType, despawnTime, vehId, spellId, privateObjectOwner);
	}

	public TempSummon SummonCreature(uint entry, Position pos, TempSummonType despawnType = TempSummonType.ManualDespawn, TimeSpan despawnTime = default, uint vehId = 0, uint spellId = 0, ObjectGuid privateObjectOwner = default)
	{
		if (pos.IsDefault)
			GetClosePoint(pos, CombatReach);

		if (pos.Orientation == 0.0f)
			pos.Orientation = Location.Orientation;

		var map = Map;

		if (map != null)
		{
			var summon = map.SummonCreature(entry, pos, null, (uint)despawnTime.TotalMilliseconds, this, spellId, vehId, privateObjectOwner);

			if (summon != null)
			{
				summon.SetTempSummonType(despawnType);

				return summon;
			}
		}

		return null;
	}

	public TempSummon SummonPersonalClone(Position pos, TempSummonType despawnType = TempSummonType.ManualDespawn, TimeSpan despawnTime = default, uint vehId = 0, uint spellId = 0, Player privateObjectOwner = null)
	{
		var map = Map;

		if (map != null)
		{
			var summon = map.SummonCreature(Entry, pos, null, (uint)despawnTime.TotalMilliseconds, privateObjectOwner, spellId, vehId, privateObjectOwner.GUID, new SmoothPhasingInfo(GUID, true, true));

			if (summon != null)
			{
				summon.SetTempSummonType(despawnType);

				return summon;
			}
		}

		return null;
	}

	public GameObject SummonGameObject(uint entry, float x, float y, float z, float ang, Quaternion rotation, TimeSpan respawnTime, GameObjectSummonType summonType = GameObjectSummonType.TimedOrCorpseDespawn)
	{
		return SummonGameObject(entry, new Position(x, y, z, ang), rotation, respawnTime, summonType);
	}

	public GameObject SummonGameObject(uint entry, Position pos, Quaternion rotation, TimeSpan respawnTime, GameObjectSummonType summonType = GameObjectSummonType.TimedOrCorpseDespawn)
	{
		if (pos.IsDefault)
		{
			GetClosePoint(pos, CombatReach);
			pos.Orientation = Location.Orientation;
		}

		if (!IsInWorld)
			return null;

		var goinfo = Global.ObjectMgr.GetGameObjectTemplate(entry);

		if (goinfo == null)
		{
			Log.outError(LogFilter.Sql, "Gameobject template {0} not found in database!", entry);

			return null;
		}

		var map = Map;
		var go = GameObject.CreateGameObject(entry, map, pos, rotation, 255, GameObjectState.Ready);

		if (!go)
			return null;

		PhasingHandler.InheritPhaseShift(go, this);

		go.SetRespawnTime((int)respawnTime.TotalSeconds);

		if (IsPlayer || (IsCreature && summonType == GameObjectSummonType.TimedOrCorpseDespawn)) //not sure how to handle this
			AsUnit.AddGameObject(go);
		else
			go.SetSpawnedByDefault(false);

		map.AddToMap(go);

		return go;
	}

	public Creature SummonTrigger(Position pos, TimeSpan despawnTime, CreatureAI AI = null)
	{
		var summonType = (despawnTime == TimeSpan.Zero) ? TempSummonType.DeadDespawn : TempSummonType.TimedDespawn;
		Creature summon = SummonCreature(SharedConst.WorldTrigger, pos, summonType, despawnTime);

		if (summon == null)
			return null;

		if (IsTypeId(TypeId.Player) || IsTypeId(TypeId.Unit))
		{
			summon.Faction = AsUnit.Faction;
			summon.SetLevel(AsUnit.Level);
		}

		if (AI != null)
			summon.InitializeAI(new CreatureAI(summon));

		return summon;
	}

	public void SummonCreatureGroup(byte group)
	{
		SummonCreatureGroup(group, out _);
	}

	public void SummonCreatureGroup(byte group, out List<TempSummon> list)
	{
		list = new List<TempSummon>();
		var data = Global.ObjectMgr.GetSummonGroup(Entry, IsTypeId(TypeId.GameObject) ? SummonerType.GameObject : SummonerType.Creature, group);

		if (data.Empty())
		{
			Log.outWarn(LogFilter.Scripts, "{0} ({1}) tried to summon non-existing summon group {2}.", GetName(), GUID.ToString(), group);

			return;
		}

		foreach (var tempSummonData in data)
		{
			var summon = SummonCreature(tempSummonData.entry, tempSummonData.pos, tempSummonData.type, TimeSpan.FromMilliseconds(tempSummonData.time));

			if (summon)
				list.Add(summon);
		}
	}

	public void GetCreatureListInGrid(List<Creature> creatureList, float maxSearchRange)
	{
		var pair = new CellCoord((uint)Location.X, (uint)Location.Y);
		var cell = new Cell(pair);
		cell.SetNoCreate();

		var check = new AllCreaturesWithinRange(this, maxSearchRange);
		var searcher = new CreatureListSearcher(this, creatureList, check, GridType.All);

		cell.Visit(pair, searcher, Map, this, maxSearchRange);
	}

	public void GetAlliesWithinRange(List<Unit> unitList, float maxSearchRange, bool includeSelf = true)
	{
		var pair = new CellCoord((uint)Location.X, (uint)Location.Y);
		var cell = new Cell(pair);
		cell.SetNoCreate();

		var check = new AnyFriendlyUnitInObjectRangeCheck(this, AsUnit, maxSearchRange);
		var searcher = new UnitListSearcher(this, unitList, check, GridType.All);

		cell.Visit(pair, searcher, Map, this, maxSearchRange);

		if (!includeSelf)
			unitList.Remove(AsUnit);
	}

	public void GetAlliesWithinRangeWithOwnedAura(List<Unit> unitList, float maxSearchRange, uint auraId, bool includeSelf = true)
	{
		GetAlliesWithinRange(unitList, maxSearchRange, includeSelf);

		unitList.RemoveIf((creature) => { return !creature.HasAura(auraId, GUID); });
	}

	public void GetEnemiesWithinRange(List<Unit> unitList, float maxSearchRange)
	{
		var u_check = new AnyUnfriendlyUnitInObjectRangeCheck(this, AsUnit, maxSearchRange);
		var searcher = new UnitListSearcher(this, unitList, u_check, GridType.All);
		Cell.VisitGrid(this, searcher, maxSearchRange);
	}

	public void GetEnemiesWithinRangeWithOwnedAura(List<Unit> unitList, float maxSearchRange, uint auraId)
	{
		GetEnemiesWithinRange(unitList, maxSearchRange);

		unitList.RemoveIf((unit) => { return !unit.HasAura(auraId, GUID); });
	}

	public Creature FindNearestCreature(uint entry, float range, bool alive = true)
	{
		var checker = new NearestCreatureEntryWithLiveStateInObjectRangeCheck(this, entry, alive, range);
		var searcher = new CreatureLastSearcher(this, checker, GridType.All);

		Cell.VisitGrid(this, searcher, range);

		return searcher.GetTarget();
	}

	public Creature FindNearestCreatureWithOptions(float range, FindCreatureOptions options)
	{
		NearestCheckCustomizer checkCustomizer = new(this, range);
		CreatureWithOptionsInObjectRangeCheck<NearestCheckCustomizer> checker = new(this, checkCustomizer, options);
		CreatureLastSearcher searcher = new(this, checker, GridType.All);

		if (options.IgnorePhases)
			searcher._phaseShift = PhasingHandler.GetAlwaysVisiblePhaseShift();

		Cell.VisitGrid(this, searcher, range);

		return searcher.GetTarget();
	}

	public GameObject FindNearestGameObject(uint entry, float range, bool spawnedOnly = true)
	{
		var checker = new NearestGameObjectEntryInObjectRangeCheck(this, entry, range, spawnedOnly);
		var searcher = new GameObjectLastSearcher(this, checker, GridType.Grid);

		Cell.VisitGrid(this, searcher, range);

		return searcher.GetTarget();
	}

	public GameObject FindNearestUnspawnedGameObject(uint entry, float range)
	{
		NearestUnspawnedGameObjectEntryInObjectRangeCheck checker = new(this, entry, range);
		GameObjectLastSearcher searcher = new(this, checker, GridType.Grid);

		Cell.VisitGrid(this, searcher, range);

		return searcher.GetTarget();
	}

	public GameObject FindNearestGameObjectOfType(GameObjectTypes type, float range)
	{
		var checker = new NearestGameObjectTypeInObjectRangeCheck(this, type, range);
		var searcher = new GameObjectLastSearcher(this, checker, GridType.Grid);

		Cell.VisitGrid(this, searcher, range);

		return searcher.GetTarget();
	}

	public Player SelectNearestPlayer(float distance)
	{
		var checker = new NearestPlayerInObjectRangeCheck(this, distance);
		var searcher = new PlayerLastSearcher(this, checker, GridType.All);
		Cell.VisitGrid(this, searcher, distance);

		return searcher.GetTarget();
	}

	public bool TryGetOwner(out Unit owner)
	{
		owner = OwnerUnit;

		return owner != null;
	}

	public bool TryGetOwner(out Player owner)
	{
		owner = OwnerUnit?.AsPlayer;

		return owner != null;
	}

	public double CalculateSpellDamage(Unit target, SpellEffectInfo spellEffectInfo, double? basePoints = null, uint castItemId = 0, int itemLevel = -1)
	{
		return CalculateSpellDamage(out _, target, spellEffectInfo, basePoints, castItemId, itemLevel);
	}

	// function uses real base points (typically value - 1)
	public double CalculateSpellDamage(out double variance, Unit target, SpellEffectInfo spellEffectInfo, double? basePoints = null, uint castItemId = 0, int itemLevel = -1)
	{
		variance = 0.0f;

		return spellEffectInfo != null ? spellEffectInfo.CalcValue(out variance, this, basePoints, target, castItemId, itemLevel) : 0;
	}

	public float GetSpellMaxRangeForTarget(Unit target, SpellInfo spellInfo)
	{
		if (spellInfo.RangeEntry == null)
			return 0.0f;

		if (spellInfo.RangeEntry.RangeMax[0] == spellInfo.RangeEntry.RangeMax[1])
			return spellInfo.GetMaxRange();

		if (!target)
			return spellInfo.GetMaxRange(true);

		return spellInfo.GetMaxRange(!IsHostileTo(target));
	}

	public float GetSpellMinRangeForTarget(Unit target, SpellInfo spellInfo)
	{
		if (spellInfo.RangeEntry == null)
			return 0.0f;

		if (spellInfo.RangeEntry.RangeMin[0] == spellInfo.RangeEntry.RangeMin[1])
			return spellInfo.GetMinRange();

		if (!target)
			return spellInfo.GetMinRange(true);

		return spellInfo.GetMinRange(!IsHostileTo(target));
	}

	public double ApplyEffectModifiers(SpellInfo spellInfo, int effIndex, double value)
	{
		var modOwner = SpellModOwner;

		if (modOwner != null)
		{
			modOwner.ApplySpellMod(spellInfo, SpellModOp.Points, ref value);

			switch (effIndex)
			{
				case 0:
					modOwner.ApplySpellMod(spellInfo, SpellModOp.PointsIndex0, ref value);

					break;
				case 1:
					modOwner.ApplySpellMod(spellInfo, SpellModOp.PointsIndex1, ref value);

					break;
				case 2:
					modOwner.ApplySpellMod(spellInfo, SpellModOp.PointsIndex2, ref value);

					break;
				case 3:
					modOwner.ApplySpellMod(spellInfo, SpellModOp.PointsIndex3, ref value);

					break;
				case 4:
					modOwner.ApplySpellMod(spellInfo, SpellModOp.PointsIndex4, ref value);

					break;
			}
		}

		return value;
	}

	public int CalcSpellDuration(SpellInfo spellInfo)
	{
		var comboPoints = 0;
		var maxComboPoints = 5;
		var unit = AsUnit;

		if (unit != null)
		{
			comboPoints = unit.GetPower(PowerType.ComboPoints);
			maxComboPoints = unit.GetMaxPower(PowerType.ComboPoints);
		}

		var minduration = spellInfo.Duration;
		var maxduration = spellInfo.MaxDuration;

		int duration;

		if (comboPoints != 0 && minduration != -1 && minduration != maxduration)
			duration = minduration + ((maxduration - minduration) * comboPoints / maxComboPoints);
		else
			duration = minduration;

		return duration;
	}

	public int ModSpellDuration(SpellInfo spellInfo, WorldObject target, int duration, bool positive, int effIndex)
	{
		return ModSpellDuration(spellInfo,
								target,
								duration,
								positive,
								new HashSet<int>()
								{
									effIndex
								});
	}

	public int ModSpellDuration(SpellInfo spellInfo, WorldObject target, int duration, bool positive, HashSet<int> effectMask)
	{
		// don't mod permanent auras duration
		if (duration < 0)
			return duration;

		// some auras are not affected by duration modifiers
		if (spellInfo.HasAttribute(SpellAttr7.IgnoreDurationMods))
			return duration;

		// cut duration only of negative effects
		var unitTarget = target.AsUnit;

		if (!unitTarget)
			return duration;

		if (!positive)
		{
			var mechanicMask = spellInfo.GetSpellMechanicMaskByEffectMask(effectMask);

			bool mechanicCheck(AuraEffect aurEff)
			{
				if ((mechanicMask & (1ul << aurEff.MiscValue)) != 0)
					return true;

				return false;
			}

			// Find total mod value (negative bonus)
			var durationMod_always = unitTarget.GetTotalAuraModifier(AuraType.MechanicDurationMod, mechanicCheck);
			// Find max mod (negative bonus)
			var durationMod_not_stack = unitTarget.GetMaxNegativeAuraModifier(AuraType.MechanicDurationModNotStack, mechanicCheck);

			// Select strongest negative mod
			var durationMod = Math.Min(durationMod_always, durationMod_not_stack);

			if (durationMod != 0)
				MathFunctions.AddPct(ref duration, durationMod);

			// there are only negative mods currently
			durationMod_always = unitTarget.GetTotalAuraModifierByMiscValue(AuraType.ModAuraDurationByDispel, (int)spellInfo.Dispel);
			durationMod_not_stack = unitTarget.GetMaxNegativeAuraModifierByMiscValue(AuraType.ModAuraDurationByDispelNotStack, (int)spellInfo.Dispel);

			durationMod = Math.Min(durationMod_always, durationMod_not_stack);

			if (durationMod != 0)
				MathFunctions.AddPct(ref duration, durationMod);
		}
		else
		{
			// else positive mods here, there are no currently
			// when there will be, change GetTotalAuraModifierByMiscValue to GetMaxPositiveAuraModifierByMiscValue

			// Mixology - duration boost
			if (unitTarget.IsPlayer)
				if (spellInfo.SpellFamilyName == SpellFamilyNames.Potion &&
					(
						Global.SpellMgr.IsSpellMemberOfSpellGroup(spellInfo.Id, SpellGroup.ElixirBattle) ||
						Global.SpellMgr.IsSpellMemberOfSpellGroup(spellInfo.Id, SpellGroup.ElixirGuardian)))
				{
					var effect = spellInfo.GetEffect(0);

					if (unitTarget.HasAura(53042) && effect != null && unitTarget.HasSpell(effect.TriggerSpell))
						duration *= 2;
				}
		}

		return Math.Max(duration, 0);
	}

	public void ModSpellCastTime(SpellInfo spellInfo, ref int castTime, Spell spell = null)
	{
		if (spellInfo == null || castTime < 0)
			return;

		// called from caster
		var modOwner = SpellModOwner;

		if (modOwner != null)
			modOwner.ApplySpellMod(spellInfo, SpellModOp.ChangeCastTime, ref castTime, spell);

		var unitCaster = AsUnit;

		if (!unitCaster)
			return;

		if (unitCaster.IsPlayer && unitCaster.AsPlayer.GetCommandStatus(PlayerCommandStates.Casttime))
			castTime = 0;
		else if (!(spellInfo.HasAttribute(SpellAttr0.IsAbility) || spellInfo.HasAttribute(SpellAttr0.IsTradeskill) || spellInfo.HasAttribute(SpellAttr3.IgnoreCasterModifiers)) && ((IsPlayer && spellInfo.SpellFamilyName != 0) || IsCreature))
			castTime = unitCaster.CanInstantCast ? 0 : (int)(castTime * unitCaster.UnitData.ModCastingSpeed);
		else if (spellInfo.HasAttribute(SpellAttr0.UsesRangedSlot) && !spellInfo.HasAttribute(SpellAttr2.AutoRepeat))
			castTime = (int)(castTime * unitCaster.ModAttackSpeedPct[(int)WeaponAttackType.RangedAttack]);
		else if (Global.SpellMgr.IsPartOfSkillLine(SkillType.Cooking, spellInfo.Id) && unitCaster.HasAura(67556)) // cooking with Chef Hat.
			castTime = 500;
	}

	public void ModSpellDurationTime(SpellInfo spellInfo, ref int duration, Spell spell = null)
	{
		if (spellInfo == null || duration < 0)
			return;

		if (spellInfo.IsChanneled && !spellInfo.HasAttribute(SpellAttr5.SpellHasteAffectsPeriodic))
			return;

		// called from caster
		var modOwner = SpellModOwner;

		if (modOwner != null)
			modOwner.ApplySpellMod(spellInfo, SpellModOp.ChangeCastTime, ref duration, spell);

		var unitCaster = AsUnit;

		if (!unitCaster)
			return;

		if (!(spellInfo.HasAttribute(SpellAttr0.IsAbility) || spellInfo.HasAttribute(SpellAttr0.IsTradeskill) || spellInfo.HasAttribute(SpellAttr3.IgnoreCasterModifiers)) &&
			((IsPlayer && spellInfo.SpellFamilyName != 0) || IsCreature))
			duration = (int)(duration * unitCaster.UnitData.ModCastingSpeed);
		else if (spellInfo.HasAttribute(SpellAttr0.UsesRangedSlot) && !spellInfo.HasAttribute(SpellAttr2.AutoRepeat))
			duration = (int)(duration * unitCaster.ModAttackSpeedPct[(int)WeaponAttackType.RangedAttack]);
	}

	public virtual double MeleeSpellMissChance(Unit victim, WeaponAttackType attType, SpellInfo spellInfo)
	{
		return 0.0f;
	}

	public virtual SpellMissInfo MeleeSpellHitResult(Unit victim, SpellInfo spellInfo)
	{
		return SpellMissInfo.None;
	}

	// Calculate spell hit result can be:
	// Every spell can: Evade/Immune/Reflect/Sucesful hit
	// For melee based spells:
	//   Miss
	//   Dodge
	//   Parry
	// For spells
	//   Resist
	public SpellMissInfo SpellHitResult(Unit victim, SpellInfo spellInfo, bool canReflect = false)
	{
		// Check for immune
		if (victim.IsImmunedToSpell(spellInfo, this))
			return SpellMissInfo.Immune;

		// Damage immunity is only checked if the spell has damage effects, this immunity must not prevent aura apply
		// returns SPELL_MISS_IMMUNE in that case, for other spells, the SMSG_SPELL_GO must show hit
		if (spellInfo.HasOnlyDamageEffects && victim.IsImmunedToDamage(spellInfo))
			return SpellMissInfo.Immune;

		// All positive spells can`t miss
		/// @todo client not show miss log for this spells - so need find info for this in dbc and use it!
		if (spellInfo.IsPositive && !IsHostileTo(victim)) // prevent from affecting enemy by "positive" spell
			return SpellMissInfo.None;

		if (this == victim)
			return SpellMissInfo.None;

		// Return evade for units in evade mode
		if (victim.IsCreature && victim.AsCreature.IsEvadingAttacks)
			return SpellMissInfo.Evade;

		// Try victim reflect spell
		if (canReflect)
		{
			var reflectchance = victim.GetTotalAuraModifier(AuraType.ReflectSpells);
			reflectchance += victim.GetTotalAuraModifierByMiscMask(AuraType.ReflectSpellsSchool, (int)spellInfo.GetSchoolMask());

			if (reflectchance > 0 && RandomHelper.randChance(reflectchance))
				return SpellMissInfo.Reflect;
		}

		if (spellInfo.HasAttribute(SpellAttr3.AlwaysHit))
			return SpellMissInfo.None;

		switch (spellInfo.DmgClass)
		{
			case SpellDmgClass.Ranged:
			case SpellDmgClass.Melee:
				return MeleeSpellHitResult(victim, spellInfo);
			case SpellDmgClass.None:
				return SpellMissInfo.None;
			case SpellDmgClass.Magic:
				return MagicSpellHitResult(victim, spellInfo);
		}

		return SpellMissInfo.None;
	}

	public void SendSpellMiss(Unit target, uint spellID, SpellMissInfo missInfo)
	{
		SpellMissLog spellMissLog = new();
		spellMissLog.SpellID = spellID;
		spellMissLog.Caster = GUID;
		spellMissLog.Entries.Add(new SpellLogMissEntry(target.GUID, (byte)missInfo));
		SendMessageToSet(spellMissLog, true);
	}

	public FactionTemplateRecord GetFactionTemplateEntry()
	{
		var factionId = Faction;
		var entry = CliDB.FactionTemplateStorage.LookupByKey(factionId);

		if (entry == null)
			switch (TypeId)
			{
				case TypeId.Player:
					Log.outError(LogFilter.Unit, $"Player {AsPlayer.GetName()} has invalid faction (faction template id) #{factionId}");

					break;
				case TypeId.Unit:
					Log.outError(LogFilter.Unit, $"Creature (template id: {AsCreature.Template.Entry}) has invalid faction (faction template Id) #{factionId}");

					break;
				case TypeId.GameObject:
					if (factionId != 0) // Gameobjects may have faction template id = 0
						Log.outError(LogFilter.Unit, $"GameObject (template id: {AsGameObject.Template.entry}) has invalid faction (faction template Id) #{factionId}");

					break;
				default:
					Log.outError(LogFilter.Unit, $"Object (name={GetName()}, type={TypeId}) has invalid faction (faction template Id) #{factionId}");

					break;
			}

		return entry;
	}

	// function based on function Unit::UnitReaction from 13850 client
	public ReputationRank GetReactionTo(WorldObject target)
	{
		// always friendly to self
		if (this == target)
			return ReputationRank.Friendly;

		bool isAttackableBySummoner(Unit me, ObjectGuid targetGuid)
		{
			if (!me)
				return false;

			var tempSummon = me.ToTempSummon();

			if (tempSummon == null || tempSummon.SummonPropertiesRecord == null)
				return false;

			if (tempSummon.SummonPropertiesRecord.GetFlags().HasFlag(SummonPropertiesFlags.AttackableBySummoner) && targetGuid == tempSummon.GetSummonerGUID())
				return true;

			return false;
		}

		if (isAttackableBySummoner(AsUnit, target.GUID) || isAttackableBySummoner(target.AsUnit, GUID))
			return ReputationRank.Neutral;

		// always friendly to charmer or owner
		if (CharmerOrOwnerOrSelf == target.CharmerOrOwnerOrSelf)
			return ReputationRank.Friendly;

		var selfPlayerOwner = AffectingPlayer;
		var targetPlayerOwner = target.AffectingPlayer;

		// check forced reputation to support SPELL_AURA_FORCE_REACTION
		if (selfPlayerOwner)
		{
			var targetFactionTemplateEntry = target.GetFactionTemplateEntry();

			if (targetFactionTemplateEntry != null)
			{
				var repRank = selfPlayerOwner.ReputationMgr.GetForcedRankIfAny(targetFactionTemplateEntry);

				if (repRank != ReputationRank.None)
					return repRank;
			}
		}
		else if (targetPlayerOwner)
		{
			var selfFactionTemplateEntry = GetFactionTemplateEntry();

			if (selfFactionTemplateEntry != null)
			{
				var repRank = targetPlayerOwner.ReputationMgr.GetForcedRankIfAny(selfFactionTemplateEntry);

				if (repRank != ReputationRank.None)
					return repRank;
			}
		}

		var unit = AsUnit ?? selfPlayerOwner;
		var targetUnit = target.AsUnit ?? targetPlayerOwner;

		if (unit && unit.HasUnitFlag(UnitFlags.PlayerControlled))
			if (targetUnit && targetUnit.HasUnitFlag(UnitFlags.PlayerControlled))
			{
				if (selfPlayerOwner && targetPlayerOwner)
				{
					// always friendly to other unit controlled by player, or to the player himself
					if (selfPlayerOwner == targetPlayerOwner)
						return ReputationRank.Friendly;

					// duel - always hostile to opponent
					if (selfPlayerOwner.Duel != null && selfPlayerOwner.Duel.Opponent == targetPlayerOwner && selfPlayerOwner.Duel.State == DuelState.InProgress)
						return ReputationRank.Hostile;

					// same group - checks dependant only on our faction - skip FFA_PVP for example
					if (selfPlayerOwner.IsInRaidWith(targetPlayerOwner))
						return ReputationRank.Friendly; // return true to allow config option AllowTwoSide.Interaction.Group to work
					// however client seems to allow mixed group parties, because in 13850 client it works like:
					// return GetFactionReactionTo(GetFactionTemplateEntry(), target);
				}

				// check FFA_PVP
				if (unit.IsFFAPvP && targetUnit.IsFFAPvP)
					return ReputationRank.Hostile;

				if (selfPlayerOwner)
				{
					var targetFactionTemplateEntry = targetUnit.GetFactionTemplateEntry();

					if (targetFactionTemplateEntry != null)
					{
						var repRank = selfPlayerOwner.ReputationMgr.GetForcedRankIfAny(targetFactionTemplateEntry);

						if (repRank != ReputationRank.None)
							return repRank;

						if (!selfPlayerOwner.HasUnitFlag2(UnitFlags2.IgnoreReputation))
						{
							var targetFactionEntry = CliDB.FactionStorage.LookupByKey(targetFactionTemplateEntry.Faction);

							if (targetFactionEntry != null)
								if (targetFactionEntry.CanHaveReputation())
								{
									// check contested flags
									if ((targetFactionTemplateEntry.Flags & (ushort)FactionTemplateFlags.ContestedGuard) != 0 && selfPlayerOwner.HasPlayerFlag(PlayerFlags.ContestedPVP))
										return ReputationRank.Hostile;

									// if faction has reputation, hostile state depends only from AtWar state
									if (selfPlayerOwner.ReputationMgr.IsAtWar(targetFactionEntry))
										return ReputationRank.Hostile;

									return ReputationRank.Friendly;
								}
						}
					}
				}
			}

		// do checks dependant only on our faction
		return GetFactionReactionTo(GetFactionTemplateEntry(), target);
	}

	public static ReputationRank GetFactionReactionTo(FactionTemplateRecord factionTemplateEntry, WorldObject target)
	{
		// always neutral when no template entry found
		if (factionTemplateEntry == null)
			return ReputationRank.Neutral;

		var targetFactionTemplateEntry = target.GetFactionTemplateEntry();

		if (targetFactionTemplateEntry == null)
			return ReputationRank.Neutral;

		var targetPlayerOwner = target.AffectingPlayer;

		if (targetPlayerOwner != null)
		{
			// check contested flags
			if ((factionTemplateEntry.Flags & (ushort)FactionTemplateFlags.ContestedGuard) != 0 && targetPlayerOwner.HasPlayerFlag(PlayerFlags.ContestedPVP))
				return ReputationRank.Hostile;

			var repRank = targetPlayerOwner.ReputationMgr.GetForcedRankIfAny(factionTemplateEntry);

			if (repRank != ReputationRank.None)
				return repRank;

			if (target.IsUnit && !target.AsUnit.HasUnitFlag2(UnitFlags2.IgnoreReputation))
			{
				var factionEntry = CliDB.FactionStorage.LookupByKey(factionTemplateEntry.Faction);

				if (factionEntry != null)
					if (factionEntry.CanHaveReputation())
					{
						// CvP case - check reputation, don't allow state higher than neutral when at war
						var repRank1 = targetPlayerOwner.ReputationMgr.GetRank(factionEntry);

						if (targetPlayerOwner.ReputationMgr.IsAtWar(factionEntry))
							repRank1 = (ReputationRank)Math.Min((int)ReputationRank.Neutral, (int)repRank1);

						return repRank1;
					}
			}
		}

		// common faction based check
		if (factionTemplateEntry.IsHostileTo(targetFactionTemplateEntry))
			return ReputationRank.Hostile;

		if (factionTemplateEntry.IsFriendlyTo(targetFactionTemplateEntry))
			return ReputationRank.Friendly;

		if (targetFactionTemplateEntry.IsFriendlyTo(factionTemplateEntry))
			return ReputationRank.Friendly;

		if ((factionTemplateEntry.Flags & (ushort)FactionTemplateFlags.HostileByDefault) != 0)
			return ReputationRank.Hostile;

		// neutral by default
		return ReputationRank.Neutral;
	}

	public bool IsHostileTo(WorldObject target)
	{
		return GetReactionTo(target) <= ReputationRank.Hostile;
	}

	public bool IsFriendlyTo(WorldObject target)
	{
		return GetReactionTo(target) >= ReputationRank.Friendly;
	}

	public bool IsHostileToPlayers()
	{
		var my_faction = GetFactionTemplateEntry();

		if (my_faction.Faction == 0)
			return false;

		var raw_faction = CliDB.FactionStorage.LookupByKey(my_faction.Faction);

		if (raw_faction != null && raw_faction.ReputationIndex >= 0)
			return false;

		return my_faction.IsHostileToPlayers();
	}

	public bool IsNeutralToAll()
	{
		var my_faction = GetFactionTemplateEntry();

		if (my_faction.Faction == 0)
			return true;

		var raw_faction = CliDB.FactionStorage.LookupByKey(my_faction.Faction);

		if (raw_faction != null && raw_faction.ReputationIndex >= 0)
			return false;

		return my_faction.IsNeutralToAll();
	}

	public SpellCastResult CastSpell(uint spellId, bool triggered = false, byte? empowerStage = null)
	{
		return CastSpell(null, spellId, triggered, empowerStage);
	}

	public SpellCastResult CastSpell<T>(WorldObject target, T spellId, bool triggered = false) where T : struct, Enum
	{
		return CastSpell(target, Convert.ToUInt32(spellId), triggered);
	}

	public SpellCastResult CastSpell(WorldObject target, uint spellId, Spell triggeringSpell)
	{
		CastSpellExtraArgs args = new(true);
		args.TriggeringSpell = triggeringSpell;

		return CastSpell(target, spellId, args);
	}

	public SpellCastResult CastSpell(WorldObject target, uint spellId, AuraEffect triggeringAura)
	{
		CastSpellExtraArgs args = new(true);
		args.TriggeringAura = triggeringAura;

		return CastSpell(target, spellId, args);
	}

	public SpellCastResult CastSpell(WorldObject target, uint spellId, bool triggered = false, byte? empowerStage = null)
	{
		CastSpellExtraArgs args = new(triggered);
		args.EmpowerStage = empowerStage;

		return CastSpell(target, spellId, args);
	}

	public SpellCastResult CastSpell(WorldObject target, uint spellId, TriggerCastFlags triggerCastFlags, bool triggered = false)
	{
		CastSpellExtraArgs args = new(triggered);
		args.TriggerFlags = triggerCastFlags;

		return CastSpell(target, spellId, args);
	}

	public SpellCastResult CastSpell(WorldObject target, uint spellId, double bp0Val, bool triggered = false)
	{
		CastSpellExtraArgs args = new(triggered);
		args.SpellValueOverrides[SpellValueMod.BasePoint0] = bp0Val;

		return CastSpell(target, spellId, args);
	}

	public SpellCastResult CastSpell(WorldObject target, uint spellId, SpellValueMod spellValueMod, double bp0Val, bool triggered = false)
	{
		CastSpellExtraArgs args = new(triggered);
		args.SpellValueOverrides[spellValueMod] = bp0Val;

		return CastSpell(target, spellId, args);
	}

	public SpellCastResult CastSpell(SpellCastTargets targets, uint spellId, CastSpellExtraArgs args)
	{
		return CastSpell(new CastSpellTargetArg(targets), spellId, args);
	}

	public SpellCastResult CastSpell(WorldObject target, uint spellId, CastSpellExtraArgs args)
	{
		return CastSpell(new CastSpellTargetArg(target), spellId, args);
	}

	public SpellCastResult CastSpell(float x, float y, float z, uint spellId, bool triggered = false)
	{
		return CastSpell(new Position(x, y, z), spellId, triggered);
	}

	public SpellCastResult CastSpell(float x, float y, float z, uint spellId, CastSpellExtraArgs args)
	{
		return CastSpell(new Position(x, y, z), spellId, args);
	}

	public SpellCastResult CastSpell(Position dest, uint spellId, bool triggered = false)
	{
		CastSpellExtraArgs args = new(triggered);

		return CastSpell(new CastSpellTargetArg(dest), spellId, args);
	}

	public SpellCastResult CastSpell(Position dest, uint spellId, CastSpellExtraArgs args)
	{
		return CastSpell(new CastSpellTargetArg(dest), spellId, args);
	}

	public SpellCastResult CastSpell(CastSpellTargetArg targets, uint spellId, CastSpellExtraArgs args)
	{
		var info = Global.SpellMgr.GetSpellInfo(spellId, args.CastDifficulty != Difficulty.None ? args.CastDifficulty : Map.DifficultyID);

		if (info == null)
		{
			Log.outError(LogFilter.Unit, $"CastSpell: unknown spell {spellId} by caster {GUID}");

			return SpellCastResult.SpellUnavailable;
		}

		if (targets.Targets == null)
		{
			Log.outError(LogFilter.Unit, $"CastSpell: Invalid target passed to spell cast {spellId} by {GUID}");

			return SpellCastResult.BadTargets;
		}

		Spell spell = new(this, info, args.TriggerFlags, args.OriginalCaster, args.OriginalCastId, args.EmpowerStage);

		foreach (var pair in args.SpellValueOverrides)
			spell.SetSpellValue(pair.Key, (float)pair.Value);

		spell.CastItem = args.CastItem;

		if (args.OriginalCastItemLevel.HasValue)
			spell.CastItemLevel = args.OriginalCastItemLevel.Value;

		if (spell.CastItem == null && info.HasAttribute(SpellAttr2.RetainItemCast))
		{
			if (args.TriggeringSpell)
			{
				spell.CastItem = args.TriggeringSpell.CastItem;
			}
			else if (args.TriggeringAura != null && !args.TriggeringAura.Base.CastItemGuid.IsEmpty)
			{
				var triggeringAuraCaster = args.TriggeringAura.Caster?.AsPlayer;

				if (triggeringAuraCaster != null)
					spell.CastItem = triggeringAuraCaster.GetItemByGuid(args.TriggeringAura.Base.CastItemGuid);
			}
		}

		spell.CustomArg = args.CustomArg;

		return spell.Prepare(targets.Targets, args.TriggeringAura);
	}

	public void SendPlaySpellVisual(WorldObject target, uint spellVisualId, ushort missReason, ushort reflectStatus, float travelSpeed, bool speedAsTime = false, float launchDelay = 0)
	{
		PlaySpellVisual playSpellVisual = new();
		playSpellVisual.Source = GUID;
		playSpellVisual.Target = target.GUID;
		playSpellVisual.TargetPosition = target.Location;
		playSpellVisual.SpellVisualID = spellVisualId;
		playSpellVisual.TravelSpeed = travelSpeed;
		playSpellVisual.MissReason = missReason;
		playSpellVisual.ReflectStatus = reflectStatus;
		playSpellVisual.SpeedAsTime = speedAsTime;
		playSpellVisual.LaunchDelay = launchDelay;
		SendMessageToSet(playSpellVisual, true);
	}

	public void SendPlaySpellVisual(Position targetPosition, float launchDelay, uint spellVisualId, ushort missReason, ushort reflectStatus, float travelSpeed, bool speedAsTime = false)
	{
		PlaySpellVisual playSpellVisual = new();
		playSpellVisual.Source = GUID;
		playSpellVisual.TargetPosition = targetPosition;
		playSpellVisual.LaunchDelay = launchDelay;
		playSpellVisual.SpellVisualID = spellVisualId;
		playSpellVisual.TravelSpeed = travelSpeed;
		playSpellVisual.MissReason = missReason;
		playSpellVisual.ReflectStatus = reflectStatus;
		playSpellVisual.SpeedAsTime = speedAsTime;
		SendMessageToSet(playSpellVisual, true);
	}

	public void SendPlaySpellVisual(Position targetPosition, uint spellVisualId, ushort missReason, ushort reflectStatus, float travelSpeed, bool speedAsTime = false)
	{
		PlaySpellVisual playSpellVisual = new();
		playSpellVisual.Source = GUID;
		playSpellVisual.TargetPosition = targetPosition;
		playSpellVisual.SpellVisualID = spellVisualId;
		playSpellVisual.TravelSpeed = travelSpeed;
		playSpellVisual.MissReason = missReason;
		playSpellVisual.ReflectStatus = reflectStatus;
		playSpellVisual.SpeedAsTime = speedAsTime;
		SendMessageToSet(playSpellVisual, true);
	}

	public void SendPlaySpellVisualKit(uint id, uint type, uint duration)
	{
		PlaySpellVisualKit playSpellVisualKit = new();
		playSpellVisualKit.Unit = GUID;
		playSpellVisualKit.KitRecID = id;
		playSpellVisualKit.KitType = type;
		playSpellVisualKit.Duration = duration;

		SendMessageToSet(playSpellVisualKit, true);
	}

	// function based on function Unit::CanAttack from 13850 client
	public bool IsValidAttackTarget(WorldObject target, SpellInfo bySpell = null)
	{
		// some positive spells can be casted at hostile target
		var isPositiveSpell = bySpell != null && bySpell.IsPositive;

		// can't attack self (spells can, attribute check)
		if (bySpell == null && this == target)
			return false;

		// can't attack unattackable units
		var unitTarget = target.AsUnit;

		if (unitTarget != null && unitTarget.HasUnitState(UnitState.Unattackable))
			return false;

		// can't attack GMs
		if (target.IsPlayer && target.AsPlayer.IsGameMaster)
			return false;

		var unit = AsUnit;

		// visibility checks (only units)
		if (unit != null)
			// can't attack invisible
			if (bySpell == null || !bySpell.HasAttribute(SpellAttr6.IgnorePhaseShift))
				if (!unit.CanSeeOrDetect(target, bySpell != null && bySpell.IsAffectingArea))
					return false;

		// can't attack dead
		if ((bySpell == null || !bySpell.IsAllowingDeadTarget) && unitTarget != null && !unitTarget.IsAlive)
			return false;

		// can't attack untargetable
		if ((bySpell == null || !bySpell.HasAttribute(SpellAttr6.CanTargetUntargetable)) && unitTarget != null && unitTarget.HasUnitFlag(UnitFlags.NonAttackable2))
			return false;

		if (unitTarget != null && unitTarget.HasUnitFlag(UnitFlags.Uninteractible))
			return false;

		var playerAttacker = AsPlayer;

		if (playerAttacker != null)
			if (playerAttacker.HasPlayerFlag(PlayerFlags.Uber))
				return false;

		// check flags
		if (unitTarget != null && unitTarget.HasUnitFlag(UnitFlags.NonAttackable | UnitFlags.OnTaxi | UnitFlags.NotAttackable1))
			return false;

		var unitOrOwner = unit;
		var go = AsGameObject;

		if (go?.GoType == GameObjectTypes.Trap)
			unitOrOwner = go.OwnerUnit;

		// ignore immunity flags when assisting
		if (unitOrOwner != null && unitTarget != null && !(isPositiveSpell && bySpell.HasAttribute(SpellAttr6.CanAssistImmunePc)))
		{
			if (!unitOrOwner.HasUnitFlag(UnitFlags.PlayerControlled) && unitTarget.IsImmuneToNPC())
				return false;

			if (!unitTarget.HasUnitFlag(UnitFlags.PlayerControlled) && unitOrOwner.IsImmuneToNPC())
				return false;

			if (bySpell == null || !bySpell.HasAttribute(SpellAttr8.AttackIgnoreImmuneToPCFlag))
			{
				if (unitOrOwner.HasUnitFlag(UnitFlags.PlayerControlled) && unitTarget.IsImmuneToPC())
					return false;

				if (unitTarget.HasUnitFlag(UnitFlags.PlayerControlled) && unitOrOwner.IsImmuneToPC())
					return false;
			}
		}

		// CvC case - can attack each other only when one of them is hostile
		if (unit && !unit.HasUnitFlag(UnitFlags.PlayerControlled) && unitTarget != null && !unitTarget.HasUnitFlag(UnitFlags.PlayerControlled))
			return IsHostileTo(unitTarget) || unitTarget.IsHostileTo(this);

		// Traps without owner or with NPC owner versus Creature case - can attack to creature only when one of them is hostile
		if (go?.GoType == GameObjectTypes.Trap)
		{
			var goOwner = go.OwnerUnit;

			if (goOwner == null || !goOwner.HasUnitFlag(UnitFlags.PlayerControlled))
				if (unitTarget && !unitTarget.HasUnitFlag(UnitFlags.PlayerControlled))
					return IsHostileTo(unitTarget) || unitTarget.IsHostileTo(this);
		}

		// PvP, PvC, CvP case
		// can't attack friendly targets
		if (IsFriendlyTo(target) || target.IsFriendlyTo(this))
			return false;

		var playerAffectingAttacker = unit != null && unit.HasUnitFlag(UnitFlags.PlayerControlled) ? AffectingPlayer : go != null ? AffectingPlayer : null;
		var playerAffectingTarget = unitTarget != null && unitTarget.HasUnitFlag(UnitFlags.PlayerControlled) ? unitTarget.AffectingPlayer : null;

		// Not all neutral creatures can be attacked (even some unfriendly faction does not react aggresive to you, like Sporaggar)
		if ((playerAffectingAttacker && !playerAffectingTarget) || (!playerAffectingAttacker && playerAffectingTarget))
		{
			var player = playerAffectingAttacker ? playerAffectingAttacker : playerAffectingTarget;
			var creature = playerAffectingAttacker ? unitTarget : unit;

			if (creature != null)
			{
				if (creature.IsContestedGuard() && player.HasPlayerFlag(PlayerFlags.ContestedPVP))
					return true;

				var factionTemplate = creature.GetFactionTemplateEntry();

				if (factionTemplate != null)
					if (player.ReputationMgr.GetForcedRankIfAny(factionTemplate) == ReputationRank.None)
					{
						var factionEntry = CliDB.FactionStorage.LookupByKey(factionTemplate.Faction);

						if (factionEntry != null)
						{
							var repState = player.ReputationMgr.GetState(factionEntry);

							if (repState != null)
								if (!repState.Flags.HasFlag(ReputationFlags.AtWar))
									return false;
						}
					}
			}
		}

		var creatureAttacker = AsCreature;

		if (creatureAttacker && creatureAttacker.Template.TypeFlags.HasFlag(CreatureTypeFlags.TreatAsRaidUnit))
			return false;

		if (playerAffectingAttacker && playerAffectingTarget)
			if (playerAffectingAttacker.Duel != null && playerAffectingAttacker.Duel.Opponent == playerAffectingTarget && playerAffectingAttacker.Duel.State == DuelState.InProgress)
				return true;

		// PvP case - can't attack when attacker or target are in sanctuary
		// however, 13850 client doesn't allow to attack when one of the unit's has sanctuary flag and is pvp
		if (unitTarget != null && unitTarget.HasUnitFlag(UnitFlags.PlayerControlled) && unitOrOwner != null && unitOrOwner.HasUnitFlag(UnitFlags.PlayerControlled) && (unitTarget.IsInSanctuary || unitOrOwner.IsInSanctuary))
			return false;

		// additional checks - only PvP case
		if (playerAffectingAttacker && playerAffectingTarget)
		{
			if (playerAffectingTarget.IsPvP || (bySpell != null && bySpell.HasAttribute(SpellAttr5.IgnoreAreaEffectPvpCheck)))
				return true;

			if (playerAffectingAttacker.IsFFAPvP && playerAffectingTarget.IsFFAPvP)
				return true;

			return playerAffectingAttacker.HasPvpFlag(UnitPVPStateFlags.Unk1) ||
					playerAffectingTarget.HasPvpFlag(UnitPVPStateFlags.Unk1);
		}

		return true;
	}

	// function based on function Unit::CanAssist from 13850 client
	public bool IsValidAssistTarget(WorldObject target, SpellInfo bySpell = null, bool spellCheck = true)
	{
		// some negative spells can be casted at friendly target
		var isNegativeSpell = bySpell != null && !bySpell.IsPositive;

		// can assist to self
		if (this == target)
			return true;

		// can't assist unattackable units
		var unitTarget = target.AsUnit;

		if (unitTarget && unitTarget.HasUnitState(UnitState.Unattackable))
			return false;

		// can't assist GMs
		if (target.IsPlayer && target.AsPlayer.IsGameMaster)
			return false;

		// can't assist own vehicle or passenger
		var unit = AsUnit;

		if (unit && unitTarget && unit.Vehicle1)
		{
			if (unit.IsOnVehicle(unitTarget))
				return false;

			if (unit.VehicleBase.IsOnVehicle(unitTarget))
				return false;
		}

		// can't assist invisible
		if ((bySpell == null || !bySpell.HasAttribute(SpellAttr6.IgnorePhaseShift)) && !CanSeeOrDetect(target, bySpell != null && bySpell.IsAffectingArea))
			return false;

		// can't assist dead
		if ((bySpell == null || !bySpell.IsAllowingDeadTarget) && unitTarget && !unitTarget.IsAlive)
			return false;

		// can't assist untargetable
		if ((bySpell == null || !bySpell.HasAttribute(SpellAttr6.CanTargetUntargetable)) && unitTarget != null && unitTarget.HasUnitFlag(UnitFlags.NonAttackable2))
			return false;

		if (unitTarget != null && unitTarget.HasUnitFlag(UnitFlags.Uninteractible))
			return false;

		// check flags for negative spells
		if (isNegativeSpell && unitTarget != null && unitTarget.HasUnitFlag(UnitFlags.NonAttackable | UnitFlags.OnTaxi | UnitFlags.NotAttackable1))
			return false;

		if (isNegativeSpell || bySpell == null || !bySpell.HasAttribute(SpellAttr6.CanAssistImmunePc))
		{
			if (unit != null && unit.HasUnitFlag(UnitFlags.PlayerControlled))
			{
				if (bySpell == null || !bySpell.HasAttribute(SpellAttr8.AttackIgnoreImmuneToPCFlag))
					if (unitTarget != null && unitTarget.IsImmuneToPC())
						return false;
			}
			else
			{
				if (unitTarget != null && unitTarget.IsImmuneToNPC())
					return false;
			}
		}

		// can't assist non-friendly targets
		if (GetReactionTo(target) < ReputationRank.Neutral && target.GetReactionTo(this) < ReputationRank.Neutral && (!AsCreature || !AsCreature.Template.TypeFlags.HasFlag(CreatureTypeFlags.TreatAsRaidUnit)))
			return false;

		// PvP case
		if (unitTarget != null && unitTarget.HasUnitFlag(UnitFlags.PlayerControlled))
		{
			if (unit != null && unit.HasUnitFlag(UnitFlags.PlayerControlled))
			{
				var selfPlayerOwner = AffectingPlayer;
				var targetPlayerOwner = unitTarget.AffectingPlayer;

				if (selfPlayerOwner != null && targetPlayerOwner != null)
					// can't assist player which is dueling someone
					if (selfPlayerOwner != targetPlayerOwner && targetPlayerOwner.Duel != null)
						return false;

				// can't assist player in ffa_pvp zone from outside
				if (unitTarget.IsFFAPvP && !unit.IsFFAPvP)
					return false;

				// can't assist player out of sanctuary from sanctuary if has pvp enabled
				if (unitTarget.IsPvP)
					if (unit.IsInSanctuary && !unitTarget.IsInSanctuary)
						return false;
			}
		}
		// PvC case - player can assist creature only if has specific type flags
		// !target.HasFlag(UNIT_FIELD_FLAGS, UnitFlags.PvpAttackable) &&
		else if (unit != null && unit.HasUnitFlag(UnitFlags.PlayerControlled))
		{
			if (bySpell == null || !bySpell.HasAttribute(SpellAttr6.CanAssistImmunePc))
				if (unitTarget != null && !unitTarget.IsPvP)
				{
					var creatureTarget = target.AsCreature;

					if (creatureTarget != null)
						return (creatureTarget.Template.TypeFlags.HasFlag(CreatureTypeFlags.TreatAsRaidUnit) || creatureTarget.Template.TypeFlags.HasFlag(CreatureTypeFlags.CanAssist));
				}
		}

		return true;
	}

	public Unit GetMagicHitRedirectTarget(Unit victim, SpellInfo spellInfo)
	{
		// Patch 1.2 notes: Spell Reflection no longer reflects abilities
		if (spellInfo.HasAttribute(SpellAttr0.IsAbility) || spellInfo.HasAttribute(SpellAttr1.NoRedirection) || spellInfo.HasAttribute(SpellAttr0.NoImmunities))
			return victim;

		var magnetAuras = victim.GetAuraEffectsByType(AuraType.SpellMagnet);

		foreach (var aurEff in magnetAuras)
		{
			var magnet = aurEff.Base.Caster;

			if (magnet != null)
				if (spellInfo.CheckExplicitTarget(this, magnet) == SpellCastResult.SpellCastOk && IsValidAttackTarget(magnet, spellInfo))
				{
					/// @todo handle this charge drop by proc in cast phase on explicit target
					if (spellInfo.HasHitDelay)
					{
						// Set up missile speed based delay
						var hitDelay = spellInfo.LaunchDelay;

						if (spellInfo.HasAttribute(SpellAttr9.SpecialDelayCalculation))
							hitDelay += spellInfo.Speed;
						else if (spellInfo.Speed > 0.0f)
							hitDelay += Math.Max(victim.GetDistance(this), 5.0f) / spellInfo.Speed;

						var delay = (uint)Math.Floor(hitDelay * 1000.0f);
						// Schedule charge drop
						aurEff.Base.DropChargeDelayed(delay, AuraRemoveMode.Expire);
					}
					else
					{
						aurEff.Base.DropCharge(AuraRemoveMode.Expire);
					}

					return magnet;
				}
		}

		return victim;
	}

	public virtual uint GetCastSpellXSpellVisualId(SpellInfo spellInfo)
	{
		return spellInfo.GetSpellXSpellVisualId(this);
	}

	public List<GameObject> GetGameObjectListWithEntryInGrid(uint entry = 0, float maxSearchRange = 250.0f)
	{
		List<GameObject> gameobjectList = new();
		var check = new AllGameObjectsWithEntryInRange(this, entry, maxSearchRange);
		var searcher = new GameObjectListSearcher(this, gameobjectList, check, GridType.Grid);

		Cell.VisitGrid(this, searcher, maxSearchRange, true);

		return gameobjectList;
	}

	public List<Creature> GetCreatureListWithEntryInGrid(uint entry = 0, float maxSearchRange = 250.0f)
	{
		List<Creature> creatureList = new();
		var check = new AllCreaturesOfEntryInRange(this, entry, maxSearchRange);
		var searcher = new CreatureListSearcher(this, creatureList, check, GridType.Grid);

		Cell.VisitGrid(this, searcher, maxSearchRange, true);

		return creatureList;
	}

	public List<Creature> GetCreatureListWithEntryInGrid(uint[] entry, float maxSearchRange = 250.0f)
	{
		List<Creature> creatureList = new();
		var check = new AllCreaturesOfEntriesInRange(this, entry, maxSearchRange);
		var searcher = new CreatureListSearcher(this, creatureList, check, GridType.Grid);

		Cell.VisitGrid(this, searcher, maxSearchRange, true);

		return creatureList;
	}

	public List<Creature> GetCreatureListWithOptionsInGrid(float maxSearchRange, FindCreatureOptions options)
	{
		List<Creature> creatureList = new();
		NoopCheckCustomizer checkCustomizer = new();
		CreatureWithOptionsInObjectRangeCheck<NoopCheckCustomizer> check = new(this, checkCustomizer, options);
		CreatureListSearcher searcher = new(this, creatureList, check, GridType.Grid);

		if (options.IgnorePhases)
			searcher._phaseShift = PhasingHandler.GetAlwaysVisiblePhaseShift();

		Cell.VisitGrid(this, searcher, maxSearchRange, true);

		return creatureList;
	}


	public List<Unit> GetPlayerListInGrid(float maxSearchRange, bool alive = true)
	{
		List<Unit> playerList = new();
		var checker = new AnyPlayerInObjectRangeCheck(this, maxSearchRange, alive);
		var searcher = new PlayerListSearcher(this, playerList, checker);

		Cell.VisitGrid(this, searcher, maxSearchRange);

		return playerList;
	}

	public bool InSamePhase(PhaseShift phaseShift)
	{
		return PhaseShift.CanSee(phaseShift);
	}

	public bool InSamePhase(WorldObject obj)
	{
		return PhaseShift.CanSee(obj.PhaseShift);
	}

	public static bool InSamePhase(WorldObject a, WorldObject b)
	{
		return a != null && b != null && a.InSamePhase(b);
	}

	public void PlayDistanceSound(uint soundId, Player target = null)
	{
		PlaySpeakerBoxSound playSpeakerBoxSound = new(GUID, soundId);

		if (target != null)
			target.SendPacket(playSpeakerBoxSound);
		else
			SendMessageToSet(playSpeakerBoxSound, true);
	}

	public void PlayDirectSound(uint soundId, Player target = null, uint broadcastTextId = 0)
	{
		PlaySound sound = new(GUID, soundId, broadcastTextId);

		if (target)
			target.SendPacket(sound);
		else
			SendMessageToSet(sound, true);
	}

	public void PlayDirectMusic(uint musicId, Player target = null)
	{
		if (target)
			target.SendPacket(new PlayMusic(musicId));
		else
			SendMessageToSet(new PlayMusic(musicId), true);
	}

	public void DestroyForNearbyPlayers()
	{
		if (!IsInWorld)
			return;

		List<Unit> targets = new();
		var check = new AnyPlayerInObjectRangeCheck(this, VisibilityRange, false);
		var searcher = new PlayerListSearcher(this, targets, check);

		Cell.VisitGrid(this, searcher, VisibilityRange);

		foreach (Player player in targets)
		{
			if (player == this)
				continue;

			if (!player.HaveAtClient(this))
				continue;

			if (IsTypeMask(TypeMask.Unit) && (AsUnit.CharmerGUID == player.GUID)) // @todo this is for puppet
				continue;

			DestroyForPlayer(player);

			lock (player.ClientGuiDs)
			{
				player.ClientGuiDs.Remove(GUID);
			}
		}
	}

	public virtual void UpdateObjectVisibility(bool force = true)
	{
		//updates object's visibility for nearby players
		var notifier = new VisibleChangesNotifier(new[]
												{
													this
												},
												GridType.World);

		Cell.VisitGrid(this, notifier, VisibilityRange);
	}

	public virtual void UpdateObjectVisibilityOnCreate()
	{
		UpdateObjectVisibility(true);
	}

	public virtual void UpdateObjectVisibilityOnDestroy()
	{
		DestroyForNearbyPlayers();
	}

	public virtual void BuildUpdate(Dictionary<Player, UpdateData> data)
	{
		var notifier = new WorldObjectChangeAccumulator(this, data, GridType.World);
		Cell.VisitGrid(this, notifier, VisibilityRange);

		ClearUpdateMask(false);
	}

	public virtual bool AddToObjectUpdate()
	{
		Map.AddUpdateObject(this);

		return true;
	}

	public virtual void RemoveFromObjectUpdate()
	{
		Map.RemoveUpdateObject(this);
	}

	public virtual string GetName(Locale locale = Locale.enUS)
	{
		return _name;
	}

	public void SetName(string name)
	{
		_name = name;
	}

	public bool IsTypeId(TypeId typeId)
	{
		return TypeId == typeId;
	}

	public bool IsTypeMask(TypeMask mask)
	{
		return Convert.ToBoolean(mask & ObjectTypeMask);
	}

	public virtual bool HasQuest(uint questId)
	{
		return false;
	}

	public virtual bool HasInvolvedQuest(uint questId)
	{
		return false;
	}

	public void SetIsNewObject(bool enable)
	{
		_isNewObject = enable;
	}

	public void SetDestroyedObject(bool destroyed)
	{
		_isDestroyedObject = destroyed;
	}

	public bool TryGetAsCreature(out Creature creature)
	{
		creature = AsCreature;

		return creature != null;
	}

	public bool TryGetAsPlayer(out Player player)
	{
		player = AsPlayer;

		return player != null;
	}

	public bool TryGetAsGameObject(out GameObject gameObject)
	{
		gameObject = AsGameObject;

		return gameObject != null;
	}

	public bool TryGetAsItem(out Item item)
	{
		item = AsItem;

		return item != null;
	}

	public bool TryGetAsUnit(out Unit unit)
	{
		unit = AsUnit;

		return unit != null;
	}

	public bool TryGetAsCorpse(out Corpse corpse)
	{
		corpse = AsCorpse;

		return corpse != null;
	}

	public bool TryGetAsDynamicObject(out DynamicObject dynObj)
	{
		dynObj = AsDynamicObject;

		return dynObj != null;
	}

	public bool TryGetAsAreaTrigger(out AreaTrigger areaTrigger)
	{
		areaTrigger = AsAreaTrigger;

		return areaTrigger != null;
	}

	public bool TryGetAsConversation(out Conversation conversation)
	{
		conversation = AsConversation;

		return conversation != null;
	}

	public bool TryGetAsSceneObject(out SceneObject sceneObject)
	{
		sceneObject = AsSceneObject;

		return sceneObject != null;
	}

	public virtual uint GetLevelForTarget(WorldObject target)
	{
		return 1;
	}

	public void AddToNotify(NotifyFlags f)
	{
		_notifyflags |= f;
	}

	public bool IsNeedNotify(NotifyFlags f)
	{
		return Convert.ToBoolean(_notifyflags & f);
	}

	public void ResetAllNotifies()
	{
		_notifyflags = 0;
	}

	public T GetTransport<T>() where T : class, ITransport
	{
		return _transport as T;
	}

	public virtual ObjectGuid GetTransGUID()
	{
		if (Transport != null)
			return Transport.GetTransportGUID();

		return ObjectGuid.Empty;
	}

	public void SetTransport(ITransport t)
	{
		_transport = t;
	}

	public virtual bool IsNeverVisibleFor(WorldObject seer)
	{
		return !IsInWorld || IsDestroyedObject;
	}

	public virtual bool IsAlwaysVisibleFor(WorldObject seer)
	{
		return false;
	}

	public virtual bool IsInvisibleDueToDespawn(WorldObject seer)
	{
		return false;
	}

	public virtual bool IsAlwaysDetectableFor(WorldObject seer)
	{
		return false;
	}

	public virtual bool LoadFromDB(ulong spawnId, Map map, bool addToMap, bool allowDuplicate)
	{
		return true;
	}

	//Position

	public float GetDistanceZ(WorldObject obj)
	{
		var dz = Math.Abs(Location.Z - obj.Location.Z);
		var sizefactor = CombatReach + obj.CombatReach;
		var dist = dz - sizefactor;

		return (dist > 0 ? dist : 0);
	}

	public virtual bool _IsWithinDist(WorldObject obj, float dist2compare, bool is3D, bool incOwnRadius = true, bool incTargetRadius = true)
	{
		float sizefactor = 0;
		sizefactor += incOwnRadius ? CombatReach : 0.0f;
		sizefactor += incTargetRadius ? obj.CombatReach : 0.0f;
		var maxdist = dist2compare + sizefactor;

		Position thisOrTransport = Location;
		Position objOrObjTransport = obj.Location;

		if (Transport != null && obj.Transport != null && obj.Transport.GetTransportGUID() == Transport.GetTransportGUID())
		{
			thisOrTransport = MovementInfo.Transport.Pos;
			objOrObjTransport = obj.MovementInfo.Transport.Pos;
		}


		if (is3D)
			return thisOrTransport.IsInDist(objOrObjTransport, maxdist);
		else
			return thisOrTransport.IsInDist2d(objOrObjTransport, maxdist);
	}

	public float GetDistance(WorldObject obj)
	{
		var d = Location.GetExactDist(obj.Location) - CombatReach - obj.CombatReach;

		return d > 0.0f ? d : 0.0f;
	}

	public float GetDistance(Position pos)
	{
		var d = Location.GetExactDist(pos) - CombatReach;

		return d > 0.0f ? d : 0.0f;
	}

	public float GetDistance(float x, float y, float z)
	{
		var d = Location.GetExactDist(x, y, z) - CombatReach;

		return d > 0.0f ? d : 0.0f;
	}

	public float GetDistance2d(WorldObject obj)
	{
		var d = Location.GetExactDist2d(obj.Location) - CombatReach - obj.CombatReach;

		return d > 0.0f ? d : 0.0f;
	}

	public float GetDistance2d(float x, float y)
	{
		var d = Location.GetExactDist2d(x, y) - CombatReach;

		return d > 0.0f ? d : 0.0f;
	}

	public bool IsSelfOrInSameMap(WorldObject obj)
	{
		if (this == obj)
			return true;

		return IsInMap(obj);
	}

	public bool IsInMap(WorldObject obj)
	{
		if (obj != null)
			return IsInWorld && obj.IsInWorld && Map.Id == obj.Map.Id;

		return false;
	}

	public bool IsWithinDist3d(float x, float y, float z, float dist)
	{
		return Location.IsInDist(x, y, z, dist + CombatReach);
	}

	public bool IsWithinDist3d(Position pos, float dist)
	{
		return Location.IsInDist(pos, dist + CombatReach);
	}

	public bool IsWithinDist2d(float x, float y, float dist)
	{
		return Location.IsInDist2d(x, y, dist + CombatReach);
	}

	public bool IsWithinDist2d(Position pos, float dist)
	{
		return Location.IsInDist2d(pos, dist + CombatReach);
	}

	public bool IsWithinDist(WorldObject obj, float dist2compare, bool is3D = true, bool incOwnRadius = true, bool incTargetRadius = true)
	{
		return obj != null && _IsWithinDist(obj, dist2compare, is3D, incOwnRadius, incTargetRadius);
	}

	public bool IsWithinDistInMap(WorldObject obj, float dist2compare, bool is3D = true, bool incOwnRadius = true, bool incTargetRadius = true)
	{
		return obj && IsInMap(obj) && InSamePhase(obj) && _IsWithinDist(obj, dist2compare, is3D, incOwnRadius, incTargetRadius);
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

			if (IsTypeId(TypeId.Player))
			{
				pos = Location.Copy();
				pos.Z += CollisionHeight;
			}
			else
			{
				GetHitSpherePointFor(new Position(ox, oy, oz), pos);
			}

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
		{
			obj.GetHitSpherePointFor(new Position(Location.X, Location.Y, Location.Z + CollisionHeight), pos);
		}

		var pos2 = new Position();

		if (IsPlayer)
		{
			pos2 = Location.Copy();
			pos2.Z += CollisionHeight;
		}
		else
		{
			GetHitSpherePointFor(new Position(obj.Location.X, obj.Location.Y, obj.Location.Z + obj.CollisionHeight), pos2);
		}

		return Map.IsInLineOfSight(PhaseShift, pos2, pos, checks, ignoreFlags);
	}

	public Position GetHitSpherePointFor(Position dest)
	{
		Vector3 vThis = new(Location.X, Location.Y, Location.Z + CollisionHeight);
		Vector3 vObj = new(dest.X, dest.Y, dest.Z);
		var contactPoint = vThis + (vObj - vThis).directionOrZero() * Math.Min(dest.GetExactDist(Location), CombatReach);

		return new Position(contactPoint.X, contactPoint.Y, contactPoint.Z, Location.GetAbsoluteAngle(contactPoint.X, contactPoint.Y));
	}

	public void GetHitSpherePointFor(Position dest, Position refDest)
	{
		var pos = GetHitSpherePointFor(dest);
		refDest.X = pos.X;
		refDest.Y = pos.Y;
		refDest.Z = pos.Z;
	}

	public bool GetDistanceOrder(WorldObject obj1, WorldObject obj2, bool is3D = true)
	{
		var dx1 = Location.X - obj1.Location.X;
		var dy1 = Location.Y - obj1.Location.Y;
		var distsq1 = dx1 * dx1 + dy1 * dy1;

		if (is3D)
		{
			var dz1 = Location.Z - obj1.Location.Z;
			distsq1 += dz1 * dz1;
		}

		var dx2 = Location.X - obj2.Location.X;
		var dy2 = Location.Y - obj2.Location.Y;
		var distsq2 = dx2 * dx2 + dy2 * dy2;

		if (is3D)
		{
			var dz2 = Location.Z - obj2.Location.Z;
			distsq2 += dz2 * dz2;
		}

		return distsq1 < distsq2;
	}

	public bool IsInRange(WorldObject obj, float minRange, float maxRange, bool is3D = true)
	{
		var dx = Location.X - obj.Location.X;
		var dy = Location.Y - obj.Location.Y;
		var distsq = dx * dx + dy * dy;

		if (is3D)
		{
			var dz = Location.Z - obj.Location.Z;
			distsq += dz * dz;
		}

		var sizefactor = CombatReach + obj.CombatReach;

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

	public bool IsInBetween(WorldObject obj1, WorldObject obj2, float size = 0)
	{
		return obj1 && obj2 && IsInBetween(obj1.Location, obj2.Location, size);
	}

	public bool IsInFront(WorldObject target, float arc = MathFunctions.PI)
	{
		return Location.HasInArc(arc, target.Location);
	}

	public bool IsInBack(WorldObject target, float arc = MathFunctions.PI)
	{
		return !Location.HasInArc(2 * MathFunctions.PI - arc, target.Location);
	}

	public void GetRandomPoint(Position pos, float distance, out float rand_x, out float rand_y, out float rand_z)
	{
		if (distance == 0)
		{
			rand_x = pos.X;
			rand_y = pos.Y;
			rand_z = pos.Z;

			return;
		}

		// angle to face `obj` to `this`
		var angle = (float)RandomHelper.NextDouble() * (2 * MathFunctions.PI);
		var new_dist = (float)RandomHelper.NextDouble() + (float)RandomHelper.NextDouble();
		new_dist = distance * (new_dist > 1 ? new_dist - 2 : new_dist);

		rand_x = (float)(pos.X + new_dist * Math.Cos(angle));
		rand_y = (float)(pos.Y + new_dist * Math.Sin(angle));
		rand_z = pos.Z;

		rand_x = GridDefines.NormalizeMapCoord(rand_x);
		rand_y = GridDefines.NormalizeMapCoord(rand_y);
		rand_z = UpdateGroundPositionZ(rand_x, rand_y, rand_z); // update to LOS height if available
	}

	public Position GetRandomPoint(Position srcPos, float distance)
	{
		GetRandomPoint(srcPos, distance, out var x, out var y, out var z);

		return new Position(x, y, z, Location.Orientation);
	}

	public float UpdateGroundPositionZ(float x, float y, float z)
	{
		var newZ = GetMapHeight(x, y, z);

		if (newZ > MapConst.InvalidHeight)
			z = newZ + (IsUnit ? AsUnit.HoverOffset : 0.0f);

		return z;
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
		if (Transport != null)
		{
			groundZ = z;

			return z;
		}

		var unit = AsUnit;

		if (unit != null)
		{
			if (!unit.CanFly)
			{
				var canSwim = unit.CanSwim;
				var ground_z = z;
				float max_z;

				if (canSwim)
					max_z = GetMapWaterOrGroundLevel(x, y, z, ref ground_z);
				else
					max_z = ground_z = GetMapHeight(x, y, z);

				if (max_z > MapConst.InvalidHeight)
				{
					// hovering units cannot go below their hover height
					var hoverOffset = unit.HoverOffset;
					max_z += hoverOffset;
					ground_z += hoverOffset;

					if (z > max_z)
						z = max_z;
					else if (z < ground_z)
						z = ground_z;
				}

				groundZ = ground_z;
			}
			else
			{
				var ground_z = GetMapHeight(x, y, z) + unit.HoverOffset;

				if (z < ground_z)
					z = ground_z;

				groundZ = ground_z;
			}
		}
		else
		{
			var ground_z = GetMapHeight(x, y, z);

			if (ground_z > MapConst.InvalidHeight)
				z = ground_z;

			groundZ = ground_z;
		}

		return z;
	}

	public void GetNearPoint2D(WorldObject searcher, out float x, out float y, float distance2d, float absAngle)
	{
		var effectiveReach = CombatReach;

		if (searcher)
		{
			effectiveReach += searcher.CombatReach;

			if (this != searcher)
			{
				var myHover = 0.0f;
				var searcherHover = 0.0f;

				var unit = AsUnit;

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

		x = Location.X + (effectiveReach + distance2d) * MathF.Cos(absAngle);
		y = Location.Y + (effectiveReach + distance2d) * MathF.Sin(absAngle);

		x = GridDefines.NormalizeMapCoord(x);
		y = GridDefines.NormalizeMapCoord(y);
	}

	public float GetNearPoint(WorldObject searcher, Position pos, float distance2d, float absAngle)
	{
		var x = pos.X;
		var y = pos.Y;
		float floor = 0;
		GetNearPoint2D(searcher, out x, out y, distance2d, absAngle);
		pos.Z = Location.Z;
		pos.Z = (searcher ?? this).UpdateAllowedPositionZ(x, y, pos.Z, ref floor);
		pos.X = x;
		pos.Y = y;

		// if detection disabled, return first point
		if (!WorldConfig.GetBoolValue(WorldCfg.DetectPosCollision))
			return floor;

		// return if the point is already in LoS
		if (IsWithinLOS(pos.X, pos.Y, pos.Z))
			return floor;

		// remember first point
		var first_x = pos.X;
		var first_y = pos.Y;

		// loop in a circle to look for a point in LoS using small steps
		for (var angle = MathFunctions.PI / 8; angle < Math.PI * 2; angle += MathFunctions.PI / 8)
		{
			GetNearPoint2D(searcher, out x, out y, distance2d, absAngle + angle);
			pos.Z = Location.Z;
			pos.Z = (searcher ?? this).UpdateAllowedPositionZ(x, y, pos.Z);
			pos.X = x;
			pos.Y = y;

			if (IsWithinLOS(pos.X, pos.Y, pos.Z))
				return floor;
		}

		// still not in LoS, give up and return first position found
		pos.X = first_x;
		pos.Y = first_y;

		return floor;
	}

	public void GetClosePoint(Position pos, float size, float distance2d = 0, float relAngle = 0)
	{
		// angle calculated from current orientation
		GetNearPoint(null, pos, distance2d + size, Location.Orientation + relAngle);
	}

	public Position GetNearPosition(float dist, float angle)
	{
		var pos = Location;
		MovePosition(pos, dist, angle);

		return pos;
	}

	public Position GetFirstCollisionPosition(float dist, float angle)
	{
		var pos = new Position(Location);
		MovePositionToFirstCollision(pos, dist, angle);

		return pos;
	}

	public Position GetRandomNearPosition(float radius)
	{
		var pos = Location;
		MovePosition(pos, radius * (float)RandomHelper.NextDouble(), (float)RandomHelper.NextDouble() * MathFunctions.PI * 2);

		return pos;
	}

	public Player FindNearestPlayer(float range, bool alive = true)
	{
		var check = new AnyPlayerInObjectRangeCheck(this, VisibilityRange);
		var searcher = new PlayerSearcher(this, check, GridType.Grid);
		Cell.VisitGrid(this, searcher, range);

		return searcher.GetTarget();
	}

	public void GetContactPoint(WorldObject obj, Position pos, float distance2d = 0.5f)
	{
		// angle to face `obj` to `this` using distance includes size of `obj`
		GetNearPoint(obj, pos, distance2d, Location.GetAbsoluteAngle(obj.Location));
	}

	public void MovePosition(Position pos, float dist, float angle)
	{
		angle += Location.Orientation;
		var destx = pos.X + dist * (float)Math.Cos(angle);
		var desty = pos.Y + dist * (float)Math.Sin(angle);

		// Prevent invalid coordinates here, position is unchanged
		if (!GridDefines.IsValidMapCoord(destx, desty, pos.Z))
		{
			Log.outError(LogFilter.Server, "WorldObject.MovePosition invalid coordinates X: {0} and Y: {1} were passed!", destx, desty);

			return;
		}

		var ground = GetMapHeight(destx, desty, MapConst.MaxHeight);
		var floor = GetMapHeight(destx, desty, pos.Z);
		var destz = Math.Abs(ground - pos.Z) <= Math.Abs(floor - pos.Z) ? ground : floor;

		var step = dist / 10.0f;

		for (byte j = 0; j < 10; ++j)
			// do not allow too big z changes
			if (Math.Abs(pos.Z - destz) > 6)
			{
				destx -= step * (float)Math.Cos(angle);
				desty -= step * (float)Math.Sin(angle);
				ground = Map.GetHeight(PhaseShift, destx, desty, MapConst.MaxHeight, true);
				floor = Map.GetHeight(PhaseShift, destx, desty, pos.Z, true);
				destz = Math.Abs(ground - pos.Z) <= Math.Abs(floor - pos.Z) ? ground : floor;
			}
			// we have correct destz now
			else
			{
				pos.Relocate(destx, desty, destz);

				break;
			}

		pos.X = GridDefines.NormalizeMapCoord(pos.X);
		pos.Y = GridDefines.NormalizeMapCoord(pos.Y);
		pos.Z = UpdateGroundPositionZ(pos.X, pos.Y, pos.Z);
		pos.Orientation = Location.Orientation;
	}

	public void MovePositionToFirstCollision(Position pos, float dist, float angle)
	{
		angle += Location.Orientation;
		var destx = pos.X + dist * (float)Math.Cos(angle);
		var desty = pos.Y + dist * (float)Math.Sin(angle);
		var destz = pos.Z;

		// Prevent invalid coordinates here, position is unchanged
		if (!GridDefines.IsValidMapCoord(destx, desty))
		{
			Log.outError(LogFilter.Server, "WorldObject.MovePositionToFirstCollision invalid coordinates X: {0} and Y: {1} were passed!", destx, desty);

			return;
		}

		// Use a detour raycast to get our first collision point
		PathGenerator path = new(this);
		path.SetUseRaycast(true);
		path.CalculatePath(new Position(destx, desty, destz), false);

		// We have a invalid path result. Skip further processing.
		if (!path.GetPathType().HasFlag(PathType.NotUsingPath))
			if ((path.GetPathType() & ~(PathType.Normal | PathType.Shortcut | PathType.Incomplete | PathType.FarFromPoly)) != 0)
				return;

		var result = path.GetPath()[path.GetPath().Length - 1];
		destx = result.X;
		desty = result.Y;
		destz = result.Z;

		// check static LOS
		var halfHeight = CollisionHeight * 0.5f;
		var col = false;

		// Unit is flying, check for potential collision via vmaps
		if (path.GetPathType().HasFlag(PathType.NotUsingPath))
		{
			col = Global.VMapMgr.GetObjectHitPos(PhasingHandler.GetTerrainMapId(PhaseShift, Location.MapId, Map.Terrain, pos.X, pos.Y),
												pos.X,
												pos.Y,
												pos.Z + halfHeight,
												destx,
												desty,
												destz + halfHeight,
												out destx,
												out desty,
												out destz,
												-0.5f);

			destz -= halfHeight;

			// Collided with static LOS object, move back to collision point
			if (col)
			{
				destx -= SharedConst.ContactDistance * MathF.Cos(angle);
				desty -= SharedConst.ContactDistance * MathF.Sin(angle);
				dist = MathF.Sqrt((pos.X - destx) * (pos.X - destx) + (pos.Y - desty) * (pos.Y - desty));
			}
		}

		// check dynamic collision
		col = Map.GetObjectHitPos(PhaseShift, pos.X, pos.Y, pos.Z + halfHeight, destx, desty, destz + halfHeight, out destx, out desty, out destz, -0.5f);

		destz -= halfHeight;

		// Collided with a gameobject, move back to collision point
		if (col)
		{
			destx -= SharedConst.ContactDistance * (float)Math.Cos(angle);
			desty -= SharedConst.ContactDistance * (float)Math.Sin(angle);
			dist = (float)Math.Sqrt((pos.X - destx) * (pos.X - destx) + (pos.Y - desty) * (pos.Y - desty));
		}

		var groundZ = MapConst.VMAPInvalidHeightValue;
		pos.X = GridDefines.NormalizeMapCoord(pos.X);
		pos.Y = GridDefines.NormalizeMapCoord(pos.Y);
		destz = UpdateAllowedPositionZ(destx, desty, destz, ref groundZ);

		pos.Orientation = Location.Orientation;
		pos.Relocate(destx, desty, destz);

		// position has no ground under it (or is too far away)
		if (groundZ <= MapConst.InvalidHeight)
		{
			var unit = AsUnit;

			if (unit != null)
			{
				// unit can fly, ignore.
				if (unit.CanFly)
					return;

				// fall back to gridHeight if any
				var gridHeight = Map.GetGridHeight(PhaseShift, pos.X, pos.Y);

				if (gridHeight > MapConst.InvalidHeight)
					pos.Z = gridHeight + unit.HoverOffset;
			}
		}
	}

	public float GetMapWaterOrGroundLevel(float x, float y, float z)
	{
		float groundLevel = 0;

		return GetMapWaterOrGroundLevel(x, y, z, ref groundLevel);
	}

	public float GetMapWaterOrGroundLevel(float x, float y, float z, ref float ground)
	{
		return Map.GetWaterOrGroundLevel(PhaseShift, x, y, z, ref ground, IsTypeMask(TypeMask.Unit) ? !AsUnit.HasAuraType(AuraType.WaterWalk) : false, CollisionHeight);
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

	public void SetLocationInstanceId(uint _instanceId)
	{
		InstanceId = _instanceId;
	}


	public static implicit operator bool(WorldObject obj)
	{
		return obj != null;
	}

	bool CanDetect(WorldObject obj, bool ignoreStealth, bool checkAlert = false)
	{
		var seer = this;

		// If a unit is possessing another one, it uses the detection of the latter
		// Pets don't have detection, they use the detection of their masters
		var thisUnit = AsUnit;

		if (thisUnit != null)
		{
			if (thisUnit.IsPossessing)
			{
				var charmed = thisUnit.Charmed;

				if (charmed != null)
					seer = charmed;
			}
			else
			{
				var controller = thisUnit.CharmerOrOwner;

				if (controller != null)
					seer = controller;
			}
		}

		if (obj.IsAlwaysDetectableFor(seer))
			return true;

		if (!ignoreStealth && !seer.CanDetectInvisibilityOf(obj))
			return false;

		if (!ignoreStealth && !seer.CanDetectStealthOf(obj, checkAlert))
			return false;

		return true;
	}

	bool CanDetectInvisibilityOf(WorldObject obj)
	{
		var mask = obj.Invisibility.GetFlags() & InvisibilityDetect.GetFlags();

		// Check for not detected types
		if (mask != obj.Invisibility.GetFlags())
			return false;

		for (var i = 0; i < (int)InvisibilityType.Max; ++i)
		{
			if (!Convert.ToBoolean(mask & (1ul << i)))
				continue;

			var objInvisibilityValue = obj.Invisibility.GetValue((InvisibilityType)i);
			var ownInvisibilityDetectValue = InvisibilityDetect.GetValue((InvisibilityType)i);

			// Too low value to detect
			if (ownInvisibilityDetectValue < objInvisibilityValue)
				return false;
		}

		return true;
	}

	bool CanDetectStealthOf(WorldObject obj, bool checkAlert = false)
	{
		// Combat reach is the minimal distance (both in front and behind),
		//   and it is also used in the range calculation.
		// One stealth point increases the visibility range by 0.3 yard.

		if (obj.Stealth.GetFlags() == 0)
			return true;

		var distance = Location.GetExactDist(obj.Location);
		var combatReach = 0.0f;

		var unit = AsUnit;

		if (unit != null)
			combatReach = unit.CombatReach;

		if (distance < combatReach)
			return true;

		// Only check back for units, it does not make sense for gameobjects
		if (unit && !Location.HasInArc(MathF.PI, obj.Location))
			return false;

		// Traps should detect stealth always
		var go = AsGameObject;

		if (go != null)
			if (go.GoType == GameObjectTypes.Trap)
				return true;

		go = obj.AsGameObject;

		for (var i = 0; i < (int)StealthType.Max; ++i)
		{
			if (!Convert.ToBoolean(obj.Stealth.GetFlags() & (1 << i)))
				continue;

			if (unit != null && unit.HasAuraTypeWithMiscvalue(AuraType.DetectStealth, i))
				return true;

			// Starting points
			var detectionValue = 30;

			// Level difference: 5 point / level, starting from level 1.
			// There may be spells for this and the starting points too, but
			// not in the DBCs of the client.
			detectionValue += (int)(GetLevelForTarget(obj) - 1) * 5;

			// Apply modifiers
			detectionValue += StealthDetect.GetValue((StealthType)i);

			if (go != null)
			{
				var owner = go.OwnerUnit;

				if (owner != null)
					detectionValue -= (int)(owner.GetLevelForTarget(this) - 1) * 5;
			}

			detectionValue -= obj.Stealth.GetValue((StealthType)i);

			// Calculate max distance
			var visibilityRange = detectionValue * 0.3f + combatReach;

			// If this unit is an NPC then player detect range doesn't apply
			if (unit && unit.IsTypeId(TypeId.Player) && visibilityRange > SharedConst.MaxPlayerStealthDetectRange)
				visibilityRange = SharedConst.MaxPlayerStealthDetectRange;

			// When checking for alert state, look 8% further, and then 1.5 yards more than that.
			if (checkAlert)
				visibilityRange += (visibilityRange * 0.08f) + 1.5f;

			// If checking for alert, and creature's visibility range is greater than aggro distance, No alert
			var tunit = obj.AsUnit;

			if (checkAlert && unit && unit.AsCreature && visibilityRange >= unit.AsCreature.GetAttackDistance(tunit) + unit.AsCreature.CombatDistance)
				return false;

			if (distance > visibilityRange)
				return false;
		}

		return true;
	}

	SpellMissInfo MagicSpellHitResult(Unit victim, SpellInfo spellInfo)
	{
		// Can`t miss on dead target (on skinning for example)
		if (!victim.IsAlive && !victim.IsPlayer)
			return SpellMissInfo.None;

		if (spellInfo.HasAttribute(SpellAttr3.NoAvoidance))
			return SpellMissInfo.None;

		double missChance;

		if (spellInfo.HasAttribute(SpellAttr7.NoAttackMiss))
		{
			missChance = 0.0f;
		}
		else
		{
			var schoolMask = spellInfo.GetSchoolMask();
			// PvP - PvE spell misschances per leveldif > 2
			var lchance = victim.IsPlayer ? 7 : 11;
			var thisLevel = GetLevelForTarget(victim);

			if (IsCreature && AsCreature.IsTrigger)
				thisLevel = Math.Max(thisLevel, spellInfo.SpellLevel);

			var leveldif = (int)(victim.GetLevelForTarget(this) - thisLevel);
			var levelBasedHitDiff = leveldif;

			// Base hit chance from attacker and victim levels
			double modHitChance = 100;

			if (levelBasedHitDiff >= 0)
			{
				if (!victim.IsPlayer)
				{
					modHitChance = 94 - 3 * Math.Min(levelBasedHitDiff, 3);
					levelBasedHitDiff -= 3;
				}
				else
				{
					modHitChance = 96 - Math.Min(levelBasedHitDiff, 2);
					levelBasedHitDiff -= 2;
				}

				if (levelBasedHitDiff > 0)
					modHitChance -= lchance * Math.Min(levelBasedHitDiff, 7);
			}
			else
			{
				modHitChance = 97 - levelBasedHitDiff;
			}

			// Spellmod from SpellModOp::HitChance
			var modOwner = SpellModOwner;

			if (modOwner != null)
				modOwner.ApplySpellMod(spellInfo, SpellModOp.HitChance, ref modHitChance);

			// Spells with SPELL_ATTR3_IGNORE_HIT_RESULT will ignore target's avoidance effects
			if (!spellInfo.HasAttribute(SpellAttr3.AlwaysHit))
				// Chance hit from victim SPELL_AURA_MOD_ATTACKER_SPELL_HIT_CHANCE auras
				modHitChance += victim.GetTotalAuraModifierByMiscMask(AuraType.ModAttackerSpellHitChance, (int)schoolMask);

			var HitChance = modHitChance;
			// Increase hit chance from attacker SPELL_AURA_MOD_SPELL_HIT_CHANCE and attacker ratings
			var unit = AsUnit;

			if (unit != null)
				HitChance += unit.ModSpellHitChance;

			MathFunctions.RoundToInterval(ref HitChance, 0.0f, 100.0f);

			missChance = 100.0f - HitChance;
		}

		var tmp = missChance * 100.0f;

		var rand = RandomHelper.IRand(0, 9999);

		if (tmp > 0 && rand < tmp)
			return SpellMissInfo.Miss;

		// Chance resist mechanic (select max value from every mechanic spell effect)
		var resist_chance = victim.GetMechanicResistChance(spellInfo) * 100;

		// Roll chance
		if (resist_chance > 0 && rand < (tmp += resist_chance))
			return SpellMissInfo.Resist;

		// cast by caster in front of victim
		if (!victim.HasUnitState(UnitState.Controlled) && (victim.Location.HasInArc(MathF.PI, Location) || victim.HasAuraType(AuraType.IgnoreHitDirection)))
		{
			var deflect_chance = victim.GetTotalAuraModifier(AuraType.DeflectSpells) * 100;

			if (deflect_chance > 0 && rand < (tmp += deflect_chance))
				return SpellMissInfo.Deflect;
		}

		return SpellMissInfo.None;
	}

	void SendCancelSpellVisual(uint id)
	{
		CancelSpellVisual cancelSpellVisual = new();
		cancelSpellVisual.Source = GUID;
		cancelSpellVisual.SpellVisualID = id;
		SendMessageToSet(cancelSpellVisual, true);
	}

	void SendPlayOrphanSpellVisual(ObjectGuid target, uint spellVisualId, float travelSpeed, bool speedAsTime = false, bool withSourceOrientation = false)
	{
		PlayOrphanSpellVisual playOrphanSpellVisual = new();
		playOrphanSpellVisual.SourceLocation = Location;

		if (withSourceOrientation)
		{
			if (IsGameObject)
			{
				var rotation = AsGameObject.GetWorldRotation();

				rotation.toEulerAnglesZYX(out playOrphanSpellVisual.SourceRotation.Z,
										out playOrphanSpellVisual.SourceRotation.Y,
										out playOrphanSpellVisual.SourceRotation.X);
			}
			else
			{
				playOrphanSpellVisual.SourceRotation = new Position(0.0f, 0.0f, Location.Orientation);
			}
		}

		playOrphanSpellVisual.Target = target; // exclusive with TargetLocation
		playOrphanSpellVisual.SpellVisualID = spellVisualId;
		playOrphanSpellVisual.TravelSpeed = travelSpeed;
		playOrphanSpellVisual.SpeedAsTime = speedAsTime;
		playOrphanSpellVisual.LaunchDelay = 0.0f;
		SendMessageToSet(playOrphanSpellVisual, true);
	}

	void SendPlayOrphanSpellVisual(Position targetLocation, uint spellVisualId, float travelSpeed, bool speedAsTime = false, bool withSourceOrientation = false)
	{
		PlayOrphanSpellVisual playOrphanSpellVisual = new();
		playOrphanSpellVisual.SourceLocation = Location;

		if (withSourceOrientation)
		{
			if (IsGameObject)
			{
				var rotation = AsGameObject.GetWorldRotation();

				rotation.toEulerAnglesZYX(out playOrphanSpellVisual.SourceRotation.Z,
										out playOrphanSpellVisual.SourceRotation.Y,
										out playOrphanSpellVisual.SourceRotation.X);
			}
			else
			{
				playOrphanSpellVisual.SourceRotation = new Position(0.0f, 0.0f, Location.Orientation);
			}
		}

		playOrphanSpellVisual.TargetLocation = targetLocation; // exclusive with Target
		playOrphanSpellVisual.SpellVisualID = spellVisualId;
		playOrphanSpellVisual.TravelSpeed = travelSpeed;
		playOrphanSpellVisual.SpeedAsTime = speedAsTime;
		playOrphanSpellVisual.LaunchDelay = 0.0f;
		SendMessageToSet(playOrphanSpellVisual, true);
	}

	void SendCancelOrphanSpellVisual(uint id)
	{
		CancelOrphanSpellVisual cancelOrphanSpellVisual = new();
		cancelOrphanSpellVisual.SpellVisualID = id;
		SendMessageToSet(cancelOrphanSpellVisual, true);
	}

	void SendCancelSpellVisualKit(uint id)
	{
		CancelSpellVisualKit cancelSpellVisualKit = new();
		cancelSpellVisualKit.Source = GUID;
		cancelSpellVisualKit.SpellVisualKitID = id;
		SendMessageToSet(cancelSpellVisualKit, true);
	}

	bool IsInBetween(Position pos1, Position pos2, float size)
	{
		var dist = Location.GetExactDist2d(pos1);

		// not using sqrt() for performance
		if ((dist * dist) >= pos1.GetExactDist2dSq(pos2))
			return false;

		if (size == 0)
			size = CombatReach / 2;

		var angle = pos1.GetAbsoluteAngle(pos2);

		// not using sqrt() for performance
		return (size * size) >= Location.GetExactDist2dSq(pos1.X + (float)Math.Cos(angle) * dist, pos1.Y + (float)Math.Sin(angle) * dist);
	}
}