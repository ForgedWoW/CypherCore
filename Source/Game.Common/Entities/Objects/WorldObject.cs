// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Numerics;
using Blizzard.Telemetry.Wow;
using Framework.Constants;
using Framework.Dynamic;
using Framework.Util;
using Game.Common.DataStorage.Structs.F;
using Game.Common.Entities.AreaTriggers;
using Game.Common.Entities.Creatures;
using Game.Common.Entities.GameObjects;
using Game.Common.Entities.Items;
using Game.Common.Entities.Objects.Update;
using Game.Common.Entities.Players;
using Game.Common.Entities.Units;
using Game.Common.Maps.Grids;
using Game.Common.Networking;
using Game.Common.Networking.Packets.CombatLog;
using Game.Common.Networking.Packets.Misc;
using Game.Common.Networking.Packets.Movement;
using Game.Common.Networking.Packets.Spell;
using Game.Common.Server;

namespace Game.Common.Entities.Objects;

public abstract class WorldObject : IDisposable
{
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
    public uint InstanceId { get; set; }
	public bool IsInWorld { get; set; }

	public FlaggedArray32<StealthType> Stealth { get; set; } = new(2);
	public FlaggedArray32<StealthType> StealthDetect { get; set; } = new(2);

	public FlaggedArray64<InvisibilityType> Invisibility { get; set; } = new((int)InvisibilityType.Max);
	public FlaggedArray64<InvisibilityType> InvisibilityDetect { get; set; } = new((int)InvisibilityType.Max);

	public FlaggedArray32<ServerSideVisibilityType> ServerSideVisibility { get; set; } = new(2);
	public FlaggedArray32<ServerSideVisibilityType> ServerSideVisibilityDetect { get; set; } = new(2);

	public WorldLocation Location { get; set; }

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

    public virtual float CombatReach => SharedConst.DefaultPlayerCombatReach;


	// Watcher
	public bool IsPrivateObject => !_privateObjectOwner.IsEmpty;

	public ObjectGuid PrivateObjectOwner
	{
		get => _privateObjectOwner;
		set => _privateObjectOwner = value;
	}

	public ObjectGuid GUID => _guid;

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

	public bool IsActiveObject => m_isActive;

	public bool IsPermanentWorldObject => _isWorldObject;

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

	public virtual ObjectGuid OwnerGUID => default;

	public virtual ObjectGuid CharmerOrOwnerGUID => OwnerGUID;

	public virtual uint Faction { get; set; }

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
	

	public Player AsPlayer => this as Player;

	public GameObject AsGameObject => this as GameObject;
	

	public Unit AsUnit => this as Unit;

	public Corpse AsCorpse => this as Corpse;

	public DynamicObject AsDynamicObject => this as DynamicObject;

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

        if (IsInWorld)
		{
			Log.outFatal(LogFilter.Misc, "WorldObject.Dispose() {0} deleted but still in world!!", GUID.ToString());
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

    public virtual void Update(uint diff)
	{
		Events.Update(diff);
	}

    public virtual void CleanupsBeforeDelete(bool finalCleanup = true)
	{
		Events.KillAllEvents(false); // non-delatable (currently cast spells) will not deleted now but it will deleted at call in Map::RemoveAllObjectsInRemoveList
	}

	public void GetZoneAndAreaId(out uint zoneid, out uint areaid)
	{
		zoneid = _zoneId;
		areaid = _areaId;
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

    public bool TryGetAsUnit(out Unit unit)
	{
		unit = AsUnit;

		return unit != null;
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



    //Position

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


    public static implicit operator bool(WorldObject obj)
    {
        return obj != null;
    }

}
