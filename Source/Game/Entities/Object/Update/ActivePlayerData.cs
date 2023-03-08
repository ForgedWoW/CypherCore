using Framework.Constants;
using Game.Networking;
using Game.Networking.Packets;

namespace Game.Entities;

public class ActivePlayerData : BaseUpdateData<Player>
{
	public static int ExploredZonesSize;
	public static int ExploredZonesBits;
	public static int QuestCompletedBitsSize;
	public static int QuestCompletedBitsPerBlock;

	public UpdateField<bool> BackpackAutoSortDisabled = new(0, 1);
	public UpdateField<bool> BankAutoSortDisabled = new(0, 2);
	public UpdateField<bool> SortBagsRightToLeft = new(0, 3);
	public UpdateField<bool> InsertItemsLeftToRight = new(0, 4);
	public UpdateField<bool> HasPerksProgramPendingReward = new(0, 5);
	public UpdateFieldArray<DynamicUpdateField<Research>> Research = new(1, 32, 33);
	public DynamicUpdateField<ulong> KnownTitles = new(0, 6);
	public DynamicUpdateField<ushort> ResearchSites = new(0, 8);
	public DynamicUpdateField<uint> ResearchSiteProgress = new(0, 9);
	public DynamicUpdateField<uint> DailyQuestsCompleted = new(0, 10);
	public DynamicUpdateField<int> AvailableQuestLineXQuestIDs = new(0, 11);
	public DynamicUpdateField<uint> Heirlooms = new(0, 12);
	public DynamicUpdateField<uint> HeirloomFlags = new(0, 13);
	public DynamicUpdateField<uint> Toys = new(0, 14);
	public DynamicUpdateField<uint> ToyFlags = new(0, 15);
	public DynamicUpdateField<uint> Transmog = new(0, 16);
	public DynamicUpdateField<uint> ConditionalTransmog = new(0, 17);
	public DynamicUpdateField<uint> SelfResSpells = new(0, 18);
	public DynamicUpdateField<uint> RuneforgePowers = new(0, 19);
	public DynamicUpdateField<uint> TransmogIllusions = new(0, 20);
	public DynamicUpdateField<SpellPctModByLabel> SpellPctModByLabel = new(0, 22);
	public DynamicUpdateField<SpellFlatModByLabel> SpellFlatModByLabel = new(0, 23);
	public DynamicUpdateField<MawPower> MawPowers = new(0, 24);
	public DynamicUpdateField<MultiFloorExplore> MultiFloorExploration = new(0, 25);
	public DynamicUpdateField<RecipeProgressionInfo> RecipeProgression = new(0, 26);
	public DynamicUpdateField<ReplayedQuest> ReplayedQuests = new(0, 27);
	public DynamicUpdateField<int> DisabledSpells = new(0, 28);
	public DynamicUpdateField<PersonalCraftingOrderCount> PersonalCraftingOrderCounts = new(0, 31);
	public DynamicUpdateField<PVPInfo> PvpInfo = new(0, 7);
	public DynamicUpdateField<CharacterRestriction> CharacterRestrictions = new(0, 21);
	public DynamicUpdateField<TraitConfig> TraitConfigs = new(0, 29);
	public DynamicUpdateField<CraftingOrder> CraftingOrders = new(0, 30);
	public UpdateField<ObjectGuid> FarsightObject = new(34, 35);
	public UpdateField<ObjectGuid> SummonedBattlePetGUID = new(34, 36);
	public UpdateField<ulong> Coinage = new(34, 37);
	public UpdateField<uint> XP = new(34, 38);
	public UpdateField<uint> NextLevelXP = new(34, 39);
	public UpdateField<int> TrialXP = new(34, 40);
	public UpdateField<SkillInfo> Skill = new(34, 41);
	public UpdateField<uint> CharacterPoints = new(34, 42);
	public UpdateField<uint> MaxTalentTiers = new(34, 43);
	public UpdateField<uint> TrackCreatureMask = new(34, 44);
	public UpdateField<float> MainhandExpertise = new(34, 45);
	public UpdateField<float> OffhandExpertise = new(34, 46);
	public UpdateField<float> RangedExpertise = new(34, 47);
	public UpdateField<float> CombatRatingExpertise = new(34, 48);
	public UpdateField<float> BlockPercentage = new(34, 49);
	public UpdateField<float> DodgePercentage = new(34, 50);
	public UpdateField<float> DodgePercentageFromAttribute = new(34, 51);
	public UpdateField<float> ParryPercentage = new(34, 52);
	public UpdateField<float> ParryPercentageFromAttribute = new(34, 53);
	public UpdateField<float> CritPercentage = new(34, 54);
	public UpdateField<float> RangedCritPercentage = new(34, 55);
	public UpdateField<float> OffhandCritPercentage = new(34, 56);
	public UpdateField<float> SpellCritPercentage = new(34, 57);
	public UpdateField<uint> ShieldBlock = new(34, 58);
	public UpdateField<float> ShieldBlockCritPercentage = new(34, 59);
	public UpdateField<float> Mastery = new(34, 60);
	public UpdateField<float> Speed = new(34, 61);
	public UpdateField<float> Avoidance = new(34, 62);
	public UpdateField<float> Sturdiness = new(34, 63);
	public UpdateField<int> Versatility = new(34, 64);
	public UpdateField<float> VersatilityBonus = new(34, 65);
	public UpdateField<float> PvpPowerDamage = new(66, 67);
	public UpdateField<float> PvpPowerHealing = new(66, 68);
	public UpdateField<int> ModHealingDonePos = new(66, 69);
	public UpdateField<float> ModHealingPercent = new(66, 70);
	public UpdateField<float> ModPeriodicHealingDonePercent = new(66, 71);
	public UpdateField<float> ModSpellPowerPercent = new(66, 72);
	public UpdateField<float> ModResiliencePercent = new(66, 73);
	public UpdateField<float> OverrideSpellPowerByAPPercent = new(66, 74);
	public UpdateField<float> OverrideAPBySpellPowerPercent = new(66, 75);
	public UpdateField<int> ModTargetResistance = new(66, 76);
	public UpdateField<int> ModTargetPhysicalResistance = new(66, 77);
	public UpdateField<uint> LocalFlags = new(66, 78);
	public UpdateField<byte> GrantableLevels = new(66, 79);
	public UpdateField<byte> MultiActionBars = new(66, 80);
	public UpdateField<byte> LifetimeMaxRank = new(66, 81);
	public UpdateField<byte> NumRespecs = new(66, 82);
	public UpdateField<uint> PvpMedals = new(66, 83);
	public UpdateField<ushort> TodayHonorableKills = new(66, 84);
	public UpdateField<ushort> YesterdayHonorableKills = new(66, 85);
	public UpdateField<uint> LifetimeHonorableKills = new(66, 86);
	public UpdateField<uint> WatchedFactionIndex = new(66, 87);
	public UpdateField<int> MaxLevel = new(66, 88);
	public UpdateField<int> ScalingPlayerLevelDelta = new(66, 89);
	public UpdateField<int> MaxCreatureScalingLevel = new(66, 90);
	public UpdateField<uint> PetSpellPower = new(66, 91);
	public UpdateField<float> UiHitModifier = new(66, 92);
	public UpdateField<float> UiSpellHitModifier = new(66, 93);
	public UpdateField<int> HomeRealmTimeOffset = new(66, 94);
	public UpdateField<float> ModPetHaste = new(66, 95);
	public UpdateField<sbyte> JailersTowerLevelMax = new(66, 96);
	public UpdateField<sbyte> JailersTowerLevel = new(66, 97);
	public UpdateField<byte> LocalRegenFlags = new(98, 99);
	public UpdateField<byte> AuraVision = new(98, 100);
	public UpdateField<byte> NumBackpackSlots = new(98, 101);
	public UpdateField<uint> OverrideSpellsID = new(98, 102);
	public UpdateField<ushort> LootSpecID = new(98, 103);
	public UpdateField<uint> OverrideZonePVPType = new(98, 104);
	public UpdateField<ObjectGuid> BnetAccount = new(98, 105);
	public UpdateField<ulong> GuildClubMemberID = new(98, 106);
	public UpdateField<uint> Honor = new(98, 107);
	public UpdateField<uint> HonorNextLevel = new(98, 108);
	public UpdateField<int> PerksProgramCurrency = new(98, 109);
	public UpdateField<byte> NumBankSlots = new(98, 110);
	public UpdateField<PerksVendorItem> FrozenPerksVendorItem = new(98, 111);
	public UpdateField<ActivePlayerUnk901> Field_1410 = new(98, 113);
	public OptionalUpdateField<QuestSession> QuestSession = new(98, 112);
	public UpdateField<int> UiChromieTimeExpansionID = new(98, 114);
	public UpdateField<int> TransportServerTime = new(98, 115);
	public UpdateField<uint> WeeklyRewardsPeriodSinceOrigin = new(98, 116); // week count since Cfg_RegionsEntry::ChallengeOrigin
	public UpdateField<short> DEBUGSoulbindConduitRank = new(98, 117);
	public UpdateField<DungeonScoreData> DungeonScore = new(98, 118);
	public UpdateField<uint> ActiveCombatTraitConfigID = new(98, 119);
	public UpdateFieldArray<ObjectGuid> InvSlots = new(218, 120, 121);
	public UpdateFieldArray<ulong> ExploredZones = new(240, 339, 340);
	public UpdateFieldArray<RestInfo> RestInfo = new(2, 580, 581);
	public UpdateFieldArray<int> ModDamageDonePos = new(7, 583, 584);
	public UpdateFieldArray<int> ModDamageDoneNeg = new(7, 583, 591);
	public UpdateFieldArray<float> ModDamageDonePercent = new(7, 583, 598);
	public UpdateFieldArray<float> ModHealingDonePercent = new(7, 583, 605);
	public UpdateFieldArray<float> WeaponDmgMultipliers = new(3, 612, 613);
	public UpdateFieldArray<float> WeaponAtkSpeedMultipliers = new(3, 612, 616);
	public UpdateFieldArray<uint> BuybackPrice = new(12, 619, 620);
	public UpdateFieldArray<ulong> BuybackTimestamp = new(12, 619, 632);
	public UpdateFieldArray<uint> CombatRatings = new(32, 644, 645);
	public UpdateFieldArray<uint> NoReagentCostMask = new(4, 677, 678);
	public UpdateFieldArray<uint> ProfessionSkillLine = new(2, 682, 683);
	public UpdateFieldArray<uint> BagSlotFlags = new(5, 685, 686);
	public UpdateFieldArray<uint> BankBagSlotFlags = new(7, 691, 692);
	public UpdateFieldArray<ulong> QuestCompleted = new(875, 699, 700);

