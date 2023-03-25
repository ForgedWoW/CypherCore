// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Forged.RealmServer.Networking;
using Forged.RealmServer.Scripting.Interfaces.IDynamicObject;
using Forged.RealmServer.Spells;

namespace Forged.RealmServer.Entities;

public class DynamicObject : WorldObject
{
	readonly DynamicObjectData _dynamicObjectData;
	Aura _aura;
	Aura _removedAura;
	Unit _caster;
	int _duration; // for non-aura dynobjects
	bool _isViewpoint;

	public override uint Faction
	{
		get
		{
			return _caster.Faction;
		}
	}

	public override ObjectGuid OwnerGUID => GetCasterGUID();

	public DynamicObject(bool isWorldObject) : base(isWorldObject)
	{
		ObjectTypeMask |= TypeMask.DynamicObject;
		ObjectTypeId = TypeId.DynamicObject;

		_updateFlag.Stationary = true;

		_dynamicObjectData = new DynamicObjectData();
	}

	public override void Dispose()
	{
		// make sure all references were properly removed
		_removedAura = null;

		base.Dispose();
	}

	public override void AddToWorld()
	{
		// Register the dynamicObject for guid lookup and for caster
		if (!IsInWorld)
		{
			Map.ObjectsStore.TryAdd(GUID, this);
			base.AddToWorld();
			BindToCaster();
		}
	}

	public override void RemoveFromWorld()
	{
		// Remove the dynamicObject from the accessor and from all lists of objects in world
		if (IsInWorld)
		{
			if (_isViewpoint)
				RemoveCasterViewpoint();

			if (_aura != null)
				RemoveAura();

			// dynobj could get removed in Aura.RemoveAura
			if (!IsInWorld)
				return;

			UnbindFromCaster();
			base.RemoveFromWorld();
			Map.ObjectsStore.TryRemove(GUID, out _);
		}
	}

	public bool CreateDynamicObject(ulong guidlow, Unit caster, SpellInfo spell, Position pos, float radius, DynamicObjectType type, SpellCastVisualField spellVisual)
	{
		Map = caster.Map;
		Location.Relocate(pos);

		if (!Location.IsPositionValid)
		{
			Log.outError(LogFilter.Server, "DynamicObject (spell {0}) not created. Suggested coordinates isn't valid (X: {1} Y: {2})", spell.Id, Location.X, Location.Y);

			return false;
		}

		Create(ObjectGuid.Create(HighGuid.DynamicObject, Location.MapId, spell.Id, guidlow));
		PhasingHandler.InheritPhaseShift(this, caster);

		UpdatePositionData();
		SetZoneScript();

		Entry = spell.Id;
		ObjectScale = 1f;

		SetUpdateFieldValue(Values.ModifyValue(_dynamicObjectData).ModifyValue(_dynamicObjectData.Caster), caster.GUID);
		SetUpdateFieldValue(Values.ModifyValue(_dynamicObjectData).ModifyValue(_dynamicObjectData.Type), (byte)type);

		SpellCastVisualField spellCastVisual = Values.ModifyValue(_dynamicObjectData).ModifyValue(_dynamicObjectData.SpellVisual);
		SetUpdateFieldValue(ref spellCastVisual.SpellXSpellVisualID, spellVisual.SpellXSpellVisualID);
		SetUpdateFieldValue(ref spellCastVisual.ScriptVisualID, spellVisual.ScriptVisualID);

		SetUpdateFieldValue(Values.ModifyValue(_dynamicObjectData).ModifyValue(_dynamicObjectData.SpellID), spell.Id);
		SetUpdateFieldValue(Values.ModifyValue(_dynamicObjectData).ModifyValue(_dynamicObjectData.Radius), radius);
		SetUpdateFieldValue(Values.ModifyValue(_dynamicObjectData).ModifyValue(_dynamicObjectData.CastTime), _gameTime.GetGameTimeMS);

		if (IsWorldObject())
			SetActive(true); //must before add to map to be put in world container

		var transport = caster.Transport;

		if (transport != null)
		{
			var newPos = pos.Copy();
			transport.CalculatePassengerOffset(newPos);
			MovementInfo.Transport.Pos.Relocate(newPos);

			// This object must be added to transport before adding to map for the client to properly display it
			transport.AddPassenger(this);
		}

		if (!Map.AddToMap(this))
		{
			// Returning false will cause the object to be deleted - remove from transport
			if (transport != null)
				transport.RemovePassenger(this);

			return false;
		}

		return true;
	}

	public override void Update(uint diff)
	{
		// caster has to be always available and in the same map
		var expired = false;

		if (_aura != null)
		{
			if (!_aura.IsRemoved)
				_aura.UpdateOwner(diff, this);

			// _aura may be set to null in Aura.UpdateOwner call
			if (_aura != null && (_aura.IsRemoved || _aura.IsExpired))
				expired = true;
		}
		else
		{
			if (GetDuration() > diff)
				_duration -= (int)diff;
			else
				expired = true;
		}

		if (expired)
			Remove();
		else
			Global.ScriptMgr.ForEach<IDynamicObjectOnUpdate>(p => p.OnUpdate(this, diff));
	}

