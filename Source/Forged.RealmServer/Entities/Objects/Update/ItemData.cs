// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Forged.RealmServer.Networking;
using Forged.RealmServer.Networking.Packets;

namespace Forged.RealmServer.Entities;

public class ItemData : BaseUpdateData<Item>
{
	public DynamicUpdateField<ArtifactPower> ArtifactPowers = new(0, 1);
	public DynamicUpdateField<SocketedGem> Gems = new(0, 2);
	public UpdateField<ObjectGuid> Owner = new(0, 3);
	public UpdateField<ObjectGuid> ContainedIn = new(0, 4);
	public UpdateField<ObjectGuid> Creator = new(0, 5);
	public UpdateField<ObjectGuid> GiftCreator = new(0, 6);
	public UpdateField<uint> StackCount = new(0, 7);
	public UpdateField<uint> Expiration = new(0, 8);
	public UpdateField<uint> DynamicFlags = new(0, 9);
	public UpdateField<uint> Durability = new(0, 10);
	public UpdateField<uint> MaxDurability = new(0, 11);
	public UpdateField<uint> CreatePlayedTime = new(0, 12);
	public UpdateField<int> Context = new(0, 13);
	public UpdateField<ulong> CreateTime = new(0, 14);
	public UpdateField<ulong> ArtifactXP = new(0, 15);
	public UpdateField<byte> ItemAppearanceModID = new(0, 16);
	public UpdateField<ItemModList> Modifiers = new(0, 17);
	public UpdateField<uint> DynamicFlags2 = new(0, 18);
	public UpdateField<ItemBonusKey> ItemBonusKey = new(0, 19);
	public UpdateField<ushort> DEBUGItemLevel = new(0, 20);
	public UpdateFieldArray<int> SpellCharges = new(5, 21, 22);
	public UpdateFieldArray<ItemEnchantment> Enchantment = new(13, 27, 28);

	public ItemData() : base(0, TypeId.Item, 41) { }

	public void WriteCreate(WorldPacket data, UpdateFieldFlag fieldVisibilityFlags, Item owner, Player receiver)
	{
		data.WritePackedGuid(Owner);
		data.WritePackedGuid(ContainedIn);
		data.WritePackedGuid(Creator);
		data.WritePackedGuid(GiftCreator);

		if (fieldVisibilityFlags.HasFlag(UpdateFieldFlag.Owner))
		{
			data.WriteUInt32(StackCount);
			data.WriteUInt32(Expiration);

			for (var i = 0; i < 5; ++i)
				data.WriteInt32(SpellCharges[i]);
		}

		data.WriteUInt32(DynamicFlags);

		for (var i = 0; i < 13; ++i)
			Enchantment[i].WriteCreate(data, owner, receiver);

		if (fieldVisibilityFlags.HasFlag(UpdateFieldFlag.Owner))
		{
			data.WriteUInt32(Durability);
			data.WriteUInt32(MaxDurability);
		}

		data.WriteUInt32(CreatePlayedTime);
		data.WriteInt32(Context);
		data.WriteUInt64(CreateTime);

		if (fieldVisibilityFlags.HasFlag(UpdateFieldFlag.Owner))
		{
			data.WriteUInt64(ArtifactXP);
			data.WriteUInt8(ItemAppearanceModID);
		}

		data.WriteInt32(ArtifactPowers.Size());
		data.WriteInt32(Gems.Size());

		if (fieldVisibilityFlags.HasFlag(UpdateFieldFlag.Owner))
			data.WriteUInt32(DynamicFlags2);

		ItemBonusKey.GetValue().Write(data);

		if (fieldVisibilityFlags.HasFlag(UpdateFieldFlag.Owner))
			data.WriteUInt16(DEBUGItemLevel);

		for (var i = 0; i < ArtifactPowers.Size(); ++i)
			ArtifactPowers[i].WriteCreate(data, owner, receiver);

		for (var i = 0; i < Gems.Size(); ++i)
			Gems[i].WriteCreate(data, owner, receiver);

		Modifiers.GetValue().WriteCreate(data, owner, receiver);
	}

	public void WriteUpdate(WorldPacket data, UpdateFieldFlag fieldVisibilityFlags, Item owner, Player receiver)
	{
		UpdateMask allowedMaskForTarget = new(41,
											new uint[]
											{
												0xF80A727Fu, 0x000001FFu
											});

		AppendAllowedFieldsMaskForFlag(allowedMaskForTarget, fieldVisibilityFlags);
		WriteUpdate(data, ChangesMask & allowedMaskForTarget, false, owner, receiver);
	}

	public void AppendAllowedFieldsMaskForFlag(UpdateMask allowedMaskForTarget, UpdateFieldFlag fieldVisibilityFlags)
	{
		if (fieldVisibilityFlags.HasFlag(UpdateFieldFlag.Owner))
			allowedMaskForTarget.OR(new UpdateMask(41,
													new uint[]
													{
														0x07F58D80u, 0x00000000u
													}));
	}

