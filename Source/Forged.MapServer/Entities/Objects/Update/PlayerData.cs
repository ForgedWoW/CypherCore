// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Players;
using Forged.MapServer.Networking;
using Forged.MapServer.Networking.Packets.MythicPlus;
using Framework.Constants;

namespace Forged.MapServer.Entities.Objects.Update;

public class PlayerData : BaseUpdateData<Player>
{
	public UpdateField<bool> HasQuestSession = new(0, 1);
	public UpdateField<bool> HasLevelLink = new(0, 2);
	public DynamicUpdateField<ChrCustomizationChoice> Customizations = new(0, 3);
	public DynamicUpdateField<QuestLog> QuestSessionQuestLog = new(0, 4);
	public DynamicUpdateField<ArenaCooldown> ArenaCooldowns = new(0, 5);
	public DynamicUpdateField<int> VisualItemReplacements = new(0, 6);
	public UpdateField<ObjectGuid> DuelArbiter = new(0, 7);
	public UpdateField<ObjectGuid> WowAccount = new(0, 8);
	public UpdateField<ObjectGuid> LootTargetGUID = new(0, 9);
	public UpdateField<uint> PlayerFlags = new(0, 10);
	public UpdateField<uint> PlayerFlagsEx = new(0, 11);
	public UpdateField<uint> GuildRankID = new(0, 12);
	public UpdateField<uint> GuildDeleteDate = new(0, 13);
	public UpdateField<uint> GuildLevel = new(0, 14);
	public UpdateField<byte> PartyType = new(0, 15);
	public UpdateField<byte> NativeSex = new(0, 16);
	public UpdateField<byte> Inebriation = new(0, 17);
	public UpdateField<byte> PvpTitle = new(0, 18);
	public UpdateField<byte> ArenaFaction = new(0, 19);
	public UpdateField<uint> DuelTeam = new(0, 20);
	public UpdateField<int> GuildTimeStamp = new(0, 21);
	public UpdateField<uint> PlayerTitle = new(0, 22);
	public UpdateField<int> FakeInebriation = new(0, 23);
	public UpdateField<uint> VirtualPlayerRealm = new(0, 24);
	public UpdateField<uint> CurrentSpecID = new(0, 25);
	public UpdateField<int> TaxiMountAnimKitID = new(0, 26);
	public UpdateField<byte> CurrentBattlePetBreedQuality = new(0, 27);
	public UpdateField<uint> HonorLevel = new(0, 28);
	public UpdateField<long> LogoutTime = new(0, 29);
	public UpdateField<int> Field_B0 = new(0, 30);
	public UpdateField<int> Field_B4 = new(0, 31);
	public UpdateField<CTROptions> CtrOptions = new(32, 33);
	public UpdateField<int> CovenantID = new(32, 34);
	public UpdateField<int> SoulbindID = new(32, 35);
	public UpdateField<DungeonScoreSummary> DungeonScore = new(32, 36);
	public UpdateFieldArray<QuestLog> QuestLog = new(125, 37, 38);
	public UpdateFieldArray<VisibleItem> VisibleItems = new(19, 163, 164);
	public UpdateFieldArray<float> AvgItemLevel = new(6, 183, 184);

	public PlayerData() : base(0, TypeId.Player, 190) { }

