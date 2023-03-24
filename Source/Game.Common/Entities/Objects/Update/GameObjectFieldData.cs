// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using System.Numerics;
using Framework.Constants;
using Game.Common.Entities.GameObjects;
using Game.Common.Entities.Players;
using Game.Common.Networking;

namespace Game.Common.Entities.Objects.Update;

public class GameObjectFieldData : BaseUpdateData<GameObject>
{
	public UpdateField<List<uint>> StateWorldEffectIDs = new(0, 1);
	public DynamicUpdateField<int> EnableDoodadSets = new(0, 2);
	public DynamicUpdateField<int> WorldEffects = new(0, 3);
	public UpdateField<uint> DisplayID = new(0, 4);
	public UpdateField<uint> SpellVisualID = new(0, 5);
	public UpdateField<uint> StateSpellVisualID = new(0, 6);
	public UpdateField<uint> SpawnTrackingStateAnimID = new(0, 7);
	public UpdateField<uint> SpawnTrackingStateAnimKitID = new(0, 8);
	public UpdateField<uint> StateWorldEffectsQuestObjectiveID = new(0, 9);
	public UpdateField<ObjectGuid> CreatedBy = new(0, 10);
	public UpdateField<ObjectGuid> GuildGUID = new(0, 11);
	public UpdateField<uint> Flags = new(0, 12);
	public UpdateField<Quaternion> ParentRotation = new(0, 13);
	public UpdateField<uint> FactionTemplate = new(0, 14);
	public UpdateField<sbyte> State = new(0, 15);
	public UpdateField<sbyte> TypeID = new(0, 16);
	public UpdateField<byte> PercentHealth = new(0, 17);
	public UpdateField<uint> ArtKit = new(0, 18);
	public UpdateField<uint> CustomParam = new(0, 19);
	public UpdateField<uint> Level = new(0, 20);
	public UpdateField<uint> AnimGroupInstance = new(0, 21);
	public UpdateField<uint> UiWidgetItemID = new(0, 22);
	public UpdateField<uint> UiWidgetItemQuality = new(0, 23);
	public UpdateField<uint> UiWidgetItemUnknown1000 = new(0, 24);

	public GameObjectFieldData() : base(0, TypeId.GameObject, 25) { }

	public void WriteCreate(WorldPacket data, UpdateFieldFlag fieldVisibilityFlags, GameObject owner, Player receiver)
	{
		data.WriteUInt32(DisplayID);
		data.WriteUInt32(SpellVisualID);
		data.WriteUInt32(StateSpellVisualID);
		data.WriteUInt32(SpawnTrackingStateAnimID);
		data.WriteUInt32(SpawnTrackingStateAnimKitID);
		data.WriteInt32(((List<uint>)StateWorldEffectIDs).Count);
		data.WriteUInt32(StateWorldEffectsQuestObjectiveID);

		for (var i = 0; i < ((List<uint>)StateWorldEffectIDs).Count; ++i)
			data.WriteUInt32(((List<uint>)StateWorldEffectIDs)[i]);

		data.WritePackedGuid(CreatedBy);
		data.WritePackedGuid(GuildGUID);
		data.WriteUInt32(GetViewerGameObjectFlags(this, owner, receiver));
		Quaternion rotation = ParentRotation;
		data.WriteFloat(rotation.X);
		data.WriteFloat(rotation.Y);
		data.WriteFloat(rotation.Z);
		data.WriteFloat(rotation.W);
		data.WriteUInt32(FactionTemplate);
		data.WriteInt8(GetViewerGameObjectState(this, owner, receiver));
		data.WriteInt8(TypeID);
		data.WriteUInt8(PercentHealth);
		data.WriteUInt32(ArtKit);
		data.WriteInt32(EnableDoodadSets.Size());
		data.WriteUInt32(CustomParam);
		data.WriteUInt32(Level);
		data.WriteUInt32(AnimGroupInstance);
		data.WriteUInt32(UiWidgetItemID);
		data.WriteUInt32(UiWidgetItemQuality);
		data.WriteUInt32(UiWidgetItemUnknown1000);
		data.WriteInt32(WorldEffects.Size());

		for (var i = 0; i < EnableDoodadSets.Size(); ++i)
			data.WriteInt32(EnableDoodadSets[i]);

		for (var i = 0; i < WorldEffects.Size(); ++i)
			data.WriteInt32(WorldEffects[i]);
	}

	public void WriteUpdate(WorldPacket data, UpdateFieldFlag fieldVisibilityFlags, GameObject owner, Player receiver)
	{
		WriteUpdate(data, ChangesMask, false, owner, receiver);
	}

