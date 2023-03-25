// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using System.Text;
using Forged.MapServer.DataStorage;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Objects.Update;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Maps;
using Forged.MapServer.Maps.Grids;
using Forged.MapServer.Networking;
using Forged.MapServer.Phasing;
using Forged.MapServer.Time;
using Framework.Collections;
using Framework.Constants;
using Framework.Database;

namespace Forged.MapServer.Entities;

public class Corpse : WorldObject
{
	readonly CorpseType _type;
	long _time;
	CellCoord _cellCoord; // gride for corpse position for fast search

	public CorpseData CorpseData { get; set; }

	public Loot.Loot Loot { get; set; }
	public Player LootRecipient { get; set; }

	public override ObjectGuid OwnerGUID => CorpseData.Owner;

	public override uint Faction
	{
		get => (uint)(int)CorpseData.FactionTemplate;
		set => SetFactionTemplate((int)value);
	}

	public Corpse(CorpseType type = CorpseType.Bones) : base(type != CorpseType.Bones)
	{
		_type = type;
		ObjectTypeId = TypeId.Corpse;
		ObjectTypeMask |= TypeMask.Corpse;

		_updateFlag.Stationary = true;

		CorpseData = new CorpseData();

		_time = GameTime.GetGameTime();
	}

	public override void AddToWorld()
	{
		// Register the corpse for guid lookup
		if (!IsInWorld)
			Map.ObjectsStore.TryAdd(GUID, this);

		base.AddToWorld();
	}

	public override void RemoveFromWorld()
	{
		// Remove the corpse from the accessor
		if (IsInWorld)
			Map.ObjectsStore.TryRemove(GUID, out _);

		base.RemoveFromWorld();
	}

	public bool Create(ulong guidlow, Map map)
	{
		Create(ObjectGuid.Create(HighGuid.Corpse, map.Id, 0, guidlow));

		return true;
	}

	public bool Create(ulong guidlow, Player owner)
	{
		Location.Relocate(owner.Location.X, owner.Location.Y, owner.Location.Z, owner.Location.Orientation);

		if (!Location.IsPositionValid)
		{
			Log.Logger.Error(
						"Corpse (guidlow {0}, owner {1}) not created. Suggested coordinates isn't valid (X: {2} Y: {3})",
						guidlow,
						owner.GetName(),
						owner.Location.X,
						owner.Location.Y);

			return false;
		}

		Create(ObjectGuid.Create(HighGuid.Corpse, owner.Location.MapId, 0, guidlow));

		ObjectScale = 1;
		SetOwnerGUID(owner.GUID);

		_cellCoord = GridDefines.ComputeCellCoord(Location.X, Location.Y);

		PhasingHandler.InheritPhaseShift(this, owner);

		return true;
	}

	public override void Update(uint diff)
	{
		base.Update(diff);

		Loot?.Update();
	}

	public void SaveToDB()
	{
		// prevent DB data inconsistence problems and duplicates
		SQLTransaction trans = new();
		DeleteFromDB(trans);

		StringBuilder items = new();

		for (var i = 0; i < CorpseData.Items.GetSize(); ++i)
			items.Append($"{CorpseData.Items[i]} ");

		byte index = 0;
		var stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_CORPSE);
		stmt.AddValue(index++, OwnerGUID.Counter);             // guid
		stmt.AddValue(index++, Location.X);                    // posX
		stmt.AddValue(index++, Location.Y);                    // posY
		stmt.AddValue(index++, Location.Z);                    // posZ
		stmt.AddValue(index++, Location.Orientation);          // orientation
		stmt.AddValue(index++, Location.MapId);                // mapId
		stmt.AddValue(index++, (uint)CorpseData.DisplayID);    // displayId
		stmt.AddValue(index++, items.ToString());              // itemCache
		stmt.AddValue(index++, (byte)CorpseData.RaceID);       // race
		stmt.AddValue(index++, (byte)CorpseData.Class);        // class
		stmt.AddValue(index++, (byte)CorpseData.Sex);          // gender
		stmt.AddValue(index++, (uint)CorpseData.Flags);        // flags
		stmt.AddValue(index++, (uint)CorpseData.DynamicFlags); // dynFlags
		stmt.AddValue(index++, (uint)_time);                   // time
		stmt.AddValue(index++, (uint)GetCorpseType());         // corpseType
		stmt.AddValue(index++, InstanceId);                    // instanceId
		trans.Append(stmt);