	public void WriteCreate(WorldPacket data, UpdateFieldFlag fieldVisibilityFlags, Player owner, Player receiver)
	{
		data.WritePackedGuid(DuelArbiter);
		data.WritePackedGuid(WowAccount);
		data.WritePackedGuid(LootTargetGUID);
		data.WriteUInt32(PlayerFlags);
		data.WriteUInt32(PlayerFlagsEx);
		data.WriteUInt32(GuildRankID);
		data.WriteUInt32(GuildDeleteDate);
		data.WriteUInt32(GuildLevel);
		data.WriteInt32(Customizations.Size());
		data.WriteUInt8(PartyType);
		data.WriteUInt8(NativeSex);
		data.WriteUInt8(Inebriation);
		data.WriteUInt8(PvpTitle);
		data.WriteUInt8(ArenaFaction);
		data.WriteUInt32(DuelTeam);
		data.WriteInt32(GuildTimeStamp);

		if (fieldVisibilityFlags.HasFlag(UpdateFieldFlag.PartyMember))
		{
			for (var i = 0; i < 125; ++i)
				QuestLog[i].WriteCreate(data, owner, receiver);

			data.WriteInt32(QuestSessionQuestLog.Size());
		}

		for (var i = 0; i < 19; ++i)
			VisibleItems[i].WriteCreate(data, owner, receiver);

		data.WriteUInt32(PlayerTitle);
		data.WriteInt32(FakeInebriation);
		data.WriteUInt32(VirtualPlayerRealm);
		data.WriteUInt32(CurrentSpecID);
		data.WriteInt32(TaxiMountAnimKitID);

		for (var i = 0; i < 6; ++i)
			data.WriteFloat(AvgItemLevel[i]);

		data.WriteUInt8(CurrentBattlePetBreedQuality);
		data.WriteUInt32(HonorLevel);
		data.WriteInt64(LogoutTime);
		data.WriteInt32(ArenaCooldowns.Size());
		data.WriteInt32(Field_B0);
		data.WriteInt32(Field_B4);
		((CTROptions)CtrOptions).WriteCreate(data, owner, receiver);
		data.WriteInt32(CovenantID);
		data.WriteInt32(SoulbindID);
		data.WriteInt32(VisualItemReplacements.Size());

		for (var i = 0; i < Customizations.Size(); ++i)
			Customizations[i].WriteCreate(data, owner, receiver);

		if (fieldVisibilityFlags.HasFlag(UpdateFieldFlag.PartyMember))
			for (var i = 0; i < QuestSessionQuestLog.Size(); ++i)
				QuestSessionQuestLog[i].WriteCreate(data, owner, receiver);

		for (var i = 0; i < ArenaCooldowns.Size(); ++i)
			ArenaCooldowns[i].WriteCreate(data, owner, receiver);

		for (var i = 0; i < VisualItemReplacements.Size(); ++i)
			data.WriteInt32(VisualItemReplacements[i]);

		if (fieldVisibilityFlags.HasFlag(UpdateFieldFlag.PartyMember))
			data.WriteBit(HasQuestSession);

		data.WriteBit(HasLevelLink);
		DungeonScore.Value.Write(data);
		data.FlushBits();
	}

	public void WriteUpdate(WorldPacket data, UpdateFieldFlag fieldVisibilityFlags, Player owner, Player receiver)
	{
		UpdateMask allowedMaskForTarget = new(188,
											new[]
											{
												0xFFFFFFEDu, 0x0000001Fu, 0x00000000u, 0x00000000u, 0x00000000u, 0x3FFFFFF8u
											});

		AppendAllowedFieldsMaskForFlag(allowedMaskForTarget, fieldVisibilityFlags);
		WriteUpdate(data, ChangesMask & allowedMaskForTarget, false, owner, receiver);
	}

	public void AppendAllowedFieldsMaskForFlag(UpdateMask allowedMaskForTarget, UpdateFieldFlag fieldVisibilityFlags)
	{
		if (fieldVisibilityFlags.HasFlag(UpdateFieldFlag.PartyMember))
			allowedMaskForTarget.OR(new UpdateMask(188,
													new[]
													{
														0x00000012u, 0xFFFFFFE0u, 0xFFFFFFFFu, 0xFFFFFFFFu, 0xFFFFFFFFu, 0x00000007u
													}));
	}

	public void FilterDisallowedFieldsMaskForFlag(UpdateMask changesMask, UpdateFieldFlag fieldVisibilityFlags)
	{
		UpdateMask allowedMaskForTarget = new(188,
											new[]
											{
												0xFFFFFFEDu, 0x0000001Fu, 0x00000000u, 0x00000000u, 0x00000000u, 0x3FFFFFF8u
											});

		AppendAllowedFieldsMaskForFlag(allowedMaskForTarget, fieldVisibilityFlags);
		changesMask.AND(allowedMaskForTarget);
	}

