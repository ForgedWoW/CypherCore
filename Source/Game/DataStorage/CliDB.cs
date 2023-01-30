﻿// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Framework.Constants;
using Framework.Database;

namespace Game.DataStorage
{
    public class CliDB
    {
        #region Main Collections

        public static DB6Storage<AchievementRecord> AchievementStorage { get; set; }
        public static DB6Storage<AchievementCategoryRecord> AchievementCategoryStorage { get; set; }
        public static DB6Storage<AdventureJournalRecord> AdventureJournalStorage { get; set; }
        public static DB6Storage<AdventureMapPOIRecord> AdventureMapPOIStorage { get; set; }
        public static DB6Storage<AnimationDataRecord> AnimationDataStorage { get; set; }
        public static DB6Storage<AnimKitRecord> AnimKitStorage { get; set; }
        public static DB6Storage<AreaGroupMemberRecord> AreaGroupMemberStorage { get; set; }
        public static DB6Storage<AreaTableRecord> AreaTableStorage { get; set; }
        public static DB6Storage<AreaTriggerRecord> AreaTriggerStorage { get; set; }
        public static DB6Storage<ArmorLocationRecord> ArmorLocationStorage { get; set; }
        public static DB6Storage<ArtifactRecord> ArtifactStorage { get; set; }
        public static DB6Storage<ArtifactAppearanceRecord> ArtifactAppearanceStorage { get; set; }
        public static DB6Storage<ArtifactAppearanceSetRecord> ArtifactAppearanceSetStorage { get; set; }
        public static DB6Storage<ArtifactCategoryRecord> ArtifactCategoryStorage { get; set; }
        public static DB6Storage<ArtifactPowerRecord> ArtifactPowerStorage { get; set; }
        public static DB6Storage<ArtifactPowerLinkRecord> ArtifactPowerLinkStorage { get; set; }
        public static DB6Storage<ArtifactPowerPickerRecord> ArtifactPowerPickerStorage { get; set; }

        public static DB6Storage<ArtifactPowerRankRecord> ArtifactPowerRankStorage { get; set; }

        //public static DB6Storage<ArtifactQuestXPRecord> ArtifactQuestXPStorage { get; set; }
        public static DB6Storage<ArtifactTierRecord> ArtifactTierStorage { get; set; }
        public static DB6Storage<ArtifactUnlockRecord> ArtifactUnlockStorage { get; set; }
        public static DB6Storage<AuctionHouseRecord> AuctionHouseStorage { get; set; }
        public static DB6Storage<AzeriteEmpoweredItemRecord> AzeriteEmpoweredItemStorage { get; set; }
        public static DB6Storage<AzeriteEssenceRecord> AzeriteEssenceStorage { get; set; }
        public static DB6Storage<AzeriteEssencePowerRecord> AzeriteEssencePowerStorage { get; set; }
        public static DB6Storage<AzeriteItemRecord> AzeriteItemStorage { get; set; }
        public static DB6Storage<AzeriteItemMilestonePowerRecord> AzeriteItemMilestonePowerStorage { get; set; }
        public static DB6Storage<AzeriteKnowledgeMultiplierRecord> AzeriteKnowledgeMultiplierStorage { get; set; }
        public static DB6Storage<AzeriteLevelInfoRecord> AzeriteLevelInfoStorage { get; set; }
        public static DB6Storage<AzeritePowerRecord> AzeritePowerStorage { get; set; }
        public static DB6Storage<AzeritePowerSetMemberRecord> AzeritePowerSetMemberStorage { get; set; }
        public static DB6Storage<AzeriteTierUnlockRecord> AzeriteTierUnlockStorage { get; set; }
        public static DB6Storage<AzeriteTierUnlockSetRecord> AzeriteTierUnlockSetStorage { get; set; }
        public static DB6Storage<AzeriteUnlockMappingRecord> AzeriteUnlockMappingStorage { get; set; }
        public static DB6Storage<BankBagSlotPricesRecord> BankBagSlotPricesStorage { get; set; }
        public static DB6Storage<BannedAddonsRecord> BannedAddOnsStorage { get; set; }
        public static DB6Storage<BarberShopStyleRecord> BarberShopStyleStorage { get; set; }
        public static DB6Storage<BattlePetBreedQualityRecord> BattlePetBreedQualityStorage { get; set; }
        public static DB6Storage<BattlePetBreedStateRecord> BattlePetBreedStateStorage { get; set; }
        public static DB6Storage<BattlePetSpeciesRecord> BattlePetSpeciesStorage { get; set; }
        public static DB6Storage<BattlePetSpeciesStateRecord> BattlePetSpeciesStateStorage { get; set; }
        public static DB6Storage<BattlemasterListRecord> BattlemasterListStorage { get; set; }
        public static DB6Storage<BroadcastTextRecord> BroadcastTextStorage { get; set; }
        public static DB6Storage<BroadcastTextDurationRecord> BroadcastTextDurationStorage { get; set; }
        public static DB6Storage<Cfg_RegionsRecord> CfgRegionsStorage { get; set; }
        public static DB6Storage<CharTitlesRecord> CharTitlesStorage { get; set; }
        public static DB6Storage<CharacterLoadoutRecord> CharacterLoadoutStorage { get; set; }
        public static DB6Storage<CharacterLoadoutItemRecord> CharacterLoadoutItemStorage { get; set; }
        public static DB6Storage<ChatChannelsRecord> ChatChannelsStorage { get; set; }
        public static DB6Storage<ChrClassUIDisplayRecord> ChrClassUIDisplayStorage { get; set; }
        public static DB6Storage<ChrClassesRecord> ChrClassesStorage { get; set; }
        public static DB6Storage<ChrClassesXPowerTypesRecord> ChrClassesXPowerTypesStorage { get; set; }
        public static DB6Storage<ChrCustomizationChoiceRecord> ChrCustomizationChoiceStorage { get; set; }
        public static DB6Storage<ChrCustomizationDisplayInfoRecord> ChrCustomizationDisplayInfoStorage { get; set; }
        public static DB6Storage<ChrCustomizationElementRecord> ChrCustomizationElementStorage { get; set; }
        public static DB6Storage<ChrCustomizationReqRecord> ChrCustomizationReqStorage { get; set; }
        public static DB6Storage<ChrCustomizationReqChoiceRecord> ChrCustomizationReqChoiceStorage { get; set; }
        public static DB6Storage<ChrModelRecord> ChrModelStorage { get; set; }
        public static DB6Storage<ChrRaceXChrModelRecord> ChrRaceXChrModelStorage { get; set; }
        public static DB6Storage<ChrCustomizationOptionRecord> ChrCustomizationOptionStorage { get; set; }
        public static DB6Storage<ChrRacesRecord> ChrRacesStorage { get; set; }
        public static DB6Storage<ChrSpecializationRecord> ChrSpecializationStorage { get; set; }
        public static DB6Storage<CinematicCameraRecord> CinematicCameraStorage { get; set; }
        public static DB6Storage<CinematicSequencesRecord> CinematicSequencesStorage { get; set; }
        public static DB6Storage<ContentTuningRecord> ContentTuningStorage { get; set; }
        public static DB6Storage<ContentTuningXExpectedRecord> ContentTuningXExpectedStorage { get; set; }
        public static DB6Storage<ConversationLineRecord> ConversationLineStorage { get; set; }
        public static DB6Storage<CorruptionEffectsRecord> CorruptionEffectsStorage { get; set; }
        public static DB6Storage<CreatureDisplayInfoRecord> CreatureDisplayInfoStorage { get; set; }
        public static DB6Storage<CreatureDisplayInfoExtraRecord> CreatureDisplayInfoExtraStorage { get; set; }
        public static DB6Storage<CreatureFamilyRecord> CreatureFamilyStorage { get; set; }
        public static DB6Storage<CreatureModelDataRecord> CreatureModelDataStorage { get; set; }
        public static DB6Storage<CreatureTypeRecord> CreatureTypeStorage { get; set; }
        public static DB6Storage<CriteriaRecord> CriteriaStorage { get; set; }
        public static DB6Storage<CriteriaTreeRecord> CriteriaTreeStorage { get; set; }
        public static DB6Storage<CurrencyContainerRecord> CurrencyContainerStorage { get; set; }
        public static DB6Storage<CurrencyTypesRecord> CurrencyTypesStorage { get; set; }
        public static DB6Storage<CurveRecord> CurveStorage { get; set; }
        public static DB6Storage<CurvePointRecord> CurvePointStorage { get; set; }
        public static DB6Storage<DestructibleModelDataRecord> DestructibleModelDataStorage { get; set; }
        public static DB6Storage<DifficultyRecord> DifficultyStorage { get; set; }
        public static DB6Storage<DungeonEncounterRecord> DungeonEncounterStorage { get; set; }
        public static DB6Storage<DurabilityCostsRecord> DurabilityCostsStorage { get; set; }
        public static DB6Storage<DurabilityQualityRecord> DurabilityQualityStorage { get; set; }
        public static DB6Storage<EmotesRecord> EmotesStorage { get; set; }
        public static DB6Storage<EmotesTextRecord> EmotesTextStorage { get; set; }
        public static DB6Storage<EmotesTextSoundRecord> EmotesTextSoundStorage { get; set; }
        public static DB6Storage<ExpectedStatRecord> ExpectedStatStorage { get; set; }
        public static DB6Storage<ExpectedStatModRecord> ExpectedStatModStorage { get; set; }
        public static DB6Storage<FactionRecord> FactionStorage { get; set; }
        public static DB6Storage<FactionTemplateRecord> FactionTemplateStorage { get; set; }
        public static DB6Storage<FriendshipRepReactionRecord> FriendshipRepReactionStorage { get; set; }
        public static DB6Storage<FriendshipReputationRecord> FriendshipReputationStorage { get; set; }
        public static DB6Storage<GameObjectArtKitRecord> GameObjectArtKitStorage { get; set; }
        public static DB6Storage<GameObjectDisplayInfoRecord> GameObjectDisplayInfoStorage { get; set; }
        public static DB6Storage<GameObjectsRecord> GameObjectsStorage { get; set; }
        public static DB6Storage<GarrAbilityRecord> GarrAbilityStorage { get; set; }
        public static DB6Storage<GarrBuildingRecord> GarrBuildingStorage { get; set; }
        public static DB6Storage<GarrBuildingPlotInstRecord> GarrBuildingPlotInstStorage { get; set; }
        public static DB6Storage<GarrClassSpecRecord> GarrClassSpecStorage { get; set; }
        public static DB6Storage<GarrFollowerRecord> GarrFollowerStorage { get; set; }
        public static DB6Storage<GarrFollowerXAbilityRecord> GarrFollowerXAbilityStorage { get; set; }
        public static DB6Storage<GarrMissionRecord> GarrMissionStorage { get; set; }
        public static DB6Storage<GarrPlotBuildingRecord> GarrPlotBuildingStorage { get; set; }
        public static DB6Storage<GarrPlotRecord> GarrPlotStorage { get; set; }
        public static DB6Storage<GarrPlotInstanceRecord> GarrPlotInstanceStorage { get; set; }
        public static DB6Storage<GarrSiteLevelRecord> GarrSiteLevelStorage { get; set; }
        public static DB6Storage<GarrSiteLevelPlotInstRecord> GarrSiteLevelPlotInstStorage { get; set; }
        public static DB6Storage<GarrTalentTreeRecord> GarrTalentTreeStorage { get; set; }
        public static DB6Storage<GemPropertiesRecord> GemPropertiesStorage { get; set; }
        public static DB6Storage<GlobalCurveRecord> GlobalCurveStorage { get; set; }
        public static DB6Storage<GlyphBindableSpellRecord> GlyphBindableSpellStorage { get; set; }
        public static DB6Storage<GlyphPropertiesRecord> GlyphPropertiesStorage { get; set; }
        public static DB6Storage<GlyphRequiredSpecRecord> GlyphRequiredSpecStorage { get; set; }
        public static DB6Storage<GossipNPCOptionRecord> GossipNPCOptionStorage { get; set; }
        public static DB6Storage<GuildColorBackgroundRecord> GuildColorBackgroundStorage { get; set; }
        public static DB6Storage<GuildColorBorderRecord> GuildColorBorderStorage { get; set; }
        public static DB6Storage<GuildColorEmblemRecord> GuildColorEmblemStorage { get; set; }
        public static DB6Storage<GuildPerkSpellsRecord> GuildPerkSpellsStorage { get; set; }
        public static DB6Storage<HeirloomRecord> HeirloomStorage { get; set; }
        public static DB6Storage<HolidaysRecord> HolidaysStorage { get; set; }
        public static DB6Storage<ImportPriceArmorRecord> ImportPriceArmorStorage { get; set; }
        public static DB6Storage<ImportPriceQualityRecord> ImportPriceQualityStorage { get; set; }
        public static DB6Storage<ImportPriceShieldRecord> ImportPriceShieldStorage { get; set; }
        public static DB6Storage<ImportPriceWeaponRecord> ImportPriceWeaponStorage { get; set; }
        public static DB6Storage<ItemAppearanceRecord> ItemAppearanceStorage { get; set; }
        public static DB6Storage<ItemArmorQualityRecord> ItemArmorQualityStorage { get; set; }
        public static DB6Storage<ItemArmorShieldRecord> ItemArmorShieldStorage { get; set; }

        public static DB6Storage<ItemArmorTotalRecord> ItemArmorTotalStorage { get; set; }

