// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Framework.Constants;
using Framework.Database;
using Framework.Threading;

namespace Game.DataStorage;

public class CliDB
{
	static LimitedThreadTaskManager _taskManager = new LimitedThreadTaskManager(50);
	public static BitSet LoadStores(string dataPath, Locale defaultLocale)
	{
		var oldMSTime = Time.MSTime;

		var db2Path = $"{dataPath}/dbc";

		BitSet availableDb2Locales = new((int)Locale.Total);

		foreach (var dir in Directory.GetDirectories(db2Path))
		{
			var locale = Path.GetFileName(dir).ToEnum<Locale>();

			if (SharedConst.IsValidLocale(locale))
				availableDb2Locales[(int)locale] = true;
		}

		if (!availableDb2Locales[(int)defaultLocale])
			return null;

		uint loadedFileCount = 0;

		DB6Storage<T> ReadDB2<T>(string fileName, HotfixStatements preparedStatement, HotfixStatements preparedStatementLocale = 0) where T : new()
		{
			DB6Storage<T> storage = new();
			storage.LoadData($"{db2Path}/{defaultLocale}/{fileName}", fileName);
			storage.LoadHotfixData(availableDb2Locales, preparedStatement, preparedStatementLocale);

			Global.DB2Mgr.AddDB2(storage.GetTableHash(), storage);
			loadedFileCount++;

			return storage;
		}

        _taskManager.Schedule(() => AchievementStorage = ReadDB2<AchievementRecord>("Achievement.db2", HotfixStatements.SEL_ACHIEVEMENT, HotfixStatements.SEL_ACHIEVEMENT_LOCALE));
        _taskManager.Schedule(() => AchievementCategoryStorage = ReadDB2<AchievementCategoryRecord>("Achievement_Category.db2", HotfixStatements.SEL_ACHIEVEMENT_CATEGORY, HotfixStatements.SEL_ACHIEVEMENT_CATEGORY_LOCALE));
		_taskManager.Schedule(() => AdventureJournalStorage = ReadDB2<AdventureJournalRecord>("AdventureJournal.db2", HotfixStatements.SEL_ADVENTURE_JOURNAL, HotfixStatements.SEL_ADVENTURE_JOURNAL_LOCALE));
		_taskManager.Schedule(() => AdventureMapPOIStorage = ReadDB2<AdventureMapPOIRecord>("AdventureMapPOI.db2", HotfixStatements.SEL_ADVENTURE_MAP_POI, HotfixStatements.SEL_ADVENTURE_MAP_POI_LOCALE));
        _taskManager.Schedule(() => AnimationDataStorage = ReadDB2<AnimationDataRecord>("AnimationData.db2", HotfixStatements.SEL_ANIMATION_DATA));
        _taskManager.Schedule(() => AnimKitStorage = ReadDB2<AnimKitRecord>("AnimKit.db2", HotfixStatements.SEL_ANIM_KIT));
        _taskManager.Schedule(() => AreaGroupMemberStorage = ReadDB2<AreaGroupMemberRecord>("AreaGroupMember.db2", HotfixStatements.SEL_AREA_GROUP_MEMBER));
        _taskManager.Schedule(() => AreaTableStorage = ReadDB2<AreaTableRecord>("AreaTable.db2", HotfixStatements.SEL_AREA_TABLE, HotfixStatements.SEL_AREA_TABLE_LOCALE));
        _taskManager.Schedule(() => AreaPOIStorage = ReadDB2<AreaPOIRecord>("AreaPOI.db2", HotfixStatements.SEL_AREA_POI, HotfixStatements.SEL_AREA_POI_LOCALE));
        _taskManager.Schedule(() => AreaPOIStateStorage = ReadDB2<AreaPOIStateRecord>("AreaPOIState.db2", HotfixStatements.SEL_AREA_POI_STATE, HotfixStatements.SEL_AREA_POI_STATE_LOCALE));
		_taskManager.Schedule(() =>AreaTriggerStorage = ReadDB2<AreaTriggerRecord>("AreaTrigger.db2", HotfixStatements.SEL_AREA_TRIGGER));
		_taskManager.Schedule(() => ArmorLocationStorage = ReadDB2<ArmorLocationRecord>("ArmorLocation.db2", HotfixStatements.SEL_ARMOR_LOCATION));
		_taskManager.Schedule(() => ArtifactStorage = ReadDB2<ArtifactRecord>("Artifact.db2", HotfixStatements.SEL_ARTIFACT, HotfixStatements.SEL_ARTIFACT_APPEARANCE_LOCALE));
		_taskManager.Schedule(() => ArtifactAppearanceStorage = ReadDB2<ArtifactAppearanceRecord>("ArtifactAppearance.db2", HotfixStatements.SEL_ARTIFACT_APPEARANCE, HotfixStatements.SEL_ARTIFACT_APPEARANCE_LOCALE));
		_taskManager.Schedule(() => ArtifactAppearanceSetStorage = ReadDB2<ArtifactAppearanceSetRecord>("ArtifactAppearanceSet.db2", HotfixStatements.SEL_ARTIFACT_APPEARANCE_SET, HotfixStatements.SEL_ARTIFACT_APPEARANCE_SET_LOCALE));
		_taskManager.Schedule(() => ArtifactCategoryStorage = ReadDB2<ArtifactCategoryRecord>("ArtifactCategory.db2", HotfixStatements.SEL_ARTIFACT_CATEGORY));
		_taskManager.Schedule(() => ArtifactPowerStorage = ReadDB2<ArtifactPowerRecord>("ArtifactPower.db2", HotfixStatements.SEL_ARTIFACT_POWER));
		_taskManager.Schedule(() => ArtifactPowerLinkStorage = ReadDB2<ArtifactPowerLinkRecord>("ArtifactPowerLink.db2", HotfixStatements.SEL_ARTIFACT_POWER_LINK));
		_taskManager.Schedule(() => ArtifactPowerPickerStorage = ReadDB2<ArtifactPowerPickerRecord>("ArtifactPowerPicker.db2", HotfixStatements.SEL_ARTIFACT_POWER_PICKER));
		_taskManager.Schedule(() => ArtifactPowerRankStorage = ReadDB2<ArtifactPowerRankRecord>("ArtifactPowerRank.db2", HotfixStatements.SEL_ARTIFACT_POWER_RANK));
		_taskManager.Schedule(() => ArtifactQuestXPStorage = ReadDB2<ArtifactQuestXPRecord>("ArtifactQuestXP.db2", HotfixStatements.SEL_ARTIFACT_QUEST_XP));
		_taskManager.Schedule(() => ArtifactTierStorage = ReadDB2<ArtifactTierRecord>("ArtifactTier.db2", HotfixStatements.SEL_ARTIFACT_TIER));
		_taskManager.Schedule(() => ArtifactUnlockStorage = ReadDB2<ArtifactUnlockRecord>("ArtifactUnlock.db2", HotfixStatements.SEL_ARTIFACT_UNLOCK));
		_taskManager.Schedule(() => AuctionHouseStorage = ReadDB2<AuctionHouseRecord>("AuctionHouse.db2", HotfixStatements.SEL_AUCTION_HOUSE, HotfixStatements.SEL_AUCTION_HOUSE_LOCALE));
		_taskManager.Schedule(() => AzeriteEmpoweredItemStorage = ReadDB2<AzeriteEmpoweredItemRecord>("AzeriteEmpoweredItem.db2", HotfixStatements.SEL_AZERITE_EMPOWERED_ITEM));
		_taskManager.Schedule(() => AzeriteEssenceStorage = ReadDB2<AzeriteEssenceRecord>("AzeriteEssence.db2", HotfixStatements.SEL_AZERITE_ESSENCE, HotfixStatements.SEL_AZERITE_ESSENCE_LOCALE));
		_taskManager.Schedule(() => AzeriteEssencePowerStorage = ReadDB2<AzeriteEssencePowerRecord>("AzeriteEssencePower.db2", HotfixStatements.SEL_AZERITE_ESSENCE_POWER, HotfixStatements.SEL_AZERITE_ESSENCE_POWER_LOCALE));
		_taskManager.Schedule(() => AzeriteItemStorage = ReadDB2<AzeriteItemRecord>("AzeriteItem.db2", HotfixStatements.SEL_AZERITE_ITEM));
		_taskManager.Schedule(() => AzeriteItemMilestonePowerStorage = ReadDB2<AzeriteItemMilestonePowerRecord>("AzeriteItemMilestonePower.db2", HotfixStatements.SEL_AZERITE_ITEM_MILESTONE_POWER));
		_taskManager.Schedule(() => AzeriteKnowledgeMultiplierStorage = ReadDB2<AzeriteKnowledgeMultiplierRecord>("AzeriteKnowledgeMultiplier.db2", HotfixStatements.SEL_AZERITE_KNOWLEDGE_MULTIPLIER));
		_taskManager.Schedule(() => AzeriteLevelInfoStorage = ReadDB2<AzeriteLevelInfoRecord>("AzeriteLevelInfo.db2", HotfixStatements.SEL_AZERITE_LEVEL_INFO));
		_taskManager.Schedule(() => AzeritePowerStorage = ReadDB2<AzeritePowerRecord>("AzeritePower.db2", HotfixStatements.SEL_AZERITE_POWER));
		_taskManager.Schedule(() => AzeritePowerSetMemberStorage = ReadDB2<AzeritePowerSetMemberRecord>("AzeritePowerSetMember.db2", HotfixStatements.SEL_AZERITE_POWER_SET_MEMBER));
		_taskManager.Schedule(() => AzeriteTierUnlockStorage = ReadDB2<AzeriteTierUnlockRecord>("AzeriteTierUnlock.db2", HotfixStatements.SEL_AZERITE_TIER_UNLOCK));
		_taskManager.Schedule(() => AzeriteTierUnlockSetStorage = ReadDB2<AzeriteTierUnlockSetRecord>("AzeriteTierUnlockSet.db2", HotfixStatements.SEL_AZERITE_TIER_UNLOCK_SET));
		_taskManager.Schedule(() => AzeriteUnlockMappingStorage = ReadDB2<AzeriteUnlockMappingRecord>("AzeriteUnlockMapping.db2", HotfixStatements.SEL_AZERITE_UNLOCK_MAPPING));
		_taskManager.Schedule(() => BankBagSlotPricesStorage = ReadDB2<BankBagSlotPricesRecord>("BankBagSlotPrices.db2", HotfixStatements.SEL_BANK_BAG_SLOT_PRICES));
		_taskManager.Schedule(() => BannedAddOnsStorage = ReadDB2<BannedAddonsRecord>("BannedAddons.db2", HotfixStatements.SEL_BANNED_ADDONS));
		_taskManager.Schedule(() => BarberShopStyleStorage = ReadDB2<BarberShopStyleRecord>("BarberShopStyle.db2", HotfixStatements.SEL_BARBER_SHOP_STYLE, HotfixStatements.SEL_BARBER_SHOP_STYLE_LOCALE));
		_taskManager.Schedule(() => BattlePetBreedQualityStorage = ReadDB2<BattlePetBreedQualityRecord>("BattlePetBreedQuality.db2", HotfixStatements.SEL_BATTLE_PET_BREED_QUALITY));
		_taskManager.Schedule(() => BattlePetBreedStateStorage = ReadDB2<BattlePetBreedStateRecord>("BattlePetBreedState.db2", HotfixStatements.SEL_BATTLE_PET_BREED_STATE));
		_taskManager.Schedule(() => BattlePetSpeciesStorage = ReadDB2<BattlePetSpeciesRecord>("BattlePetSpecies.db2", HotfixStatements.SEL_BATTLE_PET_SPECIES, HotfixStatements.SEL_BATTLE_PET_SPECIES_LOCALE));
		_taskManager.Schedule(() => BattlePetSpeciesStateStorage = ReadDB2<BattlePetSpeciesStateRecord>("BattlePetSpeciesState.db2", HotfixStatements.SEL_BATTLE_PET_SPECIES_STATE));
		_taskManager.Schedule(() => BattlemasterListStorage = ReadDB2<BattlemasterListRecord>("BattlemasterList.db2", HotfixStatements.SEL_BATTLEMASTER_LIST, HotfixStatements.SEL_BATTLEMASTER_LIST_LOCALE));
		_taskManager.Schedule(() => BroadcastTextStorage = ReadDB2<BroadcastTextRecord>("BroadcastText.db2", HotfixStatements.SEL_BROADCAST_TEXT, HotfixStatements.SEL_BROADCAST_TEXT_LOCALE));
		_taskManager.Schedule(() => BroadcastTextDurationStorage = ReadDB2<BroadcastTextDurationRecord>("BroadcastTextDuration.db2", HotfixStatements.SEL_BROADCAST_TEXT_DURATION));
		_taskManager.Schedule(() => CfgRegionsStorage = ReadDB2<Cfg_RegionsRecord>("Cfg_Regions.db2", HotfixStatements.SEL_CFG_REGIONS));
		_taskManager.Schedule(() => CharTitlesStorage = ReadDB2<CharTitlesRecord>("CharTitles.db2", HotfixStatements.SEL_CHAR_TITLES, HotfixStatements.SEL_CHAR_TITLES_LOCALE));
		_taskManager.Schedule(() => CharacterLoadoutStorage = ReadDB2<CharacterLoadoutRecord>("CharacterLoadout.db2", HotfixStatements.SEL_CHARACTER_LOADOUT));
		_taskManager.Schedule(() => CharacterLoadoutItemStorage = ReadDB2<CharacterLoadoutItemRecord>("CharacterLoadoutItem.db2", HotfixStatements.SEL_CHARACTER_LOADOUT_ITEM));
		_taskManager.Schedule(() => ChatChannelsStorage = ReadDB2<ChatChannelsRecord>("ChatChannels.db2", HotfixStatements.SEL_CHAT_CHANNELS, HotfixStatements.SEL_CHAT_CHANNELS_LOCALE));
		_taskManager.Schedule(() => ChrClassUIDisplayStorage = ReadDB2<ChrClassUIDisplayRecord>("ChrClassUIDisplay.db2", HotfixStatements.SEL_CHR_CLASS_UI_DISPLAY));
		_taskManager.Schedule(() => ChrClassesStorage = ReadDB2<ChrClassesRecord>("ChrClasses.db2", HotfixStatements.SEL_CHR_CLASSES, HotfixStatements.SEL_CHR_CLASSES_LOCALE));
		_taskManager.Schedule(() => ChrClassesXPowerTypesStorage = ReadDB2<ChrClassesXPowerTypesRecord>("ChrClassesXPowerTypes.db2", HotfixStatements.SEL_CHR_CLASSES_X_POWER_TYPES));
		_taskManager.Schedule(() => ChrCustomizationChoiceStorage = ReadDB2<ChrCustomizationChoiceRecord>("ChrCustomizationChoice.db2", HotfixStatements.SEL_CHR_CUSTOMIZATION_CHOICE, HotfixStatements.SEL_CHR_CUSTOMIZATION_CHOICE_LOCALE));
		_taskManager.Schedule(() => ChrCustomizationDisplayInfoStorage = ReadDB2<ChrCustomizationDisplayInfoRecord>("ChrCustomizationDisplayInfo.db2", HotfixStatements.SEL_CHR_CUSTOMIZATION_DISPLAY_INFO));
		_taskManager.Schedule(() => ChrCustomizationElementStorage = ReadDB2<ChrCustomizationElementRecord>("ChrCustomizationElement.db2", HotfixStatements.SEL_CHR_CUSTOMIZATION_ELEMENT));
		_taskManager.Schedule(() => ChrCustomizationOptionStorage = ReadDB2<ChrCustomizationOptionRecord>("ChrCustomizationOption.db2", HotfixStatements.SEL_CHR_CUSTOMIZATION_OPTION, HotfixStatements.SEL_CHR_CUSTOMIZATION_OPTION_LOCALE));
		_taskManager.Schedule(() => ChrCustomizationReqStorage = ReadDB2<ChrCustomizationReqRecord>("ChrCustomizationReq.db2", HotfixStatements.SEL_CHR_CUSTOMIZATION_REQ));
		_taskManager.Schedule(() => ChrCustomizationReqChoiceStorage = ReadDB2<ChrCustomizationReqChoiceRecord>("ChrCustomizationReqChoice.db2", HotfixStatements.SEL_CHR_CUSTOMIZATION_REQ_CHOICE));
		_taskManager.Schedule(() => ChrModelStorage = ReadDB2<ChrModelRecord>("ChrModel.db2", HotfixStatements.SEL_CHR_MODEL));
		_taskManager.Schedule(() => ChrRaceXChrModelStorage = ReadDB2<ChrRaceXChrModelRecord>("ChrRaceXChrModel.db2", HotfixStatements.SEL_CHR_RACE_X_CHR_MODEL));
		_taskManager.Schedule(() => ChrRacesStorage = ReadDB2<ChrRacesRecord>("ChrRaces.db2", HotfixStatements.SEL_CHR_RACES, HotfixStatements.SEL_CHR_RACES_LOCALE));
		_taskManager.Schedule(() => ChrSpecializationStorage = ReadDB2<ChrSpecializationRecord>("ChrSpecialization.db2", HotfixStatements.SEL_CHR_SPECIALIZATION, HotfixStatements.SEL_CHR_SPECIALIZATION_LOCALE));
		_taskManager.Schedule(() => CinematicCameraStorage = ReadDB2<CinematicCameraRecord>("CinematicCamera.db2", HotfixStatements.SEL_CINEMATIC_CAMERA));
		_taskManager.Schedule(() => CinematicSequencesStorage = ReadDB2<CinematicSequencesRecord>("CinematicSequences.db2", HotfixStatements.SEL_CINEMATIC_SEQUENCES));
		_taskManager.Schedule(() => ContentTuningStorage = ReadDB2<ContentTuningRecord>("ContentTuning.db2", HotfixStatements.SEL_CONTENT_TUNING));
		_taskManager.Schedule(() => ContentTuningXExpectedStorage = ReadDB2<ContentTuningXExpectedRecord>("ContentTuningXExpected.db2", HotfixStatements.SEL_CONTENT_TUNING_X_EXPECTED));
		_taskManager.Schedule(() => ConversationLineStorage = ReadDB2<ConversationLineRecord>("ConversationLine.db2", HotfixStatements.SEL_CONVERSATION_LINE));
		_taskManager.Schedule(() => CorruptionEffectsStorage = ReadDB2<CorruptionEffectsRecord>("CorruptionEffects.db2", HotfixStatements.SEL_CORRUPTION_EFFECTS));
		_taskManager.Schedule(() => CreatureDisplayInfoStorage = ReadDB2<CreatureDisplayInfoRecord>("CreatureDisplayInfo.db2", HotfixStatements.SEL_CREATURE_DISPLAY_INFO));
		_taskManager.Schedule(() => CreatureDisplayInfoExtraStorage = ReadDB2<CreatureDisplayInfoExtraRecord>("CreatureDisplayInfoExtra.db2", HotfixStatements.SEL_CREATURE_DISPLAY_INFO_EXTRA));
		_taskManager.Schedule(() => CreatureFamilyStorage = ReadDB2<CreatureFamilyRecord>("CreatureFamily.db2", HotfixStatements.SEL_CREATURE_FAMILY, HotfixStatements.SEL_CREATURE_FAMILY_LOCALE));
		_taskManager.Schedule(() => CreatureModelDataStorage = ReadDB2<CreatureModelDataRecord>("CreatureModelData.db2", HotfixStatements.SEL_CREATURE_MODEL_DATA));
		_taskManager.Schedule(() => CreatureTypeStorage = ReadDB2<CreatureTypeRecord>("CreatureType.db2", HotfixStatements.SEL_CREATURE_TYPE, HotfixStatements.SEL_CREATURE_TYPE_LOCALE));
		_taskManager.Schedule(() => CriteriaStorage = ReadDB2<CriteriaRecord>("Criteria.db2", HotfixStatements.SEL_CRITERIA));
		_taskManager.Schedule(() => CriteriaTreeStorage = ReadDB2<CriteriaTreeRecord>("CriteriaTree.db2", HotfixStatements.SEL_CRITERIA_TREE, HotfixStatements.SEL_CRITERIA_TREE_LOCALE));
		_taskManager.Schedule(() => CurrencyContainerStorage = ReadDB2<CurrencyContainerRecord>("CurrencyContainer.db2", HotfixStatements.SEL_CURRENCY_CONTAINER, HotfixStatements.SEL_CURRENCY_CONTAINER_LOCALE));
		_taskManager.Schedule(() => CurrencyTypesStorage = ReadDB2<CurrencyTypesRecord>("CurrencyTypes.db2", HotfixStatements.SEL_CURRENCY_TYPES, HotfixStatements.SEL_CURRENCY_TYPES_LOCALE));
		_taskManager.Schedule(() => CurveStorage = ReadDB2<CurveRecord>("Curve.db2", HotfixStatements.SEL_CURVE));
		_taskManager.Schedule(() => CurvePointStorage = ReadDB2<CurvePointRecord>("CurvePoint.db2", HotfixStatements.SEL_CURVE_POINT));
		_taskManager.Schedule(() => DestructibleModelDataStorage = ReadDB2<DestructibleModelDataRecord>("DestructibleModelData.db2", HotfixStatements.SEL_DESTRUCTIBLE_MODEL_DATA));
		_taskManager.Schedule(() => DifficultyStorage = ReadDB2<DifficultyRecord>("Difficulty.db2", HotfixStatements.SEL_DIFFICULTY, HotfixStatements.SEL_DIFFICULTY_LOCALE));
		_taskManager.Schedule(() => DungeonEncounterStorage = ReadDB2<DungeonEncounterRecord>("DungeonEncounter.db2", HotfixStatements.SEL_DUNGEON_ENCOUNTER, HotfixStatements.SEL_DUNGEON_ENCOUNTER_LOCALE));
		_taskManager.Schedule(() => DurabilityCostsStorage = ReadDB2<DurabilityCostsRecord>("DurabilityCosts.db2", HotfixStatements.SEL_DURABILITY_COSTS));
		_taskManager.Schedule(() => DurabilityQualityStorage = ReadDB2<DurabilityQualityRecord>("DurabilityQuality.db2", HotfixStatements.SEL_DURABILITY_QUALITY));
		_taskManager.Schedule(() => EmotesStorage = ReadDB2<EmotesRecord>("Emotes.db2", HotfixStatements.SEL_EMOTES));
		_taskManager.Schedule(() => EmotesTextStorage = ReadDB2<EmotesTextRecord>("EmotesText.db2", HotfixStatements.SEL_EMOTES_TEXT));
		_taskManager.Schedule(() => EmotesTextSoundStorage = ReadDB2<EmotesTextSoundRecord>("EmotesTextSound.db2", HotfixStatements.SEL_EMOTES_TEXT_SOUND));
		_taskManager.Schedule(() => ExpectedStatStorage = ReadDB2<ExpectedStatRecord>("ExpectedStat.db2", HotfixStatements.SEL_EXPECTED_STAT));
		_taskManager.Schedule(() => ExpectedStatModStorage = ReadDB2<ExpectedStatModRecord>("ExpectedStatMod.db2", HotfixStatements.SEL_EXPECTED_STAT_MOD));
		_taskManager.Schedule(() => FactionStorage = ReadDB2<FactionRecord>("Faction.db2", HotfixStatements.SEL_FACTION, HotfixStatements.SEL_FACTION_LOCALE));
		_taskManager.Schedule(() => FactionTemplateStorage = ReadDB2<FactionTemplateRecord>("FactionTemplate.db2", HotfixStatements.SEL_FACTION_TEMPLATE));
		_taskManager.Schedule(() => FriendshipRepReactionStorage = ReadDB2<FriendshipRepReactionRecord>("FriendshipRepReaction.db2", HotfixStatements.SEL_FRIENDSHIP_REP_REACTION, HotfixStatements.SEL_FRIENDSHIP_REP_REACTION_LOCALE));
		_taskManager.Schedule(() => FriendshipReputationStorage = ReadDB2<FriendshipReputationRecord>("FriendshipReputation.db2", HotfixStatements.SEL_FRIENDSHIP_REPUTATION, HotfixStatements.SEL_FRIENDSHIP_REPUTATION_LOCALE));
		_taskManager.Schedule(() => GameObjectArtKitStorage = ReadDB2<GameObjectArtKitRecord>("GameObjectArtKit.db2", HotfixStatements.SEL_GAMEOBJECT_ART_KIT));
		_taskManager.Schedule(() => GameObjectDisplayInfoStorage = ReadDB2<GameObjectDisplayInfoRecord>("GameObjectDisplayInfo.db2", HotfixStatements.SEL_GAMEOBJECT_DISPLAY_INFO));
		_taskManager.Schedule(() => GameObjectsStorage = ReadDB2<GameObjectsRecord>("GameObjects.db2", HotfixStatements.SEL_GAMEOBJECTS, HotfixStatements.SEL_GAMEOBJECTS_LOCALE));
		_taskManager.Schedule(() => GarrAbilityStorage = ReadDB2<GarrAbilityRecord>("GarrAbility.db2", HotfixStatements.SEL_GARR_ABILITY, HotfixStatements.SEL_GARR_ABILITY_LOCALE));
		_taskManager.Schedule(() => GarrBuildingStorage = ReadDB2<GarrBuildingRecord>("GarrBuilding.db2", HotfixStatements.SEL_GARR_BUILDING, HotfixStatements.SEL_GARR_BUILDING_LOCALE));
		_taskManager.Schedule(() => GarrBuildingPlotInstStorage = ReadDB2<GarrBuildingPlotInstRecord>("GarrBuildingPlotInst.db2", HotfixStatements.SEL_GARR_BUILDING_PLOT_INST));
		_taskManager.Schedule(() => GarrClassSpecStorage = ReadDB2<GarrClassSpecRecord>("GarrClassSpec.db2", HotfixStatements.SEL_GARR_CLASS_SPEC, HotfixStatements.SEL_GARR_CLASS_SPEC_LOCALE));
		_taskManager.Schedule(() => GarrFollowerStorage = ReadDB2<GarrFollowerRecord>("GarrFollower.db2", HotfixStatements.SEL_GARR_FOLLOWER, HotfixStatements.SEL_GARR_FOLLOWER_LOCALE));
		_taskManager.Schedule(() => GarrFollowerXAbilityStorage = ReadDB2<GarrFollowerXAbilityRecord>("GarrFollowerXAbility.db2", HotfixStatements.SEL_GARR_FOLLOWER_X_ABILITY));
		_taskManager.Schedule(() => GarrMissionStorage = ReadDB2<GarrMissionRecord>("GarrMission.db2", HotfixStatements.SEL_GARR_MISSION, HotfixStatements.SEL_GARR_MISSION_LOCALE));
		_taskManager.Schedule(() => GarrPlotStorage = ReadDB2<GarrPlotRecord>("GarrPlot.db2", HotfixStatements.SEL_GARR_PLOT));
		_taskManager.Schedule(() => GarrPlotBuildingStorage = ReadDB2<GarrPlotBuildingRecord>("GarrPlotBuilding.db2", HotfixStatements.SEL_GARR_PLOT_BUILDING));
		_taskManager.Schedule(() => GarrPlotInstanceStorage = ReadDB2<GarrPlotInstanceRecord>("GarrPlotInstance.db2", HotfixStatements.SEL_GARR_PLOT_INSTANCE));
		_taskManager.Schedule(() => GarrSiteLevelStorage = ReadDB2<GarrSiteLevelRecord>("GarrSiteLevel.db2", HotfixStatements.SEL_GARR_SITE_LEVEL));
		_taskManager.Schedule(() => GarrSiteLevelPlotInstStorage = ReadDB2<GarrSiteLevelPlotInstRecord>("GarrSiteLevelPlotInst.db2", HotfixStatements.SEL_GARR_SITE_LEVEL_PLOT_INST));
		_taskManager.Schedule(() => GarrTalentTreeStorage = ReadDB2<GarrTalentTreeRecord>("GarrTalentTree.db2", HotfixStatements.SEL_GARR_TALENT_TREE, HotfixStatements.SEL_GARR_TALENT_TREE_LOCALE));
		_taskManager.Schedule(() => GemPropertiesStorage = ReadDB2<GemPropertiesRecord>("GemProperties.db2", HotfixStatements.SEL_GEM_PROPERTIES));
		_taskManager.Schedule(() => GlobalCurveStorage = ReadDB2<GlobalCurveRecord>("GlobalCurve.db2", HotfixStatements.SEL_GLOBAL_CURVE));
		_taskManager.Schedule(() => GlyphBindableSpellStorage = ReadDB2<GlyphBindableSpellRecord>("GlyphBindableSpell.db2", HotfixStatements.SEL_GLYPH_BINDABLE_SPELL));
		_taskManager.Schedule(() => GlyphPropertiesStorage = ReadDB2<GlyphPropertiesRecord>("GlyphProperties.db2", HotfixStatements.SEL_GLYPH_PROPERTIES));
		_taskManager.Schedule(() => GlyphRequiredSpecStorage = ReadDB2<GlyphRequiredSpecRecord>("GlyphRequiredSpec.db2", HotfixStatements.SEL_GLYPH_REQUIRED_SPEC));
		_taskManager.Schedule(() => GossipNPCOptionStorage = ReadDB2<GossipNPCOptionRecord>("GossipNPCOption.db2", HotfixStatements.SEL_GOSSIP_NPC_OPTION));
		_taskManager.Schedule(() => GuildColorBackgroundStorage = ReadDB2<GuildColorBackgroundRecord>("GuildColorBackground.db2", HotfixStatements.SEL_GUILD_COLOR_BACKGROUND));
		_taskManager.Schedule(() => GuildColorBorderStorage = ReadDB2<GuildColorBorderRecord>("GuildColorBorder.db2", HotfixStatements.SEL_GUILD_COLOR_BORDER));
		_taskManager.Schedule(() => GuildColorEmblemStorage = ReadDB2<GuildColorEmblemRecord>("GuildColorEmblem.db2", HotfixStatements.SEL_GUILD_COLOR_EMBLEM));
		_taskManager.Schedule(() => GuildPerkSpellsStorage = ReadDB2<GuildPerkSpellsRecord>("GuildPerkSpells.db2", HotfixStatements.SEL_GUILD_PERK_SPELLS));
		_taskManager.Schedule(() => HeirloomStorage = ReadDB2<HeirloomRecord>("Heirloom.db2", HotfixStatements.SEL_HEIRLOOM, HotfixStatements.SEL_HEIRLOOM_LOCALE));
		_taskManager.Schedule(() => HolidaysStorage = ReadDB2<HolidaysRecord>("Holidays.db2", HotfixStatements.SEL_HOLIDAYS));
		_taskManager.Schedule(() => ImportPriceArmorStorage = ReadDB2<ImportPriceArmorRecord>("ImportPriceArmor.db2", HotfixStatements.SEL_IMPORT_PRICE_ARMOR));
		_taskManager.Schedule(() => ImportPriceQualityStorage = ReadDB2<ImportPriceQualityRecord>("ImportPriceQuality.db2", HotfixStatements.SEL_IMPORT_PRICE_QUALITY));
		_taskManager.Schedule(() => ImportPriceShieldStorage = ReadDB2<ImportPriceShieldRecord>("ImportPriceShield.db2", HotfixStatements.SEL_IMPORT_PRICE_SHIELD));
		_taskManager.Schedule(() => ImportPriceWeaponStorage = ReadDB2<ImportPriceWeaponRecord>("ImportPriceWeapon.db2", HotfixStatements.SEL_IMPORT_PRICE_WEAPON));
		_taskManager.Schedule(() => ItemAppearanceStorage = ReadDB2<ItemAppearanceRecord>("ItemAppearance.db2", HotfixStatements.SEL_ITEM_APPEARANCE));
		_taskManager.Schedule(() => ItemArmorQualityStorage = ReadDB2<ItemArmorQualityRecord>("ItemArmorQuality.db2", HotfixStatements.SEL_ITEM_ARMOR_QUALITY));
		_taskManager.Schedule(() => ItemArmorShieldStorage = ReadDB2<ItemArmorShieldRecord>("ItemArmorShield.db2", HotfixStatements.SEL_ITEM_ARMOR_SHIELD));
		_taskManager.Schedule(() => ItemArmorTotalStorage = ReadDB2<ItemArmorTotalRecord>("ItemArmorTotal.db2", HotfixStatements.SEL_ITEM_ARMOR_TOTAL));
		//ItemBagFamilyStorage = ReadDB2<ItemBagFamilyRecord>("ItemBagFamily.db2", HotfixStatements.SEL_ITEM_BAG_FAMILY, HotfixStatements.SEL_ITEM_BAG_FAMILY_LOCALE));
		_taskManager.Schedule(() => ItemBonusStorage = ReadDB2<ItemBonusRecord>("ItemBonus.db2", HotfixStatements.SEL_ITEM_BONUS));
		_taskManager.Schedule(() => ItemBonusListLevelDeltaStorage = ReadDB2<ItemBonusListLevelDeltaRecord>("ItemBonusListLevelDelta.db2", HotfixStatements.SEL_ITEM_BONUS_LIST_LEVEL_DELTA));
		_taskManager.Schedule(() => ItemBonusTreeNodeStorage = ReadDB2<ItemBonusTreeNodeRecord>("ItemBonusTreeNode.db2", HotfixStatements.SEL_ITEM_BONUS_TREE_NODE));
		_taskManager.Schedule(() => ItemChildEquipmentStorage = ReadDB2<ItemChildEquipmentRecord>("ItemChildEquipment.db2", HotfixStatements.SEL_ITEM_CHILD_EQUIPMENT));
		_taskManager.Schedule(() => ItemClassStorage = ReadDB2<ItemClassRecord>("ItemClass.db2", HotfixStatements.SEL_ITEM_CLASS, HotfixStatements.SEL_ITEM_CLASS_LOCALE));
		_taskManager.Schedule(() => ItemCurrencyCostStorage = ReadDB2<ItemCurrencyCostRecord>("ItemCurrencyCost.db2", HotfixStatements.SEL_ITEM_CURRENCY_COST));
		_taskManager.Schedule(() => ItemDamageAmmoStorage = ReadDB2<ItemDamageRecord>("ItemDamageAmmo.db2", HotfixStatements.SEL_ITEM_DAMAGE_AMMO));
		_taskManager.Schedule(() => ItemDamageOneHandStorage = ReadDB2<ItemDamageRecord>("ItemDamageOneHand.db2", HotfixStatements.SEL_ITEM_DAMAGE_ONE_HAND));
		_taskManager.Schedule(() => ItemDamageOneHandCasterStorage = ReadDB2<ItemDamageRecord>("ItemDamageOneHandCaster.db2", HotfixStatements.SEL_ITEM_DAMAGE_ONE_HAND_CASTER));
		_taskManager.Schedule(() => ItemDamageTwoHandStorage = ReadDB2<ItemDamageRecord>("ItemDamageTwoHand.db2", HotfixStatements.SEL_ITEM_DAMAGE_TWO_HAND));
		_taskManager.Schedule(() => ItemDamageTwoHandCasterStorage = ReadDB2<ItemDamageRecord>("ItemDamageTwoHandCaster.db2", HotfixStatements.SEL_ITEM_DAMAGE_TWO_HAND_CASTER));
		_taskManager.Schedule(() => ItemDisenchantLootStorage = ReadDB2<ItemDisenchantLootRecord>("ItemDisenchantLoot.db2", HotfixStatements.SEL_ITEM_DISENCHANT_LOOT));
		_taskManager.Schedule(() => ItemEffectStorage = ReadDB2<ItemEffectRecord>("ItemEffect.db2", HotfixStatements.SEL_ITEM_EFFECT));
		_taskManager.Schedule(() => ItemStorage = ReadDB2<ItemRecord>("Item.db2", HotfixStatements.SEL_ITEM));
		_taskManager.Schedule(() => ItemExtendedCostStorage = ReadDB2<ItemExtendedCostRecord>("ItemExtendedCost.db2", HotfixStatements.SEL_ITEM_EXTENDED_COST));
		_taskManager.Schedule(() => ItemLevelSelectorStorage = ReadDB2<ItemLevelSelectorRecord>("ItemLevelSelector.db2", HotfixStatements.SEL_ITEM_LEVEL_SELECTOR));
		_taskManager.Schedule(() => ItemLevelSelectorQualityStorage = ReadDB2<ItemLevelSelectorQualityRecord>("ItemLevelSelectorQuality.db2", HotfixStatements.SEL_ITEM_LEVEL_SELECTOR_QUALITY));
		_taskManager.Schedule(() => ItemLevelSelectorQualitySetStorage = ReadDB2<ItemLevelSelectorQualitySetRecord>("ItemLevelSelectorQualitySet.db2", HotfixStatements.SEL_ITEM_LEVEL_SELECTOR_QUALITY_SET));
		_taskManager.Schedule(() => ItemLimitCategoryStorage = ReadDB2<ItemLimitCategoryRecord>("ItemLimitCategory.db2", HotfixStatements.SEL_ITEM_LIMIT_CATEGORY, HotfixStatements.SEL_ITEM_LIMIT_CATEGORY_LOCALE));
		_taskManager.Schedule(() => ItemLimitCategoryConditionStorage = ReadDB2<ItemLimitCategoryConditionRecord>("ItemLimitCategoryCondition.db2", HotfixStatements.SEL_ITEM_LIMIT_CATEGORY_CONDITION));
		_taskManager.Schedule(() => ItemModifiedAppearanceStorage = ReadDB2<ItemModifiedAppearanceRecord>("ItemModifiedAppearance.db2", HotfixStatements.SEL_ITEM_MODIFIED_APPEARANCE));
		_taskManager.Schedule(() => ItemModifiedAppearanceExtraStorage = ReadDB2<ItemModifiedAppearanceExtraRecord>("ItemModifiedAppearanceExtra.db2", HotfixStatements.SEL_ITEM_MODIFIED_APPEARANCE_EXTRA));
		_taskManager.Schedule(() => ItemNameDescriptionStorage = ReadDB2<ItemNameDescriptionRecord>("ItemNameDescription.db2", HotfixStatements.SEL_ITEM_NAME_DESCRIPTION, HotfixStatements.SEL_ITEM_NAME_DESCRIPTION_LOCALE));
		_taskManager.Schedule(() => ItemPriceBaseStorage = ReadDB2<ItemPriceBaseRecord>("ItemPriceBase.db2", HotfixStatements.SEL_ITEM_PRICE_BASE));
		_taskManager.Schedule(() => ItemSearchNameStorage = ReadDB2<ItemSearchNameRecord>("ItemSearchName.db2", HotfixStatements.SEL_ITEM_SEARCH_NAME, HotfixStatements.SEL_ITEM_SEARCH_NAME_LOCALE));
		_taskManager.Schedule(() => ItemSetStorage = ReadDB2<ItemSetRecord>("ItemSet.db2", HotfixStatements.SEL_ITEM_SET, HotfixStatements.SEL_ITEM_SET_LOCALE));
		_taskManager.Schedule(() => ItemSetSpellStorage = ReadDB2<ItemSetSpellRecord>("ItemSetSpell.db2", HotfixStatements.SEL_ITEM_SET_SPELL));
		_taskManager.Schedule(() => ItemSparseStorage = ReadDB2<ItemSparseRecord>("ItemSparse.db2", HotfixStatements.SEL_ITEM_SPARSE, HotfixStatements.SEL_ITEM_SPARSE_LOCALE));
		_taskManager.Schedule(() => ItemSpecStorage = ReadDB2<ItemSpecRecord>("ItemSpec.db2", HotfixStatements.SEL_ITEM_SPEC));
		_taskManager.Schedule(() => ItemSpecOverrideStorage = ReadDB2<ItemSpecOverrideRecord>("ItemSpecOverride.db2", HotfixStatements.SEL_ITEM_SPEC_OVERRIDE));
		_taskManager.Schedule(() => ItemXBonusTreeStorage = ReadDB2<ItemXBonusTreeRecord>("ItemXBonusTree.db2", HotfixStatements.SEL_ITEM_X_BONUS_TREE));
		_taskManager.Schedule(() => ItemXItemEffectStorage = ReadDB2<ItemXItemEffectRecord>("ItemXItemEffect.db2", HotfixStatements.SEL_ITEM_X_ITEM_EFFECT));
		_taskManager.Schedule(() => JournalEncounterStorage = ReadDB2<JournalEncounterRecord>("JournalEncounter.db2", HotfixStatements.SEL_JOURNAL_ENCOUNTER, HotfixStatements.SEL_JOURNAL_ENCOUNTER_LOCALE));
		_taskManager.Schedule(() => JournalEncounterSectionStorage = ReadDB2<JournalEncounterSectionRecord>("JournalEncounterSection.db2", HotfixStatements.SEL_JOURNAL_ENCOUNTER_SECTION, HotfixStatements.SEL_JOURNAL_ENCOUNTER_SECTION_LOCALE));
		_taskManager.Schedule(() => JournalInstanceStorage = ReadDB2<JournalInstanceRecord>("JournalInstance.db2", HotfixStatements.SEL_JOURNAL_INSTANCE, HotfixStatements.SEL_JOURNAL_INSTANCE_LOCALE));
		_taskManager.Schedule(() => JournalTierStorage = ReadDB2<JournalTierRecord>("JournalTier.db2", HotfixStatements.SEL_JOURNAL_TIER, HotfixStatements.SEL_JOURNAL_TIER_LOCALE));
		//KeyChainStorage = ReadDB2<KeyChainRecord>("KeyChain.db2", HotfixStatements.SEL_KEYCHAIN));
		_taskManager.Schedule(() => KeystoneAffixStorage = ReadDB2<KeystoneAffixRecord>("KeystoneAffix.db2", HotfixStatements.SEL_KEYSTONE_AFFIX, HotfixStatements.SEL_KEYSTONE_AFFIX_LOCALE));
		_taskManager.Schedule(() => LanguageWordsStorage = ReadDB2<LanguageWordsRecord>("LanguageWords.db2", HotfixStatements.SEL_LANGUAGE_WORDS));
		_taskManager.Schedule(() => LanguagesStorage = ReadDB2<LanguagesRecord>("Languages.db2", HotfixStatements.SEL_LANGUAGES, HotfixStatements.SEL_LANGUAGES_LOCALE));
		_taskManager.Schedule(() => LFGDungeonsStorage = ReadDB2<LFGDungeonsRecord>("LFGDungeons.db2", HotfixStatements.SEL_LFG_DUNGEONS, HotfixStatements.SEL_LFG_DUNGEONS_LOCALE));
		_taskManager.Schedule(() => LightStorage = ReadDB2<LightRecord>("Light.db2", HotfixStatements.SEL_LIGHT));
		_taskManager.Schedule(() => LiquidTypeStorage = ReadDB2<LiquidTypeRecord>("LiquidType.db2", HotfixStatements.SEL_LIQUID_TYPE));
		_taskManager.Schedule(() => LockStorage = ReadDB2<LockRecord>("Lock.db2", HotfixStatements.SEL_LOCK));
		_taskManager.Schedule(() => MailTemplateStorage = ReadDB2<MailTemplateRecord>("MailTemplate.db2", HotfixStatements.SEL_MAIL_TEMPLATE, HotfixStatements.SEL_MAIL_TEMPLATE_LOCALE));
		_taskManager.Schedule(() => MapStorage = ReadDB2<MapRecord>("Map.db2", HotfixStatements.SEL_MAP, HotfixStatements.SEL_MAP_LOCALE));
		_taskManager.Schedule(() => MapChallengeModeStorage = ReadDB2<MapChallengeModeRecord>("MapChallengeMode.db2", HotfixStatements.SEL_MAP_CHALLENGE_MODE, HotfixStatements.SEL_MAP_CHALLENGE_MODE_LOCALE));
		_taskManager.Schedule(() => MapDifficultyStorage = ReadDB2<MapDifficultyRecord>("MapDifficulty.db2", HotfixStatements.SEL_MAP_DIFFICULTY, HotfixStatements.SEL_MAP_DIFFICULTY_LOCALE));
		_taskManager.Schedule(() => MapDifficultyXConditionStorage = ReadDB2<MapDifficultyXConditionRecord>("MapDifficultyXCondition.db2", HotfixStatements.SEL_MAP_DIFFICULTY_X_CONDITION, HotfixStatements.SEL_MAP_DIFFICULTY_X_CONDITION_LOCALE));
		_taskManager.Schedule(() => MawPowerStorage = ReadDB2<MawPowerRecord>("MawPower.db2", HotfixStatements.SEL_MAW_POWER));
		_taskManager.Schedule(() => ModifierTreeStorage = ReadDB2<ModifierTreeRecord>("ModifierTree.db2", HotfixStatements.SEL_MODIFIER_TREE));
		_taskManager.Schedule(() => MountCapabilityStorage = ReadDB2<MountCapabilityRecord>("MountCapability.db2", HotfixStatements.SEL_MOUNT_CAPABILITY));
		_taskManager.Schedule(() => MountStorage = ReadDB2<MountRecord>("Mount.db2", HotfixStatements.SEL_MOUNT, HotfixStatements.SEL_MOUNT_LOCALE));
		_taskManager.Schedule(() => MountTypeXCapabilityStorage = ReadDB2<MountTypeXCapabilityRecord>("MountTypeXCapability.db2", HotfixStatements.SEL_MOUNT_TYPE_X_CAPABILITY));
		_taskManager.Schedule(() => MountXDisplayStorage = ReadDB2<MountXDisplayRecord>("MountXDisplay.db2", HotfixStatements.SEL_MOUNT_X_DISPLAY));
		_taskManager.Schedule(() => MovieStorage = ReadDB2<MovieRecord>("Movie.db2", HotfixStatements.SEL_MOVIE));
		_taskManager.Schedule(() => NameGenStorage = ReadDB2<NameGenRecord>("NameGen.db2", HotfixStatements.SEL_NAME_GEN));
		_taskManager.Schedule(() => NamesProfanityStorage = ReadDB2<NamesProfanityRecord>("NamesProfanity.db2", HotfixStatements.SEL_NAMES_PROFANITY));
		_taskManager.Schedule(() => NamesReservedStorage = ReadDB2<NamesReservedRecord>("NamesReserved.db2", HotfixStatements.SEL_NAMES_RESERVED, HotfixStatements.SEL_NAMES_RESERVED_LOCALE));
		_taskManager.Schedule(() => NamesReservedLocaleStorage = ReadDB2<NamesReservedLocaleRecord>("NamesReservedLocale.db2", HotfixStatements.SEL_NAMES_RESERVED_LOCALE));
		_taskManager.Schedule(() => NumTalentsAtLevelStorage = ReadDB2<NumTalentsAtLevelRecord>("NumTalentsAtLevel.db2", HotfixStatements.SEL_NUM_TALENTS_AT_LEVEL));
		_taskManager.Schedule(() => OverrideSpellDataStorage = ReadDB2<OverrideSpellDataRecord>("OverrideSpellData.db2", HotfixStatements.SEL_OVERRIDE_SPELL_DATA));
		_taskManager.Schedule(() => ParagonReputationStorage = ReadDB2<ParagonReputationRecord>("ParagonReputation.db2", HotfixStatements.SEL_PARAGON_REPUTATION));
		_taskManager.Schedule(() => PhaseStorage = ReadDB2<PhaseRecord>("Phase.db2", HotfixStatements.SEL_PHASE));
		_taskManager.Schedule(() => PhaseXPhaseGroupStorage = ReadDB2<PhaseXPhaseGroupRecord>("PhaseXPhaseGroup.db2", HotfixStatements.SEL_PHASE_X_PHASE_GROUP));
		_taskManager.Schedule(() => PlayerConditionStorage = ReadDB2<PlayerConditionRecord>("PlayerCondition.db2", HotfixStatements.SEL_PLAYER_CONDITION, HotfixStatements.SEL_PLAYER_CONDITION_LOCALE));
		_taskManager.Schedule(() => PowerDisplayStorage = ReadDB2<PowerDisplayRecord>("PowerDisplay.db2", HotfixStatements.SEL_POWER_DISPLAY));
		_taskManager.Schedule(() => PowerTypeStorage = ReadDB2<PowerTypeRecord>("PowerType.db2", HotfixStatements.SEL_POWER_TYPE));
		_taskManager.Schedule(() => PrestigeLevelInfoStorage = ReadDB2<PrestigeLevelInfoRecord>("PrestigeLevelInfo.db2", HotfixStatements.SEL_PRESTIGE_LEVEL_INFO, HotfixStatements.SEL_PRESTIGE_LEVEL_INFO_LOCALE));
		_taskManager.Schedule(() => PvpDifficultyStorage = ReadDB2<PvpDifficultyRecord>("PVPDifficulty.db2", HotfixStatements.SEL_PVP_DIFFICULTY));
		_taskManager.Schedule(() => PvpItemStorage = ReadDB2<PvpItemRecord>("PVPItem.db2", HotfixStatements.SEL_PVP_ITEM));
		_taskManager.Schedule(() => PvpTalentStorage = ReadDB2<PvpTalentRecord>("PvpTalent.db2", HotfixStatements.SEL_PVP_TALENT, HotfixStatements.SEL_PVP_TALENT_LOCALE));
		_taskManager.Schedule(() => PvpTalentCategoryStorage = ReadDB2<PvpTalentCategoryRecord>("PvpTalentCategory.db2", HotfixStatements.SEL_PVP_TALENT_CATEGORY));
		_taskManager.Schedule(() => PvpTalentSlotUnlockStorage = ReadDB2<PvpTalentSlotUnlockRecord>("PvpTalentSlotUnlock.db2", HotfixStatements.SEL_PVP_TALENT_SLOT_UNLOCK));
		_taskManager.Schedule(() => PvpTierStorage = ReadDB2<PvpTierRecord>("PvpTier.db2", HotfixStatements.SEL_PVP_TIER, HotfixStatements.SEL_PVP_TIER_LOCALE));
		_taskManager.Schedule(() => QuestFactionRewardStorage = ReadDB2<QuestFactionRewardRecord>("QuestFactionReward.db2", HotfixStatements.SEL_QUEST_FACTION_REWARD));
		_taskManager.Schedule(() => QuestInfoStorage = ReadDB2<QuestInfoRecord>("QuestInfo.db2", HotfixStatements.SEL_QUEST_INFO, HotfixStatements.SEL_QUEST_INFO_LOCALE));
		_taskManager.Schedule(() => QuestLineXQuestStorage = ReadDB2<QuestLineXQuestRecord>("QuestLineXQuest.db2", HotfixStatements.SEL_QUEST_LINE_X_QUEST));
		_taskManager.Schedule(() => QuestMoneyRewardStorage = ReadDB2<QuestMoneyRewardRecord>("QuestMoneyReward.db2", HotfixStatements.SEL_QUEST_MONEY_REWARD));
		_taskManager.Schedule(() => QuestPackageItemStorage = ReadDB2<QuestPackageItemRecord>("QuestPackageItem.db2", HotfixStatements.SEL_QUEST_PACKAGE_ITEM));
		_taskManager.Schedule(() => QuestSortStorage = ReadDB2<QuestSortRecord>("QuestSort.db2", HotfixStatements.SEL_QUEST_SORT, HotfixStatements.SEL_QUEST_SORT_LOCALE));
		_taskManager.Schedule(() => QuestV2Storage = ReadDB2<QuestV2Record>("QuestV2.db2", HotfixStatements.SEL_QUEST_V2));
		_taskManager.Schedule(() => QuestXPStorage = ReadDB2<QuestXPRecord>("QuestXP.db2", HotfixStatements.SEL_QUEST_XP));
		_taskManager.Schedule(() => RandPropPointsStorage = ReadDB2<RandPropPointsRecord>("RandPropPoints.db2", HotfixStatements.SEL_RAND_PROP_POINTS));
		_taskManager.Schedule(() => RewardPackStorage = ReadDB2<RewardPackRecord>("RewardPack.db2", HotfixStatements.SEL_REWARD_PACK));
		_taskManager.Schedule(() => RewardPackXCurrencyTypeStorage = ReadDB2<RewardPackXCurrencyTypeRecord>("RewardPackXCurrencyType.db2", HotfixStatements.SEL_REWARD_PACK_X_CURRENCY_TYPE));
		_taskManager.Schedule(() => RewardPackXItemStorage = ReadDB2<RewardPackXItemRecord>("RewardPackXItem.db2", HotfixStatements.SEL_REWARD_PACK_X_ITEM));
		_taskManager.Schedule(() => ScenarioStorage = ReadDB2<ScenarioRecord>("Scenario.db2", HotfixStatements.SEL_SCENARIO, HotfixStatements.SEL_SCENARIO_LOCALE));
		_taskManager.Schedule(() => ScenarioStepStorage = ReadDB2<ScenarioStepRecord>("ScenarioStep.db2", HotfixStatements.SEL_SCENARIO_STEP, HotfixStatements.SEL_SCENARIO_STEP_LOCALE));
		_taskManager.Schedule(() => SceneScriptStorage = ReadDB2<SceneScriptRecord>("SceneScript.db2", HotfixStatements.SEL_SCENE_SCRIPT));
		_taskManager.Schedule(() => SceneScriptGlobalTextStorage = ReadDB2<SceneScriptGlobalTextRecord>("SceneScriptGlobalText.db2", HotfixStatements.SEL_SCENE_SCRIPT_GLOBAL_TEXT));
		_taskManager.Schedule(() => SceneScriptPackageStorage = ReadDB2<SceneScriptPackageRecord>("SceneScriptPackage.db2", HotfixStatements.SEL_SCENE_SCRIPT_PACKAGE));
		_taskManager.Schedule(() => SceneScriptTextStorage = ReadDB2<SceneScriptTextRecord>("SceneScriptText.db2", HotfixStatements.SEL_SCENE_SCRIPT_TEXT));
		_taskManager.Schedule(() => SkillLineStorage = ReadDB2<SkillLineRecord>("SkillLine.db2", HotfixStatements.SEL_SKILL_LINE, HotfixStatements.SEL_SKILL_LINE_LOCALE));
		_taskManager.Schedule(() => SkillLineAbilityStorage = ReadDB2<SkillLineAbilityRecord>("SkillLineAbility.db2", HotfixStatements.SEL_SKILL_LINE_ABILITY));
		_taskManager.Schedule(() => SkillLineXTraitTreeStorage = ReadDB2<SkillLineXTraitTreeRecord>("SkillLineXTraitTree.db2", HotfixStatements.SEL_SKILL_LINE_X_TRAIT_TREE));
		_taskManager.Schedule(() => SkillRaceClassInfoStorage = ReadDB2<SkillRaceClassInfoRecord>("SkillRaceClassInfo.db2", HotfixStatements.SEL_SKILL_RACE_CLASS_INFO));
		_taskManager.Schedule(() => SoulbindConduitRankStorage = ReadDB2<SoulbindConduitRankRecord>("SoulbindConduitRank.db2", HotfixStatements.SEL_SOULBIND_CONDUIT_RANK));
		_taskManager.Schedule(() => SoundKitStorage = ReadDB2<SoundKitRecord>("SoundKit.db2", HotfixStatements.SEL_SOUND_KIT));
		_taskManager.Schedule(() => SpecializationSpellsStorage = ReadDB2<SpecializationSpellsRecord>("SpecializationSpells.db2", HotfixStatements.SEL_SPECIALIZATION_SPELLS, HotfixStatements.SEL_SPECIALIZATION_SPELLS_LOCALE));
		_taskManager.Schedule(() => SpecSetMemberStorage = ReadDB2<SpecSetMemberRecord>("SpecSetMember.db2", HotfixStatements.SEL_SPEC_SET_MEMBER));
		_taskManager.Schedule(() => SpellStorage = ReadDB2<SpellRecord>("Spell.db2", HotfixStatements.SEL_SPELL));
		_taskManager.Schedule(() => SpellNameStorage = ReadDB2<SpellNameRecord>("SpellName.db2", HotfixStatements.SEL_SPELL_NAME, HotfixStatements.SEL_SPELL_NAME_LOCALE));
		_taskManager.Schedule(() => SpellAuraOptionsStorage = ReadDB2<SpellAuraOptionsRecord>("SpellAuraOptions.db2", HotfixStatements.SEL_SPELL_AURA_OPTIONS));
		_taskManager.Schedule(() => SpellAuraRestrictionsStorage = ReadDB2<SpellAuraRestrictionsRecord>("SpellAuraRestrictions.db2", HotfixStatements.SEL_SPELL_AURA_RESTRICTIONS));
		_taskManager.Schedule(() => SpellCastTimesStorage = ReadDB2<SpellCastTimesRecord>("SpellCastTimes.db2", HotfixStatements.SEL_SPELL_CAST_TIMES));
		_taskManager.Schedule(() => SpellCastingRequirementsStorage = ReadDB2<SpellCastingRequirementsRecord>("SpellCastingRequirements.db2", HotfixStatements.SEL_SPELL_CASTING_REQUIREMENTS));
		_taskManager.Schedule(() => SpellCategoriesStorage = ReadDB2<SpellCategoriesRecord>("SpellCategories.db2", HotfixStatements.SEL_SPELL_CATEGORIES));
		_taskManager.Schedule(() => SpellCategoryStorage = ReadDB2<SpellCategoryRecord>("SpellCategory.db2", HotfixStatements.SEL_SPELL_CATEGORY, HotfixStatements.SEL_SPELL_CATEGORY_LOCALE));
		_taskManager.Schedule(() => SpellClassOptionsStorage = ReadDB2<SpellClassOptionsRecord>("SpellClassOptions.db2", HotfixStatements.SEL_SPELL_CLASS_OPTIONS));
		_taskManager.Schedule(() => SpellCooldownsStorage = ReadDB2<SpellCooldownsRecord>("SpellCooldowns.db2", HotfixStatements.SEL_SPELL_COOLDOWNS));
		_taskManager.Schedule(() => SpellDurationStorage = ReadDB2<SpellDurationRecord>("SpellDuration.db2", HotfixStatements.SEL_SPELL_DURATION));
		_taskManager.Schedule(() => SpellEffectStorage = ReadDB2<SpellEffectRecord>("SpellEffect.db2", HotfixStatements.SEL_SPELL_EFFECT));
		_taskManager.Schedule(() => SpellEmpowerStorage = ReadDB2<SpellEmpowerRecord>("SpellEmpower.db2", HotfixStatements.SEL_SPELL_EMPOWER));
		_taskManager.Schedule(() => SpellEmpowerStageStorage = ReadDB2<SpellEmpowerStageRecord>("SpellEmpowerStage.db2", HotfixStatements.SEL_SPELL_EMPOWER_STAGE));
		_taskManager.Schedule(() => SpellEquippedItemsStorage = ReadDB2<SpellEquippedItemsRecord>("SpellEquippedItems.db2", HotfixStatements.SEL_SPELL_EQUIPPED_ITEMS));
		_taskManager.Schedule(() => SpellFocusObjectStorage = ReadDB2<SpellFocusObjectRecord>("SpellFocusObject.db2", HotfixStatements.SEL_SPELL_FOCUS_OBJECT, HotfixStatements.SEL_SPELL_FOCUS_OBJECT_LOCALE));
		_taskManager.Schedule(() => SpellInterruptsStorage = ReadDB2<SpellInterruptsRecord>("SpellInterrupts.db2", HotfixStatements.SEL_SPELL_INTERRUPTS));
		_taskManager.Schedule(() => SpellItemEnchantmentStorage = ReadDB2<SpellItemEnchantmentRecord>("SpellItemEnchantment.db2", HotfixStatements.SEL_SPELL_ITEM_ENCHANTMENT, HotfixStatements.SEL_SPELL_ITEM_ENCHANTMENT_LOCALE));
		_taskManager.Schedule(() => SpellItemEnchantmentConditionStorage = ReadDB2<SpellItemEnchantmentConditionRecord>("SpellItemEnchantmentCondition.db2", HotfixStatements.SEL_SPELL_ITEM_ENCHANTMENT_CONDITION));
		_taskManager.Schedule(() => SpellKeyboundOverrideStorage = ReadDB2<SpellKeyboundOverrideRecord>("SpellKeyboundOverride.db2", HotfixStatements.SEL_SPELL_KEYBOUND_OVERRIDE));
		_taskManager.Schedule(() => SpellLabelStorage = ReadDB2<SpellLabelRecord>("SpellLabel.db2", HotfixStatements.SEL_SPELL_LABEL));
		_taskManager.Schedule(() => SpellLearnSpellStorage = ReadDB2<SpellLearnSpellRecord>("SpellLearnSpell.db2", HotfixStatements.SEL_SPELL_LEARN_SPELL));
		_taskManager.Schedule(() => SpellLevelsStorage = ReadDB2<SpellLevelsRecord>("SpellLevels.db2", HotfixStatements.SEL_SPELL_LEVELS));
		_taskManager.Schedule(() => SpellMiscStorage = ReadDB2<SpellMiscRecord>("SpellMisc.db2", HotfixStatements.SEL_SPELL_MISC));
		_taskManager.Schedule(() => SpellPowerStorage = ReadDB2<SpellPowerRecord>("SpellPower.db2", HotfixStatements.SEL_SPELL_POWER));
		_taskManager.Schedule(() => SpellPowerDifficultyStorage = ReadDB2<SpellPowerDifficultyRecord>("SpellPowerDifficulty.db2", HotfixStatements.SEL_SPELL_POWER_DIFFICULTY));
		_taskManager.Schedule(() => SpellProcsPerMinuteStorage = ReadDB2<SpellProcsPerMinuteRecord>("SpellProcsPerMinute.db2", HotfixStatements.SEL_SPELL_PROCS_PER_MINUTE));
		_taskManager.Schedule(() => SpellProcsPerMinuteModStorage = ReadDB2<SpellProcsPerMinuteModRecord>("SpellProcsPerMinuteMod.db2", HotfixStatements.SEL_SPELL_PROCS_PER_MINUTE_MOD));
		_taskManager.Schedule(() => SpellRadiusStorage = ReadDB2<SpellRadiusRecord>("SpellRadius.db2", HotfixStatements.SEL_SPELL_RADIUS));
		_taskManager.Schedule(() => SpellRangeStorage = ReadDB2<SpellRangeRecord>("SpellRange.db2", HotfixStatements.SEL_SPELL_RANGE, HotfixStatements.SEL_SPELL_RANGE_LOCALE));
		_taskManager.Schedule(() => SpellReagentsStorage = ReadDB2<SpellReagentsRecord>("SpellReagents.db2", HotfixStatements.SEL_SPELL_REAGENTS));
		_taskManager.Schedule(() => SpellReagentsCurrencyStorage = ReadDB2<SpellReagentsCurrencyRecord>("SpellReagentsCurrency.db2", HotfixStatements.SEL_SPELL_REAGENTS_CURRENCY));
		_taskManager.Schedule(() => SpellReplacementStorage = ReadDB2<SpellReplacementRecord>("SpellReplacement.db2", HotfixStatements.SEL_SPELL_REPLACEMENT));
		_taskManager.Schedule(() => SpellScalingStorage = ReadDB2<SpellScalingRecord>("SpellScaling.db2", HotfixStatements.SEL_SPELL_SCALING));
		_taskManager.Schedule(() => SpellShapeshiftStorage = ReadDB2<SpellShapeshiftRecord>("SpellShapeshift.db2", HotfixStatements.SEL_SPELL_SHAPESHIFT));
		_taskManager.Schedule(() => SpellShapeshiftFormStorage = ReadDB2<SpellShapeshiftFormRecord>("SpellShapeshiftForm.db2", HotfixStatements.SEL_SPELL_SHAPESHIFT_FORM, HotfixStatements.SEL_SPELL_SHAPESHIFT_FORM_LOCALE));
		_taskManager.Schedule(() => SpellTargetRestrictionsStorage = ReadDB2<SpellTargetRestrictionsRecord>("SpellTargetRestrictions.db2", HotfixStatements.SEL_SPELL_TARGET_RESTRICTIONS));
		_taskManager.Schedule(() => SpellTotemsStorage = ReadDB2<SpellTotemsRecord>("SpellTotems.db2", HotfixStatements.SEL_SPELL_TOTEMS));
		_taskManager.Schedule(() => SpellVisualStorage = ReadDB2<SpellVisualRecord>("SpellVisual.db2", HotfixStatements.SEL_SPELL_VISUAL));
		_taskManager.Schedule(() => SpellVisualEffectNameStorage = ReadDB2<SpellVisualEffectNameRecord>("SpellVisualEffectName.db2", HotfixStatements.SEL_SPELL_VISUAL_EFFECT_NAME));
		_taskManager.Schedule(() => SpellVisualMissileStorage = ReadDB2<SpellVisualMissileRecord>("SpellVisualMissile.db2", HotfixStatements.SEL_SPELL_VISUAL_MISSILE));
		_taskManager.Schedule(() => SpellVisualKitStorage = ReadDB2<SpellVisualKitRecord>("SpellVisualKit.db2", HotfixStatements.SEL_SPELL_VISUAL_KIT));
		_taskManager.Schedule(() => SpellXSpellVisualStorage = ReadDB2<SpellXSpellVisualRecord>("SpellXSpellVisual.db2", HotfixStatements.SEL_SPELL_X_SPELL_VISUAL));
		_taskManager.Schedule(() => SummonPropertiesStorage = ReadDB2<SummonPropertiesRecord>("SummonProperties.db2", HotfixStatements.SEL_SUMMON_PROPERTIES));
		_taskManager.Schedule(() => TactKeyStorage = ReadDB2<TactKeyRecord>("TactKey.db2", HotfixStatements.SEL_TACT_KEY));
		_taskManager.Schedule(() => TalentStorage = ReadDB2<TalentRecord>("Talent.db2", HotfixStatements.SEL_TALENT, HotfixStatements.SEL_TALENT_LOCALE));
		_taskManager.Schedule(() => TaxiNodesStorage = ReadDB2<TaxiNodesRecord>("TaxiNodes.db2", HotfixStatements.SEL_TAXI_NODES, HotfixStatements.SEL_TAXI_NODES_LOCALE));
		_taskManager.Schedule(() => TaxiPathStorage = ReadDB2<TaxiPathRecord>("TaxiPath.db2", HotfixStatements.SEL_TAXI_PATH));
		_taskManager.Schedule(() => TaxiPathNodeStorage = ReadDB2<TaxiPathNodeRecord>("TaxiPathNode.db2", HotfixStatements.SEL_TAXI_PATH_NODE));
		_taskManager.Schedule(() => TotemCategoryStorage = ReadDB2<TotemCategoryRecord>("TotemCategory.db2", HotfixStatements.SEL_TOTEM_CATEGORY, HotfixStatements.SEL_TOTEM_CATEGORY_LOCALE));
		_taskManager.Schedule(() => ToyStorage = ReadDB2<ToyRecord>("Toy.db2", HotfixStatements.SEL_TOY, HotfixStatements.SEL_TOY_LOCALE));
		_taskManager.Schedule(() => TraitCondStorage = ReadDB2<TraitCondRecord>("TraitCond.db2", HotfixStatements.SEL_TRAIT_COND));
		_taskManager.Schedule(() => TraitCostStorage = ReadDB2<TraitCostRecord>("TraitCost.db2", HotfixStatements.SEL_TRAIT_COST));
		_taskManager.Schedule(() => TraitCurrencyStorage = ReadDB2<TraitCurrencyRecord>("TraitCurrency.db2", HotfixStatements.SEL_TRAIT_CURRENCY));
		_taskManager.Schedule(() => TraitCurrencySourceStorage = ReadDB2<TraitCurrencySourceRecord>("TraitCurrencySource.db2", HotfixStatements.SEL_TRAIT_CURRENCY_SOURCE, HotfixStatements.SEL_TRAIT_CURRENCY_SOURCE_LOCALE));
		_taskManager.Schedule(() => TraitDefinitionStorage = ReadDB2<TraitDefinitionRecord>("TraitDefinition.db2", HotfixStatements.SEL_TRAIT_DEFINITION, HotfixStatements.SEL_TRAIT_DEFINITION_LOCALE));
		_taskManager.Schedule(() => TraitDefinitionEffectPointsStorage = ReadDB2<TraitDefinitionEffectPointsRecord>("TraitDefinitionEffectPoints.db2", HotfixStatements.SEL_TRAIT_DEFINITION_EFFECT_POINTS));
		_taskManager.Schedule(() => TraitEdgeStorage = ReadDB2<TraitEdgeRecord>("TraitEdge.db2", HotfixStatements.SEL_TRAIT_EDGE));
		_taskManager.Schedule(() => TraitNodeStorage = ReadDB2<TraitNodeRecord>("TraitNode.db2", HotfixStatements.SEL_TRAIT_NODE));
		_taskManager.Schedule(() => TraitNodeEntryStorage = ReadDB2<TraitNodeEntryRecord>("TraitNodeEntry.db2", HotfixStatements.SEL_TRAIT_NODE_ENTRY));
		_taskManager.Schedule(() => TraitNodeEntryXTraitCondStorage = ReadDB2<TraitNodeEntryXTraitCondRecord>("TraitNodeEntryXTraitCond.db2", HotfixStatements.SEL_TRAIT_NODE_ENTRY_X_TRAIT_COND));
		_taskManager.Schedule(() => TraitNodeEntryXTraitCostStorage = ReadDB2<TraitNodeEntryXTraitCostRecord>("TraitNodeEntryXTraitCost.db2", HotfixStatements.SEL_TRAIT_NODE_ENTRY_X_TRAIT_COST));
		_taskManager.Schedule(() => TraitNodeGroupStorage = ReadDB2<TraitNodeGroupRecord>("TraitNodeGroup.db2", HotfixStatements.SEL_TRAIT_NODE_GROUP));
		_taskManager.Schedule(() => TraitNodeGroupXTraitCondStorage = ReadDB2<TraitNodeGroupXTraitCondRecord>("TraitNodeGroupXTraitCond.db2", HotfixStatements.SEL_TRAIT_NODE_GROUP_X_TRAIT_COND));
		_taskManager.Schedule(() => TraitNodeGroupXTraitCostStorage = ReadDB2<TraitNodeGroupXTraitCostRecord>("TraitNodeGroupXTraitCost.db2", HotfixStatements.SEL_TRAIT_NODE_GROUP_X_TRAIT_COST));
		_taskManager.Schedule(() => TraitNodeGroupXTraitNodeStorage = ReadDB2<TraitNodeGroupXTraitNodeRecord>("TraitNodeGroupXTraitNode.db2", HotfixStatements.SEL_TRAIT_NODE_GROUP_X_TRAIT_NODE));
		_taskManager.Schedule(() => TraitNodeXTraitCondStorage = ReadDB2<TraitNodeXTraitCondRecord>("TraitNodeXTraitCond.db2", HotfixStatements.SEL_TRAIT_NODE_X_TRAIT_COND));
		_taskManager.Schedule(() => TraitNodeXTraitCostStorage = ReadDB2<TraitNodeXTraitCostRecord>("TraitNodeXTraitCost.db2", HotfixStatements.SEL_TRAIT_NODE_X_TRAIT_COST));
		_taskManager.Schedule(() => TraitNodeXTraitNodeEntryStorage = ReadDB2<TraitNodeXTraitNodeEntryRecord>("TraitNodeXTraitNodeEntry.db2", HotfixStatements.SEL_TRAIT_NODE_X_TRAIT_NODE_ENTRY));
		_taskManager.Schedule(() => TraitTreeStorage = ReadDB2<TraitTreeRecord>("TraitTree.db2", HotfixStatements.SEL_TRAIT_TREE));
		_taskManager.Schedule(() => TraitTreeLoadoutStorage = ReadDB2<TraitTreeLoadoutRecord>("TraitTreeLoadout.db2", HotfixStatements.SEL_TRAIT_TREE_LOADOUT));
		_taskManager.Schedule(() => TraitTreeLoadoutEntryStorage = ReadDB2<TraitTreeLoadoutEntryRecord>("TraitTreeLoadoutEntry.db2", HotfixStatements.SEL_TRAIT_TREE_LOADOUT_ENTRY));
		_taskManager.Schedule(() => TraitTreeXTraitCostStorage = ReadDB2<TraitTreeXTraitCostRecord>("TraitTreeXTraitCost.db2", HotfixStatements.SEL_TRAIT_TREE_X_TRAIT_COST));
		_taskManager.Schedule(() => TraitTreeXTraitCurrencyStorage = ReadDB2<TraitTreeXTraitCurrencyRecord>("TraitTreeXTraitCurrency.db2", HotfixStatements.SEL_TRAIT_TREE_X_TRAIT_CURRENCY));
		_taskManager.Schedule(() => TransmogHolidayStorage = ReadDB2<TransmogHolidayRecord>("TransmogHoliday.db2", HotfixStatements.SEL_TRANSMOG_HOLIDAY));
		_taskManager.Schedule(() => TransmogIllusionStorage = ReadDB2<TransmogIllusionRecord>("TransmogIllusion.db2", HotfixStatements.SEL_TRANSMOG_ILLUSION));
		_taskManager.Schedule(() => TransmogSetStorage = ReadDB2<TransmogSetRecord>("TransmogSet.db2", HotfixStatements.SEL_TRANSMOG_SET, HotfixStatements.SEL_TRANSMOG_SET_LOCALE));
		_taskManager.Schedule(() => TransmogSetGroupStorage = ReadDB2<TransmogSetGroupRecord>("TransmogSetGroup.db2", HotfixStatements.SEL_TRANSMOG_SET_GROUP, HotfixStatements.SEL_TRANSMOG_SET_GROUP_LOCALE));
		_taskManager.Schedule(() => TransmogSetItemStorage = ReadDB2<TransmogSetItemRecord>("TransmogSetItem.db2", HotfixStatements.SEL_TRANSMOG_SET_ITEM));
		_taskManager.Schedule(() => TransportAnimationStorage = ReadDB2<TransportAnimationRecord>("TransportAnimation.db2", HotfixStatements.SEL_TRANSPORT_ANIMATION));
		_taskManager.Schedule(() => TransportRotationStorage = ReadDB2<TransportRotationRecord>("TransportRotation.db2", HotfixStatements.SEL_TRANSPORT_ROTATION));
		_taskManager.Schedule(() => UiMapStorage = ReadDB2<UiMapRecord>("UiMap.db2", HotfixStatements.SEL_UI_MAP, HotfixStatements.SEL_UI_MAP_LOCALE));
		_taskManager.Schedule(() => UiMapAssignmentStorage = ReadDB2<UiMapAssignmentRecord>("UiMapAssignment.db2", HotfixStatements.SEL_UI_MAP_ASSIGNMENT));
		_taskManager.Schedule(() => UiMapLinkStorage = ReadDB2<UiMapLinkRecord>("UiMapLink.db2", HotfixStatements.SEL_UI_MAP_LINK));
		_taskManager.Schedule(() => UiMapXMapArtStorage = ReadDB2<UiMapXMapArtRecord>("UiMapXMapArt.db2", HotfixStatements.SEL_UI_MAP_X_MAP_ART));
		_taskManager.Schedule(() => UISplashScreenStorage = ReadDB2<UISplashScreenRecord>("UISplashScreen.db2", HotfixStatements.SEL_UI_SPLASH_SCREEN, HotfixStatements.SEL_UI_SPLASH_SCREEN_LOCALE));
		_taskManager.Schedule(() => UnitConditionStorage = ReadDB2<UnitConditionRecord>("UnitCondition.db2", HotfixStatements.SEL_UNIT_CONDITION));
		_taskManager.Schedule(() => UnitPowerBarStorage = ReadDB2<UnitPowerBarRecord>("UnitPowerBar.db2", HotfixStatements.SEL_UNIT_POWER_BAR, HotfixStatements.SEL_UNIT_POWER_BAR_LOCALE));
		_taskManager.Schedule(() => VehicleStorage = ReadDB2<VehicleRecord>("Vehicle.db2", HotfixStatements.SEL_VEHICLE));
		_taskManager.Schedule(() => VehicleSeatStorage = ReadDB2<VehicleSeatRecord>("VehicleSeat.db2", HotfixStatements.SEL_VEHICLE_SEAT));
		_taskManager.Schedule(() => WMOAreaTableStorage = ReadDB2<WMOAreaTableRecord>("WMOAreaTable.db2", HotfixStatements.SEL_WMO_AREA_TABLE, HotfixStatements.SEL_WMO_AREA_TABLE_LOCALE));
		_taskManager.Schedule(() => WorldEffectStorage = ReadDB2<WorldEffectRecord>("WorldEffect.db2", HotfixStatements.SEL_WORLD_EFFECT));
		_taskManager.Schedule(() => WorldMapOverlayStorage = ReadDB2<WorldMapOverlayRecord>("WorldMapOverlay.db2", HotfixStatements.SEL_WORLD_MAP_OVERLAY));
		_taskManager.Schedule(() => WorldStateExpressionStorage = ReadDB2<WorldStateExpressionRecord>("WorldStateExpression.db2", HotfixStatements.SEL_WORLD_STATE_EXPRESSION));
		_taskManager.Schedule(() => CharBaseInfoStorage = ReadDB2<CharBaseInfo>("CharBaseInfo.db2", HotfixStatements.SEL_CHAR_BASE_INFO));

		_taskManager.Wait();

        Global.DB2Mgr.LoadStores();
#if DEBUG
		Log.outInfo(LogFilter.ServerLoading, $"DB2  TableHash");

		foreach (var kvp in DB2Manager.Instance.Storage)
			Log.outInfo(LogFilter.ServerLoading, $"{kvp.Value.GetName()}    {kvp.Key}");
#endif
		foreach (var entry in TaxiPathStorage.Values)
			TaxiPathSetBySource.Add(entry.FromTaxiNode, entry.ToTaxiNode, new TaxiPathBySourceAndDestination(entry.Id, entry.Cost));

		var pathCount = TaxiPathStorage.GetNumRows();

		// Calculate path nodes count
		var pathLength = new uint[pathCount]; // 0 and some other indexes not used

		foreach (var entry in TaxiPathNodeStorage.Values)
			if (pathLength[entry.PathID] < entry.NodeIndex + 1)
				pathLength[entry.PathID] = (uint)entry.NodeIndex + 1u;

		// Set path length
		for (uint i = 0; i < pathCount; ++i)
			TaxiPathNodesByPath[i] = new TaxiPathNodeRecord[pathLength[i]];

		// fill data
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
			var field = (node.Id - 1) / 8;
			var submask = (byte)(1 << (int)((node.Id - 1) % 8));

			TaxiNodesMask[field] |= submask;

			if (node.Flags.HasAnyFlag(TaxiNodeFlags.Horde))
				HordeTaxiNodesMask[field] |= submask;

			if (node.Flags.HasAnyFlag(TaxiNodeFlags.Alliance))
				AllianceTaxiNodesMask[field] |= submask;

			if (!Global.DB2Mgr.GetUiMapPosition(node.Pos.X, node.Pos.Y, node.Pos.Z, node.ContinentID, 0, 0, 0, UiMapSystem.Adventure, false, out int uiMapId))
				Global.DB2Mgr.GetUiMapPosition(node.Pos.X, node.Pos.Y, node.Pos.Z, node.ContinentID, 0, 0, 0, UiMapSystem.Taxi, false, out uiMapId);

			if (uiMapId == 985 || uiMapId == 986)
				OldContinentsNodesMask[field] |= submask;
		}