	public ActivePlayerData() : base(0, TypeId.ActivePlayer, 1575)
	{
		ExploredZonesSize = ExploredZones.GetSize();
		ExploredZonesBits = sizeof(ulong) * 8;

		QuestCompletedBitsSize     = QuestCompleted.GetSize();
		QuestCompletedBitsPerBlock = sizeof(ulong) * 8;
	}

	public void WriteCreate(WorldPacket data, UpdateFieldFlag fieldVisibilityFlags, Player owner, Player receiver)
	{
		for (int i = 0; i < 218; ++i)
		{
			data.WritePackedGuid(InvSlots[i]);
		}
		data.WritePackedGuid(FarsightObject);
		data.WritePackedGuid(SummonedBattlePetGUID);
		data.WriteInt32(KnownTitles.Size());
		data.WriteUInt64(Coinage);
		data.WriteUInt32(XP);
		data.WriteUInt32(NextLevelXP);
		data.WriteInt32(TrialXP);
		((SkillInfo)Skill).WriteCreate(data, owner, receiver);
		data.WriteUInt32(CharacterPoints);
		data.WriteUInt32(MaxTalentTiers);
		data.WriteUInt32(TrackCreatureMask);
		data.WriteFloat(MainhandExpertise);
		data.WriteFloat(OffhandExpertise);
		data.WriteFloat(RangedExpertise);
		data.WriteFloat(CombatRatingExpertise);
		data.WriteFloat(BlockPercentage);
		data.WriteFloat(DodgePercentage);
		data.WriteFloat(DodgePercentageFromAttribute);
		data.WriteFloat(ParryPercentage);
		data.WriteFloat(ParryPercentageFromAttribute);
		data.WriteFloat(CritPercentage);
		data.WriteFloat(RangedCritPercentage);
		data.WriteFloat(OffhandCritPercentage);
		data.WriteFloat(SpellCritPercentage);
		data.WriteUInt32(ShieldBlock);
		data.WriteFloat(ShieldBlockCritPercentage);
		data.WriteFloat(Mastery);
		data.WriteFloat(Speed);
		data.WriteFloat(Avoidance);
		data.WriteFloat(Sturdiness);
		data.WriteInt32(Versatility);
		data.WriteFloat(VersatilityBonus);
		data.WriteFloat(PvpPowerDamage);
		data.WriteFloat(PvpPowerHealing);
		for (int i = 0; i < 240; ++i)
		{
			data.WriteUInt64(ExploredZones[i]);
		}
		for (int i = 0; i < 2; ++i)
		{
			RestInfo[i].WriteCreate(data, owner, receiver);
		}
		for (int i = 0; i < 7; ++i)
		{
			data.WriteInt32(ModDamageDonePos[i]);
			data.WriteInt32(ModDamageDoneNeg[i]);
			data.WriteFloat(ModDamageDonePercent[i]);
			data.WriteFloat(ModHealingDonePercent[i]);
		}
		data.WriteInt32(ModHealingDonePos);
		data.WriteFloat(ModHealingPercent);
		data.WriteFloat(ModPeriodicHealingDonePercent);
		for (int i = 0; i < 3; ++i)
		{
			data.WriteFloat(WeaponDmgMultipliers[i]);
			data.WriteFloat(WeaponAtkSpeedMultipliers[i]);
		}
		data.WriteFloat(ModSpellPowerPercent);
		data.WriteFloat(ModResiliencePercent);
		data.WriteFloat(OverrideSpellPowerByAPPercent);
		data.WriteFloat(OverrideAPBySpellPowerPercent);
		data.WriteInt32(ModTargetResistance);
		data.WriteInt32(ModTargetPhysicalResistance);
		data.WriteUInt32(LocalFlags);
		data.WriteUInt8(GrantableLevels);
		data.WriteUInt8(MultiActionBars);
		data.WriteUInt8(LifetimeMaxRank);
		data.WriteUInt8(NumRespecs);
		data.WriteUInt32(PvpMedals);
		for (int i = 0; i < 12; ++i)
		{
			data.WriteUInt32(BuybackPrice[i]);
			data.WriteUInt64(BuybackTimestamp[i]);
		}
		data.WriteUInt16(TodayHonorableKills);
		data.WriteUInt16(YesterdayHonorableKills);
		data.WriteUInt32(LifetimeHonorableKills);
		data.WriteUInt32(WatchedFactionIndex);
		for (int i = 0; i < 32; ++i)
		{
			data.WriteUInt32(CombatRatings[i]);
		}
		data.WriteInt32(PvpInfo.Size());
		data.WriteInt32(MaxLevel);
		data.WriteInt32(ScalingPlayerLevelDelta);
		data.WriteInt32(MaxCreatureScalingLevel);
		for (int i = 0; i < 4; ++i)
		{
			data.WriteUInt32(NoReagentCostMask[i]);
		}
		data.WriteUInt32(PetSpellPower);
		for (int i = 0; i < 2; ++i)
		{
			data.WriteUInt32(ProfessionSkillLine[i]);
		}
		data.WriteFloat(UiHitModifier);
		data.WriteFloat(UiSpellHitModifier);
		data.WriteInt32(HomeRealmTimeOffset);
		data.WriteFloat(ModPetHaste);
		data.WriteInt8(JailersTowerLevelMax);
		data.WriteInt8(JailersTowerLevel);
		data.WriteUInt8(LocalRegenFlags);
		data.WriteUInt8(AuraVision);
		data.WriteUInt8(NumBackpackSlots);
		data.WriteUInt32(OverrideSpellsID);
		data.WriteUInt16(LootSpecID);
		data.WriteUInt32(OverrideZonePVPType);
		data.WritePackedGuid(BnetAccount);
		data.WriteUInt64(GuildClubMemberID);
		for (int i = 0; i < 5; ++i)
		{
			data.WriteUInt32(BagSlotFlags[i]);
		}
		for (int i = 0; i < 7; ++i)
		{
			data.WriteUInt32(BankBagSlotFlags[i]);
		}
		for (int i = 0; i < 875; ++i)
		{
			data.WriteUInt64(QuestCompleted[i]);
		}
		data.WriteUInt32(Honor);
		data.WriteUInt32(HonorNextLevel);
		data.WriteInt32(PerksProgramCurrency);
		data.WriteUInt8(NumBankSlots);
		data.WriteInt32(ResearchSites.Size());
		data.WriteInt32(ResearchSiteProgress.Size());
		data.WriteInt32(DailyQuestsCompleted.Size());
		data.WriteInt32(AvailableQuestLineXQuestIDs.Size());
		data.WriteInt32(Heirlooms.Size());
		data.WriteInt32(HeirloomFlags.Size());
		data.WriteInt32(Toys.Size());
		data.WriteInt32(ToyFlags.Size());
		data.WriteInt32(Transmog.Size());
		data.WriteInt32(ConditionalTransmog.Size());
		data.WriteInt32(SelfResSpells.Size());
		data.WriteInt32(RuneforgePowers.Size());
		data.WriteInt32(TransmogIllusions.Size());
		data.WriteInt32(CharacterRestrictions.Size());
		data.WriteInt32(SpellPctModByLabel.Size());
		data.WriteInt32(SpellFlatModByLabel.Size());
		for (int i = 0; i < 1; ++i)
		{
			data.WriteInt32(Research[i].Size());
			for (int j = 0; j < Research[i].Size(); ++j)
			{
				Research[i][j].WriteCreate(data, owner, receiver);
			}
		}
		data.WriteInt32(MawPowers.Size());
		data.WriteInt32(MultiFloorExploration.Size());
		data.WriteInt32(RecipeProgression.Size());
		data.WriteInt32(ReplayedQuests.Size());
		data.WriteInt32(DisabledSpells.Size());
		data.WriteInt32(UiChromieTimeExpansionID);
		data.WriteInt32(TransportServerTime);
		data.WriteUInt32(WeeklyRewardsPeriodSinceOrigin);
		data.WriteInt16(DEBUGSoulbindConduitRank);
		data.WriteInt32(TraitConfigs.Size());
		data.WriteUInt32(ActiveCombatTraitConfigID);
		data.WriteInt32(CraftingOrders.Size());
		data.WriteInt32(PersonalCraftingOrderCounts.Size());
		for (int i = 0; i < KnownTitles.Size(); ++i)
		{
			data.WriteUInt64(KnownTitles[i]);
		}
		for (int i = 0; i < ResearchSites.Size(); ++i)
		{
			data.WriteUInt16(ResearchSites[i]);
		}
		for (int i = 0; i < ResearchSiteProgress.Size(); ++i)
		{
			data.WriteUInt32(ResearchSiteProgress[i]);
		}
		for (int i = 0; i < DailyQuestsCompleted.Size(); ++i)
		{
			data.WriteUInt32(DailyQuestsCompleted[i]);
		}
		for (int i = 0; i < AvailableQuestLineXQuestIDs.Size(); ++i)
		{
			data.WriteInt32(AvailableQuestLineXQuestIDs[i]);
		}
		for (int i = 0; i < Heirlooms.Size(); ++i)
		{
			data.WriteUInt32(Heirlooms[i]);
		}
		for (int i = 0; i < HeirloomFlags.Size(); ++i)
		{
			data.WriteUInt32(HeirloomFlags[i]);
		}
		for (int i = 0; i < Toys.Size(); ++i)
		{
			data.WriteUInt32(Toys[i]);
		}
		for (int i = 0; i < ToyFlags.Size(); ++i)
		{
			data.WriteUInt32(ToyFlags[i]);
		}
		for (int i = 0; i < Transmog.Size(); ++i)
		{
			data.WriteUInt32(Transmog[i]);
		}
		for (int i = 0; i < ConditionalTransmog.Size(); ++i)
		{
			data.WriteUInt32(ConditionalTransmog[i]);
		}
		for (int i = 0; i < SelfResSpells.Size(); ++i)
		{
			data.WriteUInt32(SelfResSpells[i]);
		}
		for (int i = 0; i < RuneforgePowers.Size(); ++i)
		{
			data.WriteUInt32(RuneforgePowers[i]);
		}
		for (int i = 0; i < TransmogIllusions.Size(); ++i)
		{
			data.WriteUInt32(TransmogIllusions[i]);
		}
		for (int i = 0; i < SpellPctModByLabel.Size(); ++i)
		{
			SpellPctModByLabel[i].WriteCreate(data, owner, receiver);
		}
		for (int i = 0; i < SpellFlatModByLabel.Size(); ++i)
		{
			SpellFlatModByLabel[i].WriteCreate(data, owner, receiver);
		}
		for (int i = 0; i < MawPowers.Size(); ++i)
		{
			MawPowers[i].WriteCreate(data, owner, receiver);
		}
		for (int i = 0; i < MultiFloorExploration.Size(); ++i)
		{
			MultiFloorExploration[i].WriteCreate(data, owner, receiver);
		}
		for (int i = 0; i < RecipeProgression.Size(); ++i)
		{
			RecipeProgression[i].WriteCreate(data, owner, receiver);
		}
		for (int i = 0; i < ReplayedQuests.Size(); ++i)
		{
			ReplayedQuests[i].WriteCreate(data, owner, receiver);
		}
		for (int i = 0; i < DisabledSpells.Size(); ++i)
		{
			data.WriteInt32(DisabledSpells[i]);
		}
		for (int i = 0; i < PersonalCraftingOrderCounts.Size(); ++i)
		{
			PersonalCraftingOrderCounts[i].WriteCreate(data, owner, receiver);
		}
		data.FlushBits();
		data.WriteBit(BackpackAutoSortDisabled);
		data.WriteBit(BankAutoSortDisabled);
		data.WriteBit(SortBagsRightToLeft);
		data.WriteBit(InsertItemsLeftToRight);
		data.WriteBit(HasPerksProgramPendingReward);
		data.WriteBits(QuestSession.HasValue(), 1);
		FrozenPerksVendorItem.GetValue().Write(data);
		if (QuestSession.HasValue())
		{
			QuestSession.GetValue().WriteCreate(data, owner, receiver);
		}
		((ActivePlayerUnk901)Field_1410).WriteCreate(data, owner, receiver);
		DungeonScore.Value.Write(data);
		for (int i = 0; i < PvpInfo.Size(); ++i)
		{
			PvpInfo[i].WriteCreate(data, owner, receiver);
		}
		for (int i = 0; i < CharacterRestrictions.Size(); ++i)
		{
			CharacterRestrictions[i].WriteCreate(data, owner, receiver);
		}
		for (int i = 0; i < TraitConfigs.Size(); ++i)
		{
			TraitConfigs[i].WriteCreate(data, owner, receiver);
		}
		for (int i = 0; i < CraftingOrders.Size(); ++i)
		{
			CraftingOrders[i].WriteCreate(data, owner, receiver);
		}
		data.FlushBits();
	}