        //public static DB6Storage<ItemBagFamilyRecord> ItemBagFamilyStorage { get; set; }
        public static DB6Storage<ItemBonusRecord> ItemBonusStorage { get; set; }
        public static DB6Storage<ItemBonusListLevelDeltaRecord> ItemBonusListLevelDeltaStorage { get; set; }
        public static DB6Storage<ItemBonusTreeNodeRecord> ItemBonusTreeNodeStorage { get; set; }
        public static DB6Storage<ItemClassRecord> ItemClassStorage { get; set; }
        public static DB6Storage<ItemChildEquipmentRecord> ItemChildEquipmentStorage { get; set; }
        public static DB6Storage<ItemCurrencyCostRecord> ItemCurrencyCostStorage { get; set; }
        public static DB6Storage<ItemDamageRecord> ItemDamageAmmoStorage { get; set; }
        public static DB6Storage<ItemDamageRecord> ItemDamageOneHandStorage { get; set; }
        public static DB6Storage<ItemDamageRecord> ItemDamageOneHandCasterStorage { get; set; }
        public static DB6Storage<ItemDamageRecord> ItemDamageTwoHandStorage { get; set; }
        public static DB6Storage<ItemDamageRecord> ItemDamageTwoHandCasterStorage { get; set; }
        public static DB6Storage<ItemDisenchantLootRecord> ItemDisenchantLootStorage { get; set; }
        public static DB6Storage<ItemEffectRecord> ItemEffectStorage { get; set; }
        public static DB6Storage<ItemRecord> ItemStorage { get; set; }
        public static DB6Storage<ItemExtendedCostRecord> ItemExtendedCostStorage { get; set; }
        public static DB6Storage<ItemLevelSelectorRecord> ItemLevelSelectorStorage { get; set; }
        public static DB6Storage<ItemLevelSelectorQualityRecord> ItemLevelSelectorQualityStorage { get; set; }
        public static DB6Storage<ItemLevelSelectorQualitySetRecord> ItemLevelSelectorQualitySetStorage { get; set; }
        public static DB6Storage<ItemLimitCategoryRecord> ItemLimitCategoryStorage { get; set; }
        public static DB6Storage<ItemLimitCategoryConditionRecord> ItemLimitCategoryConditionStorage { get; set; }
        public static DB6Storage<ItemModifiedAppearanceRecord> ItemModifiedAppearanceStorage { get; set; }
        public static DB6Storage<ItemModifiedAppearanceExtraRecord> ItemModifiedAppearanceExtraStorage { get; set; }
        public static DB6Storage<ItemNameDescriptionRecord> ItemNameDescriptionStorage { get; set; }
        public static DB6Storage<ItemPriceBaseRecord> ItemPriceBaseStorage { get; set; }
        public static DB6Storage<ItemSearchNameRecord> ItemSearchNameStorage { get; set; }
        public static DB6Storage<ItemSetRecord> ItemSetStorage { get; set; }
        public static DB6Storage<ItemSetSpellRecord> ItemSetSpellStorage { get; set; }
        public static DB6Storage<ItemSparseRecord> ItemSparseStorage { get; set; }
        public static DB6Storage<ItemSpecRecord> ItemSpecStorage { get; set; }
        public static DB6Storage<ItemSpecOverrideRecord> ItemSpecOverrideStorage { get; set; }
        public static DB6Storage<ItemXBonusTreeRecord> ItemXBonusTreeStorage { get; set; }
        public static DB6Storage<ItemXItemEffectRecord> ItemXItemEffectStorage { get; set; }
        public static DB6Storage<JournalEncounterRecord> JournalEncounterStorage { get; set; }
        public static DB6Storage<JournalEncounterSectionRecord> JournalEncounterSectionStorage { get; set; }
        public static DB6Storage<JournalInstanceRecord> JournalInstanceStorage { get; set; }

        public static DB6Storage<JournalTierRecord> JournalTierStorage { get; set; }

        //public static DB6Storage<KeyChainRecord> KeyChainStorage { get; set; }
        public static DB6Storage<KeystoneAffixRecord> KeystoneAffixStorage { get; set; }
        public static DB6Storage<LanguageWordsRecord> LanguageWordsStorage { get; set; }
        public static DB6Storage<LanguagesRecord> LanguagesStorage { get; set; }
        public static DB6Storage<LFGDungeonsRecord> LFGDungeonsStorage { get; set; }
        public static DB6Storage<LightRecord> LightStorage { get; set; }
        public static DB6Storage<LiquidTypeRecord> LiquidTypeStorage { get; set; }
        public static DB6Storage<LockRecord> LockStorage { get; set; }
        public static DB6Storage<MailTemplateRecord> MailTemplateStorage { get; set; }
        public static DB6Storage<MapRecord> MapStorage { get; set; }
        public static DB6Storage<MapChallengeModeRecord> MapChallengeModeStorage { get; set; }
        public static DB6Storage<MapDifficultyRecord> MapDifficultyStorage { get; set; }
        public static DB6Storage<MapDifficultyXConditionRecord> MapDifficultyXConditionStorage { get; set; }
        public static DB6Storage<MawPowerRecord> MawPowerStorage { get; set; }
        public static DB6Storage<ModifierTreeRecord> ModifierTreeStorage { get; set; }
        public static DB6Storage<MountCapabilityRecord> MountCapabilityStorage { get; set; }
        public static DB6Storage<MountRecord> MountStorage { get; set; }
        public static DB6Storage<MountTypeXCapabilityRecord> MountTypeXCapabilityStorage { get; set; }
        public static DB6Storage<MountXDisplayRecord> MountXDisplayStorage { get; set; }
        public static DB6Storage<MovieRecord> MovieStorage { get; set; }
        public static DB6Storage<NameGenRecord> NameGenStorage { get; set; }
        public static DB6Storage<NamesProfanityRecord> NamesProfanityStorage { get; set; }
        public static DB6Storage<NamesReservedRecord> NamesReservedStorage { get; set; }
        public static DB6Storage<NamesReservedLocaleRecord> NamesReservedLocaleStorage { get; set; }
        public static DB6Storage<NumTalentsAtLevelRecord> NumTalentsAtLevelStorage { get; set; }
        public static DB6Storage<OverrideSpellDataRecord> OverrideSpellDataStorage { get; set; }
        public static DB6Storage<ParagonReputationRecord> ParagonReputationStorage { get; set; }
        public static DB6Storage<PhaseRecord> PhaseStorage { get; set; }
        public static DB6Storage<PhaseXPhaseGroupRecord> PhaseXPhaseGroupStorage { get; set; }
        public static DB6Storage<PlayerConditionRecord> PlayerConditionStorage { get; set; }
        public static DB6Storage<PowerDisplayRecord> PowerDisplayStorage { get; set; }
        public static DB6Storage<PowerTypeRecord> PowerTypeStorage { get; set; }
        public static DB6Storage<PrestigeLevelInfoRecord> PrestigeLevelInfoStorage { get; set; }
        public static DB6Storage<PvpDifficultyRecord> PvpDifficultyStorage { get; set; }
        public static DB6Storage<PvpItemRecord> PvpItemStorage { get; set; }
        public static DB6Storage<PvpTalentRecord> PvpTalentStorage { get; set; }
        public static DB6Storage<PvpTalentCategoryRecord> PvpTalentCategoryStorage { get; set; }
        public static DB6Storage<PvpTalentSlotUnlockRecord> PvpTalentSlotUnlockStorage { get; set; }
        public static DB6Storage<PvpTierRecord> PvpTierStorage { get; set; }
        public static DB6Storage<QuestFactionRewardRecord> QuestFactionRewardStorage { get; set; }
        public static DB6Storage<QuestInfoRecord> QuestInfoStorage { get; set; }
        public static DB6Storage<QuestLineXQuestRecord> QuestLineXQuestStorage { get; set; }
        public static DB6Storage<QuestMoneyRewardRecord> QuestMoneyRewardStorage { get; set; }
        public static DB6Storage<QuestPackageItemRecord> QuestPackageItemStorage { get; set; }
        public static DB6Storage<QuestSortRecord> QuestSortStorage { get; set; }
        public static DB6Storage<QuestV2Record> QuestV2Storage { get; set; }
        public static DB6Storage<QuestXPRecord> QuestXPStorage { get; set; }
        public static DB6Storage<RandPropPointsRecord> RandPropPointsStorage { get; set; }
        public static DB6Storage<RewardPackRecord> RewardPackStorage { get; set; }
        public static DB6Storage<RewardPackXCurrencyTypeRecord> RewardPackXCurrencyTypeStorage { get; set; }
        public static DB6Storage<RewardPackXItemRecord> RewardPackXItemStorage { get; set; }
        public static DB6Storage<ScenarioRecord> ScenarioStorage { get; set; }
        public static DB6Storage<ScenarioStepRecord> ScenarioStepStorage { get; set; }
        public static DB6Storage<SceneScriptRecord> SceneScriptStorage { get; set; }
        public static DB6Storage<SceneScriptGlobalTextRecord> SceneScriptGlobalTextStorage { get; set; }
        public static DB6Storage<SceneScriptPackageRecord> SceneScriptPackageStorage { get; set; }
        public static DB6Storage<SceneScriptTextRecord> SceneScriptTextStorage { get; set; }
        public static DB6Storage<SkillLineRecord> SkillLineStorage { get; set; }
        public static DB6Storage<SkillLineAbilityRecord> SkillLineAbilityStorage { get; set; }
        public static DB6Storage<SkillLineXTraitTreeRecord> SkillLineXTraitTreeStorage { get; set; }
        public static DB6Storage<SkillRaceClassInfoRecord> SkillRaceClassInfoStorage { get; set; }
        public static DB6Storage<SoulbindConduitRankRecord> SoulbindConduitRankStorage { get; set; }
        public static DB6Storage<SoundKitRecord> SoundKitStorage { get; set; }
        public static DB6Storage<SpecializationSpellsRecord> SpecializationSpellsStorage { get; set; }
        public static DB6Storage<SpecSetMemberRecord> SpecSetMemberStorage { get; set; }
        public static DB6Storage<SpellAuraOptionsRecord> SpellAuraOptionsStorage { get; set; }
        public static DB6Storage<SpellAuraRestrictionsRecord> SpellAuraRestrictionsStorage { get; set; }
        public static DB6Storage<SpellCastTimesRecord> SpellCastTimesStorage { get; set; }
        public static DB6Storage<SpellCastingRequirementsRecord> SpellCastingRequirementsStorage { get; set; }
        public static DB6Storage<SpellCategoriesRecord> SpellCategoriesStorage { get; set; }
        public static DB6Storage<SpellCategoryRecord> SpellCategoryStorage { get; set; }
        public static DB6Storage<SpellClassOptionsRecord> SpellClassOptionsStorage { get; set; }
        public static DB6Storage<SpellCooldownsRecord> SpellCooldownsStorage { get; set; }
        public static DB6Storage<SpellDurationRecord> SpellDurationStorage { get; set; }
        public static DB6Storage<SpellEffectRecord> SpellEffectStorage { get; set; }
        public static DB6Storage<SpellEquippedItemsRecord> SpellEquippedItemsStorage { get; set; }
        public static DB6Storage<SpellFocusObjectRecord> SpellFocusObjectStorage { get; set; }
        public static DB6Storage<SpellInterruptsRecord> SpellInterruptsStorage { get; set; }
        public static DB6Storage<SpellItemEnchantmentRecord> SpellItemEnchantmentStorage { get; set; }
        public static DB6Storage<SpellItemEnchantmentConditionRecord> SpellItemEnchantmentConditionStorage { get; set; }
        public static DB6Storage<SpellLabelRecord> SpellLabelStorage { get; set; }
        public static DB6Storage<SpellLearnSpellRecord> SpellLearnSpellStorage { get; set; }
        public static DB6Storage<SpellLevelsRecord> SpellLevelsStorage { get; set; }
        public static DB6Storage<SpellMiscRecord> SpellMiscStorage { get; set; }
        public static DB6Storage<SpellNameRecord> SpellNameStorage { get; set; }
        public static DB6Storage<SpellPowerRecord> SpellPowerStorage { get; set; }
        public static DB6Storage<SpellPowerDifficultyRecord> SpellPowerDifficultyStorage { get; set; }
        public static DB6Storage<SpellProcsPerMinuteRecord> SpellProcsPerMinuteStorage { get; set; }
        public static DB6Storage<SpellProcsPerMinuteModRecord> SpellProcsPerMinuteModStorage { get; set; }
        public static DB6Storage<SpellRadiusRecord> SpellRadiusStorage { get; set; }
        public static DB6Storage<SpellRangeRecord> SpellRangeStorage { get; set; }
        public static DB6Storage<SpellReagentsRecord> SpellReagentsStorage { get; set; }
        public static DB6Storage<SpellReagentsCurrencyRecord> SpellReagentsCurrencyStorage { get; set; }
        public static DB6Storage<SpellScalingRecord> SpellScalingStorage { get; set; }
        public static DB6Storage<SpellShapeshiftRecord> SpellShapeshiftStorage { get; set; }
        public static DB6Storage<SpellShapeshiftFormRecord> SpellShapeshiftFormStorage { get; set; }
        public static DB6Storage<SpellTargetRestrictionsRecord> SpellTargetRestrictionsStorage { get; set; }
        public static DB6Storage<SpellTotemsRecord> SpellTotemsStorage { get; set; }
        public static DB6Storage<SpellVisualRecord> SpellVisualStorage { get; set; }
        public static DB6Storage<SpellVisualEffectNameRecord> SpellVisualEffectNameStorage { get; set; }
        public static DB6Storage<SpellVisualMissileRecord> SpellVisualMissileStorage { get; set; }
        public static DB6Storage<SpellVisualKitRecord> SpellVisualKitStorage { get; set; }
        public static DB6Storage<SpellXSpellVisualRecord> SpellXSpellVisualStorage { get; set; }
        public static DB6Storage<SummonPropertiesRecord> SummonPropertiesStorage { get; set; }
        public static DB6Storage<TactKeyRecord> TactKeyStorage { get; set; }
        public static DB6Storage<TalentRecord> TalentStorage { get; set; }
        public static DB6Storage<TaxiNodesRecord> TaxiNodesStorage { get; set; }
        public static DB6Storage<TaxiPathRecord> TaxiPathStorage { get; set; }
        public static DB6Storage<TaxiPathNodeRecord> TaxiPathNodeStorage { get; set; }
        public static DB6Storage<TotemCategoryRecord> TotemCategoryStorage { get; set; }
        public static DB6Storage<ToyRecord> ToyStorage { get; set; }
        public static DB6Storage<TraitCondRecord> TraitCondStorage { get; set; }
        public static DB6Storage<TraitCostRecord> TraitCostStorage { get; set; }
        public static DB6Storage<TraitCurrencyRecord> TraitCurrencyStorage { get; set; }
        public static DB6Storage<TraitCurrencySourceRecord> TraitCurrencySourceStorage { get; set; }
        public static DB6Storage<TraitDefinitionRecord> TraitDefinitionStorage { get; set; }
        public static DB6Storage<TraitDefinitionEffectPointsRecord> TraitDefinitionEffectPointsStorage { get; set; }
        public static DB6Storage<TraitEdgeRecord> TraitEdgeStorage { get; set; }
        public static DB6Storage<TraitNodeRecord> TraitNodeStorage { get; set; }
        public static DB6Storage<TraitNodeEntryRecord> TraitNodeEntryStorage { get; set; }
        public static DB6Storage<TraitNodeEntryXTraitCondRecord> TraitNodeEntryXTraitCondStorage { get; set; }
        public static DB6Storage<TraitNodeEntryXTraitCostRecord> TraitNodeEntryXTraitCostStorage { get; set; }
        public static DB6Storage<TraitNodeGroupRecord> TraitNodeGroupStorage { get; set; }
        public static DB6Storage<TraitNodeGroupXTraitCondRecord> TraitNodeGroupXTraitCondStorage { get; set; }
        public static DB6Storage<TraitNodeGroupXTraitCostRecord> TraitNodeGroupXTraitCostStorage { get; set; }
        public static DB6Storage<TraitNodeGroupXTraitNodeRecord> TraitNodeGroupXTraitNodeStorage { get; set; }
        public static DB6Storage<TraitNodeXTraitCondRecord> TraitNodeXTraitCondStorage { get; set; }
        public static DB6Storage<TraitNodeXTraitCostRecord> TraitNodeXTraitCostStorage { get; set; }
        public static DB6Storage<TraitNodeXTraitNodeEntryRecord> TraitNodeXTraitNodeEntryStorage { get; set; }
        public static DB6Storage<TraitTreeRecord> TraitTreeStorage { get; set; }
        public static DB6Storage<TraitTreeLoadoutRecord> TraitTreeLoadoutStorage { get; set; }
        public static DB6Storage<TraitTreeLoadoutEntryRecord> TraitTreeLoadoutEntryStorage { get; set; }
        public static DB6Storage<TraitTreeXTraitCostRecord> TraitTreeXTraitCostStorage { get; set; }
        public static DB6Storage<TraitTreeXTraitCurrencyRecord> TraitTreeXTraitCurrencyStorage { get; set; }
        public static DB6Storage<TransmogHolidayRecord> TransmogHolidayStorage { get; set; }
        public static DB6Storage<TransmogIllusionRecord> TransmogIllusionStorage { get; set; }
        public static DB6Storage<TransmogSetRecord> TransmogSetStorage { get; set; }
        public static DB6Storage<TransmogSetGroupRecord> TransmogSetGroupStorage { get; set; }
        public static DB6Storage<TransmogSetItemRecord> TransmogSetItemStorage { get; set; }
        public static DB6Storage<TransportAnimationRecord> TransportAnimationStorage { get; set; }
        public static DB6Storage<TransportRotationRecord> TransportRotationStorage { get; set; }
        public static DB6Storage<UiMapRecord> UiMapStorage { get; set; }
        public static DB6Storage<UiMapAssignmentRecord> UiMapAssignmentStorage { get; set; }
        public static DB6Storage<UiMapLinkRecord> UiMapLinkStorage { get; set; }
        public static DB6Storage<UiMapXMapArtRecord> UiMapXMapArtStorage { get; set; }
        public static DB6Storage<UISplashScreenRecord> UISplashScreenStorage { get; set; }
        public static DB6Storage<UnitConditionRecord> UnitConditionStorage { get; set; }
        public static DB6Storage<UnitPowerBarRecord> UnitPowerBarStorage { get; set; }
        public static DB6Storage<VehicleRecord> VehicleStorage { get; set; }
        public static DB6Storage<VehicleSeatRecord> VehicleSeatStorage { get; set; }
        public static DB6Storage<WMOAreaTableRecord> WMOAreaTableStorage { get; set; }
        public static DB6Storage<WorldEffectRecord> WorldEffectStorage { get; set; }
        public static DB6Storage<WorldMapOverlayRecord> WorldMapOverlayStorage { get; set; }
        public static DB6Storage<WorldStateExpressionRecord> WorldStateExpressionStorage { get; set; }
        public static DB6Storage<CharBaseInfo> CharBaseInfoStorage { get; set; }