	public void Remove()
	{
		if (IsInWorld)
			AddObjectToRemoveList();
	}

	public void SetDuration(int newDuration)
	{
		if (_aura == null)
			_duration = newDuration;
		else
			_aura.SetDuration(newDuration);
	}

	public void Delay(int delaytime)
	{
		SetDuration(GetDuration() - delaytime);
	}

	public void SetAura(Aura aura)
	{
		_aura = aura;
	}

	public void SetCasterViewpoint()
	{
		var caster = _caster.AsPlayer;

		if (caster != null)
		{
			caster.SetViewpoint(this, true);
			_isViewpoint = true;
		}
	}

	public SpellInfo GetSpellInfo()
	{
		return Global.SpellMgr.GetSpellInfo(GetSpellId(), Map.DifficultyID);
	}

	public override void BuildValuesCreate(WorldPacket data, Player target)
	{
		var flags = GetUpdateFieldFlagsFor(target);
		WorldPacket buffer = new();

		buffer.WriteUInt8((byte)flags);
		ObjectData.WriteCreate(buffer, flags, this, target);
		_dynamicObjectData.WriteCreate(buffer, flags, this, target);

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

		if (Values.HasChanged(TypeId.DynamicObject))
			_dynamicObjectData.WriteUpdate(buffer, flags, this, target);

		data.WriteUInt32(buffer.GetSize());
		data.WriteBytes(buffer);
	}

	public override void ClearUpdateMask(bool remove)
	{
		Values.ClearChangesMask(_dynamicObjectData);
		base.ClearUpdateMask(remove);
	}

	public Unit GetCaster()
	{
		return _caster;
	}

	public uint GetSpellId()
	{
		return _dynamicObjectData.SpellID;
	}

	public ObjectGuid GetCasterGUID()
	{
		return _dynamicObjectData.Caster;
	}

	public float GetRadius()
	{
		return _dynamicObjectData.Radius;
	}

	int GetDuration()
	{
		if (_aura == null)
			return _duration;
		else
			return _aura.Duration;
	}

	void RemoveAura()
	{
		_removedAura = _aura;
		_aura = null;

		if (!_removedAura.IsRemoved)
			_removedAura._Remove(AuraRemoveMode.Default);
	}

	void RemoveCasterViewpoint()
	{
		var caster = _caster.AsPlayer;

		if (caster != null)
		{
			caster.SetViewpoint(this, false);
			_isViewpoint = false;
		}
	}

	void BindToCaster()
	{
		_caster = Global.ObjAccessor.GetUnit(this, GetCasterGUID());
		_caster._RegisterDynObject(this);
	}

	void UnbindFromCaster()
	{
		_caster._UnregisterDynObject(this);
		_caster = null;
	}

	void BuildValuesUpdateForPlayerWithMask(UpdateData data, UpdateMask requestedObjectMask, UpdateMask requestedDynamicObjectMask, Player target)
	{
		UpdateMask valuesMask = new((int)TypeId.Max);

		if (requestedObjectMask.IsAnySet())
			valuesMask.Set((int)TypeId.Object);

		if (requestedDynamicObjectMask.IsAnySet())
			valuesMask.Set((int)TypeId.DynamicObject);

		WorldPacket buffer = new();
		buffer.WriteUInt32(valuesMask.GetBlock(0));

		if (valuesMask[(int)TypeId.Object])
			ObjectData.WriteUpdate(buffer, requestedObjectMask, true, this, target);

		if (valuesMask[(int)TypeId.DynamicObject])
			_dynamicObjectData.WriteUpdate(buffer, requestedDynamicObjectMask, true, this, target);

		WorldPacket buffer1 = new();
		buffer1.WriteUInt8((byte)UpdateType.Values);
		buffer1.WritePackedGuid(GUID);
		buffer1.WriteUInt32(buffer.GetSize());
		buffer1.WriteBytes(buffer.GetData());

		data.AddUpdateBlock(buffer1);
	}

	class ValuesUpdateForPlayerWithMaskSender : IDoWork<Player>
	{
		readonly DynamicObject _owner;
		readonly ObjectFieldData _objectMask = new();
		readonly DynamicObjectData _dynamicObjectData = new();

		public ValuesUpdateForPlayerWithMaskSender(DynamicObject owner)
		{
			_owner = owner;
		}

		public void Invoke(Player player)
		{
			UpdateData udata = new(_owner.Location.MapId);

			_owner.BuildValuesUpdateForPlayerWithMask(udata, _objectMask.GetUpdateMask(), _dynamicObjectData.GetUpdateMask(), player);

			udata.BuildPacket(out var packet);
			player.SendPacket(packet);
		}
	}
}