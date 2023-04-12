// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Autofac;
using Forged.MapServer.AI.CoreAI;
using Forged.MapServer.BattleFields;
using Forged.MapServer.Chrono;
using Forged.MapServer.Collision.Management;
using Forged.MapServer.Conditions;
using Forged.MapServer.DataStorage;
using Forged.MapServer.Entities.AreaTriggers;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.GameObjects;
using Forged.MapServer.Entities.Items;
using Forged.MapServer.Entities.Objects.Update;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Globals;
using Forged.MapServer.Maps;
using Forged.MapServer.Maps.Checks;
using Forged.MapServer.Maps.GridNotifiers;
using Forged.MapServer.Maps.Grids;
using Forged.MapServer.Maps.Workers;
using Forged.MapServer.Movement.Generators;
using Forged.MapServer.Networking;
using Forged.MapServer.Networking.Packets.CombatLog;
using Forged.MapServer.Networking.Packets.Misc;
using Forged.MapServer.Networking.Packets.Movement;
using Forged.MapServer.OutdoorPVP;
using Forged.MapServer.Phasing;
using Forged.MapServer.Scenarios;
using Forged.MapServer.Scripting;
using Forged.MapServer.Spells;
using Forged.MapServer.Text;
using Framework.Constants;
using Framework.Dynamic;
using Framework.Util;
using Game.Common;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Forged.MapServer.Entities.Objects;

public abstract class WorldObject : IDisposable
{
    protected CreateObjectBits UpdateFlag;

    private bool _isNewObject;
    private string _name;
    private NotifyFlags _notifyFlags;
    private bool _objectUpdated;
    private ObjectGuid _privateObjectOwner;

    public WorldObject(bool isWorldObject, ClassFactory classFactory)
    {
        SpellFactory = classFactory.Resolve<SpellFactory>(new PositionalParameter(0, this));

        _name = "";
        IsPermanentWorldObject = isWorldObject;
        ClassFactory = classFactory;
        DisableManager = classFactory.Resolve<DisableManager>();
        MMapManager = classFactory.Resolve<MMapManager>();
        VMapManager = classFactory.Resolve<VMapManager>();
        SpellManager = classFactory.Resolve<SpellManager>();
        ObjectManager = classFactory.Resolve<GameObjectManager>();
        ConditionManager = classFactory.Resolve<ConditionManager>();
        ObjectAccessor = classFactory.Resolve<ObjectAccessor>();
        Configuration = classFactory.Resolve<IConfiguration>();
        ScriptManager = classFactory.Resolve<ScriptManager>();
        CliDB = classFactory.Resolve<CliDB>();
        OutdoorPvPManager = classFactory.Resolve<OutdoorPvPManager>();
        BattleFieldManager = classFactory.Resolve<BattleFieldManager>();

        ObjectTypeId = TypeId.Object;
        ObjectTypeMask = TypeMask.Object;

        Values = new UpdateFieldHolder(this);

        MovementInfo = new MovementInfo();
        UpdateFlag.Clear();

        ObjectData = new ObjectFieldData();
        Location = new WorldLocation(this);
        WorldObjectCombat = new WorldObjectCombat(this);
        Visibility = new WorldObjectVisibility(this);
    }

    public Player AffectingPlayer => CharmerOrOwnerGUID.IsEmpty ? AsPlayer : CharmerOrOwner?.CharmerOrOwnerPlayerOrPlayerItself;
    public virtual ushort AIAnimKitId => 0;
    public AreaTrigger AsAreaTrigger => this as AreaTrigger;
    public Conversation AsConversation => this as Conversation;
    public Corpse AsCorpse => this as Corpse;
    public Creature AsCreature => this as Creature;
    public DynamicObject AsDynamicObject => this as DynamicObject;
    public GameObject AsGameObject => this as GameObject;
    public Item AsItem => this as Item;
    public Player AsPlayer => this as Player;
    public SceneObject AsSceneObject => this as SceneObject;
    public Unit AsUnit => this as Unit;
    public BattleFieldManager BattleFieldManager { get; }

    public virtual Unit CharmerOrOwner
    {
        get
        {
            var unit = AsUnit;

            if (unit != null)
                return unit.CharmerOrOwner;

            var go = AsGameObject;

            return go?.OwnerUnit;
        }
    }

    public virtual ObjectGuid CharmerOrOwnerGUID => OwnerGUID;

    public ObjectGuid CharmerOrOwnerOrOwnGUID
    {
        get
        {
            var guid = CharmerOrOwnerGUID;

            return !guid.IsEmpty ? guid : GUID;
        }
    }