		foreach (var phaseId in PhaseShift.Phases.Keys)
		{
			index = 0;
			stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_CORPSE_PHASES);
			stmt.AddValue(index++, OwnerGUID.Counter); // OwnerGuid
			stmt.AddValue(index++, phaseId);           // PhaseId
			trans.Append(stmt);
		}

		foreach (var customization in CorpseData.Customizations)
		{
			index = 0;
			stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_CORPSE_CUSTOMIZATIONS);
			stmt.AddValue(index++, OwnerGUID.Counter); // OwnerGuid
			stmt.AddValue(index++, customization.ChrCustomizationOptionID);
			stmt.AddValue(index++, customization.ChrCustomizationChoiceID);
			trans.Append(stmt);
		}

		DB.Characters.CommitTransaction(trans);
	}

	public void DeleteFromDB(SQLTransaction trans)
	{
		DeleteFromDB(OwnerGUID, trans);
	}

	public static void DeleteFromDB(ObjectGuid ownerGuid, SQLTransaction trans)
	{
		var stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CORPSE);
		stmt.AddValue(0, ownerGuid.Counter);
		DB.Characters.ExecuteOrAppend(trans, stmt);

		stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CORPSE_PHASES);
		stmt.AddValue(0, ownerGuid.Counter);
		DB.Characters.ExecuteOrAppend(trans, stmt);

		stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CORPSE_CUSTOMIZATIONS);
		stmt.AddValue(0, ownerGuid.Counter);
		DB.Characters.ExecuteOrAppend(trans, stmt);
	}

	public bool LoadCorpseFromDB(ulong guid, SQLFields field)
	{
		//        0     1     2     3            4      5          6          7     8      9       10     11        12    13          14          15
		// SELECT posX, posY, posZ, orientation, mapId, displayId, itemCache, race, class, gender, flags, dynFlags, time, corpseType, instanceId, guid FROM corpse WHERE mapId = ? AND instanceId = ?

		var posX = field.Read<float>(0);
		var posY = field.Read<float>(1);
		var posZ = field.Read<float>(2);
		var o = field.Read<float>(3);
		var mapId = field.Read<ushort>(4);

		Create(ObjectGuid.Create(HighGuid.Corpse, mapId, 0, guid));

		ObjectScale = 1.0f;
		SetDisplayId(field.Read<uint>(5));
		StringArray items = new(field.Read<string>(6), ' ');

		if (items.Length == CorpseData.Items.GetSize())
			for (uint index = 0; index < CorpseData.Items.GetSize(); ++index)
				SetItem(index, uint.Parse(items[(int)index]));

		SetRace(field.Read<byte>(7));
		SetClass(field.Read<byte>(8));
		SetSex(field.Read<byte>(9));
		ReplaceAllFlags((CorpseFlags)field.Read<byte>(10));
		ReplaceAllCorpseDynamicFlags((CorpseDynFlags)field.Read<byte>(11));
		SetOwnerGUID(ObjectGuid.Create(HighGuid.Player, field.Read<ulong>(15)));
		SetFactionTemplate(CliDB.ChrRacesStorage.LookupByKey(CorpseData.RaceID).FactionID);

		_time = field.Read<uint>(12);

		var instanceId = field.Read<uint>(14);

		// place
		SetLocationInstanceId(instanceId);
		Location.MapId = mapId;
		Location.Relocate(posX, posY, posZ, o);

		if (!Location.IsPositionValid)
		{
			Log.Logger.Error(
						"Corpse ({0}, owner: {1}) is not created, given coordinates are not valid (X: {2}, Y: {3}, Z: {4})",
						GUID.ToString(),
						OwnerGUID.ToString(),
						posX,
						posY,
						posZ);

			return false;
		}

		_cellCoord = GridDefines.ComputeCellCoord(Location.X, Location.Y);

		return true;
	}

	public bool IsExpired(long t)
	{
		// Deleted character
		if (!Global.CharacterCacheStorage.HasCharacterCacheEntry(OwnerGUID))
			return true;

		if (_type == CorpseType.Bones)
			return _time < t - 60 * global::Time.Minute;
		else
			return _time < t - 3 * global::Time.Day;
	}

	public override void BuildValuesCreate(WorldPacket data, Player target)
	{
		var flags = GetUpdateFieldFlagsFor(target);
		WorldPacket buffer = new();

		ObjectData.WriteCreate(buffer, flags, this, target);
		CorpseData.WriteCreate(buffer, flags, this, target);

		data.WriteUInt32(buffer.GetSize() + 1);
		data.WriteUInt8((byte)flags);
		data.WriteBytes(buffer);
	}

	public override void BuildValuesUpdate(WorldPacket data, Player target)
	{
		var flags = GetUpdateFieldFlagsFor(target);
		WorldPacket buffer = new();

		buffer.WriteUInt32(Values.GetChangedObjectTypeMask());

		if (Values.HasChanged(TypeId.Object))
			ObjectData.WriteUpdate(buffer, flags, this, target);

		if (Values.HasChanged(TypeId.Corpse))
			CorpseData.WriteUpdate(buffer, flags, this, target);

		data.WriteUInt32(buffer.GetSize());
		data.WriteBytes(buffer);
	}

	public override void ClearUpdateMask(bool remove)
	{
		Values.ClearChangesMask(CorpseData);
		base.ClearUpdateMask(remove);
	}

	public CorpseDynFlags GetCorpseDynamicFlags()
	{
		return (CorpseDynFlags)(uint)CorpseData.DynamicFlags;
	}

	public bool HasCorpseDynamicFlag(CorpseDynFlags flag)
	{
		return (CorpseData.DynamicFlags & (uint)flag) != 0;
	}

	public void SetCorpseDynamicFlag(CorpseDynFlags dynamicFlags)
	{
		SetUpdateFieldFlagValue(Values.ModifyValue(CorpseData).ModifyValue(CorpseData.DynamicFlags), (uint)dynamicFlags);
	}

	public void RemoveCorpseDynamicFlag(CorpseDynFlags dynamicFlags)
	{
		RemoveUpdateFieldFlagValue(Values.ModifyValue(CorpseData).ModifyValue(CorpseData.DynamicFlags), (uint)dynamicFlags);
	}

	public void ReplaceAllCorpseDynamicFlags(CorpseDynFlags dynamicFlags)
	{
		SetUpdateFieldValue(Values.ModifyValue(CorpseData).ModifyValue(CorpseData.DynamicFlags), (uint)dynamicFlags);
	}

	public void SetOwnerGUID(ObjectGuid owner)
	{
		SetUpdateFieldValue(Values.ModifyValue(CorpseData).ModifyValue(CorpseData.Owner), owner);
	}

	public void SetPartyGUID(ObjectGuid partyGuid)
	{
		SetUpdateFieldValue(Values.ModifyValue(CorpseData).ModifyValue(CorpseData.PartyGUID), partyGuid);
	}

	public void SetGuildGUID(ObjectGuid guildGuid)
	{
		SetUpdateFieldValue(Values.ModifyValue(CorpseData).ModifyValue(CorpseData.GuildGUID), guildGuid);
	}

	public void SetDisplayId(uint displayId)
	{
		SetUpdateFieldValue(Values.ModifyValue(CorpseData).ModifyValue(CorpseData.DisplayID), displayId);
	}

	public void SetRace(byte race)
	{
		SetUpdateFieldValue(Values.ModifyValue(CorpseData).ModifyValue(CorpseData.RaceID), race);
	}

	public void SetClass(byte classId)
	{
		SetUpdateFieldValue(Values.ModifyValue(CorpseData).ModifyValue(CorpseData.Class), classId);
	}

	public void SetSex(byte sex)
	{
		SetUpdateFieldValue(Values.ModifyValue(CorpseData).ModifyValue(CorpseData.Sex), sex);
	}

	public void ReplaceAllFlags(CorpseFlags flags)
	{
		SetUpdateFieldValue(Values.ModifyValue(CorpseData).ModifyValue(CorpseData.Flags), (uint)flags);
	}

	public void SetFactionTemplate(int factionTemplate)
	{
		SetUpdateFieldValue(Values.ModifyValue(CorpseData).ModifyValue(CorpseData.FactionTemplate), factionTemplate);
	}

	public void SetItem(uint slot, uint item)
	{
		SetUpdateFieldValue(ref Values.ModifyValue(CorpseData).ModifyValue(CorpseData.Items, (int)slot), item);
	}

	public void SetCustomizations(List<ChrCustomizationChoice> customizations)
	{
		ClearDynamicUpdateFieldValues(Values.ModifyValue(CorpseData).ModifyValue(CorpseData.Customizations));

		foreach (var customization in customizations)
		{
			var newChoice = new ChrCustomizationChoice
            {
                ChrCustomizationOptionID = customization.ChrCustomizationOptionID,
                ChrCustomizationChoiceID = customization.ChrCustomizationChoiceID
            };

            AddDynamicUpdateFieldValue(Values.ModifyValue(CorpseData).ModifyValue(CorpseData.Customizations), newChoice);
		}
	}

	public long GetGhostTime()
	{
		return _time;
	}

	public void ResetGhostTime()
	{
		_time = GameTime.GetGameTime();
	}

	public CorpseType GetCorpseType()
	{
		return _type;
	}

	public CellCoord GetCellCoord()
	{
		return _cellCoord;
	}

	public void SetCellCoord(CellCoord cellCoord)
	{
		_cellCoord = cellCoord;
	}

	public override Loot.Loot GetLootForPlayer(Player player)
	{
		return Loot;
	}

	void BuildValuesUpdateForPlayerWithMask(UpdateData data, UpdateMask requestedObjectMask, UpdateMask requestedCorpseMask, Player target)
	{
		UpdateMask valuesMask = new((int)TypeId.Max);

		if (requestedObjectMask.IsAnySet())
			valuesMask.Set((int)TypeId.Object);

		if (requestedCorpseMask.IsAnySet())
			valuesMask.Set((int)TypeId.Corpse);

		WorldPacket buffer = new();
		buffer.WriteUInt32(valuesMask.GetBlock(0));

		if (valuesMask[(int)TypeId.Object])
			ObjectData.WriteUpdate(buffer, requestedObjectMask, true, this, target);

		if (valuesMask[(int)TypeId.Corpse])
			CorpseData.WriteUpdate(buffer, requestedCorpseMask, true, this, target);

		WorldPacket buffer1 = new();
		buffer1.WriteUInt8((byte)UpdateType.Values);
		buffer1.WritePackedGuid(GUID);
		buffer1.WriteUInt32(buffer.GetSize());
		buffer1.WriteBytes(buffer.GetData());

		data.AddUpdateBlock(buffer1);
	}

	class ValuesUpdateForPlayerWithMaskSender : IDoWork<Player>
	{
		readonly Corpse _owner;
		readonly ObjectFieldData _objectMask = new();
		readonly CorpseData _corpseData = new();

		public ValuesUpdateForPlayerWithMaskSender(Corpse owner)
		{
			_owner = owner;
		}

		public void Invoke(Player player)
		{
			UpdateData udata = new(_owner.Location.MapId);

			_owner.BuildValuesUpdateForPlayerWithMask(udata, _objectMask.GetUpdateMask(), _corpseData.GetUpdateMask(), player);

			udata.BuildPacket(out var packet);
			player.SendPacket(packet);
		}
	}
}