	public void WriteUpdate(WorldPacket data, UpdateMask changesMask, bool ignoreNestedChangesMask, Player owner, Player receiver)
	{
		data.WriteBits(changesMask.GetBlocksMask(0), 6);

		for (uint i = 0; i < 6; ++i)
			if (changesMask.GetBlock(i) != 0)
				data.WriteBits(changesMask.GetBlock(i), 32);

		var noQuestLogChangesMask = data.WriteBit(IsQuestLogChangesMaskSkipped());

		if (changesMask[0])
		{
			if (changesMask[1])
				data.WriteBit(HasQuestSession);

			if (changesMask[2])
				data.WriteBit(HasLevelLink);

			if (changesMask[3])
			{
				if (!ignoreNestedChangesMask)
					Customizations.WriteUpdateMask(data);
				else
					WriteCompleteDynamicFieldUpdateMask(Customizations.Size(), data);
			}

			if (changesMask[4])
			{
				if (!ignoreNestedChangesMask)
					QuestSessionQuestLog.WriteUpdateMask(data);
				else
					WriteCompleteDynamicFieldUpdateMask(QuestSessionQuestLog.Size(), data);
			}

			if (changesMask[5])
			{
				if (!ignoreNestedChangesMask)
					ArenaCooldowns.WriteUpdateMask(data);
				else
					WriteCompleteDynamicFieldUpdateMask(ArenaCooldowns.Size(), data);
			}

			if (changesMask[6])
			{
				if (!ignoreNestedChangesMask)
					VisualItemReplacements.WriteUpdateMask(data);
				else
					WriteCompleteDynamicFieldUpdateMask(VisualItemReplacements.Size(), data);
			}
		}

		data.FlushBits();

		if (changesMask[0])
		{
			if (changesMask[3])
				for (var i = 0; i < Customizations.Size(); ++i)
					if (Customizations.HasChanged(i) || ignoreNestedChangesMask)
						Customizations[i].WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);

			if (changesMask[4])
				for (var i = 0; i < QuestSessionQuestLog.Size(); ++i)
					if (QuestSessionQuestLog.HasChanged(i) || ignoreNestedChangesMask)
					{
						if (noQuestLogChangesMask)
							QuestSessionQuestLog[i].WriteCreate(data, owner, receiver);
						else
							QuestSessionQuestLog[i].WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
					}

			if (changesMask[5])
				for (var i = 0; i < ArenaCooldowns.Size(); ++i)
					if (ArenaCooldowns.HasChanged(i) || ignoreNestedChangesMask)
						ArenaCooldowns[i].WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);

			if (changesMask[6])
				for (var i = 0; i < VisualItemReplacements.Size(); ++i)
					if (VisualItemReplacements.HasChanged(i) || ignoreNestedChangesMask)
						data.WriteInt32(VisualItemReplacements[i]);

			if (changesMask[7])
				data.WritePackedGuid(DuelArbiter);

			if (changesMask[8])
				data.WritePackedGuid(WowAccount);

			if (changesMask[9])
				data.WritePackedGuid(LootTargetGUID);

			if (changesMask[10])
				data.WriteUInt32(PlayerFlags);

			if (changesMask[11])
				data.WriteUInt32(PlayerFlagsEx);

			if (changesMask[12])
				data.WriteUInt32(GuildRankID);

			if (changesMask[13])
				data.WriteUInt32(GuildDeleteDate);

			if (changesMask[14])
				data.WriteUInt32(GuildLevel);

			if (changesMask[15])
				data.WriteUInt8(PartyType);

			if (changesMask[16])
				data.WriteUInt8(NativeSex);

			if (changesMask[17])
				data.WriteUInt8(Inebriation);

			if (changesMask[18])
				data.WriteUInt8(PvpTitle);

			if (changesMask[19])
				data.WriteUInt8(ArenaFaction);