    public Unit CharmerOrOwnerOrSelf => CharmerOrOwner ?? AsUnit;
    public Player CharmerOrOwnerPlayerOrPlayerItself => CharmerOrOwnerGUID.IsPlayer ? ObjectAccessor.GetPlayer(this, CharmerOrOwnerGUID) : AsPlayer;
    public ClassFactory ClassFactory { get; }
    public CliDB CliDB { get; }
    public virtual float CombatReach => SharedConst.DefaultPlayerCombatReach;
    public ConditionManager ConditionManager { get; }
    public IConfiguration Configuration { get; }
    public DisableManager DisableManager { get; }

    public uint Entry
    {
        get => ObjectData.EntryId;
        set => SetUpdateFieldValue(Values.ModifyValue(ObjectData).ModifyValue(ObjectData.EntryId), value);
    }

    public EventSystem Events { get; set; } = new();
    public virtual uint Faction { get; set; }

    public float GridActivationRange
    {
        get
        {
            if (!IsActive)
                return AsCreature?.SightDistance ?? 0.0f;

            if (TypeId == TypeId.Player && AsPlayer.CinematicMgr.IsOnCinematic())
                return Math.Max(SharedConst.DefaultVisibilityInstance, Location.Map.VisibilityRange);

            return Location.Map.VisibilityRange;
        }
    }

    public ObjectGuid GUID { get; protected set; }
    public uint InstanceId { get; set; }
    public bool IsActive { get; protected set; }
    public bool IsAreaTrigger => ObjectTypeId == TypeId.AreaTrigger;
    public bool IsCorpse => ObjectTypeId == TypeId.Corpse;
    public bool IsCreature => ObjectTypeId == TypeId.Unit;
    public bool IsDestroyedObject { get; protected set; }
    public bool IsDynObject => ObjectTypeId == TypeId.DynamicObject;
    public bool IsGameObject => ObjectTypeId == TypeId.GameObject;

    public bool IsInWorldPvpZone
    {
        get
        {
            return Location.Zone switch
            {
                4197 => // Wintergrasp
                    true,
                5095 => // Tol Barad
                    true,
                6941 => // Ashran
                    true,
                _ => false
            };
        }
    }

    public bool IsItem => ObjectTypeId == TypeId.Item;
    public bool IsPermanentWorldObject { get; }
    public bool IsPlayer => ObjectTypeId == TypeId.Player;
    public bool IsPrivateObject => !_privateObjectOwner.IsEmpty;
    public bool IsUnit => IsTypeMask(TypeMask.Unit);
    public uint LastUsedScriptID { get; set; }
    public WorldLocation Location { get; set; }
    public virtual ushort MeleeAnimKitId => 0;
    public MMapManager MMapManager { get; }
    public virtual ushort MovementAnimKitId => 0;
    public MovementInfo MovementInfo { get; set; }
    public ObjectAccessor ObjectAccessor { get; }
    public ObjectFieldData ObjectData { get; set; }
    public GameObjectManager ObjectManager { get; }

    public virtual float ObjectScale
    {
        get => ObjectData.Scale;
        set => SetUpdateFieldValue(Values.ModifyValue(ObjectData).ModifyValue(ObjectData.Scale), value);
    }

    public TypeMask ObjectTypeMask { get; set; }
    public OutdoorPvPManager OutdoorPvPManager { get; }
    public virtual ObjectGuid OwnerGUID => default;
    public virtual Unit OwnerUnit => ObjectAccessor.GetUnit(this, OwnerGUID);

    public ObjectGuid PrivateObjectOwner
    {
        get => _privateObjectOwner;
        set => _privateObjectOwner = value;
    }

    public Scenario Scenario => !Location.IsInWorld ? null : Location.Map.ToInstanceMap?.InstanceScenario;
    public ScriptManager ScriptManager { get; }
    public SpellFactory SpellFactory { get; }
    public SpellManager SpellManager { get; }

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

                if (!creature.IsPet && !creature.IsTotem)
                    return null;

                var owner = creature.OwnerUnit;