        #endregion

        #region GameTables

        public static GameTable<GtArtifactKnowledgeMultiplierRecord> ArtifactKnowledgeMultiplierGameTable { get; set; }
        public static GameTable<GtArtifactLevelXPRecord> ArtifactLevelXPGameTable { get; set; }
        public static GameTable<GtBarberShopCostBaseRecord> BarberShopCostBaseGameTable { get; set; }
        public static GameTable<GtBaseMPRecord> BaseMPGameTable { get; set; }
        public static GameTable<GtBattlePetXPRecord> BattlePetXPGameTable { get; set; }
        public static GameTable<GtCombatRatingsRecord> CombatRatingsGameTable { get; set; }
        public static GameTable<GtGenericMultByILvlRecord> CombatRatingsMultByILvlGameTable { get; set; }
        public static GameTable<GtHpPerStaRecord> HpPerStaGameTable { get; set; }
        public static GameTable<GtItemSocketCostPerLevelRecord> ItemSocketCostPerLevelGameTable { get; set; }
        public static GameTable<GtNpcManaCostScalerRecord> NpcManaCostScalerGameTable { get; set; }
        public static GameTable<GtSpellScalingRecord> SpellScalingGameTable { get; set; }
        public static GameTable<GtGenericMultByILvlRecord> StaminaMultByILvlGameTable { get; set; }
        public static GameTable<GtXpRecord> XpGameTable { get; set; }

        #endregion

        #region Taxi Collections

        public static byte[] TaxiNodesMask { get; set; }
        public static byte[] OldContinentsNodesMask { get; set; }
        public static byte[] HordeTaxiNodesMask { get; set; }
        public static byte[] AllianceTaxiNodesMask { get; set; }
        public static Dictionary<uint, Dictionary<uint, TaxiPathBySourceAndDestination>> TaxiPathSetBySource { get; set; } = new();
        public static Dictionary<uint, TaxiPathNodeRecord[]> TaxiPathNodesByPath { get; set; } = new();

        #endregion