		// Check loaded DB2 files proper version
		if (!AreaTableStorage.ContainsKey(14618) ||       // last area added in 10.0.5 (47660)
			!CharTitlesStorage.ContainsKey(753) ||        // last char title added in 10.0.5 (47660)
			!GemPropertiesStorage.ContainsKey(4028) ||    // last gem property added in 10.0.5 (47660)
			!ItemStorage.ContainsKey(203716) ||           // last item added in 10.0.5 (47660)
			!ItemExtendedCostStorage.ContainsKey(7882) || // last item extended cost added in 10.0.5 (47660)
			!MapStorage.ContainsKey(2582) ||              // last map added in 10.0.5 (47660)
			!SpellNameStorage.ContainsKey(401848))        // last spell added in 10.0.5 (47660)
		{
			Log.outFatal(LogFilter.ServerLoading, "You have _outdated_ DB2 files. Please extract correct versions from current using client.");
			Environment.Exit(1);
		}

		Log.outInfo(LogFilter.ServerLoading, "Initialized {0} DB2 data storages in {1} ms", loadedFileCount, Time.GetMSTimeDiffToNow(oldMSTime));

		return availableDb2Locales;
	}

	public static void LoadGameTables(string dataPath)
	{
		var oldMSTime = Time.MSTime;

		var gtPath = dataPath + "/gt/";

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

		Log.outInfo(LogFilter.ServerLoading, "Initialized {0} DBC GameTables data stores in {1} ms", loadedFileCount, Time.GetMSTimeDiffToNow(oldMSTime));
	}

	#region Main Collections

	public static DB6Storage<AchievementRecord> AchievementStorage;
	public static DB6Storage<AchievementCategoryRecord> AchievementCategoryStorage;
	public static DB6Storage<AdventureJournalRecord> AdventureJournalStorage;
	public static DB6Storage<AdventureMapPOIRecord> AdventureMapPOIStorage;
	public static DB6Storage<AnimationDataRecord> AnimationDataStorage;
	public static DB6Storage<AnimKitRecord> AnimKitStorage;
	public static DB6Storage<AreaGroupMemberRecord> AreaGroupMemberStorage;
	public static DB6Storage<AreaTableRecord> AreaTableStorage;
	public static DB6Storage<AreaPOIRecord> AreaPOIStorage;
	public static DB6Storage<AreaPOIStateRecord> AreaPOIStateStorage;
	public static DB6Storage<AreaTriggerRecord> AreaTriggerStorage;
	public static DB6Storage<ArmorLocationRecord> ArmorLocationStorage;
	public static DB6Storage<ArtifactRecord> ArtifactStorage;
	public static DB6Storage<ArtifactAppearanceRecord> ArtifactAppearanceStorage;
	public static DB6Storage<ArtifactAppearanceSetRecord> ArtifactAppearanceSetStorage;
	public static DB6Storage<ArtifactCategoryRecord> ArtifactCategoryStorage;
	public static DB6Storage<ArtifactPowerRecord> ArtifactPowerStorage;
	public static DB6Storage<ArtifactPowerLinkRecord> ArtifactPowerLinkStorage;
	public static DB6Storage<ArtifactPowerPickerRecord> ArtifactPowerPickerStorage;
	public static DB6Storage<ArtifactPowerRankRecord> ArtifactPowerRankStorage;
	public static DB6Storage<ArtifactQuestXPRecord> ArtifactQuestXPStorage;
	public static DB6Storage<ArtifactTierRecord> ArtifactTierStorage;
	public static DB6Storage<ArtifactUnlockRecord> ArtifactUnlockStorage;
	public static DB6Storage<AuctionHouseRecord> AuctionHouseStorage;
	public static DB6Storage<AzeriteEmpoweredItemRecord> AzeriteEmpoweredItemStorage;
	public static DB6Storage<AzeriteEssenceRecord> AzeriteEssenceStorage;
	public static DB6Storage<AzeriteEssencePowerRecord> AzeriteEssencePowerStorage;
	public static DB6Storage<AzeriteItemRecord> AzeriteItemStorage;
	public static DB6Storage<AzeriteItemMilestonePowerRecord> AzeriteItemMilestonePowerStorage;
	public static DB6Storage<AzeriteKnowledgeMultiplierRecord> AzeriteKnowledgeMultiplierStorage;
	public static DB6Storage<AzeriteLevelInfoRecord> AzeriteLevelInfoStorage;
	public static DB6Storage<AzeritePowerRecord> AzeritePowerStorage;
	public static DB6Storage<AzeritePowerSetMemberRecord> AzeritePowerSetMemberStorage;
	public static DB6Storage<AzeriteTierUnlockRecord> AzeriteTierUnlockStorage;
	public static DB6Storage<AzeriteTierUnlockSetRecord> AzeriteTierUnlockSetStorage;
	public static DB6Storage<AzeriteUnlockMappingRecord> AzeriteUnlockMappingStorage;
	public static DB6Storage<BankBagSlotPricesRecord> BankBagSlotPricesStorage;
	public static DB6Storage<BannedAddonsRecord> BannedAddOnsStorage;
	public static DB6Storage<BarberShopStyleRecord> BarberShopStyleStorage;
	public static DB6Storage<BattlePetBreedQualityRecord> BattlePetBreedQualityStorage;
	public static DB6Storage<BattlePetBreedStateRecord> BattlePetBreedStateStorage;
	public static DB6Storage<BattlePetSpeciesRecord> BattlePetSpeciesStorage;
	public static DB6Storage<BattlePetSpeciesStateRecord> BattlePetSpeciesStateStorage;
	public static DB6Storage<BattlemasterListRecord> BattlemasterListStorage;
	public static DB6Storage<BroadcastTextRecord> BroadcastTextStorage;
	public static DB6Storage<BroadcastTextDurationRecord> BroadcastTextDurationStorage;
	public static DB6Storage<Cfg_RegionsRecord> CfgRegionsStorage;
	public static DB6Storage<CharTitlesRecord> CharTitlesStorage;
	public static DB6Storage<CharacterLoadoutRecord> CharacterLoadoutStorage;
	public static DB6Storage<CharacterLoadoutItemRecord> CharacterLoadoutItemStorage;
	public static DB6Storage<ChatChannelsRecord> ChatChannelsStorage;
	public static DB6Storage<ChrClassUIDisplayRecord> ChrClassUIDisplayStorage;
	public static DB6Storage<ChrClassesRecord> ChrClassesStorage;
	public static DB6Storage<ChrClassesXPowerTypesRecord> ChrClassesXPowerTypesStorage;
	public static DB6Storage<ChrCustomizationChoiceRecord> ChrCustomizationChoiceStorage;
	public static DB6Storage<ChrCustomizationDisplayInfoRecord> ChrCustomizationDisplayInfoStorage;
	public static DB6Storage<ChrCustomizationElementRecord> ChrCustomizationElementStorage;
	public static DB6Storage<ChrCustomizationReqRecord> ChrCustomizationReqStorage;
	public static DB6Storage<ChrCustomizationReqChoiceRecord> ChrCustomizationReqChoiceStorage;
	public static DB6Storage<ChrModelRecord> ChrModelStorage;
	public static DB6Storage<ChrRaceXChrModelRecord> ChrRaceXChrModelStorage;
	public static DB6Storage<ChrCustomizationOptionRecord> ChrCustomizationOptionStorage;
	public static DB6Storage<ChrRacesRecord> ChrRacesStorage;
	public static DB6Storage<ChrSpecializationRecord> ChrSpecializationStorage;
	public static DB6Storage<CinematicCameraRecord> CinematicCameraStorage;
	public static DB6Storage<CinematicSequencesRecord> CinematicSequencesStorage;
	public static DB6Storage<ContentTuningRecord> ContentTuningStorage;
	public static DB6Storage<ContentTuningXExpectedRecord> ContentTuningXExpectedStorage;
	public static DB6Storage<ConversationLineRecord> ConversationLineStorage;
	public static DB6Storage<CorruptionEffectsRecord> CorruptionEffectsStorage;
	public static DB6Storage<CreatureDisplayInfoRecord> CreatureDisplayInfoStorage;
	public static DB6Storage<CreatureDisplayInfoExtraRecord> CreatureDisplayInfoExtraStorage;
	public static DB6Storage<CreatureFamilyRecord> CreatureFamilyStorage;
	public static DB6Storage<CreatureModelDataRecord> CreatureModelDataStorage;
	public static DB6Storage<CreatureTypeRecord> CreatureTypeStorage;
	public static DB6Storage<CriteriaRecord> CriteriaStorage;
	public static DB6Storage<CriteriaTreeRecord> CriteriaTreeStorage;
	public static DB6Storage<CurrencyContainerRecord> CurrencyContainerStorage;
	public static DB6Storage<CurrencyTypesRecord> CurrencyTypesStorage;
	public static DB6Storage<CurveRecord> CurveStorage;
	public static DB6Storage<CurvePointRecord> CurvePointStorage;
	public static DB6Storage<DestructibleModelDataRecord> DestructibleModelDataStorage;
	public static DB6Storage<DifficultyRecord> DifficultyStorage;
	public static DB6Storage<DungeonEncounterRecord> DungeonEncounterStorage;
	public static DB6Storage<DurabilityCostsRecord> DurabilityCostsStorage;
	public static DB6Storage<DurabilityQualityRecord> DurabilityQualityStorage;
	public static DB6Storage<EmotesRecord> EmotesStorage;
	public static DB6Storage<EmotesTextRecord> EmotesTextStorage;
	public static DB6Storage<EmotesTextSoundRecord> EmotesTextSoundStorage;
	public static DB6Storage<ExpectedStatRecord> ExpectedStatStorage;
	public static DB6Storage<ExpectedStatModRecord> ExpectedStatModStorage;
	public static DB6Storage<FactionRecord> FactionStorage;
	public static DB6Storage<FactionTemplateRecord> FactionTemplateStorage;
	public static DB6Storage<FriendshipRepReactionRecord> FriendshipRepReactionStorage;
	public static DB6Storage<FriendshipReputationRecord> FriendshipReputationStorage;
	public static DB6Storage<GameObjectArtKitRecord> GameObjectArtKitStorage;
	public static DB6Storage<GameObjectDisplayInfoRecord> GameObjectDisplayInfoStorage;
	public static DB6Storage<GameObjectsRecord> GameObjectsStorage;
	public static DB6Storage<GarrAbilityRecord> GarrAbilityStorage;
	public static DB6Storage<GarrBuildingRecord> GarrBuildingStorage;
	public static DB6Storage<GarrBuildingPlotInstRecord> GarrBuildingPlotInstStorage;
	public static DB6Storage<GarrClassSpecRecord> GarrClassSpecStorage;
	public static DB6Storage<GarrFollowerRecord> GarrFollowerStorage;
	public static DB6Storage<GarrFollowerXAbilityRecord> GarrFollowerXAbilityStorage;
	public static DB6Storage<GarrMissionRecord> GarrMissionStorage;
	public static DB6Storage<GarrPlotBuildingRecord> GarrPlotBuildingStorage;
	public static DB6Storage<GarrPlotRecord> GarrPlotStorage;
	public static DB6Storage<GarrPlotInstanceRecord> GarrPlotInstanceStorage;
	public static DB6Storage<GarrSiteLevelRecord> GarrSiteLevelStorage;
	public static DB6Storage<GarrSiteLevelPlotInstRecord> GarrSiteLevelPlotInstStorage;
	public static DB6Storage<GarrTalentTreeRecord> GarrTalentTreeStorage;
	public static DB6Storage<GemPropertiesRecord> GemPropertiesStorage;
	public static DB6Storage<GlobalCurveRecord> GlobalCurveStorage;
	public static DB6Storage<GlyphBindableSpellRecord> GlyphBindableSpellStorage;
	public static DB6Storage<GlyphPropertiesRecord> GlyphPropertiesStorage;
	public static DB6Storage<GlyphRequiredSpecRecord> GlyphRequiredSpecStorage;
	public static DB6Storage<GossipNPCOptionRecord> GossipNPCOptionStorage;
	public static DB6Storage<GuildColorBackgroundRecord> GuildColorBackgroundStorage;
	public static DB6Storage<GuildColorBorderRecord> GuildColorBorderStorage;
	public static DB6Storage<GuildColorEmblemRecord> GuildColorEmblemStorage;
	public static DB6Storage<GuildPerkSpellsRecord> GuildPerkSpellsStorage;
	public static DB6Storage<HeirloomRecord> HeirloomStorage;
	public static DB6Storage<HolidaysRecord> HolidaysStorage;
	public static DB6Storage<ImportPriceArmorRecord> ImportPriceArmorStorage;
	public static DB6Storage<ImportPriceQualityRecord> ImportPriceQualityStorage;
	public static DB6Storage<ImportPriceShieldRecord> ImportPriceShieldStorage;
	public static DB6Storage<ImportPriceWeaponRecord> ImportPriceWeaponStorage;
	public static DB6Storage<ItemAppearanceRecord> ItemAppearanceStorage;
	public static DB6Storage<ItemArmorQualityRecord> ItemArmorQualityStorage;
	public static DB6Storage<ItemArmorShieldRecord> ItemArmorShieldStorage;

	public static DB6Storage<ItemArmorTotalRecord> ItemArmorTotalStorage;

	//public static DB6Storage<ItemBagFamilyRecord> ItemBagFamilyStorage;
	public static DB6Storage<ItemBonusRecord> ItemBonusStorage;
	public static DB6Storage<ItemBonusListLevelDeltaRecord> ItemBonusListLevelDeltaStorage;
	public static DB6Storage<ItemBonusTreeNodeRecord> ItemBonusTreeNodeStorage;
	public static DB6Storage<ItemClassRecord> ItemClassStorage;
	public static DB6Storage<ItemChildEquipmentRecord> ItemChildEquipmentStorage;
	public static DB6Storage<ItemCurrencyCostRecord> ItemCurrencyCostStorage;
	public static DB6Storage<ItemDamageRecord> ItemDamageAmmoStorage;
	public static DB6Storage<ItemDamageRecord> ItemDamageOneHandStorage;
	public static DB6Storage<ItemDamageRecord> ItemDamageOneHandCasterStorage;
	public static DB6Storage<ItemDamageRecord> ItemDamageTwoHandStorage;
	public static DB6Storage<ItemDamageRecord> ItemDamageTwoHandCasterStorage;
	public static DB6Storage<ItemDisenchantLootRecord> ItemDisenchantLootStorage;
	public static DB6Storage<ItemEffectRecord> ItemEffectStorage;
	public static DB6Storage<ItemRecord> ItemStorage;
	public static DB6Storage<ItemExtendedCostRecord> ItemExtendedCostStorage;
	public static DB6Storage<ItemLevelSelectorRecord> ItemLevelSelectorStorage;
	public static DB6Storage<ItemLevelSelectorQualityRecord> ItemLevelSelectorQualityStorage;
	public static DB6Storage<ItemLevelSelectorQualitySetRecord> ItemLevelSelectorQualitySetStorage;
	public static DB6Storage<ItemLimitCategoryRecord> ItemLimitCategoryStorage;
	public static DB6Storage<ItemLimitCategoryConditionRecord> ItemLimitCategoryConditionStorage;
	public static DB6Storage<ItemModifiedAppearanceRecord> ItemModifiedAppearanceStorage;
	public static DB6Storage<ItemModifiedAppearanceExtraRecord> ItemModifiedAppearanceExtraStorage;
	public static DB6Storage<ItemNameDescriptionRecord> ItemNameDescriptionStorage;
	public static DB6Storage<ItemPriceBaseRecord> ItemPriceBaseStorage;
	public static DB6Storage<ItemSearchNameRecord> ItemSearchNameStorage;
	public static DB6Storage<ItemSetRecord> ItemSetStorage;
	public static DB6Storage<ItemSetSpellRecord> ItemSetSpellStorage;
	public static DB6Storage<ItemSparseRecord> ItemSparseStorage;
	public static DB6Storage<ItemSpecRecord> ItemSpecStorage;
	public static DB6Storage<ItemSpecOverrideRecord> ItemSpecOverrideStorage;
	public static DB6Storage<ItemXBonusTreeRecord> ItemXBonusTreeStorage;
	public static DB6Storage<ItemXItemEffectRecord> ItemXItemEffectStorage;
	public static DB6Storage<JournalEncounterRecord> JournalEncounterStorage;
	public static DB6Storage<JournalEncounterSectionRecord> JournalEncounterSectionStorage;
	public static DB6Storage<JournalInstanceRecord> JournalInstanceStorage;

	public static DB6Storage<JournalTierRecord> JournalTierStorage;

	//public static DB6Storage<KeyChainRecord> KeyChainStorage;
	public static DB6Storage<KeystoneAffixRecord> KeystoneAffixStorage;
	public static DB6Storage<LanguageWordsRecord> LanguageWordsStorage;
	public static DB6Storage<LanguagesRecord> LanguagesStorage;
	public static DB6Storage<LFGDungeonsRecord> LFGDungeonsStorage;
	public static DB6Storage<LightRecord> LightStorage;
	public static DB6Storage<LiquidTypeRecord> LiquidTypeStorage;
	public static DB6Storage<LockRecord> LockStorage;
	public static DB6Storage<MailTemplateRecord> MailTemplateStorage;
	public static DB6Storage<MapRecord> MapStorage;
	public static DB6Storage<MapChallengeModeRecord> MapChallengeModeStorage;
	public static DB6Storage<MapDifficultyRecord> MapDifficultyStorage;
	public static DB6Storage<MapDifficultyXConditionRecord> MapDifficultyXConditionStorage;
	public static DB6Storage<MawPowerRecord> MawPowerStorage;
	public static DB6Storage<ModifierTreeRecord> ModifierTreeStorage;
	public static DB6Storage<MountCapabilityRecord> MountCapabilityStorage;
	public static DB6Storage<MountRecord> MountStorage;
	public static DB6Storage<MountTypeXCapabilityRecord> MountTypeXCapabilityStorage;
	public static DB6Storage<MountXDisplayRecord> MountXDisplayStorage;
	public static DB6Storage<MovieRecord> MovieStorage;
	public static DB6Storage<NameGenRecord> NameGenStorage;
	public static DB6Storage<NamesProfanityRecord> NamesProfanityStorage;
	public static DB6Storage<NamesReservedRecord> NamesReservedStorage;
	public static DB6Storage<NamesReservedLocaleRecord> NamesReservedLocaleStorage;
	public static DB6Storage<NumTalentsAtLevelRecord> NumTalentsAtLevelStorage;
	public static DB6Storage<OverrideSpellDataRecord> OverrideSpellDataStorage;
	public static DB6Storage<ParagonReputationRecord> ParagonReputationStorage;
	public static DB6Storage<PhaseRecord> PhaseStorage;
	public static DB6Storage<PhaseXPhaseGroupRecord> PhaseXPhaseGroupStorage;
	public static DB6Storage<PlayerConditionRecord> PlayerConditionStorage;
	public static DB6Storage<PowerDisplayRecord> PowerDisplayStorage;
	public static DB6Storage<PowerTypeRecord> PowerTypeStorage;
	public static DB6Storage<PrestigeLevelInfoRecord> PrestigeLevelInfoStorage;
	public static DB6Storage<PvpDifficultyRecord> PvpDifficultyStorage;
	public static DB6Storage<PvpItemRecord> PvpItemStorage;
	public static DB6Storage<PvpTalentRecord> PvpTalentStorage;
	public static DB6Storage<PvpTalentCategoryRecord> PvpTalentCategoryStorage;
	public static DB6Storage<PvpTalentSlotUnlockRecord> PvpTalentSlotUnlockStorage;
	public static DB6Storage<PvpTierRecord> PvpTierStorage;
	public static DB6Storage<QuestFactionRewardRecord> QuestFactionRewardStorage;
	public static DB6Storage<QuestInfoRecord> QuestInfoStorage;
	public static DB6Storage<QuestLineXQuestRecord> QuestLineXQuestStorage;
	public static DB6Storage<QuestMoneyRewardRecord> QuestMoneyRewardStorage;
	public static DB6Storage<QuestPackageItemRecord> QuestPackageItemStorage;
	public static DB6Storage<QuestSortRecord> QuestSortStorage;
	public static DB6Storage<QuestV2Record> QuestV2Storage;
	public static DB6Storage<QuestXPRecord> QuestXPStorage;
	public static DB6Storage<RandPropPointsRecord> RandPropPointsStorage;
	public static DB6Storage<RewardPackRecord> RewardPackStorage;
	public static DB6Storage<RewardPackXCurrencyTypeRecord> RewardPackXCurrencyTypeStorage;
	public static DB6Storage<RewardPackXItemRecord> RewardPackXItemStorage;
	public static DB6Storage<ScenarioRecord> ScenarioStorage;
	public static DB6Storage<ScenarioStepRecord> ScenarioStepStorage;
	public static DB6Storage<SceneScriptRecord> SceneScriptStorage;
	public static DB6Storage<SceneScriptGlobalTextRecord> SceneScriptGlobalTextStorage;
	public static DB6Storage<SceneScriptPackageRecord> SceneScriptPackageStorage;
	public static DB6Storage<SceneScriptTextRecord> SceneScriptTextStorage;
	public static DB6Storage<SkillLineRecord> SkillLineStorage;
	public static DB6Storage<SkillLineAbilityRecord> SkillLineAbilityStorage;
	public static DB6Storage<SkillLineXTraitTreeRecord> SkillLineXTraitTreeStorage;
	public static DB6Storage<SkillRaceClassInfoRecord> SkillRaceClassInfoStorage;
	public static DB6Storage<SoulbindConduitRankRecord> SoulbindConduitRankStorage;
	public static DB6Storage<SoundKitRecord> SoundKitStorage;
	public static DB6Storage<SpecializationSpellsRecord> SpecializationSpellsStorage;
	public static DB6Storage<SpecSetMemberRecord> SpecSetMemberStorage;
	public static DB6Storage<SpellRecord> SpellStorage;
	public static DB6Storage<SpellAuraOptionsRecord> SpellAuraOptionsStorage;
	public static DB6Storage<SpellAuraRestrictionsRecord> SpellAuraRestrictionsStorage;
	public static DB6Storage<SpellCastTimesRecord> SpellCastTimesStorage;
	public static DB6Storage<SpellCastingRequirementsRecord> SpellCastingRequirementsStorage;
	public static DB6Storage<SpellCategoriesRecord> SpellCategoriesStorage;
	public static DB6Storage<SpellCategoryRecord> SpellCategoryStorage;
	public static DB6Storage<SpellClassOptionsRecord> SpellClassOptionsStorage;
	public static DB6Storage<SpellCooldownsRecord> SpellCooldownsStorage;
	public static DB6Storage<SpellDurationRecord> SpellDurationStorage;
	public static DB6Storage<SpellEffectRecord> SpellEffectStorage;
	public static DB6Storage<SpellEmpowerRecord> SpellEmpowerStorage;
	public static DB6Storage<SpellEmpowerStageRecord> SpellEmpowerStageStorage;
	public static DB6Storage<SpellEquippedItemsRecord> SpellEquippedItemsStorage;
	public static DB6Storage<SpellFocusObjectRecord> SpellFocusObjectStorage;
	public static DB6Storage<SpellInterruptsRecord> SpellInterruptsStorage;
	public static DB6Storage<SpellItemEnchantmentRecord> SpellItemEnchantmentStorage;
	public static DB6Storage<SpellItemEnchantmentConditionRecord> SpellItemEnchantmentConditionStorage;
	public static DB6Storage<SpellKeyboundOverrideRecord> SpellKeyboundOverrideStorage;
	public static DB6Storage<SpellLabelRecord> SpellLabelStorage;
	public static DB6Storage<SpellLearnSpellRecord> SpellLearnSpellStorage;
	public static DB6Storage<SpellLevelsRecord> SpellLevelsStorage;
	public static DB6Storage<SpellMiscRecord> SpellMiscStorage;
	public static DB6Storage<SpellNameRecord> SpellNameStorage;
	public static DB6Storage<SpellPowerRecord> SpellPowerStorage;
	public static DB6Storage<SpellPowerDifficultyRecord> SpellPowerDifficultyStorage;
	public static DB6Storage<SpellProcsPerMinuteRecord> SpellProcsPerMinuteStorage;
	public static DB6Storage<SpellProcsPerMinuteModRecord> SpellProcsPerMinuteModStorage;
	public static DB6Storage<SpellRadiusRecord> SpellRadiusStorage;
	public static DB6Storage<SpellRangeRecord> SpellRangeStorage;
	public static DB6Storage<SpellReagentsRecord> SpellReagentsStorage;
	public static DB6Storage<SpellReagentsCurrencyRecord> SpellReagentsCurrencyStorage;
	public static DB6Storage<SpellReplacementRecord> SpellReplacementStorage;
	public static DB6Storage<SpellScalingRecord> SpellScalingStorage;
	public static DB6Storage<SpellShapeshiftRecord> SpellShapeshiftStorage;
	public static DB6Storage<SpellShapeshiftFormRecord> SpellShapeshiftFormStorage;
	public static DB6Storage<SpellTargetRestrictionsRecord> SpellTargetRestrictionsStorage;
	public static DB6Storage<SpellTotemsRecord> SpellTotemsStorage;
	public static DB6Storage<SpellVisualRecord> SpellVisualStorage;
	public static DB6Storage<SpellVisualEffectNameRecord> SpellVisualEffectNameStorage;
	public static DB6Storage<SpellVisualMissileRecord> SpellVisualMissileStorage;
	public static DB6Storage<SpellVisualKitRecord> SpellVisualKitStorage;
	public static DB6Storage<SpellXSpellVisualRecord> SpellXSpellVisualStorage;
	public static DB6Storage<SummonPropertiesRecord> SummonPropertiesStorage;
	public static DB6Storage<TactKeyRecord> TactKeyStorage;
	public static DB6Storage<TalentRecord> TalentStorage;
	public static DB6Storage<TaxiNodesRecord> TaxiNodesStorage;
	public static DB6Storage<TaxiPathRecord> TaxiPathStorage;
	public static DB6Storage<TaxiPathNodeRecord> TaxiPathNodeStorage;
	public static DB6Storage<TotemCategoryRecord> TotemCategoryStorage;
	public static DB6Storage<ToyRecord> ToyStorage;
	public static DB6Storage<TraitCondRecord> TraitCondStorage;
	public static DB6Storage<TraitCostRecord> TraitCostStorage;
	public static DB6Storage<TraitCurrencyRecord> TraitCurrencyStorage;
	public static DB6Storage<TraitCurrencySourceRecord> TraitCurrencySourceStorage;
	public static DB6Storage<TraitDefinitionRecord> TraitDefinitionStorage;
	public static DB6Storage<TraitDefinitionEffectPointsRecord> TraitDefinitionEffectPointsStorage;
	public static DB6Storage<TraitEdgeRecord> TraitEdgeStorage;
	public static DB6Storage<TraitNodeRecord> TraitNodeStorage;
	public static DB6Storage<TraitNodeEntryRecord> TraitNodeEntryStorage;
	public static DB6Storage<TraitNodeEntryXTraitCondRecord> TraitNodeEntryXTraitCondStorage;
	public static DB6Storage<TraitNodeEntryXTraitCostRecord> TraitNodeEntryXTraitCostStorage;
	public static DB6Storage<TraitNodeGroupRecord> TraitNodeGroupStorage;
	public static DB6Storage<TraitNodeGroupXTraitCondRecord> TraitNodeGroupXTraitCondStorage;
	public static DB6Storage<TraitNodeGroupXTraitCostRecord> TraitNodeGroupXTraitCostStorage;
	public static DB6Storage<TraitNodeGroupXTraitNodeRecord> TraitNodeGroupXTraitNodeStorage;
	public static DB6Storage<TraitNodeXTraitCondRecord> TraitNodeXTraitCondStorage;
	public static DB6Storage<TraitNodeXTraitCostRecord> TraitNodeXTraitCostStorage;
	public static DB6Storage<TraitNodeXTraitNodeEntryRecord> TraitNodeXTraitNodeEntryStorage;
	public static DB6Storage<TraitSystemRecord> TraitSystemStorage;
	public static DB6Storage<TraitTreeRecord> TraitTreeStorage;
	public static DB6Storage<TraitTreeLoadoutRecord> TraitTreeLoadoutStorage;
	public static DB6Storage<TraitTreeLoadoutEntryRecord> TraitTreeLoadoutEntryStorage;
	public static DB6Storage<TraitTreeXTraitCostRecord> TraitTreeXTraitCostStorage;
	public static DB6Storage<TraitTreeXTraitCurrencyRecord> TraitTreeXTraitCurrencyStorage;
	public static DB6Storage<TransmogHolidayRecord> TransmogHolidayStorage;
	public static DB6Storage<TransmogIllusionRecord> TransmogIllusionStorage;
	public static DB6Storage<TransmogSetRecord> TransmogSetStorage;
	public static DB6Storage<TransmogSetGroupRecord> TransmogSetGroupStorage;
	public static DB6Storage<TransmogSetItemRecord> TransmogSetItemStorage;
	public static DB6Storage<TransportAnimationRecord> TransportAnimationStorage;
	public static DB6Storage<TransportRotationRecord> TransportRotationStorage;
	public static DB6Storage<UiMapRecord> UiMapStorage;
	public static DB6Storage<UiMapAssignmentRecord> UiMapAssignmentStorage;
	public static DB6Storage<UiMapLinkRecord> UiMapLinkStorage;
	public static DB6Storage<UiMapXMapArtRecord> UiMapXMapArtStorage;
	public static DB6Storage<UISplashScreenRecord> UISplashScreenStorage;
	public static DB6Storage<UnitConditionRecord> UnitConditionStorage;
	public static DB6Storage<UnitPowerBarRecord> UnitPowerBarStorage;
	public static DB6Storage<VehicleRecord> VehicleStorage;
	public static DB6Storage<VehicleSeatRecord> VehicleSeatStorage;
	public static DB6Storage<WMOAreaTableRecord> WMOAreaTableStorage;
	public static DB6Storage<WorldEffectRecord> WorldEffectStorage;
	public static DB6Storage<WorldMapOverlayRecord> WorldMapOverlayStorage;
	public static DB6Storage<WorldStateExpressionRecord> WorldStateExpressionStorage;
	public static DB6Storage<CharBaseInfo> CharBaseInfoStorage;

	#endregion

	#region GameTables

	public static GameTable<GtArtifactKnowledgeMultiplierRecord> ArtifactKnowledgeMultiplierGameTable;
	public static GameTable<GtArtifactLevelXPRecord> ArtifactLevelXPGameTable;
	public static GameTable<GtBarberShopCostBaseRecord> BarberShopCostBaseGameTable;
	public static GameTable<GtBaseMPRecord> BaseMPGameTable;
	public static GameTable<GtBattlePetXPRecord> BattlePetXPGameTable;
	public static GameTable<GtCombatRatingsRecord> CombatRatingsGameTable;
	public static GameTable<GtGenericMultByILvlRecord> CombatRatingsMultByILvlGameTable;
	public static GameTable<GtHpPerStaRecord> HpPerStaGameTable;
	public static GameTable<GtItemSocketCostPerLevelRecord> ItemSocketCostPerLevelGameTable;
	public static GameTable<GtNpcManaCostScalerRecord> NpcManaCostScalerGameTable;
	public static GameTable<GtSpellScalingRecord> SpellScalingGameTable;
	public static GameTable<GtGenericMultByILvlRecord> StaminaMultByILvlGameTable;
	public static GameTable<GtXpRecord> XpGameTable;

	#endregion

	#region Taxi Collections

	public static byte[] TaxiNodesMask;
	public static byte[] OldContinentsNodesMask;
	public static byte[] HordeTaxiNodesMask;
	public static byte[] AllianceTaxiNodesMask;
	public static Dictionary<uint, Dictionary<uint, TaxiPathBySourceAndDestination>> TaxiPathSetBySource = new();
	public static Dictionary<uint, TaxiPathNodeRecord[]> TaxiPathNodesByPath = new();

	#endregion

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