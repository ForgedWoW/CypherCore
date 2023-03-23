// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Common.Entities.Items;
using Game.Entities;
using Game.Common.Entities.Objects.Update;
using Game.Common.Entities.Players;
using Game.Common.Networking;

namespace Game.Common.Entities.Objects.Update;

public class AzeriteItemData : BaseUpdateData<AzeriteItem>
{
	public UpdateField<bool> Enabled = new(0, 1);
	public DynamicUpdateField<UnlockedAzeriteEssence> UnlockedEssences = new(0, 2);
	public DynamicUpdateField<uint> UnlockedEssenceMilestones = new(0, 4);
	public DynamicUpdateField<SelectedAzeriteEssences> SelectedEssences = new(0, 3);
	public UpdateField<ulong> Xp = new(0, 5);
	public UpdateField<uint> Level = new(0, 6);
	public UpdateField<uint> AuraLevel = new(0, 7);
	public UpdateField<uint> KnowledgeLevel = new(0, 8);
	public UpdateField<int> DEBUGknowledgeWeek = new(0, 9);

	public AzeriteItemData() : base(10) { }

	public void WriteCreate(WorldPacket data, UpdateFieldFlag fieldVisibilityFlags, AzeriteItem owner, Player receiver)
	{
		if (fieldVisibilityFlags.HasFlag(UpdateFieldFlag.Owner))
		{
			data.WriteUInt64(Xp);
			data.WriteUInt32(Level);
			data.WriteUInt32(AuraLevel);
			data.WriteUInt32(KnowledgeLevel);
			data.WriteInt32(DEBUGknowledgeWeek);
		}

		data.WriteInt32(UnlockedEssences.Size());
		data.WriteInt32(SelectedEssences.Size());
		data.WriteInt32(UnlockedEssenceMilestones.Size());

		for (var i = 0; i < UnlockedEssences.Size(); ++i)
			UnlockedEssences[i].WriteCreate(data, owner, receiver);

		for (var i = 0; i < UnlockedEssenceMilestones.Size(); ++i)
			data.WriteUInt32(UnlockedEssenceMilestones[i]);

		if (fieldVisibilityFlags.HasFlag(UpdateFieldFlag.Owner))
			data.WriteBit(Enabled);

		for (var i = 0; i < SelectedEssences.Size(); ++i)
			SelectedEssences[i].WriteCreate(data, owner, receiver);

		data.FlushBits();
	}

	public void WriteUpdate(WorldPacket data, UpdateFieldFlag fieldVisibilityFlags, AzeriteItem owner, Player receiver)
	{
		UpdateMask allowedMaskForTarget = new(9,
											new[]
											{
												0x0000001Du
											});

		AppendAllowedFieldsMaskForFlag(allowedMaskForTarget, fieldVisibilityFlags);
		WriteUpdate(data, ChangesMask & allowedMaskForTarget, false, owner, receiver);
	}

	public void AppendAllowedFieldsMaskForFlag(UpdateMask allowedMaskForTarget, UpdateFieldFlag fieldVisibilityFlags)
	{
		if (fieldVisibilityFlags.HasFlag(UpdateFieldFlag.Owner))
			allowedMaskForTarget.OR(new UpdateMask(9,
													new[]
													{
														0x000003E2u
													}));
	}

	public void FilterDisallowedFieldsMaskForFlag(UpdateMask changesMask, UpdateFieldFlag fieldVisibilityFlags)
	{
		UpdateMask allowedMaskForTarget = new(9,
											new[]
											{
												0x0000001Du
											});

		AppendAllowedFieldsMaskForFlag(allowedMaskForTarget, fieldVisibilityFlags);
		changesMask.AND(allowedMaskForTarget);
	}

	public void WriteUpdate(WorldPacket data, UpdateMask changesMask, bool ignoreNestedChangesMask, AzeriteItem owner, Player receiver)
	{
		data.WriteBits(changesMask.GetBlock(0), 10);

		if (changesMask[0])
		{
			if (changesMask[1])
				data.WriteBit(Enabled);

			if (changesMask[2])
			{
				if (!ignoreNestedChangesMask)
					UnlockedEssences.WriteUpdateMask(data);
				else
					WriteCompleteDynamicFieldUpdateMask(UnlockedEssences.Size(), data);
			}

			if (changesMask[3])
			{
				if (!ignoreNestedChangesMask)
					SelectedEssences.WriteUpdateMask(data);
				else
					WriteCompleteDynamicFieldUpdateMask(SelectedEssences.Size(), data);
			}

			if (changesMask[4])
			{
				if (!ignoreNestedChangesMask)
					UnlockedEssenceMilestones.WriteUpdateMask(data);
				else
					WriteCompleteDynamicFieldUpdateMask(UnlockedEssenceMilestones.Size(), data);
			}
		}

		data.FlushBits();

		if (changesMask[0])
		{
			if (changesMask[2])
				for (var i = 0; i < UnlockedEssences.Size(); ++i)
					if (UnlockedEssences.HasChanged(i) || ignoreNestedChangesMask)
						UnlockedEssences[i].WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);

			if (changesMask[4])
				for (var i = 0; i < UnlockedEssenceMilestones.Size(); ++i)
					if (UnlockedEssenceMilestones.HasChanged(i) || ignoreNestedChangesMask)
						data.WriteUInt32(UnlockedEssenceMilestones[i]);

			if (changesMask[3])
				for (var i = 0; i < SelectedEssences.Size(); ++i)
					if (SelectedEssences.HasChanged(i) || ignoreNestedChangesMask)
						SelectedEssences[i].WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);

			if (changesMask[5])
				data.WriteUInt64(Xp);

			if (changesMask[6])
				data.WriteUInt32(Level);

			if (changesMask[7])
				data.WriteUInt32(AuraLevel);

			if (changesMask[8])
				data.WriteUInt32(KnowledgeLevel);

			if (changesMask[9])
				data.WriteInt32(DEBUGknowledgeWeek);
		}

		data.FlushBits();
	}

	public override void ClearChangesMask()
	{
		ClearChangesMask(Enabled);
		ClearChangesMask(UnlockedEssences);
		ClearChangesMask(UnlockedEssenceMilestones);
		ClearChangesMask(SelectedEssences);
		ClearChangesMask(Xp);
		ClearChangesMask(Level);
		ClearChangesMask(AuraLevel);
		ClearChangesMask(KnowledgeLevel);
		ClearChangesMask(DEBUGknowledgeWeek);
		ChangesMask.ResetAll();
	}
}