        public static BitSet LoadStores(string dataPath, Locale defaultLocale)
        {
            uint oldMSTime = Time.GetMSTime();

            string db2Path = dataPath + "/dbc";

            BitSet availableDb2Locales = new((int)Locale.Total);

            foreach (var dir in Directory.GetDirectories(db2Path))
            {
                Locale locale = Path.GetFileName(dir).ToEnum<Locale>();

                if (SharedConst.IsValidLocale(locale))
                    availableDb2Locales[(int)locale] = true;
            }

            if (!availableDb2Locales[(int)defaultLocale])
                return null;

            uint loadedFileCount = 0;

            DB6Storage<T> ReadDB2<T>(string fileName, HotfixStatements preparedStatement, HotfixStatements preparedStatementLocale = 0) where T : new()
            {
                return DBReader.Read<T>(availableDb2Locales, $"{db2Path}/{defaultLocale}/", fileName, preparedStatement, preparedStatementLocale, ref loadedFileCount);
            }

            AchievementStorage = ReadDB2<AchievementRecord>("Achievement.db2", HotfixStatements.SEL_ACHIEVEMENT, HotfixStatements.SEL_ACHIEVEMENT_LOCALE);
            AchievementCategoryStorage = ReadDB2<AchievementCategoryRecord>("Achievement_Category.db2", HotfixStatements.SEL_ACHIEVEMENT_CATEGORY, HotfixStatements.SEL_ACHIEVEMENT_CATEGORY_LOCALE);
            AdventureJournalStorage = ReadDB2<AdventureJournalRecord>("AdventureJournal.db2", HotfixStatements.SEL_ADVENTURE_JOURNAL, HotfixStatements.SEL_ADVENTURE_JOURNAL_LOCALE);
            AdventureMapPOIStorage = ReadDB2<AdventureMapPOIRecord>("AdventureMapPOI.db2", HotfixStatements.SEL_ADVENTURE_MAP_POI, HotfixStatements.SEL_ADVENTURE_MAP_POI_LOCALE);
            AnimationDataStorage = ReadDB2<AnimationDataRecord>("AnimationData.db2", HotfixStatements.SEL_ANIMATION_DATA);
            AnimKitStorage = ReadDB2<AnimKitRecord>("AnimKit.db2", HotfixStatements.SEL_ANIM_KIT);
            AreaGroupMemberStorage = ReadDB2<AreaGroupMemberRecord>("AreaGroupMember.db2", HotfixStatements.SEL_AREA_GROUP_MEMBER);
            AreaTableStorage = ReadDB2<AreaTableRecord>("AreaTable.db2", HotfixStatements.SEL_AREA_TABLE, HotfixStatements.SEL_AREA_TABLE_LOCALE);
            AreaTriggerStorage = ReadDB2<AreaTriggerRecord>("AreaTrigger.db2", HotfixStatements.SEL_AREA_TRIGGER);
            ArmorLocationStorage = ReadDB2<ArmorLocationRecord>("ArmorLocation.db2", HotfixStatements.SEL_ARMOR_LOCATION);
            ArtifactStorage = ReadDB2<ArtifactRecord>("Artifact.db2", HotfixStatements.SEL_ARTIFACT, HotfixStatements.SEL_ARTIFACT_APPEARANCE_LOCALE);
            ArtifactAppearanceStorage = ReadDB2<ArtifactAppearanceRecord>("ArtifactAppearance.db2", HotfixStatements.SEL_ARTIFACT_APPEARANCE, HotfixStatements.SEL_ARTIFACT_APPEARANCE_LOCALE);
            ArtifactAppearanceSetStorage = ReadDB2<ArtifactAppearanceSetRecord>("ArtifactAppearanceSet.db2", HotfixStatements.SEL_ARTIFACT_APPEARANCE_SET, HotfixStatements.SEL_ARTIFACT_APPEARANCE_SET_LOCALE);
            //ArtifactCategoryStorage = ReadDB2<ArtifactCategoryRecord>("ArtifactCategory.db2", HotfixStatements.SEL_ARTIFACT_CATEGORY);
            ArtifactPowerStorage = ReadDB2<ArtifactPowerRecord>("ArtifactPower.db2", HotfixStatements.SEL_ARTIFACT_POWER);
            ArtifactPowerLinkStorage = ReadDB2<ArtifactPowerLinkRecord>("ArtifactPowerLink.db2", HotfixStatements.SEL_ARTIFACT_POWER_LINK);
            ArtifactPowerPickerStorage = ReadDB2<ArtifactPowerPickerRecord>("ArtifactPowerPicker.db2", HotfixStatements.SEL_ARTIFACT_POWER_PICKER);
            ArtifactPowerRankStorage = ReadDB2<ArtifactPowerRankRecord>("ArtifactPowerRank.db2", HotfixStatements.SEL_ARTIFACT_POWER_RANK);
            //ArtifactQuestXPStorage = ReadDB2<ArtifactQuestXPRecord>("ArtifactQuestXP.db2", HotfixStatements.SEL_ARTIFACT_QUEST_XP);
            ArtifactTierStorage = ReadDB2<ArtifactTierRecord>("ArtifactTier.db2", HotfixStatements.SEL_ARTIFACT_TIER);
            ArtifactUnlockStorage = ReadDB2<ArtifactUnlockRecord>("ArtifactUnlock.db2", HotfixStatements.SEL_ARTIFACT_UNLOCK);
            AuctionHouseStorage = ReadDB2<AuctionHouseRecord>("AuctionHouse.db2", HotfixStatements.SEL_AUCTION_HOUSE, HotfixStatements.SEL_AUCTION_HOUSE_LOCALE);
            AzeriteEmpoweredItemStorage = ReadDB2<AzeriteEmpoweredItemRecord>("AzeriteEmpoweredItem.db2", HotfixStatements.SEL_AZERITE_EMPOWERED_ITEM);
            AzeriteEssenceStorage = ReadDB2<AzeriteEssenceRecord>("AzeriteEssence.db2", HotfixStatements.SEL_AZERITE_ESSENCE, HotfixStatements.SEL_AZERITE_ESSENCE_LOCALE);
            AzeriteEssencePowerStorage = ReadDB2<AzeriteEssencePowerRecord>("AzeriteEssencePower.db2", HotfixStatements.SEL_AZERITE_ESSENCE_POWER, HotfixStatements.SEL_AZERITE_ESSENCE_POWER_LOCALE);
            AzeriteItemStorage = ReadDB2<AzeriteItemRecord>("AzeriteItem.db2", HotfixStatements.SEL_AZERITE_ITEM);
            AzeriteItemMilestonePowerStorage = ReadDB2<AzeriteItemMilestonePowerRecord>("AzeriteItemMilestonePower.db2", HotfixStatements.SEL_AZERITE_ITEM_MILESTONE_POWER);
            AzeriteKnowledgeMultiplierStorage = ReadDB2<AzeriteKnowledgeMultiplierRecord>("AzeriteKnowledgeMultiplier.db2", HotfixStatements.SEL_AZERITE_KNOWLEDGE_MULTIPLIER);
            AzeriteLevelInfoStorage = ReadDB2<AzeriteLevelInfoRecord>("AzeriteLevelInfo.db2", HotfixStatements.SEL_AZERITE_LEVEL_INFO);
            AzeritePowerStorage = ReadDB2<AzeritePowerRecord>("AzeritePower.db2", HotfixStatements.SEL_AZERITE_POWER);
            AzeritePowerSetMemberStorage = ReadDB2<AzeritePowerSetMemberRecord>("AzeritePowerSetMember.db2", HotfixStatements.SEL_AZERITE_POWER_SET_MEMBER);
            AzeriteTierUnlockStorage = ReadDB2<AzeriteTierUnlockRecord>("AzeriteTierUnlock.db2", HotfixStatements.SEL_AZERITE_TIER_UNLOCK);
            AzeriteTierUnlockSetStorage = ReadDB2<AzeriteTierUnlockSetRecord>("AzeriteTierUnlockSet.db2", HotfixStatements.SEL_AZERITE_TIER_UNLOCK_SET);
            AzeriteUnlockMappingStorage = ReadDB2<AzeriteUnlockMappingRecord>("AzeriteUnlockMapping.db2", HotfixStatements.SEL_AZERITE_UNLOCK_MAPPING);
            BankBagSlotPricesStorage = ReadDB2<BankBagSlotPricesRecord>("BankBagSlotPrices.db2", HotfixStatements.SEL_BANK_BAG_SLOT_PRICES);
            BannedAddOnsStorage = ReadDB2<BannedAddonsRecord>("BannedAddons.db2", HotfixStatements.SEL_BANNED_ADDONS);
            BarberShopStyleStorage = ReadDB2<BarberShopStyleRecord>("BarberShopStyle.db2", HotfixStatements.SEL_BARBER_SHOP_STYLE, HotfixStatements.SEL_BARBER_SHOP_STYLE_LOCALE);
            BattlePetBreedQualityStorage = ReadDB2<BattlePetBreedQualityRecord>("BattlePetBreedQuality.db2", HotfixStatements.SEL_BATTLE_PET_BREED_QUALITY);
            BattlePetBreedStateStorage = ReadDB2<BattlePetBreedStateRecord>("BattlePetBreedState.db2", HotfixStatements.SEL_BATTLE_PET_BREED_STATE);
            BattlePetSpeciesStorage = ReadDB2<BattlePetSpeciesRecord>("BattlePetSpecies.db2", HotfixStatements.SEL_BATTLE_PET_SPECIES, HotfixStatements.SEL_BATTLE_PET_SPECIES_LOCALE);
            BattlePetSpeciesStateStorage = ReadDB2<BattlePetSpeciesStateRecord>("BattlePetSpeciesState.db2", HotfixStatements.SEL_BATTLE_PET_SPECIES_STATE);
            BattlemasterListStorage = ReadDB2<BattlemasterListRecord>("BattlemasterList.db2", HotfixStatements.SEL_BATTLEMASTER_LIST, HotfixStatements.SEL_BATTLEMASTER_LIST_LOCALE);
            BroadcastTextStorage = ReadDB2<BroadcastTextRecord>("BroadcastText.db2", HotfixStatements.SEL_BROADCAST_TEXT, HotfixStatements.SEL_BROADCAST_TEXT_LOCALE);
            BroadcastTextDurationStorage = ReadDB2<BroadcastTextDurationRecord>("BroadcastTextDuration.db2", HotfixStatements.SEL_BROADCAST_TEXT_DURATION);
            CfgRegionsStorage = ReadDB2<Cfg_RegionsRecord>("Cfg_Regions.db2", HotfixStatements.SEL_CFG_REGIONS);
            CharTitlesStorage = ReadDB2<CharTitlesRecord>("CharTitles.db2", HotfixStatements.SEL_CHAR_TITLES, HotfixStatements.SEL_CHAR_TITLES_LOCALE);
            CharacterLoadoutStorage = ReadDB2<CharacterLoadoutRecord>("CharacterLoadout.db2", HotfixStatements.SEL_CHARACTER_LOADOUT);
            CharacterLoadoutItemStorage = ReadDB2<CharacterLoadoutItemRecord>("CharacterLoadoutItem.db2", HotfixStatements.SEL_CHARACTER_LOADOUT_ITEM);
            ChatChannelsStorage = ReadDB2<ChatChannelsRecord>("ChatChannels.db2", HotfixStatements.SEL_CHAT_CHANNELS, HotfixStatements.SEL_CHAT_CHANNELS_LOCALE);
            ChrClassUIDisplayStorage = ReadDB2<ChrClassUIDisplayRecord>("ChrClassUIDisplay.db2", HotfixStatements.SEL_CHR_CLASS_UI_DISPLAY);
            ChrClassesStorage = ReadDB2<ChrClassesRecord>("ChrClasses.db2", HotfixStatements.SEL_CHR_CLASSES, HotfixStatements.SEL_CHR_CLASSES_LOCALE);
            ChrClassesXPowerTypesStorage = ReadDB2<ChrClassesXPowerTypesRecord>("ChrClassesXPowerTypes.db2", HotfixStatements.SEL_CHR_CLASSES_X_POWER_TYPES);
            ChrCustomizationChoiceStorage = ReadDB2<ChrCustomizationChoiceRecord>("ChrCustomizationChoice.db2", HotfixStatements.SEL_CHR_CUSTOMIZATION_CHOICE, HotfixStatements.SEL_CHR_CUSTOMIZATION_CHOICE_LOCALE);
            ChrCustomizationDisplayInfoStorage = ReadDB2<ChrCustomizationDisplayInfoRecord>("ChrCustomizationDisplayInfo.db2", HotfixStatements.SEL_CHR_CUSTOMIZATION_DISPLAY_INFO);
            ChrCustomizationElementStorage = ReadDB2<ChrCustomizationElementRecord>("ChrCustomizationElement.db2", HotfixStatements.SEL_CHR_CUSTOMIZATION_ELEMENT);
            ChrCustomizationOptionStorage = ReadDB2<ChrCustomizationOptionRecord>("ChrCustomizationOption.db2", HotfixStatements.SEL_CHR_CUSTOMIZATION_OPTION, HotfixStatements.SEL_CHR_CUSTOMIZATION_OPTION_LOCALE);
            ChrCustomizationReqStorage = ReadDB2<ChrCustomizationReqRecord>("ChrCustomizationReq.db2", HotfixStatements.SEL_CHR_CUSTOMIZATION_REQ);
            ChrCustomizationReqChoiceStorage = ReadDB2<ChrCustomizationReqChoiceRecord>("ChrCustomizationReqChoice.db2", HotfixStatements.SEL_CHR_CUSTOMIZATION_REQ_CHOICE);
            ChrModelStorage = ReadDB2<ChrModelRecord>("ChrModel.db2", HotfixStatements.SEL_CHR_MODEL);
            ChrRaceXChrModelStorage = ReadDB2<ChrRaceXChrModelRecord>("ChrRaceXChrModel.db2", HotfixStatements.SEL_CHR_RACE_X_CHR_MODEL);
            ChrRacesStorage = ReadDB2<ChrRacesRecord>("ChrRaces.db2", HotfixStatements.SEL_CHR_RACES, HotfixStatements.SEL_CHR_RACES_LOCALE);
            ChrSpecializationStorage = ReadDB2<ChrSpecializationRecord>("ChrSpecialization.db2", HotfixStatements.SEL_CHR_SPECIALIZATION, HotfixStatements.SEL_CHR_SPECIALIZATION_LOCALE);
            CinematicCameraStorage = ReadDB2<CinematicCameraRecord>("CinematicCamera.db2", HotfixStatements.SEL_CINEMATIC_CAMERA);
            CinematicSequencesStorage = ReadDB2<CinematicSequencesRecord>("CinematicSequences.db2", HotfixStatements.SEL_CINEMATIC_SEQUENCES);
            ContentTuningStorage = ReadDB2<ContentTuningRecord>("ContentTuning.db2", HotfixStatements.SEL_CONTENT_TUNING);
            ContentTuningXExpectedStorage = ReadDB2<ContentTuningXExpectedRecord>("ContentTuningXExpected.db2", HotfixStatements.SEL_CONTENT_TUNING_X_EXPECTED);
            ConversationLineStorage = ReadDB2<ConversationLineRecord>("ConversationLine.db2", HotfixStatements.SEL_CONVERSATION_LINE);
            CorruptionEffectsStorage = ReadDB2<CorruptionEffectsRecord>("CorruptionEffects.db2", HotfixStatements.SEL_CORRUPTION_EFFECTS);
            CreatureDisplayInfoStorage = ReadDB2<CreatureDisplayInfoRecord>("CreatureDisplayInfo.db2", HotfixStatements.SEL_CREATURE_DISPLAY_INFO);
            CreatureDisplayInfoExtraStorage = ReadDB2<CreatureDisplayInfoExtraRecord>("CreatureDisplayInfoExtra.db2", HotfixStatements.SEL_CREATURE_DISPLAY_INFO_EXTRA);
            CreatureFamilyStorage = ReadDB2<CreatureFamilyRecord>("CreatureFamily.db2", HotfixStatements.SEL_CREATURE_FAMILY, HotfixStatements.SEL_CREATURE_FAMILY_LOCALE);
            CreatureModelDataStorage = ReadDB2<CreatureModelDataRecord>("CreatureModelData.db2", HotfixStatements.SEL_CREATURE_MODEL_DATA);
            CreatureTypeStorage = ReadDB2<CreatureTypeRecord>("CreatureType.db2", HotfixStatements.SEL_CREATURE_TYPE, HotfixStatements.SEL_CREATURE_TYPE_LOCALE);
            CriteriaStorage = ReadDB2<CriteriaRecord>("Criteria.db2", HotfixStatements.SEL_CRITERIA);
            CriteriaTreeStorage = ReadDB2<CriteriaTreeRecord>("CriteriaTree.db2", HotfixStatements.SEL_CRITERIA_TREE, HotfixStatements.SEL_CRITERIA_TREE_LOCALE);
            CurrencyContainerStorage = ReadDB2<CurrencyContainerRecord>("CurrencyContainer.db2", HotfixStatements.SEL_CURRENCY_CONTAINER, HotfixStatements.SEL_CURRENCY_CONTAINER_LOCALE);
            CurrencyTypesStorage = ReadDB2<CurrencyTypesRecord>("CurrencyTypes.db2", HotfixStatements.SEL_CURRENCY_TYPES, HotfixStatements.SEL_CURRENCY_TYPES_LOCALE);
            CurveStorage = ReadDB2<CurveRecord>("Curve.db2", HotfixStatements.SEL_CURVE);
            CurvePointStorage = ReadDB2<CurvePointRecord>("CurvePoint.db2", HotfixStatements.SEL_CURVE_POINT);
            DestructibleModelDataStorage = ReadDB2<DestructibleModelDataRecord>("DestructibleModelData.db2", HotfixStatements.SEL_DESTRUCTIBLE_MODEL_DATA);
            DifficultyStorage = ReadDB2<DifficultyRecord>("Difficulty.db2", HotfixStatements.SEL_DIFFICULTY, HotfixStatements.SEL_DIFFICULTY_LOCALE);
            DungeonEncounterStorage = ReadDB2<DungeonEncounterRecord>("DungeonEncounter.db2", HotfixStatements.SEL_DUNGEON_ENCOUNTER, HotfixStatements.SEL_DUNGEON_ENCOUNTER_LOCALE);
            DurabilityCostsStorage = ReadDB2<DurabilityCostsRecord>("DurabilityCosts.db2", HotfixStatements.SEL_DURABILITY_COSTS);
            DurabilityQualityStorage = ReadDB2<DurabilityQualityRecord>("DurabilityQuality.db2", HotfixStatements.SEL_DURABILITY_QUALITY);
            EmotesStorage = ReadDB2<EmotesRecord>("Emotes.db2", HotfixStatements.SEL_EMOTES);
            EmotesTextStorage = ReadDB2<EmotesTextRecord>("EmotesText.db2", HotfixStatements.SEL_EMOTES_TEXT);
            EmotesTextSoundStorage = ReadDB2<EmotesTextSoundRecord>("EmotesTextSound.db2", HotfixStatements.SEL_EMOTES_TEXT_SOUND);
            ExpectedStatStorage = ReadDB2<ExpectedStatRecord>("ExpectedStat.db2", HotfixStatements.SEL_EXPECTED_STAT);
            ExpectedStatModStorage = ReadDB2<ExpectedStatModRecord>("ExpectedStatMod.db2", HotfixStatements.SEL_EXPECTED_STAT_MOD);
            FactionStorage = ReadDB2<FactionRecord>("Faction.db2", HotfixStatements.SEL_FACTION, HotfixStatements.SEL_FACTION_LOCALE);
            FactionTemplateStorage = ReadDB2<FactionTemplateRecord>("FactionTemplate.db2", HotfixStatements.SEL_FACTION_TEMPLATE);
            FriendshipRepReactionStorage = ReadDB2<FriendshipRepReactionRecord>("FriendshipRepReaction.db2", HotfixStatements.SEL_FRIENDSHIP_REP_REACTION, HotfixStatements.SEL_FRIENDSHIP_REP_REACTION_LOCALE);
            FriendshipReputationStorage = ReadDB2<FriendshipReputationRecord>("FriendshipReputation.db2", HotfixStatements.SEL_FRIENDSHIP_REPUTATION, HotfixStatements.SEL_FRIENDSHIP_REPUTATION_LOCALE);
            GameObjectArtKitStorage = ReadDB2<GameObjectArtKitRecord>("GameObjectArtKit.db2", HotfixStatements.SEL_GAMEOBJECT_ART_KIT);
            GameObjectDisplayInfoStorage = ReadDB2<GameObjectDisplayInfoRecord>("GameObjectDisplayInfo.db2", HotfixStatements.SEL_GAMEOBJECT_DISPLAY_INFO);
            GameObjectsStorage = ReadDB2<GameObjectsRecord>("GameObjects.db2", HotfixStatements.SEL_GAMEOBJECTS, HotfixStatements.SEL_GAMEOBJECTS_LOCALE);
            GarrAbilityStorage = ReadDB2<GarrAbilityRecord>("GarrAbility.db2", HotfixStatements.SEL_GARR_ABILITY, HotfixStatements.SEL_GARR_ABILITY_LOCALE);
            GarrBuildingStorage = ReadDB2<GarrBuildingRecord>("GarrBuilding.db2", HotfixStatements.SEL_GARR_BUILDING, HotfixStatements.SEL_GARR_BUILDING_LOCALE);
            GarrBuildingPlotInstStorage = ReadDB2<GarrBuildingPlotInstRecord>("GarrBuildingPlotInst.db2", HotfixStatements.SEL_GARR_BUILDING_PLOT_INST);
            GarrClassSpecStorage = ReadDB2<GarrClassSpecRecord>("GarrClassSpec.db2", HotfixStatements.SEL_GARR_CLASS_SPEC, HotfixStatements.SEL_GARR_CLASS_SPEC_LOCALE);
            GarrFollowerStorage = ReadDB2<GarrFollowerRecord>("GarrFollower.db2", HotfixStatements.SEL_GARR_FOLLOWER, HotfixStatements.SEL_GARR_FOLLOWER_LOCALE);
            GarrFollowerXAbilityStorage = ReadDB2<GarrFollowerXAbilityRecord>("GarrFollowerXAbility.db2", HotfixStatements.SEL_GARR_FOLLOWER_X_ABILITY);
            GarrMissionStorage = ReadDB2<GarrMissionRecord>("GarrMission.db2", HotfixStatements.SEL_GARR_MISSION, HotfixStatements.SEL_GARR_MISSION_LOCALE);
            GarrPlotStorage = ReadDB2<GarrPlotRecord>("GarrPlot.db2", HotfixStatements.SEL_GARR_PLOT);
            GarrPlotBuildingStorage = ReadDB2<GarrPlotBuildingRecord>("GarrPlotBuilding.db2", HotfixStatements.SEL_GARR_PLOT_BUILDING);
            GarrPlotInstanceStorage = ReadDB2<GarrPlotInstanceRecord>("GarrPlotInstance.db2", HotfixStatements.SEL_GARR_PLOT_INSTANCE);
            GarrSiteLevelStorage = ReadDB2<GarrSiteLevelRecord>("GarrSiteLevel.db2", HotfixStatements.SEL_GARR_SITE_LEVEL);
            GarrSiteLevelPlotInstStorage = ReadDB2<GarrSiteLevelPlotInstRecord>("GarrSiteLevelPlotInst.db2", HotfixStatements.SEL_GARR_SITE_LEVEL_PLOT_INST);
            GarrTalentTreeStorage = ReadDB2<GarrTalentTreeRecord>("GarrTalentTree.db2", HotfixStatements.SEL_GARR_TALENT_TREE, HotfixStatements.SEL_GARR_TALENT_TREE_LOCALE);
            GemPropertiesStorage = ReadDB2<GemPropertiesRecord>("GemProperties.db2", HotfixStatements.SEL_GEM_PROPERTIES);
            GlobalCurveStorage = ReadDB2<GlobalCurveRecord>("GlobalCurve.db2", HotfixStatements.SEL_GLOBAL_CURVE);
            GlyphBindableSpellStorage = ReadDB2<GlyphBindableSpellRecord>("GlyphBindableSpell.db2", HotfixStatements.SEL_GLYPH_BINDABLE_SPELL);
            GlyphPropertiesStorage = ReadDB2<GlyphPropertiesRecord>("GlyphProperties.db2", HotfixStatements.SEL_GLYPH_PROPERTIES);
            GlyphRequiredSpecStorage = ReadDB2<GlyphRequiredSpecRecord>("GlyphRequiredSpec.db2", HotfixStatements.SEL_GLYPH_REQUIRED_SPEC);
            GossipNPCOptionStorage = ReadDB2<GossipNPCOptionRecord>("GossipNPCOption.db2", HotfixStatements.SEL_GOSSIP_NPC_OPTION);
            GuildColorBackgroundStorage = ReadDB2<GuildColorBackgroundRecord>("GuildColorBackground.db2", HotfixStatements.SEL_GUILD_COLOR_BACKGROUND);
            GuildColorBorderStorage = ReadDB2<GuildColorBorderRecord>("GuildColorBorder.db2", HotfixStatements.SEL_GUILD_COLOR_BORDER);
            GuildColorEmblemStorage = ReadDB2<GuildColorEmblemRecord>("GuildColorEmblem.db2", HotfixStatements.SEL_GUILD_COLOR_EMBLEM);
            GuildPerkSpellsStorage = ReadDB2<GuildPerkSpellsRecord>("GuildPerkSpells.db2", HotfixStatements.SEL_GUILD_PERK_SPELLS);
            HeirloomStorage = ReadDB2<HeirloomRecord>("Heirloom.db2", HotfixStatements.SEL_HEIRLOOM, HotfixStatements.SEL_HEIRLOOM_LOCALE);
            HolidaysStorage = ReadDB2<HolidaysRecord>("Holidays.db2", HotfixStatements.SEL_HOLIDAYS);
            ImportPriceArmorStorage = ReadDB2<ImportPriceArmorRecord>("ImportPriceArmor.db2", HotfixStatements.SEL_IMPORT_PRICE_ARMOR);
            ImportPriceQualityStorage = ReadDB2<ImportPriceQualityRecord>("ImportPriceQuality.db2", HotfixStatements.SEL_IMPORT_PRICE_QUALITY);
            ImportPriceShieldStorage = ReadDB2<ImportPriceShieldRecord>("ImportPriceShield.db2", HotfixStatements.SEL_IMPORT_PRICE_SHIELD);
            ImportPriceWeaponStorage = ReadDB2<ImportPriceWeaponRecord>("ImportPriceWeapon.db2", HotfixStatements.SEL_IMPORT_PRICE_WEAPON);
            ItemAppearanceStorage = ReadDB2<ItemAppearanceRecord>("ItemAppearance.db2", HotfixStatements.SEL_ITEM_APPEARANCE);
            ItemArmorQualityStorage = ReadDB2<ItemArmorQualityRecord>("ItemArmorQuality.db2", HotfixStatements.SEL_ITEM_ARMOR_QUALITY);
            ItemArmorShieldStorage = ReadDB2<ItemArmorShieldRecord>("ItemArmorShield.db2", HotfixStatements.SEL_ITEM_ARMOR_SHIELD);
            ItemArmorTotalStorage = ReadDB2<ItemArmorTotalRecord>("ItemArmorTotal.db2", HotfixStatements.SEL_ITEM_ARMOR_TOTAL);
            //ItemBagFamilyStorage = ReadDB2<ItemBagFamilyRecord>("ItemBagFamily.db2", HotfixStatements.SEL_ITEM_BAG_FAMILY, HotfixStatements.SEL_ITEM_BAG_FAMILY_LOCALE);
            ItemBonusStorage = ReadDB2<ItemBonusRecord>("ItemBonus.db2", HotfixStatements.SEL_ITEM_BONUS);
            ItemBonusListLevelDeltaStorage = ReadDB2<ItemBonusListLevelDeltaRecord>("ItemBonusListLevelDelta.db2", HotfixStatements.SEL_ITEM_BONUS_LIST_LEVEL_DELTA);
            ItemBonusTreeNodeStorage = ReadDB2<ItemBonusTreeNodeRecord>("ItemBonusTreeNode.db2", HotfixStatements.SEL_ITEM_BONUS_TREE_NODE);
            ItemChildEquipmentStorage = ReadDB2<ItemChildEquipmentRecord>("ItemChildEquipment.db2", HotfixStatements.SEL_ITEM_CHILD_EQUIPMENT);
            ItemClassStorage = ReadDB2<ItemClassRecord>("ItemClass.db2", HotfixStatements.SEL_ITEM_CLASS, HotfixStatements.SEL_ITEM_CLASS_LOCALE);
            ItemCurrencyCostStorage = ReadDB2<ItemCurrencyCostRecord>("ItemCurrencyCost.db2", HotfixStatements.SEL_ITEM_CURRENCY_COST);
            ItemDamageAmmoStorage = ReadDB2<ItemDamageRecord>("ItemDamageAmmo.db2", HotfixStatements.SEL_ITEM_DAMAGE_AMMO);
            ItemDamageOneHandStorage = ReadDB2<ItemDamageRecord>("ItemDamageOneHand.db2", HotfixStatements.SEL_ITEM_DAMAGE_ONE_HAND);
            ItemDamageOneHandCasterStorage = ReadDB2<ItemDamageRecord>("ItemDamageOneHandCaster.db2", HotfixStatements.SEL_ITEM_DAMAGE_ONE_HAND_CASTER);
            ItemDamageTwoHandStorage = ReadDB2<ItemDamageRecord>("ItemDamageTwoHand.db2", HotfixStatements.SEL_ITEM_DAMAGE_TWO_HAND);
            ItemDamageTwoHandCasterStorage = ReadDB2<ItemDamageRecord>("ItemDamageTwoHandCaster.db2", HotfixStatements.SEL_ITEM_DAMAGE_TWO_HAND_CASTER);
            ItemDisenchantLootStorage = ReadDB2<ItemDisenchantLootRecord>("ItemDisenchantLoot.db2", HotfixStatements.SEL_ITEM_DISENCHANT_LOOT);
            ItemEffectStorage = ReadDB2<ItemEffectRecord>("ItemEffect.db2", HotfixStatements.SEL_ITEM_EFFECT);
            ItemStorage = ReadDB2<ItemRecord>("Item.db2", HotfixStatements.SEL_ITEM);
            ItemExtendedCostStorage = ReadDB2<ItemExtendedCostRecord>("ItemExtendedCost.db2", HotfixStatements.SEL_ITEM_EXTENDED_COST);
            ItemLevelSelectorStorage = ReadDB2<ItemLevelSelectorRecord>("ItemLevelSelector.db2", HotfixStatements.SEL_ITEM_LEVEL_SELECTOR);
            ItemLevelSelectorQualityStorage = ReadDB2<ItemLevelSelectorQualityRecord>("ItemLevelSelectorQuality.db2", HotfixStatements.SEL_ITEM_LEVEL_SELECTOR_QUALITY);
            ItemLevelSelectorQualitySetStorage = ReadDB2<ItemLevelSelectorQualitySetRecord>("ItemLevelSelectorQualitySet.db2", HotfixStatements.SEL_ITEM_LEVEL_SELECTOR_QUALITY_SET);
            ItemLimitCategoryStorage = ReadDB2<ItemLimitCategoryRecord>("ItemLimitCategory.db2", HotfixStatements.SEL_ITEM_LIMIT_CATEGORY, HotfixStatements.SEL_ITEM_LIMIT_CATEGORY_LOCALE);
            ItemLimitCategoryConditionStorage = ReadDB2<ItemLimitCategoryConditionRecord>("ItemLimitCategoryCondition.db2", HotfixStatements.SEL_ITEM_LIMIT_CATEGORY_CONDITION);
            ItemModifiedAppearanceStorage = ReadDB2<ItemModifiedAppearanceRecord>("ItemModifiedAppearance.db2", HotfixStatements.SEL_ITEM_MODIFIED_APPEARANCE);
            ItemModifiedAppearanceExtraStorage = ReadDB2<ItemModifiedAppearanceExtraRecord>("ItemModifiedAppearanceExtra.db2", HotfixStatements.SEL_ITEM_MODIFIED_APPEARANCE_EXTRA);
            ItemNameDescriptionStorage = ReadDB2<ItemNameDescriptionRecord>("ItemNameDescription.db2", HotfixStatements.SEL_ITEM_NAME_DESCRIPTION, HotfixStatements.SEL_ITEM_NAME_DESCRIPTION_LOCALE);
            ItemPriceBaseStorage = ReadDB2<ItemPriceBaseRecord>("ItemPriceBase.db2", HotfixStatements.SEL_ITEM_PRICE_BASE);
            ItemSearchNameStorage = ReadDB2<ItemSearchNameRecord>("ItemSearchName.db2", HotfixStatements.SEL_ITEM_SEARCH_NAME, HotfixStatements.SEL_ITEM_SEARCH_NAME_LOCALE);
            ItemSetStorage = ReadDB2<ItemSetRecord>("ItemSet.db2", HotfixStatements.SEL_ITEM_SET, HotfixStatements.SEL_ITEM_SET_LOCALE);
            ItemSetSpellStorage = ReadDB2<ItemSetSpellRecord>("ItemSetSpell.db2", HotfixStatements.SEL_ITEM_SET_SPELL);
            ItemSparseStorage = ReadDB2<ItemSparseRecord>("ItemSparse.db2", HotfixStatements.SEL_ITEM_SPARSE, HotfixStatements.SEL_ITEM_SPARSE_LOCALE);
            ItemSpecStorage = ReadDB2<ItemSpecRecord>("ItemSpec.db2", HotfixStatements.SEL_ITEM_SPEC);
            ItemSpecOverrideStorage = ReadDB2<ItemSpecOverrideRecord>("ItemSpecOverride.db2", HotfixStatements.SEL_ITEM_SPEC_OVERRIDE);
            ItemXBonusTreeStorage = ReadDB2<ItemXBonusTreeRecord>("ItemXBonusTree.db2", HotfixStatements.SEL_ITEM_X_BONUS_TREE);
            ItemXItemEffectStorage = ReadDB2<ItemXItemEffectRecord>("ItemXItemEffect.db2", HotfixStatements.SEL_ITEM_X_ITEM_EFFECT);
            JournalEncounterStorage = ReadDB2<JournalEncounterRecord>("JournalEncounter.db2", HotfixStatements.SEL_JOURNAL_ENCOUNTER, HotfixStatements.SEL_JOURNAL_ENCOUNTER_LOCALE);
            JournalEncounterSectionStorage = ReadDB2<JournalEncounterSectionRecord>("JournalEncounterSection.db2", HotfixStatements.SEL_JOURNAL_ENCOUNTER_SECTION, HotfixStatements.SEL_JOURNAL_ENCOUNTER_SECTION_LOCALE);
            JournalInstanceStorage = ReadDB2<JournalInstanceRecord>("JournalInstance.db2", HotfixStatements.SEL_JOURNAL_INSTANCE, HotfixStatements.SEL_JOURNAL_INSTANCE_LOCALE);
            JournalTierStorage = ReadDB2<JournalTierRecord>("JournalTier.db2", HotfixStatements.SEL_JOURNAL_TIER, HotfixStatements.SEL_JOURNAL_TIER_LOCALE);
            //KeyChainStorage = ReadDB2<KeyChainRecord>("KeyChain.db2", HotfixStatements.SEL_KEYCHAIN);
            KeystoneAffixStorage = ReadDB2<KeystoneAffixRecord>("KeystoneAffix.db2", HotfixStatements.SEL_KEYSTONE_AFFIX, HotfixStatements.SEL_KEYSTONE_AFFIX_LOCALE);
            LanguageWordsStorage = ReadDB2<LanguageWordsRecord>("LanguageWords.db2", HotfixStatements.SEL_LANGUAGE_WORDS);
            LanguagesStorage = ReadDB2<LanguagesRecord>("Languages.db2", HotfixStatements.SEL_LANGUAGES, HotfixStatements.SEL_LANGUAGES_LOCALE);
            LFGDungeonsStorage = ReadDB2<LFGDungeonsRecord>("LFGDungeons.db2", HotfixStatements.SEL_LFG_DUNGEONS, HotfixStatements.SEL_LFG_DUNGEONS_LOCALE);
            LightStorage = ReadDB2<LightRecord>("Light.db2", HotfixStatements.SEL_LIGHT);
            LiquidTypeStorage = ReadDB2<LiquidTypeRecord>("LiquidType.db2", HotfixStatements.SEL_LIQUID_TYPE);
            LockStorage = ReadDB2<LockRecord>("Lock.db2", HotfixStatements.SEL_LOCK);
            MailTemplateStorage = ReadDB2<MailTemplateRecord>("MailTemplate.db2", HotfixStatements.SEL_MAIL_TEMPLATE, HotfixStatements.SEL_MAIL_TEMPLATE_LOCALE);
            MapStorage = ReadDB2<MapRecord>("Map.db2", HotfixStatements.SEL_MAP, HotfixStatements.SEL_MAP_LOCALE);
            MapChallengeModeStorage = ReadDB2<MapChallengeModeRecord>("MapChallengeMode.db2", HotfixStatements.SEL_MAP_CHALLENGE_MODE, HotfixStatements.SEL_MAP_CHALLENGE_MODE_LOCALE);
            MapDifficultyStorage = ReadDB2<MapDifficultyRecord>("MapDifficulty.db2", HotfixStatements.SEL_MAP_DIFFICULTY, HotfixStatements.SEL_MAP_DIFFICULTY_LOCALE);
            MapDifficultyXConditionStorage = ReadDB2<MapDifficultyXConditionRecord>("MapDifficultyXCondition.db2", HotfixStatements.SEL_MAP_DIFFICULTY_X_CONDITION, HotfixStatements.SEL_MAP_DIFFICULTY_X_CONDITION_LOCALE);
            MawPowerStorage = ReadDB2<MawPowerRecord>("MawPower.db2", HotfixStatements.SEL_MAW_POWER);
            ModifierTreeStorage = ReadDB2<ModifierTreeRecord>("ModifierTree.db2", HotfixStatements.SEL_MODIFIER_TREE);
            MountCapabilityStorage = ReadDB2<MountCapabilityRecord>("MountCapability.db2", HotfixStatements.SEL_MOUNT_CAPABILITY);
            MountStorage = ReadDB2<MountRecord>("Mount.db2", HotfixStatements.SEL_MOUNT, HotfixStatements.SEL_MOUNT_LOCALE);
            MountTypeXCapabilityStorage = ReadDB2<MountTypeXCapabilityRecord>("MountTypeXCapability.db2", HotfixStatements.SEL_MOUNT_TYPE_X_CAPABILITY);
            MountXDisplayStorage = ReadDB2<MountXDisplayRecord>("MountXDisplay.db2", HotfixStatements.SEL_MOUNT_X_DISPLAY);
            MovieStorage = ReadDB2<MovieRecord>("Movie.db2", HotfixStatements.SEL_MOVIE);
            NameGenStorage = ReadDB2<NameGenRecord>("NameGen.db2", HotfixStatements.SEL_NAME_GEN);
            NamesProfanityStorage = ReadDB2<NamesProfanityRecord>("NamesProfanity.db2", HotfixStatements.SEL_NAMES_PROFANITY);
            NamesReservedStorage = ReadDB2<NamesReservedRecord>("NamesReserved.db2", HotfixStatements.SEL_NAMES_RESERVED, HotfixStatements.SEL_NAMES_RESERVED_LOCALE);
            NamesReservedLocaleStorage = ReadDB2<NamesReservedLocaleRecord>("NamesReservedLocale.db2", HotfixStatements.SEL_NAMES_RESERVED_LOCALE);
            NumTalentsAtLevelStorage = ReadDB2<NumTalentsAtLevelRecord>("NumTalentsAtLevel.db2", HotfixStatements.SEL_NUM_TALENTS_AT_LEVEL);
            OverrideSpellDataStorage = ReadDB2<OverrideSpellDataRecord>("OverrideSpellData.db2", HotfixStatements.SEL_OVERRIDE_SPELL_DATA);
            ParagonReputationStorage = ReadDB2<ParagonReputationRecord>("ParagonReputation.db2", HotfixStatements.SEL_PARAGON_REPUTATION);
            PhaseStorage = ReadDB2<PhaseRecord>("Phase.db2", HotfixStatements.SEL_PHASE);
            PhaseXPhaseGroupStorage = ReadDB2<PhaseXPhaseGroupRecord>("PhaseXPhaseGroup.db2", HotfixStatements.SEL_PHASE_X_PHASE_GROUP);
            PlayerConditionStorage = ReadDB2<PlayerConditionRecord>("PlayerCondition.db2", HotfixStatements.SEL_PLAYER_CONDITION, HotfixStatements.SEL_PLAYER_CONDITION_LOCALE);
            PowerDisplayStorage = ReadDB2<PowerDisplayRecord>("PowerDisplay.db2", HotfixStatements.SEL_POWER_DISPLAY);
            PowerTypeStorage = ReadDB2<PowerTypeRecord>("PowerType.db2", HotfixStatements.SEL_POWER_TYPE);
            PrestigeLevelInfoStorage = ReadDB2<PrestigeLevelInfoRecord>("PrestigeLevelInfo.db2", HotfixStatements.SEL_PRESTIGE_LEVEL_INFO, HotfixStatements.SEL_PRESTIGE_LEVEL_INFO_LOCALE);
            PvpDifficultyStorage = ReadDB2<PvpDifficultyRecord>("PVPDifficulty.db2", HotfixStatements.SEL_PVP_DIFFICULTY);
            PvpItemStorage = ReadDB2<PvpItemRecord>("PVPItem.db2", HotfixStatements.SEL_PVP_ITEM);
            PvpTalentStorage = ReadDB2<PvpTalentRecord>("PvpTalent.db2", HotfixStatements.SEL_PVP_TALENT, HotfixStatements.SEL_PVP_TALENT_LOCALE);
            PvpTalentCategoryStorage = ReadDB2<PvpTalentCategoryRecord>("PvpTalentCategory.db2", HotfixStatements.SEL_PVP_TALENT_CATEGORY);
            PvpTalentSlotUnlockStorage = ReadDB2<PvpTalentSlotUnlockRecord>("PvpTalentSlotUnlock.db2", HotfixStatements.SEL_PVP_TALENT_SLOT_UNLOCK);
            PvpTierStorage = ReadDB2<PvpTierRecord>("PvpTier.db2", HotfixStatements.SEL_PVP_TIER, HotfixStatements.SEL_PVP_TIER_LOCALE);
            QuestFactionRewardStorage = ReadDB2<QuestFactionRewardRecord>("QuestFactionReward.db2", HotfixStatements.SEL_QUEST_FACTION_REWARD);
            QuestInfoStorage = ReadDB2<QuestInfoRecord>("QuestInfo.db2", HotfixStatements.SEL_QUEST_INFO, HotfixStatements.SEL_QUEST_INFO_LOCALE);
            QuestLineXQuestStorage = ReadDB2<QuestLineXQuestRecord>("QuestLineXQuest.db2", HotfixStatements.SEL_QUEST_LINE_X_QUEST);
            QuestMoneyRewardStorage = ReadDB2<QuestMoneyRewardRecord>("QuestMoneyReward.db2", HotfixStatements.SEL_QUEST_MONEY_REWARD);
            QuestPackageItemStorage = ReadDB2<QuestPackageItemRecord>("QuestPackageItem.db2", HotfixStatements.SEL_QUEST_PACKAGE_ITEM);
            QuestSortStorage = ReadDB2<QuestSortRecord>("QuestSort.db2", HotfixStatements.SEL_QUEST_SORT, HotfixStatements.SEL_QUEST_SORT_LOCALE);
            QuestV2Storage = ReadDB2<QuestV2Record>("QuestV2.db2", HotfixStatements.SEL_QUEST_V2);
            QuestXPStorage = ReadDB2<QuestXPRecord>("QuestXP.db2", HotfixStatements.SEL_QUEST_XP);
            RandPropPointsStorage = ReadDB2<RandPropPointsRecord>("RandPropPoints.db2", HotfixStatements.SEL_RAND_PROP_POINTS);
            RewardPackStorage = ReadDB2<RewardPackRecord>("RewardPack.db2", HotfixStatements.SEL_REWARD_PACK);
            RewardPackXCurrencyTypeStorage = ReadDB2<RewardPackXCurrencyTypeRecord>("RewardPackXCurrencyType.db2", HotfixStatements.SEL_REWARD_PACK_X_CURRENCY_TYPE);
            RewardPackXItemStorage = ReadDB2<RewardPackXItemRecord>("RewardPackXItem.db2", HotfixStatements.SEL_REWARD_PACK_X_ITEM);
            ScenarioStorage = ReadDB2<ScenarioRecord>("Scenario.db2", HotfixStatements.SEL_SCENARIO, HotfixStatements.SEL_SCENARIO_LOCALE);
            ScenarioStepStorage = ReadDB2<ScenarioStepRecord>("ScenarioStep.db2", HotfixStatements.SEL_SCENARIO_STEP, HotfixStatements.SEL_SCENARIO_STEP_LOCALE);
            SceneScriptStorage = ReadDB2<SceneScriptRecord>("SceneScript.db2", HotfixStatements.SEL_SCENE_SCRIPT);
            SceneScriptGlobalTextStorage = ReadDB2<SceneScriptGlobalTextRecord>("SceneScriptGlobalText.db2", HotfixStatements.SEL_SCENE_SCRIPT_GLOBAL_TEXT);
            SceneScriptPackageStorage = ReadDB2<SceneScriptPackageRecord>("SceneScriptPackage.db2", HotfixStatements.SEL_SCENE_SCRIPT_PACKAGE);
            SceneScriptTextStorage = ReadDB2<SceneScriptTextRecord>("SceneScriptText.db2", HotfixStatements.SEL_SCENE_SCRIPT_TEXT);
            SkillLineStorage = ReadDB2<SkillLineRecord>("SkillLine.db2", HotfixStatements.SEL_SKILL_LINE, HotfixStatements.SEL_SKILL_LINE_LOCALE);
            SkillLineAbilityStorage = ReadDB2<SkillLineAbilityRecord>("SkillLineAbility.db2", HotfixStatements.SEL_SKILL_LINE_ABILITY);
            SkillLineXTraitTreeStorage = ReadDB2<SkillLineXTraitTreeRecord>("SkillLineXTraitTree.db2", HotfixStatements.SEL_SKILL_LINE_X_TRAIT_TREE);
            SkillRaceClassInfoStorage = ReadDB2<SkillRaceClassInfoRecord>("SkillRaceClassInfo.db2", HotfixStatements.SEL_SKILL_RACE_CLASS_INFO);
            SoulbindConduitRankStorage = ReadDB2<SoulbindConduitRankRecord>("SoulbindConduitRank.db2", HotfixStatements.SEL_SOULBIND_CONDUIT_RANK);
            SoundKitStorage = ReadDB2<SoundKitRecord>("SoundKit.db2", HotfixStatements.SEL_SOUND_KIT);
            SpecializationSpellsStorage = ReadDB2<SpecializationSpellsRecord>("SpecializationSpells.db2", HotfixStatements.SEL_SPECIALIZATION_SPELLS, HotfixStatements.SEL_SPECIALIZATION_SPELLS_LOCALE);
            SpecSetMemberStorage = ReadDB2<SpecSetMemberRecord>("SpecSetMember.db2", HotfixStatements.SEL_SPEC_SET_MEMBER);
            SpellNameStorage = ReadDB2<SpellNameRecord>("SpellName.db2", HotfixStatements.SEL_SPELL_NAME, HotfixStatements.SEL_SPELL_NAME_LOCALE);
            SpellAuraOptionsStorage = ReadDB2<SpellAuraOptionsRecord>("SpellAuraOptions.db2", HotfixStatements.SEL_SPELL_AURA_OPTIONS);
            SpellAuraRestrictionsStorage = ReadDB2<SpellAuraRestrictionsRecord>("SpellAuraRestrictions.db2", HotfixStatements.SEL_SPELL_AURA_RESTRICTIONS);
            SpellCastTimesStorage = ReadDB2<SpellCastTimesRecord>("SpellCastTimes.db2", HotfixStatements.SEL_SPELL_CAST_TIMES);
            SpellCastingRequirementsStorage = ReadDB2<SpellCastingRequirementsRecord>("SpellCastingRequirements.db2", HotfixStatements.SEL_SPELL_CASTING_REQUIREMENTS);
            SpellCategoriesStorage = ReadDB2<SpellCategoriesRecord>("SpellCategories.db2", HotfixStatements.SEL_SPELL_CATEGORIES);
            SpellCategoryStorage = ReadDB2<SpellCategoryRecord>("SpellCategory.db2", HotfixStatements.SEL_SPELL_CATEGORY, HotfixStatements.SEL_SPELL_CATEGORY_LOCALE);
            SpellClassOptionsStorage = ReadDB2<SpellClassOptionsRecord>("SpellClassOptions.db2", HotfixStatements.SEL_SPELL_CLASS_OPTIONS);
            SpellCooldownsStorage = ReadDB2<SpellCooldownsRecord>("SpellCooldowns.db2", HotfixStatements.SEL_SPELL_COOLDOWNS);
            SpellDurationStorage = ReadDB2<SpellDurationRecord>("SpellDuration.db2", HotfixStatements.SEL_SPELL_DURATION);
            SpellEffectStorage = ReadDB2<SpellEffectRecord>("SpellEffect.db2", HotfixStatements.SEL_SPELL_EFFECT);
            SpellEquippedItemsStorage = ReadDB2<SpellEquippedItemsRecord>("SpellEquippedItems.db2", HotfixStatements.SEL_SPELL_EQUIPPED_ITEMS);
            SpellFocusObjectStorage = ReadDB2<SpellFocusObjectRecord>("SpellFocusObject.db2", HotfixStatements.SEL_SPELL_FOCUS_OBJECT, HotfixStatements.SEL_SPELL_FOCUS_OBJECT_LOCALE);
            SpellInterruptsStorage = ReadDB2<SpellInterruptsRecord>("SpellInterrupts.db2", HotfixStatements.SEL_SPELL_INTERRUPTS);
            SpellItemEnchantmentStorage = ReadDB2<SpellItemEnchantmentRecord>("SpellItemEnchantment.db2", HotfixStatements.SEL_SPELL_ITEM_ENCHANTMENT, HotfixStatements.SEL_SPELL_ITEM_ENCHANTMENT_LOCALE);
            SpellItemEnchantmentConditionStorage = ReadDB2<SpellItemEnchantmentConditionRecord>("SpellItemEnchantmentCondition.db2", HotfixStatements.SEL_SPELL_ITEM_ENCHANTMENT_CONDITION);
            SpellLabelStorage = ReadDB2<SpellLabelRecord>("SpellLabel.db2", HotfixStatements.SEL_SPELL_LABEL);
            SpellLearnSpellStorage = ReadDB2<SpellLearnSpellRecord>("SpellLearnSpell.db2", HotfixStatements.SEL_SPELL_LEARN_SPELL);
            SpellLevelsStorage = ReadDB2<SpellLevelsRecord>("SpellLevels.db2", HotfixStatements.SEL_SPELL_LEVELS);
            SpellMiscStorage = ReadDB2<SpellMiscRecord>("SpellMisc.db2", HotfixStatements.SEL_SPELL_MISC);
            SpellPowerStorage = ReadDB2<SpellPowerRecord>("SpellPower.db2", HotfixStatements.SEL_SPELL_POWER);
            SpellPowerDifficultyStorage = ReadDB2<SpellPowerDifficultyRecord>("SpellPowerDifficulty.db2", HotfixStatements.SEL_SPELL_POWER_DIFFICULTY);
            SpellProcsPerMinuteStorage = ReadDB2<SpellProcsPerMinuteRecord>("SpellProcsPerMinute.db2", HotfixStatements.SEL_SPELL_PROCS_PER_MINUTE);
            SpellProcsPerMinuteModStorage = ReadDB2<SpellProcsPerMinuteModRecord>("SpellProcsPerMinuteMod.db2", HotfixStatements.SEL_SPELL_PROCS_PER_MINUTE_MOD);
            SpellRadiusStorage = ReadDB2<SpellRadiusRecord>("SpellRadius.db2", HotfixStatements.SEL_SPELL_RADIUS);
            SpellRangeStorage = ReadDB2<SpellRangeRecord>("SpellRange.db2", HotfixStatements.SEL_SPELL_RANGE, HotfixStatements.SEL_SPELL_RANGE_LOCALE);
            SpellReagentsStorage = ReadDB2<SpellReagentsRecord>("SpellReagents.db2", HotfixStatements.SEL_SPELL_REAGENTS);
            SpellReagentsCurrencyStorage = ReadDB2<SpellReagentsCurrencyRecord>("SpellReagentsCurrency.db2", HotfixStatements.SEL_SPELL_REAGENTS_CURRENCY);
            SpellScalingStorage = ReadDB2<SpellScalingRecord>("SpellScaling.db2", HotfixStatements.SEL_SPELL_SCALING);
            SpellShapeshiftStorage = ReadDB2<SpellShapeshiftRecord>("SpellShapeshift.db2", HotfixStatements.SEL_SPELL_SHAPESHIFT);
            SpellShapeshiftFormStorage = ReadDB2<SpellShapeshiftFormRecord>("SpellShapeshiftForm.db2", HotfixStatements.SEL_SPELL_SHAPESHIFT_FORM, HotfixStatements.SEL_SPELL_SHAPESHIFT_FORM_LOCALE);
            SpellTargetRestrictionsStorage = ReadDB2<SpellTargetRestrictionsRecord>("SpellTargetRestrictions.db2", HotfixStatements.SEL_SPELL_TARGET_RESTRICTIONS);
            SpellTotemsStorage = ReadDB2<SpellTotemsRecord>("SpellTotems.db2", HotfixStatements.SEL_SPELL_TOTEMS);
            SpellVisualStorage = ReadDB2<SpellVisualRecord>("SpellVisual.db2", HotfixStatements.SEL_SPELL_VISUAL);
            SpellVisualEffectNameStorage = ReadDB2<SpellVisualEffectNameRecord>("SpellVisualEffectName.db2", HotfixStatements.SEL_SPELL_VISUAL_EFFECT_NAME);
            SpellVisualMissileStorage = ReadDB2<SpellVisualMissileRecord>("SpellVisualMissile.db2", HotfixStatements.SEL_SPELL_VISUAL_MISSILE);
            SpellVisualKitStorage = ReadDB2<SpellVisualKitRecord>("SpellVisualKit.db2", HotfixStatements.SEL_SPELL_VISUAL_KIT);
            SpellXSpellVisualStorage = ReadDB2<SpellXSpellVisualRecord>("SpellXSpellVisual.db2", HotfixStatements.SEL_SPELL_X_SPELL_VISUAL);
            SummonPropertiesStorage = ReadDB2<SummonPropertiesRecord>("SummonProperties.db2", HotfixStatements.SEL_SUMMON_PROPERTIES);
            TactKeyStorage = ReadDB2<TactKeyRecord>("TactKey.db2", HotfixStatements.SEL_TACT_KEY);
            TalentStorage = ReadDB2<TalentRecord>("Talent.db2", HotfixStatements.SEL_TALENT, HotfixStatements.SEL_TALENT_LOCALE);
            TaxiNodesStorage = ReadDB2<TaxiNodesRecord>("TaxiNodes.db2", HotfixStatements.SEL_TAXI_NODES, HotfixStatements.SEL_TAXI_NODES_LOCALE);
            TaxiPathStorage = ReadDB2<TaxiPathRecord>("TaxiPath.db2", HotfixStatements.SEL_TAXI_PATH);
            TaxiPathNodeStorage = ReadDB2<TaxiPathNodeRecord>("TaxiPathNode.db2", HotfixStatements.SEL_TAXI_PATH_NODE);
            TotemCategoryStorage = ReadDB2<TotemCategoryRecord>("TotemCategory.db2", HotfixStatements.SEL_TOTEM_CATEGORY, HotfixStatements.SEL_TOTEM_CATEGORY_LOCALE);
            ToyStorage = ReadDB2<ToyRecord>("Toy.db2", HotfixStatements.SEL_TOY, HotfixStatements.SEL_TOY_LOCALE);
            TraitCondStorage = ReadDB2<TraitCondRecord>("TraitCond.db2", HotfixStatements.SEL_TRAIT_COND);
            TraitCostStorage = ReadDB2<TraitCostRecord>("TraitCost.db2", HotfixStatements.SEL_TRAIT_COST);
            TraitCurrencyStorage = ReadDB2<TraitCurrencyRecord>("TraitCurrency.db2", HotfixStatements.SEL_TRAIT_CURRENCY);
            TraitCurrencySourceStorage = ReadDB2<TraitCurrencySourceRecord>("TraitCurrencySource.db2", HotfixStatements.SEL_TRAIT_CURRENCY_SOURCE, HotfixStatements.SEL_TRAIT_CURRENCY_SOURCE_LOCALE);
            TraitDefinitionStorage = ReadDB2<TraitDefinitionRecord>("TraitDefinition.db2", HotfixStatements.SEL_TRAIT_DEFINITION, HotfixStatements.SEL_TRAIT_DEFINITION_LOCALE);
            TraitDefinitionEffectPointsStorage = ReadDB2<TraitDefinitionEffectPointsRecord>("TraitDefinitionEffectPoints.db2", HotfixStatements.SEL_TRAIT_DEFINITION_EFFECT_POINTS);
            TraitEdgeStorage = ReadDB2<TraitEdgeRecord>("TraitEdge.db2", HotfixStatements.SEL_TRAIT_EDGE);
            TraitNodeStorage = ReadDB2<TraitNodeRecord>("TraitNode.db2", HotfixStatements.SEL_TRAIT_NODE);
            TraitNodeEntryStorage = ReadDB2<TraitNodeEntryRecord>("TraitNodeEntry.db2", HotfixStatements.SEL_TRAIT_NODE_ENTRY);
            TraitNodeEntryXTraitCondStorage = ReadDB2<TraitNodeEntryXTraitCondRecord>("TraitNodeEntryXTraitCond.db2", HotfixStatements.SEL_TRAIT_NODE_ENTRY_X_TRAIT_COND);
            TraitNodeEntryXTraitCostStorage = ReadDB2<TraitNodeEntryXTraitCostRecord>("TraitNodeEntryXTraitCost.db2", HotfixStatements.SEL_TRAIT_NODE_ENTRY_X_TRAIT_COST);
            TraitNodeGroupStorage = ReadDB2<TraitNodeGroupRecord>("TraitNodeGroup.db2", HotfixStatements.SEL_TRAIT_NODE_GROUP);
            TraitNodeGroupXTraitCondStorage = ReadDB2<TraitNodeGroupXTraitCondRecord>("TraitNodeGroupXTraitCond.db2", HotfixStatements.SEL_TRAIT_NODE_GROUP_X_TRAIT_COND);
            TraitNodeGroupXTraitCostStorage = ReadDB2<TraitNodeGroupXTraitCostRecord>("TraitNodeGroupXTraitCost.db2", HotfixStatements.SEL_TRAIT_NODE_GROUP_X_TRAIT_COST);
            TraitNodeGroupXTraitNodeStorage = ReadDB2<TraitNodeGroupXTraitNodeRecord>("TraitNodeGroupXTraitNode.db2", HotfixStatements.SEL_TRAIT_NODE_GROUP_X_TRAIT_NODE);
            TraitNodeXTraitCondStorage = ReadDB2<TraitNodeXTraitCondRecord>("TraitNodeXTraitCond.db2", HotfixStatements.SEL_TRAIT_NODE_X_TRAIT_COND);
            TraitNodeXTraitCostStorage = ReadDB2<TraitNodeXTraitCostRecord>("TraitNodeXTraitCost.db2", HotfixStatements.SEL_TRAIT_NODE_X_TRAIT_COST);
            TraitNodeXTraitNodeEntryStorage = ReadDB2<TraitNodeXTraitNodeEntryRecord>("TraitNodeXTraitNodeEntry.db2", HotfixStatements.SEL_TRAIT_NODE_X_TRAIT_NODE_ENTRY);
            TraitTreeStorage = ReadDB2<TraitTreeRecord>("TraitTree.db2", HotfixStatements.SEL_TRAIT_TREE);
            TraitTreeLoadoutStorage = ReadDB2<TraitTreeLoadoutRecord>("TraitTreeLoadout.db2", HotfixStatements.SEL_TRAIT_TREE_LOADOUT);
            TraitTreeLoadoutEntryStorage = ReadDB2<TraitTreeLoadoutEntryRecord>("TraitTreeLoadoutEntry.db2", HotfixStatements.SEL_TRAIT_TREE_LOADOUT_ENTRY);
            TraitTreeXTraitCostStorage = ReadDB2<TraitTreeXTraitCostRecord>("TraitTreeXTraitCost.db2", HotfixStatements.SEL_TRAIT_TREE_X_TRAIT_COST);
            TraitTreeXTraitCurrencyStorage = ReadDB2<TraitTreeXTraitCurrencyRecord>("TraitTreeXTraitCurrency.db2", HotfixStatements.SEL_TRAIT_TREE_X_TRAIT_CURRENCY);
            TransmogHolidayStorage = ReadDB2<TransmogHolidayRecord>("TransmogHoliday.db2", HotfixStatements.SEL_TRANSMOG_HOLIDAY);
            TransmogIllusionStorage = ReadDB2<TransmogIllusionRecord>("TransmogIllusion.db2", HotfixStatements.SEL_TRANSMOG_ILLUSION);
            TransmogSetStorage = ReadDB2<TransmogSetRecord>("TransmogSet.db2", HotfixStatements.SEL_TRANSMOG_SET, HotfixStatements.SEL_TRANSMOG_SET_LOCALE);
            TransmogSetGroupStorage = ReadDB2<TransmogSetGroupRecord>("TransmogSetGroup.db2", HotfixStatements.SEL_TRANSMOG_SET_GROUP, HotfixStatements.SEL_TRANSMOG_SET_GROUP_LOCALE);
            TransmogSetItemStorage = ReadDB2<TransmogSetItemRecord>("TransmogSetItem.db2", HotfixStatements.SEL_TRANSMOG_SET_ITEM);
            TransportAnimationStorage = ReadDB2<TransportAnimationRecord>("TransportAnimation.db2", HotfixStatements.SEL_TRANSPORT_ANIMATION);
            TransportRotationStorage = ReadDB2<TransportRotationRecord>("TransportRotation.db2", HotfixStatements.SEL_TRANSPORT_ROTATION);
            UiMapStorage = ReadDB2<UiMapRecord>("UiMap.db2", HotfixStatements.SEL_UI_MAP, HotfixStatements.SEL_UI_MAP_LOCALE);
            UiMapAssignmentStorage = ReadDB2<UiMapAssignmentRecord>("UiMapAssignment.db2", HotfixStatements.SEL_UI_MAP_ASSIGNMENT);
            UiMapLinkStorage = ReadDB2<UiMapLinkRecord>("UiMapLink.db2", HotfixStatements.SEL_UI_MAP_LINK);
            UiMapXMapArtStorage = ReadDB2<UiMapXMapArtRecord>("UiMapXMapArt.db2", HotfixStatements.SEL_UI_MAP_X_MAP_ART);
            UISplashScreenStorage = ReadDB2<UISplashScreenRecord>("UISplashScreen.db2", HotfixStatements.SEL_UI_SPLASH_SCREEN, HotfixStatements.SEL_UI_SPLASH_SCREEN_LOCALE);
            UnitConditionStorage = ReadDB2<UnitConditionRecord>("UnitCondition.db2", HotfixStatements.SEL_UNIT_CONDITION);
            UnitPowerBarStorage = ReadDB2<UnitPowerBarRecord>("UnitPowerBar.db2", HotfixStatements.SEL_UNIT_POWER_BAR, HotfixStatements.SEL_UNIT_POWER_BAR_LOCALE);
            VehicleStorage = ReadDB2<VehicleRecord>("Vehicle.db2", HotfixStatements.SEL_VEHICLE);
            VehicleSeatStorage = ReadDB2<VehicleSeatRecord>("VehicleSeat.db2", HotfixStatements.SEL_VEHICLE_SEAT);
            WMOAreaTableStorage = ReadDB2<WMOAreaTableRecord>("WMOAreaTable.db2", HotfixStatements.SEL_WMO_AREA_TABLE, HotfixStatements.SEL_WMO_AREA_TABLE_LOCALE);
            WorldEffectStorage = ReadDB2<WorldEffectRecord>("WorldEffect.db2", HotfixStatements.SEL_WORLD_EFFECT);
            WorldMapOverlayStorage = ReadDB2<WorldMapOverlayRecord>("WorldMapOverlay.db2", HotfixStatements.SEL_WORLD_MAP_OVERLAY);
            WorldStateExpressionStorage = ReadDB2<WorldStateExpressionRecord>("WorldStateExpression.db2", HotfixStatements.SEL_WORLD_STATE_EXPRESSION);
            CharBaseInfoStorage = ReadDB2<CharBaseInfo>("CharBaseInfo.db2", HotfixStatements.SEL_CHAR_BASE_INFO);

            Global.DB2Mgr.LoadStores();

            foreach (var entry in TaxiPathStorage.Values)
            {
                if (!TaxiPathSetBySource.ContainsKey(entry.FromTaxiNode))
                    TaxiPathSetBySource.Add(entry.FromTaxiNode, new Dictionary<uint, TaxiPathBySourceAndDestination>());

                TaxiPathSetBySource[entry.FromTaxiNode][entry.ToTaxiNode] = new TaxiPathBySourceAndDestination(entry.Id, entry.Cost);
            }

            uint pathCount = TaxiPathStorage.GetNumRows();

            // Calculate path nodes Count
            uint[] pathLength = new uint[pathCount]; // 0 and some other indexes not used

            foreach (TaxiPathNodeRecord entry in TaxiPathNodeStorage.Values)
                if (pathLength[entry.PathID] < entry.NodeIndex + 1)
                    pathLength[entry.PathID] = (uint)entry.NodeIndex + 1u;

            // Set path length
            for (uint i = 0; i < pathCount; ++i)
                TaxiPathNodesByPath[i] = new TaxiPathNodeRecord[pathLength[i]];

            // fill _data
            foreach (var entry in TaxiPathNodeStorage.Values)
                TaxiPathNodesByPath[entry.PathID][entry.NodeIndex] = entry;

            var taxiMaskSize = ((TaxiNodesStorage.GetNumRows() - 1) / 8) + 1;
            TaxiNodesMask = new byte[taxiMaskSize];
            OldContinentsNodesMask = new byte[taxiMaskSize];
            HordeTaxiNodesMask = new byte[taxiMaskSize];
            AllianceTaxiNodesMask = new byte[taxiMaskSize];

            foreach (var node in TaxiNodesStorage.Values)
            {
                if (!node.Flags.HasAnyFlag(TaxiNodeFlags.Alliance | TaxiNodeFlags.Horde))
                    continue;

                // valid taxi network node
                uint field = (node.Id - 1) / 8;
                byte submask = (byte)(1 << (int)((node.Id - 1) % 8));

                TaxiNodesMask[field] |= submask;

                if (node.Flags.HasAnyFlag(TaxiNodeFlags.Horde))
                    HordeTaxiNodesMask[field] |= submask;

                if (node.Flags.HasAnyFlag(TaxiNodeFlags.Alliance))
                    AllianceTaxiNodesMask[field] |= submask;

                int uiMapId;

                if (!Global.DB2Mgr.GetUiMapPosition(node.Pos.X, node.Pos.Y, node.Pos.Z, node.ContinentID, 0, 0, 0, UiMapSystem.Adventure, false, out uiMapId))
                    Global.DB2Mgr.GetUiMapPosition(node.Pos.X, node.Pos.Y, node.Pos.Z, node.ContinentID, 0, 0, 0, UiMapSystem.Taxi, false, out uiMapId);

                if (uiMapId == 985 ||
                    uiMapId == 986)
                    OldContinentsNodesMask[field] |= submask;
            }

            // Check loaded DB2 files proper version
            if (!AreaTableStorage.ContainsKey(14618) ||       // last area added in 10.0.2 (46741)
                !CharTitlesStorage.ContainsKey(749) ||        // last char title added in 10.0.2 (46741)
                !GemPropertiesStorage.ContainsKey(4028) ||    // last gem property added in 10.0.2 (46741)
                !ItemStorage.ContainsKey(202712) ||           // last Item added in 10.0.2 (46741)
                !ItemExtendedCostStorage.ContainsKey(7862) || // last Item extended cost added in 10.0.2 (46741)
                !MapStorage.ContainsKey(2582) ||              // last map added in 10.0.2 (46741)
                !SpellNameStorage.ContainsKey(399311))        // last spell added in 10.0.2 (46741)
            {
                Log.outError(LogFilter.Misc, "You have _outdated_ DB2 files. Please extract correct versions from current using client.");
                Environment.Exit(1);
            }

            Log.outInfo(LogFilter.ServerLoading, "Initialized {0} DB2 _data storages in {1} ms", loadedFileCount, Time.GetMSTimeDiffToNow(oldMSTime));

            return availableDb2Locales;
        }