	public void WriteUpdate(WorldPacket data, UpdateMask changesMask, bool ignoreNestedChangesMask, GameObject owner, Player receiver)
	{
		data.WriteBits(changesMask.GetBlock(0), 25);

		if (changesMask[0])
			if (changesMask[1])
			{
				data.WriteBits(((List<uint>)StateWorldEffectIDs).Count, 32);

				for (var i = 0; i < ((List<uint>)StateWorldEffectIDs).Count; ++i)
					data.WriteUInt32(((List<uint>)StateWorldEffectIDs)[i]);
			}

		data.FlushBits();

		if (changesMask[0])
		{
			if (changesMask[2])
			{
				if (!ignoreNestedChangesMask)
					EnableDoodadSets.WriteUpdateMask(data);
				else
					WriteCompleteDynamicFieldUpdateMask(EnableDoodadSets.Size(), data);
			}

			if (changesMask[3])
			{
				if (!ignoreNestedChangesMask)
					WorldEffects.WriteUpdateMask(data);
				else
					WriteCompleteDynamicFieldUpdateMask(WorldEffects.Size(), data);
			}
		}

		data.FlushBits();

		if (changesMask[0])
		{
			if (changesMask[2])
				for (var i = 0; i < EnableDoodadSets.Size(); ++i)
					if (EnableDoodadSets.HasChanged(i) || ignoreNestedChangesMask)
						data.WriteInt32(EnableDoodadSets[i]);

			if (changesMask[3])
				for (var i = 0; i < WorldEffects.Size(); ++i)
					if (WorldEffects.HasChanged(i) || ignoreNestedChangesMask)
						data.WriteInt32(WorldEffects[i]);

			if (changesMask[4])
				data.WriteUInt32(DisplayID);

			if (changesMask[5])
				data.WriteUInt32(SpellVisualID);

			if (changesMask[6])
				data.WriteUInt32(StateSpellVisualID);

			if (changesMask[7])
				data.WriteUInt32(SpawnTrackingStateAnimID);

			if (changesMask[8])
				data.WriteUInt32(SpawnTrackingStateAnimKitID);

			if (changesMask[9])
				data.WriteUInt32(StateWorldEffectsQuestObjectiveID);

			if (changesMask[10])
				data.WritePackedGuid(CreatedBy);

			if (changesMask[11])
				data.WritePackedGuid(GuildGUID);

			if (changesMask[12])
				data.WriteUInt32(GetViewerGameObjectFlags(this, owner, receiver));

			if (changesMask[13])
			{
				data.WriteFloat(((Quaternion)ParentRotation).X);
				data.WriteFloat(((Quaternion)ParentRotation).Y);
				data.WriteFloat(((Quaternion)ParentRotation).Z);
				data.WriteFloat(((Quaternion)ParentRotation).W);
			}

			if (changesMask[14])
				data.WriteUInt32(FactionTemplate);

			if (changesMask[15])
				data.WriteInt8(GetViewerGameObjectState(this, owner, receiver));

			if (changesMask[16])
				data.WriteInt8(TypeID);

			if (changesMask[17])
				data.WriteUInt8(PercentHealth);

			if (changesMask[18])
				data.WriteUInt32(ArtKit);

			if (changesMask[19])
				data.WriteUInt32(CustomParam);

			if (changesMask[20])
				data.WriteUInt32(Level);

			if (changesMask[21])
				data.WriteUInt32(AnimGroupInstance);

			if (changesMask[22])
				data.WriteUInt32(UiWidgetItemID);

			if (changesMask[23])
				data.WriteUInt32(UiWidgetItemQuality);

			if (changesMask[24])
				data.WriteUInt32(UiWidgetItemUnknown1000);
		}
	}

	public override void ClearChangesMask()
	{
		ClearChangesMask(StateWorldEffectIDs);
		ClearChangesMask(EnableDoodadSets);
		ClearChangesMask(WorldEffects);
		ClearChangesMask(DisplayID);
		ClearChangesMask(SpellVisualID);
		ClearChangesMask(StateSpellVisualID);
		ClearChangesMask(SpawnTrackingStateAnimID);
		ClearChangesMask(SpawnTrackingStateAnimKitID);
		ClearChangesMask(StateWorldEffectsQuestObjectiveID);
		ClearChangesMask(CreatedBy);
		ClearChangesMask(GuildGUID);
		ClearChangesMask(Flags);
		ClearChangesMask(ParentRotation);
		ClearChangesMask(FactionTemplate);
		ClearChangesMask(State);
		ClearChangesMask(TypeID);
		ClearChangesMask(PercentHealth);
		ClearChangesMask(ArtKit);
		ClearChangesMask(CustomParam);
		ClearChangesMask(Level);
		ClearChangesMask(AnimGroupInstance);
		ClearChangesMask(UiWidgetItemID);
		ClearChangesMask(UiWidgetItemQuality);
		ClearChangesMask(UiWidgetItemUnknown1000);
		ChangesMask.ResetAll();
	}

	uint GetViewerGameObjectFlags(GameObjectFieldData gameObjectData, GameObject gameObject, Player receiver)
	{
		uint flags = gameObjectData.Flags;

		if (gameObject.GoType == GameObjectTypes.Chest)
			if (gameObject.Template.Chest.usegrouplootrules != 0 && !gameObject.IsLootAllowedFor(receiver))
				flags |= (uint)(GameObjectFlags.Locked | GameObjectFlags.NotSelectable);

		return flags;
	}

	sbyte GetViewerGameObjectState(GameObjectFieldData gameObjectData, GameObject gameObject, Player receiver)
	{
		return (sbyte)gameObject.GetGoStateFor(receiver.GUID);
	}
}
