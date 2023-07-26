// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Players;
using Forged.MapServer.Networking;
using Forged.MapServer.Networking.Packets.MythicPlus;
using Forged.MapServer.Networking.Packets.PerksPorgram;
using Framework.Constants;
using System.Collections.Generic;

namespace Forged.MapServer.Entities.Objects.Update;

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
    public DynamicUpdateField<DynamicUpdateField<ushort>> ResearchSites = new(35, 36);
    public DynamicUpdateField<DynamicUpdateField<uint>> ResearchSiteProgress = new(37, 38);
    public UpdateFieldArray<DynamicUpdateField<Research>> Research = new(1, 39, 40);
    public DynamicUpdateField<ulong> KnownTitles = new(0, 6);
    public DynamicUpdateField<uint> DailyQuestsCompleted = new(0, 8);
    public DynamicUpdateField<int> AvailableQuestLineXQuestIDs = new(0, 9);
    public DynamicUpdateField<uint> Heirlooms = new(0, 10);
    public DynamicUpdateField<uint> HeirloomFlags = new(0, 11);
    public DynamicUpdateField<uint> Toys = new(0, 12);
    public DynamicUpdateField<uint> ToyFlags = new(0, 13);
    public DynamicUpdateField<uint> Transmog = new(0, 14);
    public DynamicUpdateField<uint> ConditionalTransmog = new(0, 15);
    public DynamicUpdateField<uint> SelfResSpells = new(0, 16);
    public DynamicUpdateField<uint> RuneforgePowers = new(0, 17);
    public DynamicUpdateField<uint> TransmogIllusions = new(0, 18);
    public DynamicUpdateField<SpellPctModByLabel> SpellPctModByLabel = new(0, 20);
    public DynamicUpdateField<SpellFlatModByLabel> SpellFlatModByLabel = new(0, 21);
    public DynamicUpdateField<MawPower> MawPowers = new(0, 22);
    public DynamicUpdateField<MultiFloorExplore> MultiFloorExploration = new(0, 23);
    public DynamicUpdateField<RecipeProgressionInfo> RecipeProgression = new(0, 24);
    public DynamicUpdateField<ReplayedQuest> ReplayedQuests = new(0, 25);
    public DynamicUpdateField<QuestLog> TaskQuests = new(0, 26);
    public DynamicUpdateField<int> DisabledSpells = new(0, 27);
    public DynamicUpdateField<PersonalCraftingOrderCount> PersonalCraftingOrderCounts = new(0, 30);
    public DynamicUpdateField<CategoryCooldownMod> CategoryCooldownMods = new(0, 31);
    public DynamicUpdateField<WeeklySpellUse> WeeklySpellUses = new(32, 33);
    public DynamicUpdateField<long> TrackedCollectableSources = new(32, 34);
    public DynamicUpdateField<PVPInfo> PvpInfo = new(0, 7);
    public DynamicUpdateField<CharacterRestriction> CharacterRestrictions = new(0, 19);
    public DynamicUpdateField<TraitConfig> TraitConfigs = new(0, 28);
    public DynamicUpdateField<CraftingOrder> CraftingOrders = new(0, 29);
    public UpdateField<ObjectGuid> FarsightObject = new(32, 41);
    public UpdateField<ObjectGuid> SummonedBattlePetGUID = new(32, 42);
    public UpdateField<ulong> Coinage = new(32, 43);
    public UpdateField<uint> XP = new(32, 44);
    public UpdateField<uint> NextLevelXP = new(32, 45);
    public UpdateField<int> TrialXP = new(32, 46);
    public UpdateField<SkillInfo> Skill = new(32, 47);
    public UpdateField<uint> CharacterPoints = new(32, 48);
    public UpdateField<uint> MaxTalentTiers = new(32, 49);
    public UpdateField<uint> TrackCreatureMask = new(32, 50);
    public UpdateField<float> MainhandExpertise = new(32, 51);
    public UpdateField<float> OffhandExpertise = new(32, 52);
    public UpdateField<float> RangedExpertise = new(32, 53);
    public UpdateField<float> CombatRatingExpertise = new(32, 54);
    public UpdateField<float> BlockPercentage = new(32, 55);
    public UpdateField<float> DodgePercentage = new(32, 56);
    public UpdateField<float> DodgePercentageFromAttribute = new(32, 57);
    public UpdateField<float> ParryPercentage = new(32, 58);
    public UpdateField<float> ParryPercentageFromAttribute = new(32, 59);
    public UpdateField<float> CritPercentage = new(32, 60);
    public UpdateField<float> RangedCritPercentage = new(32, 61);
    public UpdateField<float> OffhandCritPercentage = new(32, 62);
    public UpdateField<float> SpellCritPercentage = new(32, 63);
    public UpdateField<uint> ShieldBlock = new(32, 64);
    public UpdateField<float> ShieldBlockCritPercentage = new(32, 65);
    public UpdateField<float> Mastery = new(32, 66);
    public UpdateField<float> Speed = new(32, 67);
    public UpdateField<float> Avoidance = new(32, 68);
    public UpdateField<float> Sturdiness = new(32, 69);
    public UpdateField<int> Versatility = new(70, 71);
    public UpdateField<float> VersatilityBonus = new(70, 72);
    public UpdateField<float> PvpPowerDamage = new(70, 73);
    public UpdateField<float> PvpPowerHealing = new(70, 74);
    public UpdateField<int> ModHealingDonePos = new(70, 75);
    public UpdateField<float> ModHealingPercent = new(70, 76);
    public UpdateField<float> ModPeriodicHealingDonePercent = new(70, 77);
    public UpdateField<float> ModSpellPowerPercent = new(70, 78);
    public UpdateField<float> ModResiliencePercent = new(70, 79);
    public UpdateField<float> OverrideSpellPowerByAPPercent = new(70, 80);
    public UpdateField<float> OverrideAPBySpellPowerPercent = new(70, 81);
    public UpdateField<int> ModTargetResistance = new(70, 82);
    public UpdateField<int> ModTargetPhysicalResistance = new(70, 83);
    public UpdateField<uint> LocalFlags = new(70, 84);
    public UpdateField<byte> GrantableLevels = new(70, 85);
    public UpdateField<byte> MultiActionBars = new(70, 86);
    public UpdateField<byte> LifetimeMaxRank = new(70, 87);
    public UpdateField<byte> NumRespecs = new(70, 88);
    public UpdateField<uint> PvpMedals = new(70, 89);
    public UpdateField<ushort> TodayHonorableKills = new(70, 90);
    public UpdateField<ushort> YesterdayHonorableKills = new(70, 91);
    public UpdateField<uint> LifetimeHonorableKills = new(70, 92);
    public UpdateField<uint> WatchedFactionIndex = new(70, 93);
    public UpdateField<int> MaxLevel = new(70, 94);
    public UpdateField<int> ScalingPlayerLevelDelta = new(70, 95);
    public UpdateField<int> MaxCreatureScalingLevel = new(70, 96);
    public UpdateField<uint> PetSpellPower = new(70, 97);
    public UpdateField<float> UiHitModifier = new(70, 98);
    public UpdateField<float> UiSpellHitModifier = new(70, 99);
    public UpdateField<int> HomeRealmTimeOffset = new(70, 100);
    public UpdateField<float> ModPetHaste = new(70, 101);
    public UpdateField<sbyte> JailersTowerLevelMax = new(102, 103);
    public UpdateField<sbyte> JailersTowerLevel = new(102, 104);
    public UpdateField<byte> LocalRegenFlags = new(102, 105);
    public UpdateField<byte> AuraVision = new(102, 106);
    public UpdateField<byte> NumBackpackSlots = new(102, 107);
    public UpdateField<uint> OverrideSpellsID = new(102, 108);
    public UpdateField<ushort> LootSpecID = new(102, 109);
    public UpdateField<uint> OverrideZonePVPType = new(102, 110);
    public UpdateField<ObjectGuid> BnetAccount = new(102, 111);
    public UpdateField<ulong> GuildClubMemberID = new(102, 112);
    public UpdateField<uint> Honor = new(102, 113);
    public UpdateField<uint> HonorNextLevel = new(102, 114);
    public UpdateField<int> PerksProgramCurrency = new(102, 115);
    public UpdateField<byte> NumBankSlots = new(102, 116);
    public UpdateField<ResearchHistory> ResearchHistory = new(102, 117);
    public UpdateField<PerksVendorItem> FrozenPerksVendorItem = new(102, 118);
    public UpdateField<ActivePlayerUnk901> Field1410 = new(102, 120);
    public OptionalUpdateField<QuestSession> QuestSession = new(102, 119);
    public UpdateField<int> UiChromieTimeExpansionID = new(102, 121);
    public UpdateField<int> TransportServerTime = new(102, 122);
    public UpdateField<uint> WeeklyRewardsPeriodSinceOrigin = new(102, 123);
    public UpdateField<short> DEBUGSoulbindConduitRank = new(102, 124);
    public UpdateField<DungeonScoreData> DungeonScore = new(102, 125);
    public UpdateField<uint> ActiveCombatTraitConfigID = new(102, 126);
    public UpdateField<int> ItemUpgradeHighOnehandWeaponItemID = new(102, 127);
    public UpdateField<int> ItemUpgradeHighFingerItemID = new(102, 128);
    public UpdateField<float> ItemUpgradeHighFingerWatermark = new(102, 129);
    public UpdateField<int> ItemUpgradeHighTrinketItemID = new(102, 130);
    public UpdateField<float> ItemUpgradeHighTrinketWatermark = new(102, 131);
    public UpdateField<ulong> LootHistoryInstanceID = new(102, 132);
    public OptionalUpdateField<StableInfo> PetStable = new(102, 133);
    public UpdateField<byte> RequiredMountCapabilityFlags = new(134, 135);
    public UpdateFieldArray<ObjectGuid> InvSlots = new(227, 136, 137);
    public UpdateFieldArray<ulong> ExploredZones = new(240, 364, 365);
    public UpdateFieldArray<RestInfo> RestInfo = new(2, 605, 606);
    public UpdateFieldArray<int> ModDamageDonePos = new(7, 608, 609);
    public UpdateFieldArray<int> ModDamageDoneNeg = new(7, 608, 616);
    public UpdateFieldArray<float> ModDamageDonePercent = new(7, 608, 623);
    public UpdateFieldArray<float> ModHealingDonePercent = new(7, 608, 630);
    public UpdateFieldArray<float> WeaponDmgMultipliers = new(3, 637, 638);
    public UpdateFieldArray<float> WeaponAtkSpeedMultipliers = new(3, 637, 641);
    public UpdateFieldArray<uint> BuybackPrice = new(12, 644, 645);
    public UpdateFieldArray<ulong> BuybackTimestamp = new(12, 644, 657);
    public UpdateFieldArray<uint> CombatRatings = new(32, 669, 670);
    public UpdateFieldArray<uint> NoReagentCostMask = new(4, 702, 703);
    public UpdateFieldArray<uint> ProfessionSkillLine = new(2, 707, 708);
    public UpdateFieldArray<uint> BagSlotFlags = new(5, 710, 711);
    public UpdateFieldArray<uint> BankBagSlotFlags = new(7, 716, 717);
    public UpdateFieldArray<ulong> QuestCompleted = new(875, 724, 725);
    public UpdateFieldArray<float> ItemUpgradeHighWatermark = new(17, 1600, 1601);

    public ActivePlayerData() : base(0, TypeId.ActivePlayer, 1605)
    {
        ExploredZonesSize = ExploredZones.GetSize();
        ExploredZonesBits = sizeof(ulong) * 8;

        QuestCompletedBitsSize = QuestCompleted.GetSize();
        QuestCompletedBitsPerBlock = sizeof(ulong) * 8;
    }

    public void WriteCreate(WorldPacket data, UpdateFieldFlag fieldVisibilityFlags, Player owner, Player receiver)
    {
        for (int i = 0; i < 227; ++i)
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

        for (int i = 0; i < 1; ++i)
        {
            data.WriteUInt32((uint)ResearchSites[i].Size());
            data.WriteUInt32((uint)ResearchSiteProgress[i].Size());
            data.WriteUInt32((uint)Research[i].Size());
            for (int j = 0; j < ResearchSites[i].Size(); ++j)
            {
                data.WriteUInt16(ResearchSites[i][j]);
            }
            for (int j = 0; j < ResearchSiteProgress[i].Size(); ++j)
            {
                data.WriteUInt32(ResearchSiteProgress[i][j]);
            }
            for (int j = 0; j < Research[i].Size(); ++j)
            {
                Research[i][j].WriteCreate(data, owner, receiver);
            }
        }

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
        data.WriteInt32(MawPowers.Size());
        data.WriteInt32(MultiFloorExploration.Size());
        data.WriteInt32(RecipeProgression.Size());
        data.WriteInt32(ReplayedQuests.Size());
        data.WriteInt32(TaskQuests.Size());
        data.WriteInt32(DisabledSpells.Size());
        data.WriteInt32(UiChromieTimeExpansionID);
        data.WriteInt32(TransportServerTime);
        data.WriteUInt32(WeeklyRewardsPeriodSinceOrigin);
        data.WriteInt16(DEBUGSoulbindConduitRank);
        data.WriteInt32(TraitConfigs.Size());
        data.WriteUInt32(ActiveCombatTraitConfigID);
        data.WriteInt32(CraftingOrders.Size());
        data.WriteInt32(PersonalCraftingOrderCounts.Size());
        data.WriteInt32(CategoryCooldownMods.Size());
        data.WriteInt32(WeeklySpellUses.Size());
        for (int i = 0; i < 17; ++i)
        {
            data.WriteFloat(ItemUpgradeHighWatermark[i]);
        }
        data.WriteInt32(ItemUpgradeHighOnehandWeaponItemID);
        data.WriteInt32(ItemUpgradeHighFingerItemID);
        data.WriteFloat(ItemUpgradeHighFingerWatermark);
        data.WriteInt32(ItemUpgradeHighTrinketItemID);
        data.WriteFloat(ItemUpgradeHighTrinketWatermark);
        data.WriteUInt64(LootHistoryInstanceID);
        data.WriteUInt32((uint)TrackedCollectableSources.Size());
        data.WriteUInt8(RequiredMountCapabilityFlags);
        for (int i = 0; i < KnownTitles.Size(); ++i)
        {
            data.WriteUInt64(KnownTitles[i]);
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
        for (int i = 0; i < TaskQuests.Size(); ++i)
        {
            TaskQuests[i].WriteCreate(data, owner, receiver);
        }
        for (int i = 0; i < DisabledSpells.Size(); ++i)
        {
            data.WriteInt32(DisabledSpells[i]);
        }
        for (int i = 0; i < PersonalCraftingOrderCounts.Size(); ++i)
        {
            PersonalCraftingOrderCounts[i].WriteCreate(data, owner, receiver);
        }
        for (int i = 0; i < CategoryCooldownMods.Size(); ++i)
        {
            CategoryCooldownMods[i].WriteCreate(data, owner, receiver);
        }
        for (int i = 0; i < WeeklySpellUses.Size(); ++i)
        {
            WeeklySpellUses[i].WriteCreate(data, owner, receiver);
        }
        for (int i = 0; i < TrackedCollectableSources.Size(); ++i)
        {
            data.WriteInt64(TrackedCollectableSources[i]);
        }
        data.FlushBits();
        data.WriteBit(BackpackAutoSortDisabled);
        data.WriteBit(BankAutoSortDisabled);
        data.WriteBit(SortBagsRightToLeft);
        data.WriteBit(InsertItemsLeftToRight);
        data.WriteBit(HasPerksProgramPendingReward);
        data.WriteBits(QuestSession.HasValue(), 1);
        data.WriteBits(PetStable.has_value(), 1);
        data.FlushBits();
        ResearchHistory.Value.WriteCreate(data, owner, receiver);
        if (QuestSession.HasValue())
        {
            QuestSession.Value.WriteCreate(data, owner, receiver);
        }
        FrozenPerksVendorItem.Value.Write(data);
        ((ActivePlayerUnk901)Field1410).WriteCreate(data, owner, receiver);
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
        if (PetStable.HasValue())
        {
            PetStable.Value.WriteCreate(data, owner, receiver);
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
        data.WriteBits(changesMask.GetBlocksMask(1), 19);
        for (uint i = 0; i < 51; ++i)
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
                {
                    KnownTitles.WriteUpdateMask(data);
                }
                else
                {
                    WriteCompleteDynamicFieldUpdateMask(KnownTitles.Size(), data);
                }
            }
            if (changesMask[7])
            {
                if (!ignoreNestedChangesMask)
                {
                    PvpInfo.WriteUpdateMask(data);
                }
                else
                {
                    WriteCompleteDynamicFieldUpdateMask(PvpInfo.Size(), data);
                }
            }
        }
        if (changesMask[35])
        {
            for (int i = 0; i < 1; ++i)
            {
                if (changesMask[36 + i])
                {
                    if (!ignoreNestedChangesMask)
                    {
                        ResearchSites[i].WriteUpdateMask(data);
                    }
                    else
                    {
                        WriteCompleteDynamicFieldUpdateMask(ResearchSites[i].Size(), data);
                    }
                }
            }
        }
        if (changesMask[37])
        {
            for (int i = 0; i < 1; ++i)
            {
                if (changesMask[38 + i])
                {
                    if (!ignoreNestedChangesMask)
                    {
                        ResearchSiteProgress[i].WriteUpdateMask(data);
                    }
                    else
                    {
                        WriteCompleteDynamicFieldUpdateMask(ResearchSiteProgress[i].Size(), data);
                    }
                }
            }
        }
        if (changesMask[39])
        {
            for (int i = 0; i < 1; ++i)
            {
                if (changesMask[40 + i])
                {
                    if (!ignoreNestedChangesMask)
                    {
                        Research[i].WriteUpdateMask(data);
                    }
                    else
                    {
                        WriteCompleteDynamicFieldUpdateMask(Research[i].Size(), data);
                    }
                }
            }
        }
        if (changesMask[35])
        {
            for (int i = 0; i < 1; ++i)
            {
                if (changesMask[36 + i])
                {
                    for (int j = 0; j < ResearchSites[i].Size(); ++j)
                    {
                        if (ResearchSites[i].HasChanged(j) || ignoreNestedChangesMask)
                        {
                            data.WriteUInt16(ResearchSites[i][j]);
                        }
                    }
                }
            }
        }
        if (changesMask[37])
        {
            for (int i = 0; i < 1; ++i)
            {
                if (changesMask[38 + i])
                {
                    for (int j = 0; j < ResearchSiteProgress[i].Size(); ++j)
                    {
                        if (ResearchSiteProgress[i].HasChanged(j) || ignoreNestedChangesMask)
                        {
                            data.WriteUInt32(ResearchSiteProgress[i][j]);
                        }
                    }
                }
            }
        }
        if (changesMask[39])
        {
            for (int i = 0; i < 1; ++i)
            {
                if (changesMask[40 + i])
                {
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
            if (changesMask[8])
            {
                if (!ignoreNestedChangesMask)
                {
                    DailyQuestsCompleted.WriteUpdateMask(data);
                }
                else
                {
                    WriteCompleteDynamicFieldUpdateMask(DailyQuestsCompleted.Size(), data);
                }
            }
            if (changesMask[9])
            {
                if (!ignoreNestedChangesMask)
                {
                    AvailableQuestLineXQuestIDs.WriteUpdateMask(data);
                }
                else
                {
                    WriteCompleteDynamicFieldUpdateMask(AvailableQuestLineXQuestIDs.Size(), data);
                }
            }
            if (changesMask[10])
            {
                if (!ignoreNestedChangesMask)
                {
                    Heirlooms.WriteUpdateMask(data);
                }
                else
                {
                    WriteCompleteDynamicFieldUpdateMask(Heirlooms.Size(), data);
                }
            }
            if (changesMask[11])
            {
                if (!ignoreNestedChangesMask)
                {
                    HeirloomFlags.WriteUpdateMask(data);
                }
                else
                {
                    WriteCompleteDynamicFieldUpdateMask(HeirloomFlags.Size(), data);
                }
            }
            if (changesMask[12])
            {
                if (!ignoreNestedChangesMask)
                {
                    Toys.WriteUpdateMask(data);
                }
                else
                {
                    WriteCompleteDynamicFieldUpdateMask(Toys.Size(), data);
                }
            }
            if (changesMask[13])
            {
                if (!ignoreNestedChangesMask)
                {
                    ToyFlags.WriteUpdateMask(data);
                }
                else
                {
                    WriteCompleteDynamicFieldUpdateMask(ToyFlags.Size(), data);
                }
            }
            if (changesMask[14])
            {
                if (!ignoreNestedChangesMask)
                {
                    Transmog.WriteUpdateMask(data);
                }
                else
                {
                    WriteCompleteDynamicFieldUpdateMask(Transmog.Size(), data);
                }
            }
            if (changesMask[15])
            {
                if (!ignoreNestedChangesMask)
                {
                    ConditionalTransmog.WriteUpdateMask(data);
                }
                else
                {
                    WriteCompleteDynamicFieldUpdateMask(ConditionalTransmog.Size(), data);
                }
            }
            if (changesMask[16])
            {
                if (!ignoreNestedChangesMask)
                {
                    SelfResSpells.WriteUpdateMask(data);
                }
                else
                {
                    WriteCompleteDynamicFieldUpdateMask(SelfResSpells.Size(), data);
                }
            }
            if (changesMask[17])
            {
                if (!ignoreNestedChangesMask)
                {
                    RuneforgePowers.WriteUpdateMask(data);
                }
                else
                {
                    WriteCompleteDynamicFieldUpdateMask(RuneforgePowers.Size(), data);
                }
            }
            if (changesMask[18])
            {
                if (!ignoreNestedChangesMask)
                {
                    TransmogIllusions.WriteUpdateMask(data);
                }
                else
                {
                    WriteCompleteDynamicFieldUpdateMask(TransmogIllusions.Size(), data);
                }
            }
            if (changesMask[19])
            {
                if (!ignoreNestedChangesMask)
                {
                    CharacterRestrictions.WriteUpdateMask(data);
                }
                else
                {
                    WriteCompleteDynamicFieldUpdateMask(CharacterRestrictions.Size(), data);
                }
            }
            if (changesMask[20])
            {
                if (!ignoreNestedChangesMask)
                {
                    SpellPctModByLabel.WriteUpdateMask(data);
                }
                else
                {
                    WriteCompleteDynamicFieldUpdateMask(SpellPctModByLabel.Size(), data);
                }
            }
            if (changesMask[21])
            {
                if (!ignoreNestedChangesMask)
                {
                    SpellFlatModByLabel.WriteUpdateMask(data);
                }
                else
                {
                    WriteCompleteDynamicFieldUpdateMask(SpellFlatModByLabel.Size(), data);
                }
            }
            if (changesMask[22])
            {
                if (!ignoreNestedChangesMask)
                {
                    MawPowers.WriteUpdateMask(data);
                }
                else
                {
                    WriteCompleteDynamicFieldUpdateMask(MawPowers.Size(), data);
                }
            }
            if (changesMask[23])
            {
                if (!ignoreNestedChangesMask)
                {
                    MultiFloorExploration.WriteUpdateMask(data);
                }
                else
                {
                    WriteCompleteDynamicFieldUpdateMask(MultiFloorExploration.Size(), data);
                }
            }
            if (changesMask[24])
            {
                if (!ignoreNestedChangesMask)
                {
                    RecipeProgression.WriteUpdateMask(data);
                }
                else
                {
                    WriteCompleteDynamicFieldUpdateMask(RecipeProgression.Size(), data);
                }
            }
            if (changesMask[25])
            {
                if (!ignoreNestedChangesMask)
                {
                    ReplayedQuests.WriteUpdateMask(data);
                }
                else
                {
                    WriteCompleteDynamicFieldUpdateMask(ReplayedQuests.Size(), data);
                }
            }
            if (changesMask[26])
            {
                if (!ignoreNestedChangesMask)
                {
                    TaskQuests.WriteUpdateMask(data);
                }
                else
                {
                    WriteCompleteDynamicFieldUpdateMask(TaskQuests.Size(), data);
                }
            }
            if (changesMask[27])
            {
                if (!ignoreNestedChangesMask)
                {
                    DisabledSpells.WriteUpdateMask(data);
                }
                else
                {
                    WriteCompleteDynamicFieldUpdateMask(DisabledSpells.Size(), data);
                }
            }
            if (changesMask[28])
            {
                if (!ignoreNestedChangesMask)
                {
                    TraitConfigs.WriteUpdateMask(data);
                }
                else
                {
                    WriteCompleteDynamicFieldUpdateMask(TraitConfigs.Size(), data);
                }
            }
            if (changesMask[29])
            {
                if (!ignoreNestedChangesMask)
                {
                    CraftingOrders.WriteUpdateMask(data);
                }
                else
                {
                    WriteCompleteDynamicFieldUpdateMask(CraftingOrders.Size(), data);
                }
            }
            if (changesMask[30])
            {
                if (!ignoreNestedChangesMask)
                {
                    PersonalCraftingOrderCounts.WriteUpdateMask(data);
                }
                else
                {
                    WriteCompleteDynamicFieldUpdateMask(PersonalCraftingOrderCounts.Size(), data);
                }
            }
            if (changesMask[31])
            {
                if (!ignoreNestedChangesMask)
                {
                    CategoryCooldownMods.WriteUpdateMask(data);
                }
                else
                {
                    WriteCompleteDynamicFieldUpdateMask(CategoryCooldownMods.Size(), data);
                }
            }
        }
        if (changesMask[32])
        {
            if (changesMask[33])
            {
                if (!ignoreNestedChangesMask)
                {
                    WeeklySpellUses.WriteUpdateMask(data);
                }
                else
                {
                    WriteCompleteDynamicFieldUpdateMask(WeeklySpellUses.Size(), data);
                }
            }
            if (changesMask[34])
            {
                if (!ignoreNestedChangesMask)
                {
                    TrackedCollectableSources.WriteUpdateMask(data);
                }
                else
                {
                    WriteCompleteDynamicFieldUpdateMask(TrackedCollectableSources.Size(), data);
                }
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
                for (int i = 0; i < DailyQuestsCompleted.Size(); ++i)
                {
                    if (DailyQuestsCompleted.HasChanged(i) || ignoreNestedChangesMask)
                    {
                        data.WriteInt32((int)DailyQuestsCompleted[i]);
                    }
                }
            }
            if (changesMask[9])
            {
                for (int i = 0; i < AvailableQuestLineXQuestIDs.Size(); ++i)
                {
                    if (AvailableQuestLineXQuestIDs.HasChanged(i) || ignoreNestedChangesMask)
                    {
                        data.WriteInt32(AvailableQuestLineXQuestIDs[i]);
                    }
                }
            }
            if (changesMask[10])
            {
                for (int i = 0; i < Heirlooms.Size(); ++i)
                {
                    if (Heirlooms.HasChanged(i) || ignoreNestedChangesMask)
                    {
                        data.WriteInt32((int)Heirlooms[i]);
                    }
                }
            }
            if (changesMask[11])
            {
                for (int i = 0; i < HeirloomFlags.Size(); ++i)
                {
                    if (HeirloomFlags.HasChanged(i) || ignoreNestedChangesMask)
                    {
                        data.WriteUInt32(HeirloomFlags[i]);
                    }
                }
            }
            if (changesMask[12])
            {
                for (int i = 0; i < Toys.Size(); ++i)
                {
                    if (Toys.HasChanged(i) || ignoreNestedChangesMask)
                    {
                        data.WriteInt32((int)Toys[i]);
                    }
                }
            }
            if (changesMask[13])
            {
                for (int i = 0; i < ToyFlags.Size(); ++i)
                {
                    if (ToyFlags.HasChanged(i) || ignoreNestedChangesMask)
                    {
                        data.WriteUInt32(ToyFlags[i]);
                    }
                }
            }
            if (changesMask[14])
            {
                for (int i = 0; i < Transmog.Size(); ++i)
                {
                    if (Transmog.HasChanged(i) || ignoreNestedChangesMask)
                    {
                        data.WriteUInt32(Transmog[i]);
                    }
                }
            }
            if (changesMask[15])
            {
                for (int i = 0; i < ConditionalTransmog.Size(); ++i)
                {
                    if (ConditionalTransmog.HasChanged(i) || ignoreNestedChangesMask)
                    {
                        data.WriteInt32((int)ConditionalTransmog[i]);
                    }
                }
            }
            if (changesMask[16])
            {
                for (int i = 0; i < SelfResSpells.Size(); ++i)
                {
                    if (SelfResSpells.HasChanged(i) || ignoreNestedChangesMask)
                    {
                        data.WriteInt32((int)SelfResSpells[i]);
                    }
                }
            }
            if (changesMask[17])
            {
                for (int i = 0; i < RuneforgePowers.Size(); ++i)
                {
                    if (RuneforgePowers.HasChanged(i) || ignoreNestedChangesMask)
                    {
                        data.WriteUInt32(RuneforgePowers[i]);
                    }
                }
            }
            if (changesMask[18])
            {
                for (int i = 0; i < TransmogIllusions.Size(); ++i)
                {
                    if (TransmogIllusions.HasChanged(i) || ignoreNestedChangesMask)
                    {
                        data.WriteUInt32(TransmogIllusions[i]);
                    }
                }
            }
            if (changesMask[20])
            {
                for (int i = 0; i < SpellPctModByLabel.Size(); ++i)
                {
                    if (SpellPctModByLabel.HasChanged(i) || ignoreNestedChangesMask)
                    {
                        SpellPctModByLabel[i].WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
                    }
                }
            }
            if (changesMask[21])
            {
                for (int i = 0; i < SpellFlatModByLabel.Size(); ++i)
                {
                    if (SpellFlatModByLabel.HasChanged(i) || ignoreNestedChangesMask)
                    {
                        SpellFlatModByLabel[i].WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
                    }
                }
            }
            if (changesMask[22])
            {
                for (int i = 0; i < MawPowers.Size(); ++i)
                {
                    if (MawPowers.HasChanged(i) || ignoreNestedChangesMask)
                    {
                        MawPowers[i].WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
                    }
                }
            }
            if (changesMask[23])
            {
                for (int i = 0; i < MultiFloorExploration.Size(); ++i)
                {
                    if (MultiFloorExploration.HasChanged(i) || ignoreNestedChangesMask)
                    {
                        MultiFloorExploration[i].WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
                    }
                }
            }
            if (changesMask[24])
            {
                for (int i = 0; i < RecipeProgression.Size(); ++i)
                {
                    if (RecipeProgression.HasChanged(i) || ignoreNestedChangesMask)
                    {
                        RecipeProgression[i].WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
                    }
                }
            }
            if (changesMask[25])
            {
                for (int i = 0; i < ReplayedQuests.Size(); ++i)
                {
                    if (ReplayedQuests.HasChanged(i) || ignoreNestedChangesMask)
                    {
                        ReplayedQuests[i].WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
                    }
                }
            }
            if (changesMask[26])
            {
                for (int i = 0; i < TaskQuests.Size(); ++i)
                {
                    if (TaskQuests.HasChanged(i) || ignoreNestedChangesMask)
                    {
                        TaskQuests[i].WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
                    }
                }
            }
            if (changesMask[27])
            {
                for (int i = 0; i < DisabledSpells.Size(); ++i)
                {
                    if (DisabledSpells.HasChanged(i) || ignoreNestedChangesMask)
                    {
                        data.WriteInt32(DisabledSpells[i]);
                    }
                }
            }
            if (changesMask[30])
            {
                for (int i = 0; i < PersonalCraftingOrderCounts.Size(); ++i)
                {
                    if (PersonalCraftingOrderCounts.HasChanged(i) || ignoreNestedChangesMask)
                    {
                        PersonalCraftingOrderCounts[i].WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
                    }
                }
            }
            if (changesMask[31])
            {
                for (int i = 0; i < CategoryCooldownMods.Size(); ++i)
                {
                    if (CategoryCooldownMods.HasChanged(i) || ignoreNestedChangesMask)
                    {
                        CategoryCooldownMods[i].WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
                    }
                }
            }
        }
        if (changesMask[32])
        {
            if (changesMask[33])
            {
                for (int i = 0; i < WeeklySpellUses.Size(); ++i)
                {
                    if (WeeklySpellUses.HasChanged(i) || ignoreNestedChangesMask)
                    {
                        WeeklySpellUses[i].WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
                    }
                }
            }
            if (changesMask[34])
            {
                for (int i = 0; i < TrackedCollectableSources.Size(); ++i)
                {
                    if (TrackedCollectableSources.HasChanged(i) || ignoreNestedChangesMask)
                    {
                        data.WriteInt64(TrackedCollectableSources[i]);
                    }
                }
            }
        }
        if (changesMask[0])
        {
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
            if (changesMask[19])
            {
                for (int i = 0; i < CharacterRestrictions.Size(); ++i)
                {
                    if (CharacterRestrictions.HasChanged(i) || ignoreNestedChangesMask)
                    {
                        CharacterRestrictions[i].WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
                    }
                }
            }
            if (changesMask[28])
            {
                for (int i = 0; i < TraitConfigs.Size(); ++i)
                {
                    if (TraitConfigs.HasChanged(i) || ignoreNestedChangesMask)
                    {
                        TraitConfigs[i].WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
                    }
                }
            }
            if (changesMask[29])
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
        if (changesMask[32])
        {
            if (changesMask[41])
            {
                data.WritePackedGuid(FarsightObject);
            }
            if (changesMask[42])
            {
                data.WritePackedGuid(SummonedBattlePetGUID);
            }
            if (changesMask[43])
            {
                data.WriteUInt64(Coinage);
            }
            if (changesMask[44])
            {
                data.WriteInt32((int)XP.Value);
            }
            if (changesMask[45])
            {
                data.WriteInt32((int)NextLevelXP.Value);
            }
            if (changesMask[46])
            {
                data.WriteInt32(TrialXP);
            }
            if (changesMask[47])
            {
                Skill.Value.WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
            }
            if (changesMask[48])
            {
                data.WriteInt32((int)CharacterPoints.Value);
            }
            if (changesMask[49])
            {
                data.WriteInt32((int)MaxTalentTiers.Value);
            }
            if (changesMask[50])
            {
                data.WriteUInt32(TrackCreatureMask);
            }
            if (changesMask[51])
            {
                data.WriteFloat(MainhandExpertise);
            }
            if (changesMask[52])
            {
                data.WriteFloat(OffhandExpertise);
            }
            if (changesMask[53])
            {
                data.WriteFloat(RangedExpertise);
            }
            if (changesMask[54])
            {
                data.WriteFloat(CombatRatingExpertise);
            }
            if (changesMask[55])
            {
                data.WriteFloat(BlockPercentage);
            }
            if (changesMask[56])
            {
                data.WriteFloat(DodgePercentage);
            }
            if (changesMask[57])
            {
                data.WriteFloat(DodgePercentageFromAttribute);
            }
            if (changesMask[58])
            {
                data.WriteFloat(ParryPercentage);
            }
            if (changesMask[59])
            {
                data.WriteFloat(ParryPercentageFromAttribute);
            }
            if (changesMask[60])
            {
                data.WriteFloat(CritPercentage);
            }
            if (changesMask[61])
            {
                data.WriteFloat(RangedCritPercentage);
            }
            if (changesMask[62])
            {
                data.WriteFloat(OffhandCritPercentage);
            }
            if (changesMask[63])
            {
                data.WriteFloat(SpellCritPercentage);
            }
            if (changesMask[64])
            {
                data.WriteInt32((int)ShieldBlock.Value);
            }
            if (changesMask[65])
            {
                data.WriteFloat(ShieldBlockCritPercentage);
            }
            if (changesMask[66])
            {
                data.WriteFloat(Mastery);
            }
            if (changesMask[67])
            {
                data.WriteFloat(Speed);
            }
            if (changesMask[68])
            {
                data.WriteFloat(Avoidance);
            }
            if (changesMask[69])
            {
                data.WriteFloat(Sturdiness);
            }
        }
        if (changesMask[70])
        {
            if (changesMask[71])
            {
                data.WriteInt32(Versatility);
            }
            if (changesMask[72])
            {
                data.WriteFloat(VersatilityBonus);
            }
            if (changesMask[73])
            {
                data.WriteFloat(PvpPowerDamage);
            }
            if (changesMask[74])
            {
                data.WriteFloat(PvpPowerHealing);
            }
            if (changesMask[75])
            {
                data.WriteInt32(ModHealingDonePos);
            }
            if (changesMask[76])
            {
                data.WriteFloat(ModHealingPercent);
            }
            if (changesMask[77])
            {
                data.WriteFloat(ModPeriodicHealingDonePercent);
            }
            if (changesMask[78])
            {
                data.WriteFloat(ModSpellPowerPercent);
            }
            if (changesMask[79])
            {
                data.WriteFloat(ModResiliencePercent);
            }
            if (changesMask[80])
            {
                data.WriteFloat(OverrideSpellPowerByAPPercent);
            }
            if (changesMask[81])
            {
                data.WriteFloat(OverrideAPBySpellPowerPercent);
            }
            if (changesMask[82])
            {
                data.WriteInt32(ModTargetResistance);
            }
            if (changesMask[83])
            {
                data.WriteInt32(ModTargetPhysicalResistance);
            }
            if (changesMask[84])
            {
                data.WriteUInt32(LocalFlags);
            }
            if (changesMask[85])
            {
                data.WriteUInt8(GrantableLevels);
            }
            if (changesMask[86])
            {
                data.WriteUInt8(MultiActionBars);
            }
            if (changesMask[87])
            {
                data.WriteUInt8(LifetimeMaxRank);
            }
            if (changesMask[88])
            {
                data.WriteUInt8(NumRespecs);
            }
            if (changesMask[89])
            {
                data.WriteUInt32(PvpMedals);
            }
            if (changesMask[90])
            {
                data.WriteUInt16(TodayHonorableKills);
            }
            if (changesMask[91])
            {
                data.WriteUInt16(YesterdayHonorableKills);
            }
            if (changesMask[92])
            {
                data.WriteUInt32(LifetimeHonorableKills);
            }
            if (changesMask[93])
            {
                data.WriteInt32((int)WatchedFactionIndex.Value);
            }
            if (changesMask[94])
            {
                data.WriteInt32(MaxLevel);
            }
            if (changesMask[95])
            {
                data.WriteInt32(ScalingPlayerLevelDelta);
            }
            if (changesMask[96])
            {
                data.WriteInt32(MaxCreatureScalingLevel);
            }
            if (changesMask[97])
            {
                data.WriteInt32((int)PetSpellPower.Value);
            }
            if (changesMask[98])
            {
                data.WriteFloat(UiHitModifier);
            }
            if (changesMask[99])
            {
                data.WriteFloat(UiSpellHitModifier);
            }
            if (changesMask[100])
            {
                data.WriteInt32(HomeRealmTimeOffset);
            }
            if (changesMask[101])
            {
                data.WriteFloat(ModPetHaste);
            }
        }
        if (changesMask[102])
        {
            if (changesMask[103])
            {
                data.WriteInt8(JailersTowerLevelMax);
            }
            if (changesMask[104])
            {
                data.WriteInt8(JailersTowerLevel);
            }
            if (changesMask[105])
            {
                data.WriteUInt8(LocalRegenFlags);
            }
            if (changesMask[106])
            {
                data.WriteUInt8(AuraVision);
            }
            if (changesMask[107])
            {
                data.WriteUInt8(NumBackpackSlots);
            }
            if (changesMask[108])
            {
                data.WriteInt32((int)OverrideSpellsID.Value);
            }
            if (changesMask[109])
            {
                data.WriteUInt16(LootSpecID);
            }
            if (changesMask[110])
            {
                data.WriteUInt32(OverrideZonePVPType);
            }
            if (changesMask[111])
            {
                data.WritePackedGuid(BnetAccount);
            }
            if (changesMask[112])
            {
                data.WriteUInt64(GuildClubMemberID);
            }
            if (changesMask[113])
            {
                data.WriteInt32((int)Honor.Value);
            }
            if (changesMask[114])
            {
                data.WriteInt32((int)HonorNextLevel.Value);
            }
            if (changesMask[115])
            {
                data.WriteInt32(PerksProgramCurrency);
            }
            if (changesMask[116])
            {
                data.WriteUInt8(NumBankSlots);
            }
            if (changesMask[121])
            {
                data.WriteInt32(UiChromieTimeExpansionID);
            }
            if (changesMask[122])
            {
                data.WriteInt32(TransportServerTime);
            }
            if (changesMask[123])
            {
                data.WriteUInt32(WeeklyRewardsPeriodSinceOrigin);
            }
            if (changesMask[124])
            {
                data.WriteInt16(DEBUGSoulbindConduitRank);
            }
            if (changesMask[126])
            {
                data.WriteUInt32(ActiveCombatTraitConfigID);
            }
            if (changesMask[127])
            {
                data.WriteInt32(ItemUpgradeHighOnehandWeaponItemID);
            }
            if (changesMask[128])
            {
                data.WriteInt32(ItemUpgradeHighFingerItemID);
            }
            if (changesMask[129])
            {
                data.WriteFloat(ItemUpgradeHighFingerWatermark);
            }
            if (changesMask[130])
            {
                data.WriteInt32(ItemUpgradeHighTrinketItemID);
            }
            if (changesMask[131])
            {
                data.WriteFloat(ItemUpgradeHighTrinketWatermark);
            }
            if (changesMask[132])
            {
                data.WriteUInt64(LootHistoryInstanceID);
            }
        }
        if (changesMask[134])
        {
            if (changesMask[135])
            {
                data.WriteUInt8(RequiredMountCapabilityFlags);
            }
        }
        if (changesMask[102])
        {
            data.WriteBits(QuestSession.HasValue(), 1);
            data.WriteBits(PetStable.HasValue(), 1);
            data.FlushBits();
            if (changesMask[117])
            {
                ResearchHistory.Value.WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
            }
            if (changesMask[119])
            {
                if (QuestSession.HasValue())
                {
                    QuestSession.Value.WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
                }
            }
            if (changesMask[118])
            {
                FrozenPerksVendorItem.Value.Write(data);
            }
            if (changesMask[120])
            {
                Field1410.Value.WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
            }
            if (changesMask[125])
            {
                DungeonScore.Value.Write(data);
            }
            if (changesMask[133])
            {
                if (PetStable.HasValue())
                {
                    PetStable.Value.WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
                }
            }
        }
        if (changesMask[136])
        {
            for (int i = 0; i < 227; ++i)
            {
                if (changesMask[137 + i])
                {
                    data.Write(InvSlots[i]);
                }
            }
        }
        if (changesMask[364])
        {
            for (int i = 0; i < 240; ++i)
            {
                if (changesMask[365 + i])
                {
                    data.WriteUInt64(ExploredZones[i]);
                }
            }
        }
        if (changesMask[605])
        {
            for (int i = 0; i < 2; ++i)
            {
                if (changesMask[606 + i])
                {
                    RestInfo[i].WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
                }
            }
        }
        if (changesMask[608])
        {
            for (int i = 0; i < 7; ++i)
            {
                if (changesMask[609 + i])
                {
                    data.WriteInt32(ModDamageDonePos[i]);
                }
                if (changesMask[616 + i])
                {
                    data.WriteInt32(ModDamageDoneNeg[i]);
                }
                if (changesMask[623 + i])
                {
                    data.WriteFloat(ModDamageDonePercent[i]);
                }
                if (changesMask[630 + i])
                {
                    data.WriteFloat(ModHealingDonePercent[i]);
                }
            }
        }
        if (changesMask[637])
        {
            for (int i = 0; i < 3; ++i)
            {
                if (changesMask[638 + i])
                {
                    data.WriteFloat(WeaponDmgMultipliers[i]);
                }
                if (changesMask[641 + i])
                {
                    data.WriteFloat(WeaponAtkSpeedMultipliers[i]);
                }
            }
        }
        if (changesMask[644])
        {
            for (int i = 0; i < 12; ++i)
            {
                if (changesMask[645 + i])
                {
                    data.WriteUInt32(BuybackPrice[i]);
                }
                if (changesMask[657 + i])
                {
                    data.WriteInt64((long)BuybackTimestamp[i]);
                }
            }
        }
        if (changesMask[669])
        {
            for (int i = 0; i < 32; ++i)
            {
                if (changesMask[670 + i])
                {
                    data.WriteInt32((int)CombatRatings[i]);
                }
            }
        }
        if (changesMask[702])
        {
            for (int i = 0; i < 4; ++i)
            {
                if (changesMask[703 + i])
                {
                    data.WriteUInt32(NoReagentCostMask[i]);
                }
            }
        }
        if (changesMask[707])
        {
            for (int i = 0; i < 2; ++i)
            {
                if (changesMask[708 + i])
                {
                    data.WriteInt32((int)ProfessionSkillLine[i]);
                }
            }
        }
        if (changesMask[710])
        {
            for (int i = 0; i < 5; ++i)
            {
                if (changesMask[711 + i])
                {
                    data.WriteUInt32(BagSlotFlags[i]);
                }
            }
        }
        if (changesMask[716])
        {
            for (int i = 0; i < 7; ++i)
            {
                if (changesMask[717 + i])
                {
                    data.WriteUInt32(BankBagSlotFlags[i]);
                }
            }
        }
        if (changesMask[724])
        {
            for (int i = 0; i < 875; ++i)
            {
                if (changesMask[725 + i])
                {
                    data.WriteUInt64(QuestCompleted[i]);
                }
            }
        }
        if (changesMask[1600])
        {
            for (int i = 0; i < 17; ++i)
            {
                if (changesMask[1601 + i])
                {
                    data.WriteFloat(ItemUpgradeHighWatermark[i]);
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
        ClearChangesMask(ResearchSites);
        ClearChangesMask(ResearchSiteProgress);
        ClearChangesMask(Research);
        ClearChangesMask(KnownTitles);
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
        ClearChangesMask(TaskQuests);
        ClearChangesMask(DisabledSpells);
        ClearChangesMask(PersonalCraftingOrderCounts);
        ClearChangesMask(CategoryCooldownMods);
        ClearChangesMask(WeeklySpellUses);
        ClearChangesMask(TrackedCollectableSources);
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
        ClearChangesMask(ResearchHistory);
        ClearChangesMask(FrozenPerksVendorItem);
        ClearChangesMask(Field1410);
        ClearChangesMask(QuestSession);
        ClearChangesMask(UiChromieTimeExpansionID);
        ClearChangesMask(TransportServerTime);
        ClearChangesMask(WeeklyRewardsPeriodSinceOrigin);
        ClearChangesMask(DEBUGSoulbindConduitRank);
        ClearChangesMask(DungeonScore);
        ClearChangesMask(ActiveCombatTraitConfigID);
        ClearChangesMask(ItemUpgradeHighOnehandWeaponItemID);
        ClearChangesMask(ItemUpgradeHighFingerItemID);
        ClearChangesMask(ItemUpgradeHighFingerWatermark);
        ClearChangesMask(ItemUpgradeHighTrinketItemID);
        ClearChangesMask(ItemUpgradeHighTrinketWatermark);
        ClearChangesMask(LootHistoryInstanceID);
        ClearChangesMask(PetStable);
        ClearChangesMask(RequiredMountCapabilityFlags);
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
        ClearChangesMask(ItemUpgradeHighWatermark);
        ChangesMask.ResetAll();
    }
}