        public static void LoadGameTables(string dataPath)
        {
            uint oldMSTime = Time.GetMSTime();

            string gtPath = dataPath + "/gt/";

            uint loadedFileCount = 0;

            GameTable<T> ReadGameTable<T>(string fileName) where T : new()
            {
                return GameTableReader.Read<T>(gtPath, fileName, ref loadedFileCount);
            }

            ArtifactKnowledgeMultiplierGameTable = ReadGameTable<GtArtifactKnowledgeMultiplierRecord>("ArtifactKnowledgeMultiplier.txt");
            ArtifactLevelXPGameTable = ReadGameTable<GtArtifactLevelXPRecord>("artifactLevelXP.txt");
            BarberShopCostBaseGameTable = ReadGameTable<GtBarberShopCostBaseRecord>("BarberShopCostBase.txt");
            BaseMPGameTable = ReadGameTable<GtBaseMPRecord>("BaseMp.txt");
            BattlePetXPGameTable = ReadGameTable<GtBattlePetXPRecord>("BattlePetXP.txt");
            CombatRatingsGameTable = ReadGameTable<GtCombatRatingsRecord>("CombatRatings.txt");
            CombatRatingsMultByILvlGameTable = ReadGameTable<GtGenericMultByILvlRecord>("CombatRatingsMultByILvl.txt");
            ItemSocketCostPerLevelGameTable = ReadGameTable<GtItemSocketCostPerLevelRecord>("ItemSocketCostPerLevel.txt");
            HpPerStaGameTable = ReadGameTable<GtHpPerStaRecord>("HpPerSta.txt");
            NpcManaCostScalerGameTable = ReadGameTable<GtNpcManaCostScalerRecord>("NPCManaCostScaler.txt");
            SpellScalingGameTable = ReadGameTable<GtSpellScalingRecord>("SpellScaling.txt");
            StaminaMultByILvlGameTable = ReadGameTable<GtGenericMultByILvlRecord>("StaminaMultByILvl.txt");
            XpGameTable = ReadGameTable<GtXpRecord>("xp.txt");

            Log.outInfo(LogFilter.ServerLoading, "Initialized {0} DBC GameTables _data stores in {1} ms", loadedFileCount, Time.GetMSTimeDiffToNow(oldMSTime));
        }

 

