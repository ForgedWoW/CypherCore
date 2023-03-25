// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Players;
using Forged.MapServer.Networking;
using Framework.Constants;

namespace Forged.MapServer.Entities.Objects.Update;

public class CorpseData : BaseUpdateData<Corpse>
{
	public DynamicUpdateField<ChrCustomizationChoice> Customizations = new(0, 1);
	public UpdateField<uint> DynamicFlags = new(0, 2);
	public UpdateField<ObjectGuid> Owner = new(0, 3);
	public UpdateField<ObjectGuid> PartyGUID = new(0, 4);
	public UpdateField<ObjectGuid> GuildGUID = new(0, 5);
	public UpdateField<uint> DisplayID = new(0, 6);
	public UpdateField<byte> RaceID = new(0, 7);
	public UpdateField<byte> Sex = new(0, 8);
	public UpdateField<byte> Class = new(0, 9);
	public UpdateField<uint> Flags = new(0, 10);
	public UpdateField<int> FactionTemplate = new(0, 11);
	public UpdateField<uint> StateSpellVisualKitID = new(0, 12);
	public UpdateFieldArray<uint> Items = new(19, 13, 14);

	public CorpseData() : base(0, TypeId.Corpse, 33) { }

	public void WriteCreate(WorldPacket data, UpdateFieldFlag fieldVisibilityFlags, Corpse owner, Player receiver)
	{
		data.WriteUInt32(DynamicFlags);
		data.WritePackedGuid(Owner);
		data.WritePackedGuid(PartyGUID);
		data.WritePackedGuid(GuildGUID);
		data.WriteUInt32(DisplayID);

		for (var i = 0; i < 19; ++i)
			data.WriteUInt32(Items[i]);

		data.WriteUInt8(RaceID);
		data.WriteUInt8(Sex);
		data.WriteUInt8(Class);
		data.WriteInt32(Customizations.Size());
		data.WriteUInt32(Flags);
		data.WriteInt32(FactionTemplate);
		data.WriteUInt32(StateSpellVisualKitID);

		for (var i = 0; i < Customizations.Size(); ++i)
			Customizations[i].WriteCreate(data, owner, receiver);
	}

	public void WriteUpdate(WorldPacket data, UpdateFieldFlag fieldVisibilityFlags, Corpse owner, Player receiver)
	{
		WriteUpdate(data, ChangesMask, false, owner, receiver);
	}

	public void WriteUpdate(WorldPacket data, UpdateMask changesMask, bool ignoreNestedChangesMask, Corpse owner, Player receiver)
	{
		data.WriteBits(changesMask.GetBlocksMask(0), 2);

		for (uint i = 0; i < 2; ++i)
			if (changesMask.GetBlock(i) != 0)
				data.WriteBits(changesMask.GetBlock(i), 32);

		if (changesMask[0])
			if (changesMask[1])
			{
				if (!ignoreNestedChangesMask)
					Customizations.WriteUpdateMask(data);
				else
					WriteCompleteDynamicFieldUpdateMask(Customizations.Size(), data);
			}

		data.FlushBits();

		if (changesMask[0])
		{
			if (changesMask[1])
				for (var i = 0; i < Customizations.Size(); ++i)
					if (Customizations.HasChanged(i) || ignoreNestedChangesMask)
						Customizations[i].WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);

			if (changesMask[2])
				data.WriteUInt32(DynamicFlags);

			if (changesMask[3])
				data.WritePackedGuid(Owner);

			if (changesMask[4])
				data.WritePackedGuid(PartyGUID);

			if (changesMask[5])
				data.WritePackedGuid(GuildGUID);

			if (changesMask[6])
				data.WriteUInt32(DisplayID);

			if (changesMask[7])
				data.WriteUInt8(RaceID);

			if (changesMask[8])
				data.WriteUInt8(Sex);

			if (changesMask[9])
				data.WriteUInt8(Class);

			if (changesMask[10])
				data.WriteUInt32(Flags);

			if (changesMask[11])
				data.WriteInt32(FactionTemplate);

			if (changesMask[12])
				data.WriteUInt32(StateSpellVisualKitID);
		}

		if (changesMask[13])
			for (var i = 0; i < 19; ++i)
				if (changesMask[14 + i])
					data.WriteUInt32(Items[i]);
	}

	public override void ClearChangesMask()
	{
		ClearChangesMask(Customizations);
		ClearChangesMask(DynamicFlags);
		ClearChangesMask(Owner);
		ClearChangesMask(PartyGUID);
		ClearChangesMask(GuildGUID);
		ClearChangesMask(DisplayID);
		ClearChangesMask(RaceID);
		ClearChangesMask(Sex);
		ClearChangesMask(Class);
		ClearChangesMask(Flags);
		ClearChangesMask(FactionTemplate);
		ClearChangesMask(StateSpellVisualKitID);
		ClearChangesMask(Items);
		ChangesMask.ResetAll();
	}
}