			if (changesMask[20])
				data.WriteUInt32(DuelTeam);

			if (changesMask[21])
				data.WriteInt32(GuildTimeStamp);

			if (changesMask[22])
				data.WriteUInt32(PlayerTitle);

			if (changesMask[23])
				data.WriteInt32(FakeInebriation);

			if (changesMask[24])
				data.WriteUInt32(VirtualPlayerRealm);

			if (changesMask[25])
				data.WriteUInt32(CurrentSpecID);

			if (changesMask[26])
				data.WriteInt32(TaxiMountAnimKitID);

			if (changesMask[27])
				data.WriteUInt8(CurrentBattlePetBreedQuality);

			if (changesMask[28])
				data.WriteUInt32(HonorLevel);

			if (changesMask[29])
				data.WriteInt64(LogoutTime);

			if (changesMask[30])
				data.WriteInt32(Field_B0);

			if (changesMask[31])
				data.WriteInt32(Field_B4);
		}

		if (changesMask[32])
		{
			if (changesMask[33])
				CtrOptions.GetValue().WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);

			if (changesMask[34])
				data.WriteInt32(CovenantID);

			if (changesMask[35])
				data.WriteInt32(SoulbindID);

			if (changesMask[36])
				DungeonScore.GetValue().Write(data);
		}

		if (changesMask[37])
			for (var i = 0; i < 125; ++i)
				if (changesMask[38 + i])
				{
					if (noQuestLogChangesMask)
						QuestLog[i].WriteCreate(data, owner, receiver);
					else
						QuestLog[i].WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
				}

		if (changesMask[163])
			for (var i = 0; i < 19; ++i)
				if (changesMask[164 + i])
					VisibleItems[i].WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);

		if (changesMask[183])
			for (var i = 0; i < 6; ++i)
				if (changesMask[184 + i])
					data.WriteFloat(AvgItemLevel[i]);

		data.FlushBits();
	}

	public override void ClearChangesMask()
	{
		ClearChangesMask(HasQuestSession);
		ClearChangesMask(HasLevelLink);
		ClearChangesMask(Customizations);
		ClearChangesMask(QuestSessionQuestLog);
		ClearChangesMask(ArenaCooldowns);
		ClearChangesMask(VisualItemReplacements);
		ClearChangesMask(DuelArbiter);
		ClearChangesMask(WowAccount);
		ClearChangesMask(LootTargetGUID);
		ClearChangesMask(PlayerFlags);
		ClearChangesMask(PlayerFlagsEx);
		ClearChangesMask(GuildRankID);
		ClearChangesMask(GuildDeleteDate);
		ClearChangesMask(GuildLevel);
		ClearChangesMask(PartyType);
		ClearChangesMask(NativeSex);
		ClearChangesMask(Inebriation);
		ClearChangesMask(PvpTitle);
		ClearChangesMask(ArenaFaction);
		ClearChangesMask(DuelTeam);
		ClearChangesMask(GuildTimeStamp);
		ClearChangesMask(PlayerTitle);
		ClearChangesMask(FakeInebriation);
		ClearChangesMask(VirtualPlayerRealm);
		ClearChangesMask(CurrentSpecID);
		ClearChangesMask(TaxiMountAnimKitID);
		ClearChangesMask(CurrentBattlePetBreedQuality);
		ClearChangesMask(HonorLevel);
		ClearChangesMask(LogoutTime);
		ClearChangesMask(Field_B0);
		ClearChangesMask(Field_B4);
		ClearChangesMask(CtrOptions);
		ClearChangesMask(CovenantID);
		ClearChangesMask(SoulbindID);
		ClearChangesMask(DungeonScore);
		ClearChangesMask(QuestLog);
		ClearChangesMask(VisibleItems);
		ClearChangesMask(AvgItemLevel);
		ChangesMask.ResetAll();
	}

    private bool IsQuestLogChangesMaskSkipped()
	{
		return false;
	} // bandwidth savings aren't worth the cpu time
}