        #region Helper Methods

        public static float GetGameTableColumnForClass(dynamic row, Class class_)
        {
            switch (class_)
            {
                case Class.Warrior:
                    return row.Warrior;
                case Class.Paladin:
                    return row.Paladin;
                case Class.Hunter:
                    return row.Hunter;
                case Class.Rogue:
                    return row.Rogue;
                case Class.Priest:
                    return row.Priest;
                case Class.Deathknight:
                    return row.DeathKnight;
                case Class.Shaman:
                    return row.Shaman;
                case Class.Mage:
                    return row.Mage;
                case Class.Warlock:
                    return row.Warlock;
                case Class.Monk:
                    return row.Monk;
                case Class.Druid:
                    return row.Druid;
                case Class.DemonHunter:
                    return row.DemonHunter;
                case Class.Evoker:
                    return row.Evoker;
                case Class.Adventurer:
                    return row.Adventurer;
                default:
                    break;
            }

            return 0.0f;
        }

        public static float GetSpellScalingColumnForClass(GtSpellScalingRecord row, int class_)
        {
            switch (class_)
            {
                case (int)Class.Warrior:
                    return row.Warrior;
                case (int)Class.Paladin:
                    return row.Paladin;
                case (int)Class.Hunter:
                    return row.Hunter;
                case (int)Class.Rogue:
                    return row.Rogue;
                case (int)Class.Priest:
                    return row.Priest;
                case (int)Class.Deathknight:
                    return row.DeathKnight;
                case (int)Class.Shaman:
                    return row.Shaman;
                case (int)Class.Mage:
                    return row.Mage;
                case (int)Class.Warlock:
                    return row.Warlock;
                case (int)Class.Monk:
                    return row.Monk;
                case (int)Class.Druid:
                    return row.Druid;
                case (int)Class.DemonHunter:
                    return row.DemonHunter;
                case (int)Class.Evoker:
                    return row.Evoker;
                case (int)Class.Adventurer:
                    return row.Adventurer;
                case -1:
                case -7:
                    return row.Item;
                case -2:
                    return row.Consumable;
                case -3:
                    return row.Gem1;
                case -4:
                    return row.Gem2;
                case -5:
                    return row.Gem3;
                case -6:
                    return row.Health;
                case -8:
                    return row.DamageReplaceStat;
                case -9:
                    return row.DamageSecondary;
                case -10:
                    return row.ManaConsumable;
                default:
                    break;
            }

            return 0.0f;
        }

        public static float GetBattlePetXPPerLevel(GtBattlePetXPRecord row)
        {
            return row.Wins * row.Xp;
        }

        public static float GetIlvlStatMultiplier(GtGenericMultByILvlRecord row, InventoryType invType)
        {
            switch (invType)
            {
                case InventoryType.Neck:
                case InventoryType.Finger:
                    return row.JewelryMultiplier;
                case InventoryType.Trinket:
                    return row.TrinketMultiplier;
                case InventoryType.Weapon:
                case InventoryType.Shield:
                case InventoryType.Ranged:
                case InventoryType.Weapon2Hand:
                case InventoryType.WeaponMainhand:
                case InventoryType.WeaponOffhand:
                case InventoryType.Holdable:
                case InventoryType.RangedRight:
                    return row.WeaponMultiplier;
                default:
                    return row.ArmorMultiplier;
            }
        }

        #endregion
    }
}