	public void FilterDisallowedFieldsMaskForFlag(UpdateMask changesMask, UpdateFieldFlag fieldVisibilityFlags)
	{
		UpdateMask allowedMaskForTarget = new(41,
											new[]
											{
												0xF80A727Fu, 0x000001FFu
											});

		AppendAllowedFieldsMaskForFlag(allowedMaskForTarget, fieldVisibilityFlags);
		changesMask.AND(allowedMaskForTarget);
	}

	public void WriteUpdate(WorldPacket data, UpdateMask changesMask, bool ignoreNestedChangesMask, Item owner, Player receiver)
	{
		data.WriteBits(changesMask.GetBlocksMask(0), 2);

		for (uint i = 0; i < 2; ++i)
			if (changesMask.GetBlock(i) != 0)
				data.WriteBits(changesMask.GetBlock(i), 32);

		if (changesMask[0])
		{
			if (changesMask[1])
			{
				if (!ignoreNestedChangesMask)
					ArtifactPowers.WriteUpdateMask(data);
				else
					WriteCompleteDynamicFieldUpdateMask(ArtifactPowers.Size(), data);
			}

			if (changesMask[2])
			{
				if (!ignoreNestedChangesMask)
					Gems.WriteUpdateMask(data);
				else
					WriteCompleteDynamicFieldUpdateMask(Gems.Size(), data);
			}
		}

		data.FlushBits();

		if (changesMask[0])
		{
			if (changesMask[1])
				for (var i = 0; i < ArtifactPowers.Size(); ++i)
					if (ArtifactPowers.HasChanged(i) || ignoreNestedChangesMask)
						ArtifactPowers[i].WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);

			if (changesMask[2])
				for (var i = 0; i < Gems.Size(); ++i)
					if (Gems.HasChanged(i) || ignoreNestedChangesMask)
						Gems[i].WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);

			if (changesMask[3])
				data.WritePackedGuid(Owner);

			if (changesMask[4])
				data.WritePackedGuid(ContainedIn);

			if (changesMask[5])
				data.WritePackedGuid(Creator);

			if (changesMask[6])
				data.WritePackedGuid(GiftCreator);

			if (changesMask[7])
				data.WriteUInt32(StackCount);

			if (changesMask[8])
				data.WriteUInt32(Expiration);

			if (changesMask[9])
				data.WriteUInt32(DynamicFlags);

			if (changesMask[10])
				data.WriteUInt32(Durability);

			if (changesMask[11])
				data.WriteUInt32(MaxDurability);

			if (changesMask[12])
				data.WriteUInt32(CreatePlayedTime);

			if (changesMask[13])
				data.WriteInt32(Context);

			if (changesMask[14])
				data.WriteUInt64(CreateTime);

			if (changesMask[15])
				data.WriteUInt64(ArtifactXP);

			if (changesMask[16])
				data.WriteUInt8(ItemAppearanceModID);

			if (changesMask[18])
				data.WriteUInt32(DynamicFlags2);

			if (changesMask[19])
				ItemBonusKey.GetValue().Write(data);

			if (changesMask[20])
				data.WriteUInt16(DEBUGItemLevel);

			if (changesMask[17])
				Modifiers.GetValue().WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
		}

		if (changesMask[21])
			for (var i = 0; i < 5; ++i)
				if (changesMask[22 + i])
					data.WriteInt32(SpellCharges[i]);

		if (changesMask[27])
			for (var i = 0; i < 13; ++i)
				if (changesMask[28 + i])
					Enchantment[i].WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
	}

	public override void ClearChangesMask()
	{
		ClearChangesMask(ArtifactPowers);
		ClearChangesMask(Gems);
		ClearChangesMask(Owner);
		ClearChangesMask(ContainedIn);
		ClearChangesMask(Creator);
		ClearChangesMask(GiftCreator);
		ClearChangesMask(StackCount);
		ClearChangesMask(Expiration);
		ClearChangesMask(DynamicFlags);
		ClearChangesMask(Durability);
		ClearChangesMask(MaxDurability);
		ClearChangesMask(CreatePlayedTime);
		ClearChangesMask(Context);
		ClearChangesMask(CreateTime);
		ClearChangesMask(ArtifactXP);
		ClearChangesMask(ItemAppearanceModID);
		ClearChangesMask(Modifiers);
		ClearChangesMask(DynamicFlags2);
		ClearChangesMask(ItemBonusKey);
		ClearChangesMask(DEBUGItemLevel);
		ClearChangesMask(SpellCharges);
		ClearChangesMask(Enchantment);
		ChangesMask.ResetAll();
	}
}