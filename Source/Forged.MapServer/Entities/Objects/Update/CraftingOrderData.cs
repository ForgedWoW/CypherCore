// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Networking;
using Forged.MapServer.Networking.Packets.Item;

namespace Forged.MapServer.Entities.Objects.Update;

public class CraftingOrderData : BaseUpdateData<Player>
{
	public DynamicUpdateField<CraftingOrderItem> Reagents = new(0, 1);
	public UpdateField<int> Field_0 = new(0, 2);
	public UpdateField<ulong> OrderID = new(0, 3);
	public UpdateField<int> SkillLineAbilityID = new(0, 4);
	public UpdateField<byte> OrderState = new(5, 6);
	public UpdateField<byte> OrderType = new(5, 7);
	public UpdateField<byte> MinQuality = new(5, 8);
	public UpdateField<long> ExpirationTime = new(5, 9);
	public UpdateField<long> ClaimEndTime = new(10, 11);
	public UpdateField<long> TipAmount = new(10, 12);
	public UpdateField<long> ConsortiumCut = new(10, 13);
	public UpdateField<uint> Flags = new(10, 14);
	public UpdateField<ObjectGuid> CustomerGUID = new(15, 16);
	public UpdateField<ObjectGuid> CustomerAccountGUID = new(15, 17);
	public UpdateField<ObjectGuid> CrafterGUID = new(15, 18);
	public UpdateField<ObjectGuid> PersonalCrafterGUID = new(15, 19);
	public UpdateFieldString CustomerNotes = new(20, 21);
	public OptionalUpdateField<CraftingOrderItem> OutputItem = new(20, 22);
	public OptionalUpdateField<ItemInstance> OutputItemData = new(20, 23);

	public CraftingOrderData() : base(24) { }

	public void WriteCreate(WorldPacket data, Player owner, Player receiver)
	{
		data.WriteInt32(Field_0);
		data.WriteUInt64(OrderID);
		data.WriteInt32(SkillLineAbilityID);
		data.WriteUInt8(OrderState);
		data.WriteUInt8(OrderType);
		data.WriteUInt8(MinQuality);
		data.WriteInt64(ExpirationTime);
		data.WriteInt64(ClaimEndTime);
		data.WriteInt64(TipAmount);
		data.WriteInt64(ConsortiumCut);
		data.WriteUInt32(Flags);
		data.WritePackedGuid(CustomerGUID);
		data.WritePackedGuid(CustomerAccountGUID);
		data.WritePackedGuid(CrafterGUID);
		data.WritePackedGuid(PersonalCrafterGUID);
		data.WriteInt32(Reagents.Size());
		data.WriteBits(CustomerNotes.GetValue().GetByteCount(), 10);
		data.WriteBits(OutputItem.HasValue(), 1);
		data.WriteBits(OutputItemData.HasValue(), 1);

		for (var i = 0; i < Reagents.Size(); ++i)
			Reagents[i].WriteCreate(data, owner, receiver);

		data.WriteString(CustomerNotes);

		if (OutputItem.HasValue())
			OutputItem.GetValue().WriteCreate(data, owner, receiver);

		if (OutputItemData.HasValue())
			OutputItemData.GetValue().Write(data);

		data.FlushBits();
	}

	public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, Player owner, Player receiver)
	{
		var changesMask = ChangesMask;

		if (ignoreChangesMask)
			changesMask.SetAll();

		data.WriteBits(changesMask.GetBlock(0), 24);

		if (changesMask[0])
			if (changesMask[1])
			{
				if (!ignoreChangesMask)
					Reagents.WriteUpdateMask(data);
				else
					WriteCompleteDynamicFieldUpdateMask(Reagents.Size(), data);
			}

		data.FlushBits();

		if (changesMask[0])
		{
			if (changesMask[1])
				for (var i = 0; i < Reagents.Size(); ++i)
					if (Reagents.HasChanged(i) || ignoreChangesMask)
						Reagents[i].WriteUpdate(data, ignoreChangesMask, owner, receiver);

			if (changesMask[2])
				data.WriteInt32(Field_0);

			if (changesMask[3])
				data.WriteUInt64(OrderID);

			if (changesMask[4])
				data.WriteInt32(SkillLineAbilityID);
		}

		if (changesMask[5])
		{
			if (changesMask[6])
				data.WriteUInt8(OrderState);

			if (changesMask[7])
				data.WriteUInt8(OrderType);

			if (changesMask[8])
				data.WriteUInt8(MinQuality);

			if (changesMask[9])
				data.WriteInt64(ExpirationTime);
		}

		if (changesMask[10])
		{
			if (changesMask[11])
				data.WriteInt64(ClaimEndTime);

			if (changesMask[12])
				data.WriteInt64(TipAmount);

			if (changesMask[13])
				data.WriteInt64(ConsortiumCut);

			if (changesMask[14])
				data.WriteUInt32(Flags);
		}

		if (changesMask[15])
		{
			if (changesMask[16])
				data.WritePackedGuid(CustomerGUID);

			if (changesMask[17])
				data.WritePackedGuid(CustomerAccountGUID);

			if (changesMask[18])
				data.WritePackedGuid(CrafterGUID);

			if (changesMask[19])
				data.WritePackedGuid(PersonalCrafterGUID);
		}

		if (changesMask[20])
		{
			if (changesMask[21])
			{
				data.WriteBits(CustomerNotes.GetValue().GetByteCount(), 10);
				data.WriteString(CustomerNotes);
			}

			data.WriteBits(OutputItem.HasValue(), 1);
			data.WriteBits(OutputItemData.HasValue(), 1);

			if (changesMask[22])
				if (OutputItem.HasValue())
					OutputItem.GetValue().WriteUpdate(data, ignoreChangesMask, owner, receiver);

			if (changesMask[23])
				if (OutputItemData.HasValue())
					OutputItemData.GetValue().Write(data);
		}

		data.FlushBits();
	}

	public override void ClearChangesMask()
	{
		ClearChangesMask(Reagents);
		ClearChangesMask(Field_0);
		ClearChangesMask(OrderID);
		ClearChangesMask(SkillLineAbilityID);
		ClearChangesMask(OrderState);
		ClearChangesMask(OrderType);
		ClearChangesMask(MinQuality);
		ClearChangesMask(ExpirationTime);
		ClearChangesMask(ClaimEndTime);
		ClearChangesMask(TipAmount);
		ClearChangesMask(ConsortiumCut);
		ClearChangesMask(Flags);
		ClearChangesMask(CustomerGUID);
		ClearChangesMask(CustomerAccountGUID);
		ClearChangesMask(CrafterGUID);
		ClearChangesMask(PersonalCrafterGUID);
		ClearChangesMask(CustomerNotes);
		ClearChangesMask(OutputItem);
		ClearChangesMask(OutputItemData);
		ChangesMask.ResetAll();
	}
}