	public void WriteUpdate(WorldPacket data, UpdateFieldFlag fieldVisibilityFlags, Player owner, Player receiver)
	{
		WriteUpdate(data, ChangesMask, false, owner, receiver);
	}

	public void WriteUpdate(WorldPacket data, UpdateMask changesMask, bool ignoreNestedChangesMask, Player owner, Player receiver)
	{
		for (uint i = 0; i < 1; ++i)
			data.WriteUInt32(changesMask.GetBlocksMask(i));
		data.WriteBits(changesMask.GetBlocksMask(1), 18);
		for (uint i = 0; i < 50; ++i)
			if (changesMask.GetBlock(i) != 0)
				data.WriteBits(changesMask.GetBlock(i), 32);

		if (changesMask[0])
		{
			if (changesMask[1])
			{
				data.WriteBit(BackpackAutoSortDisabled);
			}
			if (changesMask[2])
			{
				data.WriteBit(BankAutoSortDisabled);
			}
			if (changesMask[3])
			{
				data.WriteBit(SortBagsRightToLeft);
			}
			if (changesMask[4])
			{
				data.WriteBit(InsertItemsLeftToRight);
			}
			if (changesMask[5])
			{
				data.WriteBit(HasPerksProgramPendingReward);
			}
			if (changesMask[6])
			{
				if (!ignoreNestedChangesMask)
					KnownTitles.WriteUpdateMask(data);
				else
					WriteCompleteDynamicFieldUpdateMask(KnownTitles.Size(), data);
			}
			if (changesMask[7])
			{
				if (!ignoreNestedChangesMask)
					PvpInfo.WriteUpdateMask(data);
				else
					WriteCompleteDynamicFieldUpdateMask(PvpInfo.Size(), data);
			}
			if (changesMask[8])
			{
				if (!ignoreNestedChangesMask)
					ResearchSites.WriteUpdateMask(data);
				else
					WriteCompleteDynamicFieldUpdateMask(ResearchSites.Size(), data);
			}
			if (changesMask[9])
			{
				if (!ignoreNestedChangesMask)
					ResearchSiteProgress.WriteUpdateMask(data);
				else
					WriteCompleteDynamicFieldUpdateMask(ResearchSiteProgress.Size(), data);
			}
			if (changesMask[10])
			{
				if (!ignoreNestedChangesMask)
					DailyQuestsCompleted.WriteUpdateMask(data);
				else
					WriteCompleteDynamicFieldUpdateMask(DailyQuestsCompleted.Size(), data);
			}
			if (changesMask[11])
			{
				if (!ignoreNestedChangesMask)
					AvailableQuestLineXQuestIDs.WriteUpdateMask(data);
				else
					WriteCompleteDynamicFieldUpdateMask(AvailableQuestLineXQuestIDs.Size(), data);
			}
			if (changesMask[12])
			{
				if (!ignoreNestedChangesMask)
					Heirlooms.WriteUpdateMask(data);
				else
					WriteCompleteDynamicFieldUpdateMask(Heirlooms.Size(), data);
			}
			if (changesMask[13])
			{
				if (!ignoreNestedChangesMask)
					HeirloomFlags.WriteUpdateMask(data);
				else
					WriteCompleteDynamicFieldUpdateMask(HeirloomFlags.Size(), data);
			}
			if (changesMask[14])
			{
				if (!ignoreNestedChangesMask)
					Toys.WriteUpdateMask(data);
				else
					WriteCompleteDynamicFieldUpdateMask(Toys.Size(), data);
			}
			if (changesMask[15])
			{
				if (!ignoreNestedChangesMask)
					ToyFlags.WriteUpdateMask(data);
				else
					WriteCompleteDynamicFieldUpdateMask(ToyFlags.Size(), data);
			}
			if (changesMask[16])
			{
				if (!ignoreNestedChangesMask)
					Transmog.WriteUpdateMask(data);
				else
					WriteCompleteDynamicFieldUpdateMask(Transmog.Size(), data);
			}
			if (changesMask[17])
			{
				if (!ignoreNestedChangesMask)
					ConditionalTransmog.WriteUpdateMask(data);
				else
					WriteCompleteDynamicFieldUpdateMask(ConditionalTransmog.Size(), data);
			}
			if (changesMask[18])
			{
				if (!ignoreNestedChangesMask)
					SelfResSpells.WriteUpdateMask(data);
				else
					WriteCompleteDynamicFieldUpdateMask(SelfResSpells.Size(), data);
			}
			if (changesMask[19])
			{
				if (!ignoreNestedChangesMask)
					RuneforgePowers.WriteUpdateMask(data);
				else
					WriteCompleteDynamicFieldUpdateMask(RuneforgePowers.Size(), data);
			}
			if (changesMask[20])
			{
				if (!ignoreNestedChangesMask)
					TransmogIllusions.WriteUpdateMask(data);
				else
					WriteCompleteDynamicFieldUpdateMask(TransmogIllusions.Size(), data);
			}
			if (changesMask[21])
			{
				if (!ignoreNestedChangesMask)
					CharacterRestrictions.WriteUpdateMask(data);
				else
					WriteCompleteDynamicFieldUpdateMask(CharacterRestrictions.Size(), data);
			}
			if (changesMask[22])
			{
				if (!ignoreNestedChangesMask)
					SpellPctModByLabel.WriteUpdateMask(data);
				else
					WriteCompleteDynamicFieldUpdateMask(SpellPctModByLabel.Size(), data);
			}
			if (changesMask[23])
			{
				if (!ignoreNestedChangesMask)
					SpellFlatModByLabel.WriteUpdateMask(data);
				else
					WriteCompleteDynamicFieldUpdateMask(SpellFlatModByLabel.Size(), data);
			}
		}
		if (changesMask[32])
		{
			for (int i = 0; i < 1; ++i)
			{
				if (changesMask[33 + i])
				{
					if (!ignoreNestedChangesMask)
						Research[i].WriteUpdateMask(data);
					else
						WriteCompleteDynamicFieldUpdateMask(Research[i].Size(), data);
					for (int j = 0; j < Research[i].Size(); ++j)
					{
						if (Research[i].HasChanged(j) || ignoreNestedChangesMask)
						{
							Research[i][j].WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
						}
					}
				}
			}
		}
		data.FlushBits();
		if (changesMask[0])
		{
			if (changesMask[24])
			{
				if (!ignoreNestedChangesMask)
					MawPowers.WriteUpdateMask(data);
				else
					WriteCompleteDynamicFieldUpdateMask(MawPowers.Size(), data);
			}
			if (changesMask[25])
			{
				if (!ignoreNestedChangesMask)
					MultiFloorExploration.WriteUpdateMask(data);
				else
					WriteCompleteDynamicFieldUpdateMask(MultiFloorExploration.Size(), data);
			}
			if (changesMask[26])
			{
				if (!ignoreNestedChangesMask)
					RecipeProgression.WriteUpdateMask(data);
				else
					WriteCompleteDynamicFieldUpdateMask(RecipeProgression.Size(), data);
			}
			if (changesMask[27])
			{
				if (!ignoreNestedChangesMask)
					ReplayedQuests.WriteUpdateMask(data);
				else
					WriteCompleteDynamicFieldUpdateMask(ReplayedQuests.Size(), data);
			}
			if (changesMask[28])
			{
				if (!ignoreNestedChangesMask)
					DisabledSpells.WriteUpdateMask(data);
				else
					WriteCompleteDynamicFieldUpdateMask(DisabledSpells.Size(), data);
			}
			if (changesMask[29])
			{
				if (!ignoreNestedChangesMask)
					TraitConfigs.WriteUpdateMask(data);
				else
					WriteCompleteDynamicFieldUpdateMask(TraitConfigs.Size(), data);
			}
			if (changesMask[30])
			{
				if (!ignoreNestedChangesMask)
					CraftingOrders.WriteUpdateMask(data);
				else
					WriteCompleteDynamicFieldUpdateMask(CraftingOrders.Size(), data);
			}
			if (changesMask[31])
			{
				if (!ignoreNestedChangesMask)
					PersonalCraftingOrderCounts.WriteUpdateMask(data);
				else
					WriteCompleteDynamicFieldUpdateMask(PersonalCraftingOrderCounts.Size(), data);
			}
		}
		data.FlushBits();
		if (changesMask[0])
		{
			if (changesMask[6])
			{
				for (int i = 0; i < KnownTitles.Size(); ++i)
				{
					if (KnownTitles.HasChanged(i) || ignoreNestedChangesMask)
					{
						data.WriteUInt64(KnownTitles[i]);
					}
				}
			}
			if (changesMask[8])
			{
				for (int i = 0; i < ResearchSites.Size(); ++i)
				{
					if (ResearchSites.HasChanged(i) || ignoreNestedChangesMask)
					{
						data.WriteUInt16(ResearchSites[i]);
					}
				}
			}
			if (changesMask[9])
			{
				for (int i = 0; i < ResearchSiteProgress.Size(); ++i)
				{
					if (ResearchSiteProgress.HasChanged(i) || ignoreNestedChangesMask)
					{
						data.WriteUInt32(ResearchSiteProgress[i]);
					}
				}
			}
			if (changesMask[10])
			{
				for (int i = 0; i < DailyQuestsCompleted.Size(); ++i)
				{
					if (DailyQuestsCompleted.HasChanged(i) || ignoreNestedChangesMask)
					{
						data.WriteUInt32(DailyQuestsCompleted[i]);
					}
				}
			}
			if (changesMask[11])
			{
				for (int i = 0; i < AvailableQuestLineXQuestIDs.Size(); ++i)
				{
					if (AvailableQuestLineXQuestIDs.HasChanged(i) || ignoreNestedChangesMask)
					{
						data.WriteInt32(AvailableQuestLineXQuestIDs[i]);
					}
				}
			}
			if (changesMask[12])
			{
				for (int i = 0; i < Heirlooms.Size(); ++i)
				{
					if (Heirlooms.HasChanged(i) || ignoreNestedChangesMask)
					{
						data.WriteUInt32(Heirlooms[i]);
					}
				}
			}
			if (changesMask[13])
			{
				for (int i = 0; i < HeirloomFlags.Size(); ++i)
				{
					if (HeirloomFlags.HasChanged(i) || ignoreNestedChangesMask)
					{
						data.WriteUInt32(HeirloomFlags[i]);
					}
				}
			}
			if (changesMask[14])
			{
				for (int i = 0; i < Toys.Size(); ++i)
				{
					if (Toys.HasChanged(i) || ignoreNestedChangesMask)
					{
						data.WriteUInt32(Toys[i]);
					}
				}
			}
			if (changesMask[15])
			{
				for (int i = 0; i < ToyFlags.Size(); ++i)
				{
					if (ToyFlags.HasChanged(i) || ignoreNestedChangesMask)
					{
						data.WriteUInt32(ToyFlags[i]);
					}
				}
			}
			if (changesMask[16])
			{
				for (int i = 0; i < Transmog.Size(); ++i)
				{
					if (Transmog.HasChanged(i) || ignoreNestedChangesMask)
					{
						data.WriteUInt32(Transmog[i]);
					}
				}
			}
			if (changesMask[17])
			{
				for (int i = 0; i < ConditionalTransmog.Size(); ++i)
				{
					if (ConditionalTransmog.HasChanged(i) || ignoreNestedChangesMask)
					{
						data.WriteUInt32(ConditionalTransmog[i]);
					}
				}
			}
			if (changesMask[18])
			{
				for (int i = 0; i < SelfResSpells.Size(); ++i)
				{
					if (SelfResSpells.HasChanged(i) || ignoreNestedChangesMask)
					{
						data.WriteUInt32(SelfResSpells[i]);
					}
				}
			}
			if (changesMask[19])
			{
				for (int i = 0; i < RuneforgePowers.Size(); ++i)
				{
					if (RuneforgePowers.HasChanged(i) || ignoreNestedChangesMask)
					{
						data.WriteUInt32(RuneforgePowers[i]);
					}
				}
			}
			if (changesMask[20])
			{
				for (int i = 0; i < TransmogIllusions.Size(); ++i)
				{
					if (TransmogIllusions.HasChanged(i) || ignoreNestedChangesMask)
					{
						data.WriteUInt32(TransmogIllusions[i]);
					}
				}
			}
			if (changesMask[22])
			{
				for (int i = 0; i < SpellPctModByLabel.Size(); ++i)
				{
					if (SpellPctModByLabel.HasChanged(i) || ignoreNestedChangesMask)
					{
						SpellPctModByLabel[i].WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
					}
				}
			}
			if (changesMask[23])
			{
				for (int i = 0; i < SpellFlatModByLabel.Size(); ++i)
				{
					if (SpellFlatModByLabel.HasChanged(i) || ignoreNestedChangesMask)
					{
						SpellFlatModByLabel[i].WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
					}
				}
			}
			if (changesMask[24])
			{
				for (int i = 0; i < MawPowers.Size(); ++i)
				{
					if (MawPowers.HasChanged(i) || ignoreNestedChangesMask)
					{
						MawPowers[i].WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
					}
				}
			}
			if (changesMask[25])
			{
				for (int i = 0; i < MultiFloorExploration.Size(); ++i)
				{
					if (MultiFloorExploration.HasChanged(i) || ignoreNestedChangesMask)
					{
						MultiFloorExploration[i].WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
					}
				}
			}
			if (changesMask[26])
			{
				for (int i = 0; i < RecipeProgression.Size(); ++i)
				{
					if (RecipeProgression.HasChanged(i) || ignoreNestedChangesMask)
					{
						RecipeProgression[i].WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
					}
				}
			}
			if (changesMask[27])
			{
				for (int i = 0; i < ReplayedQuests.Size(); ++i)
				{
					if (ReplayedQuests.HasChanged(i) || ignoreNestedChangesMask)
					{
						ReplayedQuests[i].WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
					}
				}
			}
			if (changesMask[28])
			{
				for (int i = 0; i < DisabledSpells.Size(); ++i)
				{
					if (DisabledSpells.HasChanged(i) || ignoreNestedChangesMask)
					{
						data.WriteInt32(DisabledSpells[i]);
					}
				}
			}
			if (changesMask[31])
			{
				for (int i = 0; i < PersonalCraftingOrderCounts.Size(); ++i)
				{
					if (PersonalCraftingOrderCounts.HasChanged(i) || ignoreNestedChangesMask)
					{
						PersonalCraftingOrderCounts[i].WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
					}
				}
			}
			if (changesMask[7])
			{
				for (int i = 0; i < PvpInfo.Size(); ++i)
				{
					if (PvpInfo.HasChanged(i) || ignoreNestedChangesMask)
					{
						PvpInfo[i].WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
					}
				}
			}
			if (changesMask[21])
			{
				for (int i = 0; i < CharacterRestrictions.Size(); ++i)
				{
					if (CharacterRestrictions.HasChanged(i) || ignoreNestedChangesMask)
					{
						CharacterRestrictions[i].WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
					}
				}
			}
			if (changesMask[29])
			{
				for (int i = 0; i < TraitConfigs.Size(); ++i)
				{
					if (TraitConfigs.HasChanged(i) || ignoreNestedChangesMask)
					{
						TraitConfigs[i].WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
					}
				}
			}
			if (changesMask[30])
			{
				for (int i = 0; i < CraftingOrders.Size(); ++i)
				{
					if (CraftingOrders.HasChanged(i) || ignoreNestedChangesMask)
					{
						CraftingOrders[i].WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
					}
				}
			}
		}
		if (changesMask[34])
		{
			if (changesMask[35])
			{
				data.WritePackedGuid(FarsightObject);
			}
			if (changesMask[36])
			{
				data.WritePackedGuid(SummonedBattlePetGUID);
			}
			if (changesMask[37])
			{
				data.WriteUInt64(Coinage);
			}
			if (changesMask[38])
			{
				data.WriteUInt32(XP);
			}
			if (changesMask[39])
			{
				data.WriteUInt32(NextLevelXP);
			}
			if (changesMask[40])
			{
				data.WriteInt32(TrialXP);
			}
			if (changesMask[41])
			{
				((SkillInfo)Skill).WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
			}
			if (changesMask[42])
			{
				data.WriteUInt32(CharacterPoints);
			}
			if (changesMask[43])
			{
				data.WriteUInt32(MaxTalentTiers);
			}
			if (changesMask[44])
			{
				data.WriteUInt32(TrackCreatureMask);
			}
			if (changesMask[45])
			{
				data.WriteFloat(MainhandExpertise);
			}
			if (changesMask[46])
			{
				data.WriteFloat(OffhandExpertise);
			}
			if (changesMask[47])
			{
				data.WriteFloat(RangedExpertise);
			}
			if (changesMask[48])
			{
				data.WriteFloat(CombatRatingExpertise);
			}
			if (changesMask[49])
			{
				data.WriteFloat(BlockPercentage);
			}
			if (changesMask[50])
			{
				data.WriteFloat(DodgePercentage);
			}
			if (changesMask[51])
			{
				data.WriteFloat(DodgePercentageFromAttribute);
			}
			if (changesMask[52])
			{
				data.WriteFloat(ParryPercentage);
			}
			if (changesMask[53])
			{
				data.WriteFloat(ParryPercentageFromAttribute);
			}
			if (changesMask[54])
			{
				data.WriteFloat(CritPercentage);
			}
			if (changesMask[55])
			{
				data.WriteFloat(RangedCritPercentage);
			}
			if (changesMask[56])
			{
				data.WriteFloat(OffhandCritPercentage);
			}
			if (changesMask[57])
			{
				data.WriteFloat(SpellCritPercentage);
			}
			if (changesMask[58])
			{
				data.WriteUInt32(ShieldBlock);
			}
			if (changesMask[59])
			{
				data.WriteFloat(ShieldBlockCritPercentage);
			}
			if (changesMask[60])
			{
				data.WriteFloat(Mastery);
			}
			if (changesMask[61])
			{
				data.WriteFloat(Speed);
			}
			if (changesMask[62])
			{
				data.WriteFloat(Avoidance);
			}
			if (changesMask[63])
			{
				data.WriteFloat(Sturdiness);
			}
			if (changesMask[64])
			{
				data.WriteInt32(Versatility);
			}
			if (changesMask[65])
			{
				data.WriteFloat(VersatilityBonus);
			}
		}
		if (changesMask[66])
		{
			if (changesMask[67])
			{
				data.WriteFloat(PvpPowerDamage);
			}
			if (changesMask[68])
			{
				data.WriteFloat(PvpPowerHealing);
			}
			if (changesMask[69])
			{
				data.WriteInt32(ModHealingDonePos);
			}
			if (changesMask[70])
			{
				data.WriteFloat(ModHealingPercent);
			}
			if (changesMask[71])
			{
				data.WriteFloat(ModPeriodicHealingDonePercent);
			}
			if (changesMask[72])
			{
				data.WriteFloat(ModSpellPowerPercent);
			}
			if (changesMask[73])
			{
				data.WriteFloat(ModResiliencePercent);
			}
			if (changesMask[74])
			{
				data.WriteFloat(OverrideSpellPowerByAPPercent);
			}
			if (changesMask[75])
			{
				data.WriteFloat(OverrideAPBySpellPowerPercent);
			}
			if (changesMask[76])
			{
				data.WriteInt32(ModTargetResistance);
			}
			if (changesMask[77])
			{
				data.WriteInt32(ModTargetPhysicalResistance);
			}
			if (changesMask[78])
			{
				data.WriteUInt32(LocalFlags);
			}
			if (changesMask[79])
			{
				data.WriteUInt8(GrantableLevels);
			}
			if (changesMask[80])
			{
				data.WriteUInt8(MultiActionBars);
			}
			if (changesMask[81])
			{
				data.WriteUInt32(LifetimeMaxRank);
			}
			if (changesMask[82])
			{
				data.WriteUInt16(NumRespecs);
			}
			if (changesMask[83])
			{
				data.WriteUInt32(PvpMedals);
			}
			if (changesMask[84])
			{
				data.WriteUInt32(TodayHonorableKills);
			}
			if (changesMask[85])
			{
				data.WriteUInt32(YesterdayHonorableKills);
			}
			if (changesMask[86])
			{
				data.WriteUInt32(LifetimeHonorableKills);
			}
			if (changesMask[87])
			{
				data.WriteUInt32(WatchedFactionIndex);
			}
			if (changesMask[88])
			{
				data.WriteInt32(MaxLevel);
			}
			if (changesMask[89])
			{
				data.WriteInt32(ScalingPlayerLevelDelta);
			}
			if (changesMask[90])
			{
				data.WriteInt32(MaxCreatureScalingLevel);
			}
			if (changesMask[91])
			{
				data.WriteUInt32(PetSpellPower);
			}
			if (changesMask[92])
			{
				data.WriteFloat(UiHitModifier);
			}
			if (changesMask[93])
			{
				data.WriteFloat(UiSpellHitModifier);
			}
			if (changesMask[94])
			{
				data.WriteInt32(HomeRealmTimeOffset);
			}
			if (changesMask[95])
			{
				data.WriteFloat(ModPetHaste);
			}
			if (changesMask[96])
			{
				data.WriteInt8(JailersTowerLevelMax);
			}
			if (changesMask[97])
			{
				data.WriteInt8(JailersTowerLevel);
			}
		}
		if (changesMask[98])
		{
			if (changesMask[99])
			{
				data.WriteUInt8(LocalRegenFlags);
			}
			if (changesMask[100])
			{
				data.WriteUInt32(AuraVision);
			}
			if (changesMask[101])
			{
				data.WriteUInt16(NumBackpackSlots);
			}
			if (changesMask[102])
			{
				data.WriteUInt32(OverrideSpellsID);
			}
			if (changesMask[103])
			{
				data.WriteUInt16(LootSpecID);
			}
			if (changesMask[104])
			{
				data.WriteUInt64(OverrideZonePVPType);
			}
			if (changesMask[105])
			{
				data.WritePackedGuid(BnetAccount);
			}
			if (changesMask[106])
			{
				data.WriteUInt64(GuildClubMemberID);
			}
			if (changesMask[107])
			{
				data.WriteUInt32(Honor);
			}
			if (changesMask[108])
			{
				data.WriteUInt32(HonorNextLevel);
			}
			if (changesMask[109])
			{
				data.WriteInt32(PerksProgramCurrency);
			}
			if (changesMask[110])
			{
				data.WriteInt32(NumBankSlots);
			}
			if (changesMask[114])
			{
				data.WriteInt32(UiChromieTimeExpansionID);
			}
			if (changesMask[115])
			{
				data.WriteInt32(TransportServerTime);
			}
			if (changesMask[116])
			{
				data.WriteUInt32(WeeklyRewardsPeriodSinceOrigin);
			}
			if (changesMask[117])
			{
				data.WriteInt16(DEBUGSoulbindConduitRank);
			}
			if (changesMask[119])
			{
				data.WriteUInt32(ActiveCombatTraitConfigID);
			}
		}
		if (changesMask[98])
		{
			data.WriteBits(QuestSession.HasValue(), 1);
			if (changesMask[111])
			{
				FrozenPerksVendorItem.GetValue().Write(data);
			}
			if (changesMask[112])
			{
				if (QuestSession.HasValue())
				{
					QuestSession.GetValue().WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
				}
			}
			if (changesMask[113])
			{
				((ActivePlayerUnk901)Field_1410).WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
			}
			if (changesMask[118])
			{
				DungeonScore.GetValue().Write(data);
			}
		}
		if (changesMask[120])
		{
			for (int i = 0; i < 218; ++i)
			{
				if (changesMask[121 + i])
				{
					data.WritePackedGuid(InvSlots[i]);
				}
			}
		}
		if (changesMask[339])
		{
			for (int i = 0; i < 240; ++i)
			{
				if (changesMask[340 + i])
				{
					data.WriteUInt64(ExploredZones[i]);
				}
			}
		}
		if (changesMask[580])
		{
			for (int i = 0; i < 2; ++i)
			{
				if (changesMask[581 + i])
				{
					RestInfo[i].WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
				}
			}
		}
		if (changesMask[583])
		{
			for (int i = 0; i < 7; ++i)
			{
				if (changesMask[584 + i])
				{
					data.WriteInt32(ModDamageDonePos[i]);
				}
				if (changesMask[591 + i])
				{
					data.WriteInt32(ModDamageDoneNeg[i]);
				}
				if (changesMask[598 + i])
				{
					data.WriteFloat(ModDamageDonePercent[i]);
				}
				if (changesMask[605 + i])
				{
					data.WriteFloat(ModHealingDonePercent[i]);
				}
			}
		}
		if (changesMask[612])
		{
			for (int i = 0; i < 3; ++i)
			{
				if (changesMask[613 + i])
				{
					data.WriteFloat(WeaponDmgMultipliers[i]);
				}
				if (changesMask[616 + i])
				{
					data.WriteFloat(WeaponAtkSpeedMultipliers[i]);
				}
			}
		}
		if (changesMask[619])
		{
			for (int i = 0; i < 12; ++i)
			{
				if (changesMask[620 + i])
				{
					data.WriteUInt32(BuybackPrice[i]);
				}
				if (changesMask[632 + i])
				{
					data.WriteUInt64(BuybackTimestamp[i]);
				}
			}
		}
		if (changesMask[644])
		{
			for (int i = 0; i < 32; ++i)
			{
				if (changesMask[645 + i])
				{
					data.WriteUInt32(CombatRatings[i]);
				}
			}
		}
		if (changesMask[677])
		{
			for (int i = 0; i < 4; ++i)
			{
				if (changesMask[678 + i])
				{
					data.WriteUInt32(NoReagentCostMask[i]);
				}
			}
		}
		if (changesMask[682])
		{
			for (int i = 0; i < 2; ++i)
			{
				if (changesMask[683 + i])
				{
					data.WriteUInt32(ProfessionSkillLine[i]);
				}
			}
		}
		if (changesMask[685])
		{
			for (int i = 0; i < 5; ++i)
			{
				if (changesMask[686 + i])
				{
					data.WriteUInt32(BagSlotFlags[i]);
				}
			}
		}
		if (changesMask[691])
		{
			for (int i = 0; i < 7; ++i)
			{
				if (changesMask[692 + i])
				{
					data.WriteUInt32(BankBagSlotFlags[i]);
				}
			}
		}
		if (changesMask[699])
		{
			for (int i = 0; i < 875; ++i)
			{
				if (changesMask[700 + i])
				{
					data.WriteUInt64(QuestCompleted[i]);
				}
			}
		}
		data.FlushBits();
	}