                if (owner != null)
                    return owner.AsPlayer;
            }
            else if (IsGameObject)
            {
                return AsPlayer;
            }

            return null;
        }
    }

    public ITransport Transport { get; set; }
    public TypeId TypeId => ObjectTypeId;
    public UpdateFieldHolder Values { get; set; }
    public VariableStore VariableStorage { get; } = new();
    public WorldObjectVisibility Visibility { get; }
    public VMapManager VMapManager { get; }
    public WorldObjectCombat WorldObjectCombat { get; }
    public ZoneScript ZoneScript { get; set; }
    protected TypeId ObjectTypeId { get; set; }

    public virtual bool _IsWithinDist(WorldObject obj, float dist2Compare, bool is3D, bool incOwnRadius = true, bool incTargetRadius = true)
    {
        return Location._IsWithinDist(obj, dist2Compare, is3D, incOwnRadius, incTargetRadius);
    }

    public void AddDynamicUpdateFieldValue<T>(DynamicUpdateField<T> updateField, T value) where T : new()
    {
        AddToObjectUpdateIfNeeded();
        updateField.AddValue(value);
    }

    public void AddToNotify(NotifyFlags f)
    {
        _notifyFlags |= f;
    }

    public virtual bool AddToObjectUpdate()
    {
        Location.Map.AddUpdateObject(this);

        return true;
    }

    public void AddToObjectUpdateIfNeeded()
    {
        if (Location.IsInWorld && !_objectUpdated)
            _objectUpdated = AddToObjectUpdate();
    }

    public virtual void AddToWorld()
    {
        if (Location.IsInWorld)
            return;

        Location.IsInWorld = true;
        ClearUpdateMask(true);

        if (Location.Map == null)
            return;

        Location.Map.GetZoneAndAreaId(Location.PhaseShift, out var zoneid, out var areaid, Location.X, Location.Y, Location.Z);
        Location.Zone = zoneid;
        Location.Area = areaid;
    }

    public void ApplyModUpdateFieldValue<T>(IUpdateField<T> updateField, T mod, bool apply) where T : new()
    {
        dynamic value = updateField.Value;

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
        dynamic value = updateField.Value;

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

    public virtual void BuildCreateUpdateBlockForPlayer(UpdateData data, Player target)
    {
        if (target == null)
            return;

        var updateType = _isNewObject ? UpdateType.CreateObject2 : UpdateType.CreateObject;
        var tempObjectType = ObjectTypeId;
        var flags = UpdateFlag;

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

        if (Visibility.GetSmoothPhasing()?.GetInfoForSeer(target.GUID) != null)
            flags.SmoothPhasing = true;

        var unit = AsUnit;

        if (unit != null)
        {
            flags.PlayHoverAnim = unit.IsPlayingHoverAnim;

            if (unit.Victim != null)
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

    public void BuildDestroyUpdateBlock(UpdateData data)
    {
        data.AddDestroyObject(GUID);
    }

    public void BuildFieldsUpdate(Player player, Dictionary<Player, UpdateData> dataMap)
    {
        if (!dataMap.ContainsKey(player))
            dataMap.Add(player, new UpdateData(player.Location.MapId));

        BuildValuesUpdateBlockForPlayer(dataMap[player], player);
    }

    public void BuildMovementUpdate(WorldPacket data, CreateObjectBits flags, Player target)
    {
        List<uint> pauseTimes = null;
        var go = AsGameObject;

        if (go != null)
            pauseTimes = go.GetPauseTimes();

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
            var hasFallDirection = unit.HasUnitMovementFlag(MovementFlag.Falling);
            var hasFall = hasFallDirection || unit.MovementInfo.Jump.FallTime != 0;
            var hasSpline = unit.IsSplineEnabled;
            var hasInertia = unit.MovementInfo.Inertia.HasValue;
            var hasAdvFlying = unit.MovementInfo.AdvFlying.HasValue;

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
            data.WriteBit(hasFall);                                   // HasFall
            data.WriteBit(hasSpline);                                 // HasSpline - marks that the unit uses spline movement
            data.WriteBit(false);                                     // HeightChangeFailed
            data.WriteBit(false);                                     // RemoteTimeValid
            data.WriteBit(hasInertia);                                // HasInertia

            if (!unit.MovementInfo.Transport.Guid.IsEmpty)
                MovementExtensions.WriteTransportInfo(data, unit.MovementInfo.Transport);

            if (hasInertia)
            {
                data.WriteInt32(unit.MovementInfo.Inertia.Value.Id);
                data.WriteXYZ(unit.MovementInfo.Inertia.Value.Force);
                data.WriteUInt32(unit.MovementInfo.Inertia.Value.Lifetime);
            }

            if (hasAdvFlying)
            {
                data.WriteFloat(unit.MovementInfo.AdvFlying.Value.ForwardVelocity);
                data.WriteFloat(unit.MovementInfo.AdvFlying.Value.UpVelocity);
            }

            if (hasFall)
            {
                data.WriteUInt32(unit.MovementInfo.Jump.FallTime); // Time
                data.WriteFloat(unit.MovementInfo.Jump.Zspeed);    // JumpVelocity

                if (data.WriteBit(hasFallDirection))
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

            data.WriteBit(hasSpline);
            data.FlushBits();

            if (movementForces != null)
                foreach (var force in movementForces.GetForces())
                    MovementExtensions.WriteMovementForceWithDirection(force, data, unit.Location);

            // HasMovementSpline - marks that spline data is present in packet
            if (hasSpline)
                MovementExtensions.WriteCreateObjectSplineDataBlock(unit.MoveSpline, data);
        }

        data.WriteInt32(pauseTimes?.Count ?? 0);

        if (flags.Stationary)
        {
            var self = this;
            data.WriteFloat(self.Location.X);
            data.WriteFloat(self.Location.Y);
            data.WriteFloat(self.Location.Z);
            data.WriteFloat(self.Location.Orientation);
        }

        if (flags.CombatVictim)
            data.WritePackedGuid(AsUnit.Victim.GUID); // CombatVictim

        if (flags.ServerTime)
            data.WriteUInt32(GameTime.CurrentTimeMS);

        if (flags.Vehicle)
        {
            var unit = AsUnit;
            data.WriteUInt32(unit.VehicleKit.GetVehicleInfo().Id); // RecID
            data.WriteFloat(unit.Location.Orientation);            // InitialRawFacing
        }

        if (flags.AnimKit)
        {
            data.WriteUInt16(AIAnimKitId);       // AiID
            data.WriteUInt16(MovementAnimKitId); // MovementID
            data.WriteUInt16(MeleeAnimKitId);    // MeleeID
        }

        if (flags.Rotation)
            data.WriteInt64(AsGameObject.PackedLocalRotation); // Rotation

        if (pauseTimes != null && !pauseTimes.Empty())
            foreach (var stopFrame in pauseTimes)
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
            var hasAreaTriggerSphere = shape.TriggerType == AreaTriggerTypes.Sphere;
            var hasAreaTriggerBox = shape.TriggerType == AreaTriggerTypes.Box;
            var hasAreaTriggerPolygon = createProperties != null && shape.TriggerType == AreaTriggerTypes.Polygon;
            var hasAreaTriggerCylinder = shape.TriggerType == AreaTriggerTypes.Cylinder;
            var hasDisk = shape.TriggerType == AreaTriggerTypes.Disk;
            var hasBoundedPlane = shape.TriggerType == AreaTriggerTypes.BoundedPlane;
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
            uint int1 = 0;

            var gameObject = AsGameObject;

            data.WriteUInt32(gameObject.WorldEffectID);

            data.WriteBit(bit8);
            data.FlushBits();

            if (bit8)
                data.WriteUInt32(int1);
        }

        if (flags.SmoothPhasing)
        {
            var smoothPhasingInfo = Visibility.GetSmoothPhasing().GetInfoForSeer(target.GUID);

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
        }

        if (flags.ActivePlayer)
        {
            var player = AsPlayer;

            var hasSceneInstanceIDs = !player.SceneMgr.GetSceneTemplateByInstanceMap().Empty();
            var hasRuneState = AsUnit.GetPowerIndex(PowerType.Runes) != (int)PowerType.Max;

            data.WriteBit(hasSceneInstanceIDs);
            data.WriteBit(hasRuneState);
            data.FlushBits();

            if (hasSceneInstanceIDs)
            {
                data.WriteInt32(player.SceneMgr.GetSceneTemplateByInstanceMap().Count);

                foreach (var pair in player.SceneMgr.GetSceneTemplateByInstanceMap())
                    data.WriteUInt32(pair.Key);
            }

            if (hasRuneState)
            {
                float baseCd = player.GetRuneBaseCooldown();
                var maxRunes = (uint)player.GetMaxPower(PowerType.Runes);

                data.WriteUInt8((byte)((1 << (int)maxRunes) - 1u));
                data.WriteUInt8(player.GetRunesState());
                data.WriteUInt32(maxRunes);

                for (byte i = 0; i < maxRunes; ++i)
                    data.WriteUInt8((byte)((baseCd - player.GetRuneCooldown(i)) / baseCd * 255));
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

    public void BuildOutOfRangeUpdateBlock(UpdateData data)
    {
        data.AddOutOfRangeGUID(GUID);
    }

    public virtual void BuildUpdate(Dictionary<Player, UpdateData> data)
    {
        var notifier = new WorldObjectChangeAccumulator(this, data, GridType.World);
        Cell.VisitGrid(this, notifier, Visibility.VisibilityRange);

        ClearUpdateMask(false);
    }

    public abstract void BuildValuesCreate(WorldPacket data, Player target);

    public abstract void BuildValuesUpdate(WorldPacket data, Player target);

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

    public virtual void BuildValuesUpdateWithFlag(WorldPacket data, UpdateFieldFlag flags, Player target)
    {
        data.WriteUInt32(0);
        data.WriteUInt32(0);
    }

    public virtual bool CanAlwaysSee(WorldObject obj)
    {
        return Visibility.CanAlwaysSee(obj);
    }

    public virtual bool CanNeverSee(WorldObject obj)
    {
        return Visibility.CanNeverSee(obj);
    }

    public void CheckAddToMap()
    {
        if (IsWorldObject())
            Location.Map.AddWorldObject(this);
    }

    public virtual void CleanupsBeforeDelete(bool finalCleanup = true)
    {
        if (Location.IsInWorld)
            RemoveFromWorld();

        var transport = Transport;

        transport?.RemovePassenger(this);

        Events.KillAllEvents(false); // non-delatable (currently cast spells) will not deleted now but it will deleted at call in Map::RemoveAllObjectsInRemoveList
    }

    public void ClearDynamicUpdateFieldValues<T>(DynamicUpdateField<T> updateField) where T : new()
    {
        AddToObjectUpdateIfNeeded();
        updateField.Clear();
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

    public void Create(ObjectGuid guid)
    {
        _objectUpdated = false;
        GUID = guid;
    }

    public void DestroyForNearbyPlayers()
    {
        if (!Location.IsInWorld)
            return;

        List<Unit> targets = new();
        var check = new AnyPlayerInObjectRangeCheck(this, Visibility.VisibilityRange, false);
        var searcher = new PlayerListSearcher(this, targets, check);

        Cell.VisitGrid(this, searcher, Visibility.VisibilityRange);

        foreach (var unit in targets)
        {
            var player = unit as Player;

            if (player == this || player == null)
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

    public virtual void DestroyForPlayer(Player target)
    {
        UpdateData updateData = new(target.Location.MapId);
        BuildDestroyUpdateBlock(updateData);
        updateData.BuildPacket(out var packet);
        target.SendPacket(packet);
    }

    public virtual void Dispose()
    {
        // this may happen because there are many !create/delete
        if (IsWorldObject() && Location.Map != null)
        {
            if (IsTypeId(TypeId.Corpse))
                Log.Logger.Fatal("WorldObject.Dispose() Corpse Type: {0} ({1}) deleted but still in map!!", AsCorpse.GetCorpseType(), GUID.ToString());
            else
                Location.ResetMap();
        }

        if (Location.IsInWorld)
        {
            Log.Logger.Fatal("WorldObject.Dispose() {0} deleted but still in world!!", GUID.ToString());

            if (IsTypeMask(TypeMask.Item))
                Log.Logger.Fatal("Item slot {0}", ((Item)this).Slot);
        }

        if (_objectUpdated)
            Log.Logger.Fatal("WorldObject.Dispose() {0} deleted but still in update list!!", GUID.ToString());
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

    public void ForceUpdateFieldChange()
    {
        AddToObjectUpdateIfNeeded();
    }

    public virtual uint GetCastSpellXSpellVisualId(SpellInfo spellInfo)
    {
        return WorldObjectCombat.GetCastSpellXSpellVisualId(spellInfo);
    }

    public virtual string GetDebugInfo()
    {
        return $"{Location.GetDebugInfo()}\n{GUID} Entry: {Entry}\nName: {GetName()}";
    }

    public virtual uint GetLevelForTarget(WorldObject target)
    {
        return 1;
    }

    public virtual LootManagement.Loot GetLootForPlayer(Player player)
    {
        return null;
    }

    public virtual string GetName(Locale locale = Locale.enUS)
    {
        return _name;
    }

    public virtual ObjectGuid GetTransGUID()
    {
        if (Transport != null)
            return Transport.GetTransportGUID();

        return ObjectGuid.Empty;
    }

    public T GetTransport<T>() where T : class, ITransport
    {
        return Transport as T;
    }

    public virtual UpdateFieldFlag GetUpdateFieldFlagsFor(Player target)
    {
        return UpdateFieldFlag.None;
    }

    public virtual bool HasInvolvedQuest(uint questId)
    {
        return false;
    }

    public virtual bool HasQuest(uint questId)
    {
        return false;
    }

    public void InsertDynamicUpdateFieldValue<T>(DynamicUpdateField<T> updateField, int index, T value) where T : new()
    {
        AddToObjectUpdateIfNeeded();
        updateField.InsertValue(index, value);
    }

    public virtual bool IsAlwaysDetectableFor(WorldObject seer)
    {
        return Visibility.IsAlwaysDetectableFor(seer);
    }

    public virtual bool IsAlwaysVisibleFor(WorldObject seer)
    {
        return Visibility.IsAlwaysVisibleFor(seer);
    }

    public virtual bool IsInvisibleDueToDespawn(WorldObject seer)
    {
        return Visibility.IsInvisibleDueToDespawn(seer);
    }

    public bool IsNeedNotify(NotifyFlags f)
    {
        return Convert.ToBoolean(_notifyFlags & f);
    }

    public virtual bool IsNeverVisibleFor(WorldObject seer)
    {
        return Visibility.IsNeverVisibleFor(seer);
    }

    public bool IsTypeId(TypeId typeId)
    {
        return TypeId == typeId;
    }

    public bool IsTypeMask(TypeMask mask)
    {
        return Convert.ToBoolean(mask & ObjectTypeMask);
    }

    public bool IsWorldObject()
    {
        if (IsPermanentWorldObject)
            return true;

        if (IsTypeId(TypeId.Unit) && AsCreature.IsTempWorldObject)
            return true;

        return false;
    }

    public virtual bool LoadFromDB(ulong spawnId, Map map, bool addToMap, bool allowDuplicate)
    {
        return true;
    }

    //Position
    public void MovePosition(Position pos, float dist, float angle)
    {
        angle += Location.Orientation;
        var destx = pos.X + dist * (float)Math.Cos(angle);
        var desty = pos.Y + dist * (float)Math.Sin(angle);

        // Prevent invalid coordinates here, position is unchanged
        if (!GridDefines.IsValidMapCoord(destx, desty, pos.Z))
        {
            Log.Logger.Error("WorldObject.MovePosition invalid coordinates X: {0} and Y: {1} were passed!", destx, desty);

            return;
        }

        var ground = Location.GetMapHeight(destx, desty, MapConst.MaxHeight);
        var floor = Location.GetMapHeight(destx, desty, pos.Z);
        var destz = Math.Abs(ground - pos.Z) <= Math.Abs(floor - pos.Z) ? ground : floor;

        var step = dist / 10.0f;

        for (byte j = 0; j < 10; ++j)
            // do not allow too big z changes
            if (Math.Abs(pos.Z - destz) > 6)
            {
                destx -= step * (float)Math.Cos(angle);
                desty -= step * (float)Math.Sin(angle);
                ground = Location.Map.GetHeight(Location.PhaseShift, destx, desty, MapConst.MaxHeight);
                floor = Location.Map.GetHeight(Location.PhaseShift, destx, desty, pos.Z);
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
        pos.Z = Location.UpdateGroundPositionZ(pos.X, pos.Y, pos.Z);
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
            Log.Logger.Error("WorldObject.MovePositionToFirstCollision invalid coordinates X: {0} and Y: {1} were passed!", destx, desty);

            return;
        }

        // Use a detour raycast to get our first collision point
        PathGenerator path = new(this);
        path.SetUseRaycast(true);
        path.CalculatePath(new Position(destx, desty, destz));

        // We have a invalid path result. Skip further processing.
        if (!path.GetPathType().HasFlag(PathType.NotUsingPath))
            if ((path.GetPathType() & ~(PathType.Normal | PathType.Shortcut | PathType.Incomplete | PathType.FarFromPoly)) != 0)
                return;

        var result = path.GetPath()[path.GetPath().Length - 1];
        destx = result.X;
        desty = result.Y;
        destz = result.Z;

        // check static LOS
        var halfHeight = Location.CollisionHeight * 0.5f;
        bool col;

        // Unit is flying, check for potential collision via vmaps
        if (path.GetPathType().HasFlag(PathType.NotUsingPath))
        {
            col = VMapManager.GetObjectHitPos(PhasingHandler.GetTerrainMapId(Location.PhaseShift, Location.MapId, Location.Map.Terrain, pos.X, pos.Y),
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
            }
        }

        // check dynamic collision
        col = Location.Map.GetObjectHitPos(Location.PhaseShift, pos.X, pos.Y, pos.Z + halfHeight, destx, desty, destz + halfHeight, out destx, out desty, out destz, -0.5f);

        destz -= halfHeight;

        // Collided with a gameobject, move back to collision point
        if (col)
        {
            destx -= SharedConst.ContactDistance * (float)Math.Cos(angle);
            desty -= SharedConst.ContactDistance * (float)Math.Sin(angle);
        }

        var groundZ = MapConst.VMAPInvalidHeightValue;
        pos.X = GridDefines.NormalizeMapCoord(pos.X);
        pos.Y = GridDefines.NormalizeMapCoord(pos.Y);
        destz = Location.UpdateAllowedPositionZ(destx, desty, destz, ref groundZ);

        pos.Orientation = Location.Orientation;
        pos.Relocate(destx, desty, destz);

        // position has no ground under it (or is too far away)
        if (!(groundZ <= MapConst.InvalidHeight))
            return;

        if (!TryGetAsUnit(out var unit))
            return;

        // unit can fly, ignore.
        if (unit.CanFly)
            return;

        // fall back to gridHeight if any
        var gridHeight = Location.Map.GetGridHeight(Location.PhaseShift, pos.X, pos.Y);

        if (gridHeight > MapConst.InvalidHeight)
            pos.Z = gridHeight + unit.HoverOffset;
    }

    public void PlayDirectMusic(uint musicId, Player target = null)
    {
        if (target != null)
            target.SendPacket(new PlayMusic(musicId));
        else
            SendMessageToSet(new PlayMusic(musicId), true);
    }

    public void PlayDirectSound(uint soundId, Player target = null, uint broadcastTextId = 0)
    {
        PlaySound sound = new(GUID, soundId, broadcastTextId);

        if (target != null)
            target.SendPacket(sound);
        else
            SendMessageToSet(sound, true);
    }

    public void PlayDistanceSound(uint soundId, Player target = null)
    {
        PlaySpeakerBoxSound playSpeakerBoxSound = new(GUID, soundId);

        if (target != null)
            target.SendPacket(playSpeakerBoxSound);
        else
            SendMessageToSet(playSpeakerBoxSound, true);
    }

    public void RemoveDynamicUpdateFieldValue<T>(DynamicUpdateField<T> updateField, int index) where T : new()
    {
        AddToObjectUpdateIfNeeded();
        updateField.RemoveValue(index);
    }

    public virtual void RemoveFromObjectUpdate()
    {
        Location.Map.RemoveUpdateObject(this);
    }

    public virtual void RemoveFromWorld()
    {
        if (!Location.IsInWorld)
            return;

        if (!ObjectTypeMask.HasAnyFlag(TypeMask.Item | TypeMask.Container))
            UpdateObjectVisibilityOnDestroy();

        Location.IsInWorld = false;
        ClearUpdateMask(true);
    }

    public void RemoveUpdateFieldFlagValue<T>(IUpdateField<T> updateField, T flag)
    {
        //static_assert(std::is_integral < T >::value, "SetUpdateFieldFlagValue must be used with integral types");
        SetUpdateFieldValue(updateField, (T)(updateField.Value & ~(dynamic)flag));
    }

    public void RemoveUpdateFieldFlagValue<T>(ref T value, T flag) where T : new()
    {
        //static_assert(std::is_integral < T >::value, "RemoveUpdateFieldFlagValue must be used with integral types");
        SetUpdateFieldValue(ref value, (T)(value & ~(dynamic)flag));
    }

    public void ResetAllNotifies()
    {
        _notifyFlags = 0;
    }

    public void SendCombatLogMessage(CombatLogServerPacket combatLog)
    {
        CombatLogSender combatLogSender = new(combatLog);

        var self = AsPlayer;

        if (self != null)
            combatLogSender.Invoke(self);

        MessageDistDeliverer<CombatLogSender> notifier = new(this, combatLogSender, Visibility.VisibilityRange);
        Cell.VisitGrid(this, notifier, Visibility.VisibilityRange);
    }

    public virtual void SendMessageToSet(ServerPacket packet, bool self)
    {
        if (Location.IsInWorld)
            SendMessageToSetInRange(packet, Visibility.VisibilityRange, self);
    }

    public virtual void SendMessageToSet(ServerPacket data, Player skip)
    {
        PacketSenderRef sender = new(data);
        var notifier = new MessageDistDeliverer<PacketSenderRef>(this, sender, Visibility.VisibilityRange, false, skip);
        Cell.VisitGrid(this, notifier, Visibility.VisibilityRange);
    }

    public virtual void SendMessageToSetInRange(ServerPacket data, float dist, bool self)
    {
        PacketSenderRef sender = new(data);
        MessageDistDeliverer<PacketSenderRef> notifier = new(this, sender, dist);
        Cell.VisitGrid(this, notifier, dist);
    }

    public void SendOutOfRangeForPlayer(Player target)
    {
        UpdateData updateData = new(target.Location.MapId);
        BuildOutOfRangeUpdateBlock(updateData);
        updateData.BuildPacket(out var packet);
        target.SendPacket(packet);
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

    public void SetActive(bool on)
    {
        if (IsActive == on)
            return;

        if (IsTypeId(TypeId.Player))
            return;

        IsActive = on;

        if (on && !Location.IsInWorld)
            return;

        var map = Location.Map;

        if (map == null)
            return;

        if (on)
            map.AddToActive(this);
        else
            map.RemoveFromActive(this);
    }

    public void SetDestroyedObject(bool destroyed)
    {
        IsDestroyedObject = destroyed;
    }

    public void SetIsNewObject(bool enable)
    {
        _isNewObject = enable;
    }

    public void SetName(string name)
    {
        _name = name;
    }

    public void SetUpdateFieldFlagValue<T>(IUpdateField<T> updateField, T flag) where T : new()
    {
        //static_assert(std::is_integral < T >::value, "SetUpdateFieldFlagValue must be used with integral types");
        SetUpdateFieldValue(updateField, (T)(updateField.Value | (dynamic)flag));
    }

    public void SetUpdateFieldFlagValue<T>(ref T value, T flag) where T : new()
    {
        //static_assert(std::is_integral < T >::value, "SetUpdateFieldFlagValue must be used with integral types");
        SetUpdateFieldValue(ref value, (T)(value | (dynamic)flag));
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

    public void SetUpdateFieldValue<T>(IUpdateField<T> updateField, T newValue)
    {
        if (!newValue.Equals(updateField.Value))
        {
            updateField.Value = newValue;
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

    public void SetWorldObject(bool on)
    {
        if (!Location.IsInWorld)
            return;

        Location.Map.AddObjectToSwitchList(this, on);
    }

    public TempSummon SummonCreature(uint entry, float x, float y, float z, float o = 0, TempSummonType despawnType = TempSummonType.ManualDespawn, TimeSpan despawnTime = default, uint vehId = 0, uint spellId = 0, ObjectGuid privateObjectOwner = default)
    {
        return SummonCreature(entry, new Position(x, y, z, o), despawnType, despawnTime, vehId, spellId, privateObjectOwner);
    }

    public TempSummon SummonCreature(uint entry, Position pos, TempSummonType despawnType = TempSummonType.ManualDespawn, TimeSpan despawnTime = default, uint vehId = 0, uint spellId = 0, ObjectGuid privateObjectOwner = default)
    {
        if (pos.IsDefault)
            Location.GetClosePoint(pos, CombatReach);

        if (pos.Orientation == 0.0f)
            pos.Orientation = Location.Orientation;

        var map = Location.Map;

        var summon = map?.SummonCreature(entry, pos, null, (uint)despawnTime.TotalMilliseconds, this, spellId, vehId, privateObjectOwner);

        if (summon == null)
            return null;

        summon.SetTempSummonType(despawnType);

        return summon;
    }

    public void SummonCreatureGroup(byte group)
    {
        SummonCreatureGroup(group, out _);
    }

    public void SummonCreatureGroup(byte group, out List<TempSummon> list)
    {
        var data = ObjectManager.GetSummonGroup(Entry, IsTypeId(TypeId.GameObject) ? SummonerType.GameObject : SummonerType.Creature, group);

        if (data.Empty())
        {
            Log.Logger.Warning("{0} ({1}) tried to summon non-existing summon group {2}.", GetName(), GUID.ToString(), group);
            list = new List<TempSummon>();

            return;
        }

        list = data.Select(tempSummonData => SummonCreature(tempSummonData.entry, tempSummonData.pos, tempSummonData.type, TimeSpan.FromMilliseconds(tempSummonData.time))).Where(summon => summon != null).ToList();
    }

    public GameObject SummonGameObject(uint entry, float x, float y, float z, float ang, Quaternion rotation, TimeSpan respawnTime, GameObjectSummonType summonType = GameObjectSummonType.TimedOrCorpseDespawn)
    {
        return SummonGameObject(entry, new Position(x, y, z, ang), rotation, respawnTime, summonType);
    }

    public GameObject SummonGameObject(uint entry, Position pos, Quaternion rotation, TimeSpan respawnTime, GameObjectSummonType summonType = GameObjectSummonType.TimedOrCorpseDespawn)
    {
        if (pos.IsDefault)
        {
            Location.GetClosePoint(pos, CombatReach);
            pos.Orientation = Location.Orientation;
        }

        if (!Location.IsInWorld)
            return null;

        var goinfo = ObjectManager.GetGameObjectTemplate(entry);

        if (goinfo == null)
        {
            Log.Logger.Error("Gameobject template {0} not found in database!", entry);

            return null;
        }

        var map = Location.Map;
        var go = GameObjectFactory.CreateGameObject(entry, map, pos, rotation, 255, GameObjectState.Ready);

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

    public TempSummon SummonPersonalClone(Position pos, TempSummonType despawnType, TimeSpan despawnTime, uint vehId, uint spellId, Player privateObjectOwner)
    {
        var map = Location.Map;

        var summon = map?.SummonCreature(Entry, pos, null, (uint)despawnTime.TotalMilliseconds, privateObjectOwner, spellId, vehId, privateObjectOwner.GUID, new SmoothPhasingInfo(GUID, true, true));

        if (summon == null)
            return null;

        summon.SetTempSummonType(despawnType);

        return summon;
    }

    public Creature SummonTrigger(Position pos, TimeSpan despawnTime, CreatureAI ai = null)
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

        if (ai != null)
            summon.InitializeAI(new CreatureAI(summon));

        return summon;
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

    public bool TryGetAsCorpse(out Corpse corpse)
    {
        corpse = AsCorpse;

        return corpse != null;
    }

    public bool TryGetAsCreature(out Creature creature)
    {
        creature = AsCreature;

        return creature != null;
    }

    public bool TryGetAsDynamicObject(out DynamicObject dynObj)
    {
        dynObj = AsDynamicObject;

        return dynObj != null;
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

    public bool TryGetAsPlayer(out Player player)
    {
        player = AsPlayer;

        return player != null;
    }

    public bool TryGetAsSceneObject(out SceneObject sceneObject)
    {
        sceneObject = AsSceneObject;

        return sceneObject != null;
    }

    public bool TryGetAsUnit(out Unit unit)
    {
        unit = AsUnit;

        return unit != null;
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

    public virtual void Update(uint diff)
    {
        Events.Update(diff);
    }

    public virtual void UpdateObjectVisibility(bool force = true)
    {
        //updates object's visibility for nearby players
        var notifier = new VisibleChangesNotifier(new[]
                                                  {
                                                      this
                                                  },
                                                  GridType.World);

        Cell.VisitGrid(this, notifier, Visibility.VisibilityRange);
    }

    public virtual void UpdateObjectVisibilityOnCreate()
    {
        UpdateObjectVisibility();
    }

    public virtual void UpdateObjectVisibilityOnDestroy()
    {
        DestroyForNearbyPlayers();
    }
}