	public override void ClearChangesMask()
	{
		ClearChangesMask(BackpackAutoSortDisabled);
		ClearChangesMask(BankAutoSortDisabled);
		ClearChangesMask(SortBagsRightToLeft);
		ClearChangesMask(InsertItemsLeftToRight);
		ClearChangesMask(HasPerksProgramPendingReward);
		ClearChangesMask(Research);
		ClearChangesMask(KnownTitles);
		ClearChangesMask(ResearchSites);
		ClearChangesMask(ResearchSiteProgress);
		ClearChangesMask(DailyQuestsCompleted);
		ClearChangesMask(AvailableQuestLineXQuestIDs);
		ClearChangesMask(Heirlooms);
		ClearChangesMask(HeirloomFlags);
		ClearChangesMask(Toys);
		ClearChangesMask(ToyFlags);
		ClearChangesMask(Transmog);
		ClearChangesMask(ConditionalTransmog);
		ClearChangesMask(SelfResSpells);
		ClearChangesMask(RuneforgePowers);
		ClearChangesMask(TransmogIllusions);
		ClearChangesMask(SpellPctModByLabel);
		ClearChangesMask(SpellFlatModByLabel);
		ClearChangesMask(MawPowers);
		ClearChangesMask(MultiFloorExploration);
		ClearChangesMask(RecipeProgression);
		ClearChangesMask(ReplayedQuests);
		ClearChangesMask(DisabledSpells);
		ClearChangesMask(PersonalCraftingOrderCounts);
		ClearChangesMask(PvpInfo);
		ClearChangesMask(CharacterRestrictions);
		ClearChangesMask(TraitConfigs);
		ClearChangesMask(CraftingOrders);
		ClearChangesMask(FarsightObject);
		ClearChangesMask(SummonedBattlePetGUID);
		ClearChangesMask(Coinage);
		ClearChangesMask(XP);
		ClearChangesMask(NextLevelXP);
		ClearChangesMask(TrialXP);
		ClearChangesMask(Skill);
		ClearChangesMask(CharacterPoints);
		ClearChangesMask(MaxTalentTiers);
		ClearChangesMask(TrackCreatureMask);
		ClearChangesMask(MainhandExpertise);
		ClearChangesMask(OffhandExpertise);
		ClearChangesMask(RangedExpertise);
		ClearChangesMask(CombatRatingExpertise);
		ClearChangesMask(BlockPercentage);
		ClearChangesMask(DodgePercentage);
		ClearChangesMask(DodgePercentageFromAttribute);
		ClearChangesMask(ParryPercentage);
		ClearChangesMask(ParryPercentageFromAttribute);
		ClearChangesMask(CritPercentage);
		ClearChangesMask(RangedCritPercentage);
		ClearChangesMask(OffhandCritPercentage);
		ClearChangesMask(SpellCritPercentage);
		ClearChangesMask(ShieldBlock);
		ClearChangesMask(ShieldBlockCritPercentage);
		ClearChangesMask(Mastery);
		ClearChangesMask(Speed);
		ClearChangesMask(Avoidance);
		ClearChangesMask(Sturdiness);
		ClearChangesMask(Versatility);
		ClearChangesMask(VersatilityBonus);
		ClearChangesMask(PvpPowerDamage);
		ClearChangesMask(PvpPowerHealing);
		ClearChangesMask(ModHealingDonePos);
		ClearChangesMask(ModHealingPercent);
		ClearChangesMask(ModPeriodicHealingDonePercent);
		ClearChangesMask(ModSpellPowerPercent);
		ClearChangesMask(ModResiliencePercent);
		ClearChangesMask(OverrideSpellPowerByAPPercent);
		ClearChangesMask(OverrideAPBySpellPowerPercent);
		ClearChangesMask(ModTargetResistance);
		ClearChangesMask(ModTargetPhysicalResistance);
		ClearChangesMask(LocalFlags);
		ClearChangesMask(GrantableLevels);
		ClearChangesMask(MultiActionBars);
		ClearChangesMask(LifetimeMaxRank);
		ClearChangesMask(NumRespecs);
		ClearChangesMask(PvpMedals);
		ClearChangesMask(TodayHonorableKills);
		ClearChangesMask(YesterdayHonorableKills);
		ClearChangesMask(LifetimeHonorableKills);
		ClearChangesMask(WatchedFactionIndex);
		ClearChangesMask(MaxLevel);
		ClearChangesMask(ScalingPlayerLevelDelta);
		ClearChangesMask(MaxCreatureScalingLevel);
		ClearChangesMask(PetSpellPower);
		ClearChangesMask(UiHitModifier);
		ClearChangesMask(UiSpellHitModifier);
		ClearChangesMask(HomeRealmTimeOffset);
		ClearChangesMask(ModPetHaste);
		ClearChangesMask(JailersTowerLevelMax);
		ClearChangesMask(JailersTowerLevel);
		ClearChangesMask(LocalRegenFlags);
		ClearChangesMask(AuraVision);
		ClearChangesMask(NumBackpackSlots);
		ClearChangesMask(OverrideSpellsID);
		ClearChangesMask(LootSpecID);
		ClearChangesMask(OverrideZonePVPType);
		ClearChangesMask(BnetAccount);
		ClearChangesMask(GuildClubMemberID);
		ClearChangesMask(Honor);
		ClearChangesMask(HonorNextLevel);
		ClearChangesMask(PerksProgramCurrency);
		ClearChangesMask(NumBankSlots);
		ClearChangesMask(FrozenPerksVendorItem);
		ClearChangesMask(Field_1410);
		ClearChangesMask(QuestSession);
		ClearChangesMask(UiChromieTimeExpansionID);
		ClearChangesMask(TransportServerTime);
		ClearChangesMask(WeeklyRewardsPeriodSinceOrigin);
		ClearChangesMask(DEBUGSoulbindConduitRank);
		ClearChangesMask(DungeonScore);
		ClearChangesMask(ActiveCombatTraitConfigID);
		ClearChangesMask(InvSlots);
		ClearChangesMask(ExploredZones);
		ClearChangesMask(RestInfo);
		ClearChangesMask(ModDamageDonePos);
		ClearChangesMask(ModDamageDoneNeg);
		ClearChangesMask(ModDamageDonePercent);
		ClearChangesMask(ModHealingDonePercent);
		ClearChangesMask(WeaponDmgMultipliers);
		ClearChangesMask(WeaponAtkSpeedMultipliers);
		ClearChangesMask(BuybackPrice);
		ClearChangesMask(BuybackTimestamp);
		ClearChangesMask(CombatRatings);
		ClearChangesMask(NoReagentCostMask);
		ClearChangesMask(ProfessionSkillLine);
		ClearChangesMask(BagSlotFlags);
		ClearChangesMask(BankBagSlotFlags);
		ClearChangesMask(QuestCompleted);
		ChangesMask.ResetAll();
	}
}