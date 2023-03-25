// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks.Dataflow;
using Autofac;
using Forged.MapServer.DataStorage.ClientReader;
using Forged.MapServer.DataStorage.Structs.A;
using Forged.MapServer.DataStorage.Structs.B;
using Forged.MapServer.DataStorage.Structs.C;
using Forged.MapServer.DataStorage.Structs.D;
using Forged.MapServer.DataStorage.Structs.E;
using Forged.MapServer.DataStorage.Structs.F;
using Forged.MapServer.DataStorage.Structs.G;
using Forged.MapServer.DataStorage.Structs.GameTable;
using Forged.MapServer.DataStorage.Structs.H;
using Forged.MapServer.DataStorage.Structs.I;
using Forged.MapServer.DataStorage.Structs.J;
using Forged.MapServer.DataStorage.Structs.K;
using Forged.MapServer.DataStorage.Structs.L;
using Forged.MapServer.DataStorage.Structs.M;
using Forged.MapServer.DataStorage.Structs.MetaStructs;
using Forged.MapServer.DataStorage.Structs.N;
using Forged.MapServer.DataStorage.Structs.O;
using Forged.MapServer.DataStorage.Structs.P;
using Forged.MapServer.DataStorage.Structs.Q;
using Forged.MapServer.DataStorage.Structs.R;
using Forged.MapServer.DataStorage.Structs.S;
using Forged.MapServer.DataStorage.Structs.T;
using Forged.MapServer.DataStorage.Structs.U;
using Forged.MapServer.DataStorage.Structs.V;
using Forged.MapServer.DataStorage.Structs.W;
using Framework.Constants;
using Framework.Database;
using Serilog;

namespace Forged.MapServer.DataStorage;

public class CliDB
{
	private readonly HotfixDatabase _hotfixDatabase;
	private readonly DB2Manager _db2Manager;

	public CliDB(HotfixDatabase hotfixDatabase, DB2Manager db2Manager)
	{
		_hotfixDatabase = hotfixDatabase;
		_db2Manager = db2Manager;
	}

	public BitSet LoadStores(string dataPath, Locale defaultLocale, ContainerBuilder builder)
	{
		ActionBlock<Action> _taskManager = new(a => a(),
												new ExecutionDataflowBlockOptions()
												{
													MaxDegreeOfParallelism = 20
												});

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
			DB6Storage<T> storage = new(_hotfixDatabase, defaultLocale);
			storage.LoadData($"{db2Path}/{defaultLocale}/{fileName}", fileName);
			storage.LoadHotfixData(availableDb2Locales, preparedStatement, preparedStatementLocale);

			_db2Manager.AddDB2(storage.GetTableHash(), storage);
			loadedFileCount++;

			return storage;
		}

		_taskManager.Post(() => builder.Register((c, p) => AchievementStorage = ReadDB2<AchievementRecord>("Achievement.db2", HotfixStatements.SEL_ACHIEVEMENT, HotfixStatements.SEL_ACHIEVEMENT_LOCALE)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => AchievementCategoryStorage = ReadDB2<AchievementCategoryRecord>("Achievement_Category.db2", HotfixStatements.SEL_ACHIEVEMENT_CATEGORY, HotfixStatements.SEL_ACHIEVEMENT_CATEGORY_LOCALE)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => AdventureJournalStorage = ReadDB2<AdventureJournalRecord>("AdventureJournal.db2", HotfixStatements.SEL_ADVENTURE_JOURNAL, HotfixStatements.SEL_ADVENTURE_JOURNAL_LOCALE)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => AdventureMapPOIStorage = ReadDB2<AdventureMapPOIRecord>("AdventureMapPOI.db2", HotfixStatements.SEL_ADVENTURE_MAP_POI, HotfixStatements.SEL_ADVENTURE_MAP_POI_LOCALE)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => AnimationDataStorage = ReadDB2<AnimationDataRecord>("AnimationData.db2", HotfixStatements.SEL_ANIMATION_DATA)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => AnimKitStorage = ReadDB2<AnimKitRecord>("AnimKit.db2", HotfixStatements.SEL_ANIM_KIT)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => AreaGroupMemberStorage = ReadDB2<AreaGroupMemberRecord>("AreaGroupMember.db2", HotfixStatements.SEL_AREA_GROUP_MEMBER)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => AreaTableStorage = ReadDB2<AreaTableRecord>("AreaTable.db2", HotfixStatements.SEL_AREA_TABLE, HotfixStatements.SEL_AREA_TABLE_LOCALE)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => AreaPOIStorage = ReadDB2<AreaPOIRecord>("AreaPOI.db2", HotfixStatements.SEL_AREA_POI, HotfixStatements.SEL_AREA_POI_LOCALE)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => AreaPOIStateStorage = ReadDB2<AreaPOIStateRecord>("AreaPOIState.db2", HotfixStatements.SEL_AREA_POI_STATE, HotfixStatements.SEL_AREA_POI_STATE_LOCALE)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => AreaTriggerStorage = ReadDB2<AreaTriggerRecord>("AreaTrigger.db2", HotfixStatements.SEL_AREA_TRIGGER)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => ArmorLocationStorage = ReadDB2<ArmorLocationRecord>("ArmorLocation.db2", HotfixStatements.SEL_ARMOR_LOCATION)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => ArtifactStorage = ReadDB2<ArtifactRecord>("Artifact.db2", HotfixStatements.SEL_ARTIFACT, HotfixStatements.SEL_ARTIFACT_APPEARANCE_LOCALE)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => ArtifactAppearanceStorage = ReadDB2<ArtifactAppearanceRecord>("ArtifactAppearance.db2", HotfixStatements.SEL_ARTIFACT_APPEARANCE, HotfixStatements.SEL_ARTIFACT_APPEARANCE_LOCALE)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => ArtifactAppearanceSetStorage = ReadDB2<ArtifactAppearanceSetRecord>("ArtifactAppearanceSet.db2", HotfixStatements.SEL_ARTIFACT_APPEARANCE_SET, HotfixStatements.SEL_ARTIFACT_APPEARANCE_SET_LOCALE)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => ArtifactCategoryStorage = ReadDB2<ArtifactCategoryRecord>("ArtifactCategory.db2", HotfixStatements.SEL_ARTIFACT_CATEGORY)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => ArtifactPowerStorage = ReadDB2<ArtifactPowerRecord>("ArtifactPower.db2", HotfixStatements.SEL_ARTIFACT_POWER)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => ArtifactPowerLinkStorage = ReadDB2<ArtifactPowerLinkRecord>("ArtifactPowerLink.db2", HotfixStatements.SEL_ARTIFACT_POWER_LINK)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => ArtifactPowerPickerStorage = ReadDB2<ArtifactPowerPickerRecord>("ArtifactPowerPicker.db2", HotfixStatements.SEL_ARTIFACT_POWER_PICKER)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => ArtifactPowerRankStorage = ReadDB2<ArtifactPowerRankRecord>("ArtifactPowerRank.db2", HotfixStatements.SEL_ARTIFACT_POWER_RANK)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => ArtifactQuestXPStorage = ReadDB2<ArtifactQuestXPRecord>("ArtifactQuestXP.db2", HotfixStatements.SEL_ARTIFACT_QUEST_XP)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => ArtifactTierStorage = ReadDB2<ArtifactTierRecord>("ArtifactTier.db2", HotfixStatements.SEL_ARTIFACT_TIER)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => ArtifactUnlockStorage = ReadDB2<ArtifactUnlockRecord>("ArtifactUnlock.db2", HotfixStatements.SEL_ARTIFACT_UNLOCK)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => AuctionHouseStorage = ReadDB2<AuctionHouseRecord>("AuctionHouse.db2", HotfixStatements.SEL_AUCTION_HOUSE, HotfixStatements.SEL_AUCTION_HOUSE_LOCALE)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => AzeriteEmpoweredItemStorage = ReadDB2<AzeriteEmpoweredItemRecord>("AzeriteEmpoweredItem.db2", HotfixStatements.SEL_AZERITE_EMPOWERED_ITEM)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => AzeriteEssenceStorage = ReadDB2<AzeriteEssenceRecord>("AzeriteEssence.db2", HotfixStatements.SEL_AZERITE_ESSENCE, HotfixStatements.SEL_AZERITE_ESSENCE_LOCALE)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => AzeriteEssencePowerStorage = ReadDB2<AzeriteEssencePowerRecord>("AzeriteEssencePower.db2", HotfixStatements.SEL_AZERITE_ESSENCE_POWER, HotfixStatements.SEL_AZERITE_ESSENCE_POWER_LOCALE)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => AzeriteItemStorage = ReadDB2<AzeriteItemRecord>("AzeriteItem.db2", HotfixStatements.SEL_AZERITE_ITEM)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => AzeriteItemMilestonePowerStorage = ReadDB2<AzeriteItemMilestonePowerRecord>("AzeriteItemMilestonePower.db2", HotfixStatements.SEL_AZERITE_ITEM_MILESTONE_POWER)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => AzeriteKnowledgeMultiplierStorage = ReadDB2<AzeriteKnowledgeMultiplierRecord>("AzeriteKnowledgeMultiplier.db2", HotfixStatements.SEL_AZERITE_KNOWLEDGE_MULTIPLIER)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => AzeriteLevelInfoStorage = ReadDB2<AzeriteLevelInfoRecord>("AzeriteLevelInfo.db2", HotfixStatements.SEL_AZERITE_LEVEL_INFO)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => AzeritePowerStorage = ReadDB2<AzeritePowerRecord>("AzeritePower.db2", HotfixStatements.SEL_AZERITE_POWER)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => AzeritePowerSetMemberStorage = ReadDB2<AzeritePowerSetMemberRecord>("AzeritePowerSetMember.db2", HotfixStatements.SEL_AZERITE_POWER_SET_MEMBER)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => AzeriteTierUnlockStorage = ReadDB2<AzeriteTierUnlockRecord>("AzeriteTierUnlock.db2", HotfixStatements.SEL_AZERITE_TIER_UNLOCK)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => AzeriteTierUnlockSetStorage = ReadDB2<AzeriteTierUnlockSetRecord>("AzeriteTierUnlockSet.db2", HotfixStatements.SEL_AZERITE_TIER_UNLOCK_SET)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => AzeriteUnlockMappingStorage = ReadDB2<AzeriteUnlockMappingRecord>("AzeriteUnlockMapping.db2", HotfixStatements.SEL_AZERITE_UNLOCK_MAPPING)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => BankBagSlotPricesStorage = ReadDB2<BankBagSlotPricesRecord>("BankBagSlotPrices.db2", HotfixStatements.SEL_BANK_BAG_SLOT_PRICES)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => BannedAddOnsStorage = ReadDB2<BannedAddonsRecord>("BannedAddons.db2", HotfixStatements.SEL_BANNED_ADDONS)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => BarberShopStyleStorage = ReadDB2<BarberShopStyleRecord>("BarberShopStyle.db2", HotfixStatements.SEL_BARBER_SHOP_STYLE, HotfixStatements.SEL_BARBER_SHOP_STYLE_LOCALE)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => BattlePetBreedQualityStorage = ReadDB2<BattlePetBreedQualityRecord>("BattlePetBreedQuality.db2", HotfixStatements.SEL_BATTLE_PET_BREED_QUALITY)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => BattlePetBreedStateStorage = ReadDB2<BattlePetBreedStateRecord>("BattlePetBreedState.db2", HotfixStatements.SEL_BATTLE_PET_BREED_STATE)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => BattlePetSpeciesStorage = ReadDB2<BattlePetSpeciesRecord>("BattlePetSpecies.db2", HotfixStatements.SEL_BATTLE_PET_SPECIES, HotfixStatements.SEL_BATTLE_PET_SPECIES_LOCALE)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => BattlePetSpeciesStateStorage = ReadDB2<BattlePetSpeciesStateRecord>("BattlePetSpeciesState.db2", HotfixStatements.SEL_BATTLE_PET_SPECIES_STATE)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => BattlemasterListStorage = ReadDB2<BattlemasterListRecord>("BattlemasterList.db2", HotfixStatements.SEL_BATTLEMASTER_LIST, HotfixStatements.SEL_BATTLEMASTER_LIST_LOCALE)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => BroadcastTextStorage = ReadDB2<BroadcastTextRecord>("BroadcastText.db2", HotfixStatements.SEL_BROADCAST_TEXT, HotfixStatements.SEL_BROADCAST_TEXT_LOCALE)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => BroadcastTextDurationStorage = ReadDB2<BroadcastTextDurationRecord>("BroadcastTextDuration.db2", HotfixStatements.SEL_BROADCAST_TEXT_DURATION)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => CfgRegionsStorage = ReadDB2<Cfg_RegionsRecord>("Cfg_Regions.db2", HotfixStatements.SEL_CFG_REGIONS)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => CharTitlesStorage = ReadDB2<CharTitlesRecord>("CharTitles.db2", HotfixStatements.SEL_CHAR_TITLES, HotfixStatements.SEL_CHAR_TITLES_LOCALE)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => CharacterLoadoutStorage = ReadDB2<CharacterLoadoutRecord>("CharacterLoadout.db2", HotfixStatements.SEL_CHARACTER_LOADOUT)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => CharacterLoadoutItemStorage = ReadDB2<CharacterLoadoutItemRecord>("CharacterLoadoutItem.db2", HotfixStatements.SEL_CHARACTER_LOADOUT_ITEM)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => ChatChannelsStorage = ReadDB2<ChatChannelsRecord>("ChatChannels.db2", HotfixStatements.SEL_CHAT_CHANNELS, HotfixStatements.SEL_CHAT_CHANNELS_LOCALE)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => ChrClassUIDisplayStorage = ReadDB2<ChrClassUIDisplayRecord>("ChrClassUIDisplay.db2", HotfixStatements.SEL_CHR_CLASS_UI_DISPLAY)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => ChrClassesStorage = ReadDB2<ChrClassesRecord>("ChrClasses.db2", HotfixStatements.SEL_CHR_CLASSES, HotfixStatements.SEL_CHR_CLASSES_LOCALE)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => ChrClassesXPowerTypesStorage = ReadDB2<ChrClassesXPowerTypesRecord>("ChrClassesXPowerTypes.db2", HotfixStatements.SEL_CHR_CLASSES_X_POWER_TYPES)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => ChrCustomizationChoiceStorage = ReadDB2<ChrCustomizationChoiceRecord>("ChrCustomizationChoice.db2", HotfixStatements.SEL_CHR_CUSTOMIZATION_CHOICE, HotfixStatements.SEL_CHR_CUSTOMIZATION_CHOICE_LOCALE)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => ChrCustomizationDisplayInfoStorage = ReadDB2<ChrCustomizationDisplayInfoRecord>("ChrCustomizationDisplayInfo.db2", HotfixStatements.SEL_CHR_CUSTOMIZATION_DISPLAY_INFO)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => ChrCustomizationElementStorage = ReadDB2<ChrCustomizationElementRecord>("ChrCustomizationElement.db2", HotfixStatements.SEL_CHR_CUSTOMIZATION_ELEMENT)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => ChrCustomizationOptionStorage = ReadDB2<ChrCustomizationOptionRecord>("ChrCustomizationOption.db2", HotfixStatements.SEL_CHR_CUSTOMIZATION_OPTION, HotfixStatements.SEL_CHR_CUSTOMIZATION_OPTION_LOCALE)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => ChrCustomizationReqStorage = ReadDB2<ChrCustomizationReqRecord>("ChrCustomizationReq.db2", HotfixStatements.SEL_CHR_CUSTOMIZATION_REQ)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => ChrCustomizationReqChoiceStorage = ReadDB2<ChrCustomizationReqChoiceRecord>("ChrCustomizationReqChoice.db2", HotfixStatements.SEL_CHR_CUSTOMIZATION_REQ_CHOICE)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => ChrModelStorage = ReadDB2<ChrModelRecord>("ChrModel.db2", HotfixStatements.SEL_CHR_MODEL)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => ChrRaceXChrModelStorage = ReadDB2<ChrRaceXChrModelRecord>("ChrRaceXChrModel.db2", HotfixStatements.SEL_CHR_RACE_X_CHR_MODEL)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => ChrRacesStorage = ReadDB2<ChrRacesRecord>("ChrRaces.db2", HotfixStatements.SEL_CHR_RACES, HotfixStatements.SEL_CHR_RACES_LOCALE)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => ChrSpecializationStorage = ReadDB2<ChrSpecializationRecord>("ChrSpecialization.db2", HotfixStatements.SEL_CHR_SPECIALIZATION, HotfixStatements.SEL_CHR_SPECIALIZATION_LOCALE)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => CinematicCameraStorage = ReadDB2<CinematicCameraRecord>("CinematicCamera.db2", HotfixStatements.SEL_CINEMATIC_CAMERA)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => CinematicSequencesStorage = ReadDB2<CinematicSequencesRecord>("CinematicSequences.db2", HotfixStatements.SEL_CINEMATIC_SEQUENCES)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => ContentTuningStorage = ReadDB2<ContentTuningRecord>("ContentTuning.db2", HotfixStatements.SEL_CONTENT_TUNING)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => ContentTuningXExpectedStorage = ReadDB2<ContentTuningXExpectedRecord>("ContentTuningXExpected.db2", HotfixStatements.SEL_CONTENT_TUNING_X_EXPECTED)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => ConversationLineStorage = ReadDB2<ConversationLineRecord>("ConversationLine.db2", HotfixStatements.SEL_CONVERSATION_LINE)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => CorruptionEffectsStorage = ReadDB2<CorruptionEffectsRecord>("CorruptionEffects.db2", HotfixStatements.SEL_CORRUPTION_EFFECTS)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => CreatureDisplayInfoStorage = ReadDB2<CreatureDisplayInfoRecord>("CreatureDisplayInfo.db2", HotfixStatements.SEL_CREATURE_DISPLAY_INFO)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => CreatureDisplayInfoExtraStorage = ReadDB2<CreatureDisplayInfoExtraRecord>("CreatureDisplayInfoExtra.db2", HotfixStatements.SEL_CREATURE_DISPLAY_INFO_EXTRA)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => CreatureFamilyStorage = ReadDB2<CreatureFamilyRecord>("CreatureFamily.db2", HotfixStatements.SEL_CREATURE_FAMILY, HotfixStatements.SEL_CREATURE_FAMILY_LOCALE)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => CreatureModelDataStorage = ReadDB2<CreatureModelDataRecord>("CreatureModelData.db2", HotfixStatements.SEL_CREATURE_MODEL_DATA)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => CreatureTypeStorage = ReadDB2<CreatureTypeRecord>("CreatureType.db2", HotfixStatements.SEL_CREATURE_TYPE, HotfixStatements.SEL_CREATURE_TYPE_LOCALE)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => CriteriaStorage = ReadDB2<CriteriaRecord>("Criteria.db2", HotfixStatements.SEL_CRITERIA)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => CriteriaTreeStorage = ReadDB2<CriteriaTreeRecord>("CriteriaTree.db2", HotfixStatements.SEL_CRITERIA_TREE, HotfixStatements.SEL_CRITERIA_TREE_LOCALE)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => CurrencyContainerStorage = ReadDB2<CurrencyContainerRecord>("CurrencyContainer.db2", HotfixStatements.SEL_CURRENCY_CONTAINER, HotfixStatements.SEL_CURRENCY_CONTAINER_LOCALE)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => CurrencyTypesStorage = ReadDB2<CurrencyTypesRecord>("CurrencyTypes.db2", HotfixStatements.SEL_CURRENCY_TYPES, HotfixStatements.SEL_CURRENCY_TYPES_LOCALE)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => CurveStorage = ReadDB2<CurveRecord>("Curve.db2", HotfixStatements.SEL_CURVE)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => CurvePointStorage = ReadDB2<CurvePointRecord>("CurvePoint.db2", HotfixStatements.SEL_CURVE_POINT)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => DestructibleModelDataStorage = ReadDB2<DestructibleModelDataRecord>("DestructibleModelData.db2", HotfixStatements.SEL_DESTRUCTIBLE_MODEL_DATA)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => DifficultyStorage = ReadDB2<DifficultyRecord>("Difficulty.db2", HotfixStatements.SEL_DIFFICULTY, HotfixStatements.SEL_DIFFICULTY_LOCALE)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => DungeonEncounterStorage = ReadDB2<DungeonEncounterRecord>("DungeonEncounter.db2", HotfixStatements.SEL_DUNGEON_ENCOUNTER, HotfixStatements.SEL_DUNGEON_ENCOUNTER_LOCALE)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => DurabilityCostsStorage = ReadDB2<DurabilityCostsRecord>("DurabilityCosts.db2", HotfixStatements.SEL_DURABILITY_COSTS)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => DurabilityQualityStorage = ReadDB2<DurabilityQualityRecord>("DurabilityQuality.db2", HotfixStatements.SEL_DURABILITY_QUALITY)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => EmotesStorage = ReadDB2<EmotesRecord>("Emotes.db2", HotfixStatements.SEL_EMOTES)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => EmotesTextStorage = ReadDB2<EmotesTextRecord>("EmotesText.db2", HotfixStatements.SEL_EMOTES_TEXT)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => EmotesTextSoundStorage = ReadDB2<EmotesTextSoundRecord>("EmotesTextSound.db2", HotfixStatements.SEL_EMOTES_TEXT_SOUND)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => ExpectedStatStorage = ReadDB2<ExpectedStatRecord>("ExpectedStat.db2", HotfixStatements.SEL_EXPECTED_STAT)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => ExpectedStatModStorage = ReadDB2<ExpectedStatModRecord>("ExpectedStatMod.db2", HotfixStatements.SEL_EXPECTED_STAT_MOD)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => FactionStorage = ReadDB2<FactionRecord>("Faction.db2", HotfixStatements.SEL_FACTION, HotfixStatements.SEL_FACTION_LOCALE)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => FactionTemplateStorage = ReadDB2<FactionTemplateRecord>("FactionTemplate.db2", HotfixStatements.SEL_FACTION_TEMPLATE)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => FriendshipRepReactionStorage = ReadDB2<FriendshipRepReactionRecord>("FriendshipRepReaction.db2", HotfixStatements.SEL_FRIENDSHIP_REP_REACTION, HotfixStatements.SEL_FRIENDSHIP_REP_REACTION_LOCALE)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => FriendshipReputationStorage = ReadDB2<FriendshipReputationRecord>("FriendshipReputation.db2", HotfixStatements.SEL_FRIENDSHIP_REPUTATION, HotfixStatements.SEL_FRIENDSHIP_REPUTATION_LOCALE)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => GameObjectArtKitStorage = ReadDB2<GameObjectArtKitRecord>("GameObjectArtKit.db2", HotfixStatements.SEL_GAMEOBJECT_ART_KIT)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => GameObjectDisplayInfoStorage = ReadDB2<GameObjectDisplayInfoRecord>("GameObjectDisplayInfo.db2", HotfixStatements.SEL_GAMEOBJECT_DISPLAY_INFO)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => GameObjectsStorage = ReadDB2<GameObjectsRecord>("GameObjects.db2", HotfixStatements.SEL_GAMEOBJECTS, HotfixStatements.SEL_GAMEOBJECTS_LOCALE)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => GarrAbilityStorage = ReadDB2<GarrAbilityRecord>("GarrAbility.db2", HotfixStatements.SEL_GARR_ABILITY, HotfixStatements.SEL_GARR_ABILITY_LOCALE)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => GarrBuildingStorage = ReadDB2<GarrBuildingRecord>("GarrBuilding.db2", HotfixStatements.SEL_GARR_BUILDING, HotfixStatements.SEL_GARR_BUILDING_LOCALE)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => GarrBuildingPlotInstStorage = ReadDB2<GarrBuildingPlotInstRecord>("GarrBuildingPlotInst.db2", HotfixStatements.SEL_GARR_BUILDING_PLOT_INST)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => GarrClassSpecStorage = ReadDB2<GarrClassSpecRecord>("GarrClassSpec.db2", HotfixStatements.SEL_GARR_CLASS_SPEC, HotfixStatements.SEL_GARR_CLASS_SPEC_LOCALE)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => GarrFollowerStorage = ReadDB2<GarrFollowerRecord>("GarrFollower.db2", HotfixStatements.SEL_GARR_FOLLOWER, HotfixStatements.SEL_GARR_FOLLOWER_LOCALE)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => GarrFollowerXAbilityStorage = ReadDB2<GarrFollowerXAbilityRecord>("GarrFollowerXAbility.db2", HotfixStatements.SEL_GARR_FOLLOWER_X_ABILITY)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => GarrMissionStorage = ReadDB2<GarrMissionRecord>("GarrMission.db2", HotfixStatements.SEL_GARR_MISSION, HotfixStatements.SEL_GARR_MISSION_LOCALE)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => GarrPlotStorage = ReadDB2<GarrPlotRecord>("GarrPlot.db2", HotfixStatements.SEL_GARR_PLOT)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => GarrPlotBuildingStorage = ReadDB2<GarrPlotBuildingRecord>("GarrPlotBuilding.db2", HotfixStatements.SEL_GARR_PLOT_BUILDING)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => GarrPlotInstanceStorage = ReadDB2<GarrPlotInstanceRecord>("GarrPlotInstance.db2", HotfixStatements.SEL_GARR_PLOT_INSTANCE)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => GarrSiteLevelStorage = ReadDB2<GarrSiteLevelRecord>("GarrSiteLevel.db2", HotfixStatements.SEL_GARR_SITE_LEVEL)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => GarrSiteLevelPlotInstStorage = ReadDB2<GarrSiteLevelPlotInstRecord>("GarrSiteLevelPlotInst.db2", HotfixStatements.SEL_GARR_SITE_LEVEL_PLOT_INST)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => GarrTalentTreeStorage = ReadDB2<GarrTalentTreeRecord>("GarrTalentTree.db2", HotfixStatements.SEL_GARR_TALENT_TREE, HotfixStatements.SEL_GARR_TALENT_TREE_LOCALE)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => GemPropertiesStorage = ReadDB2<GemPropertiesRecord>("GemProperties.db2", HotfixStatements.SEL_GEM_PROPERTIES)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => GlobalCurveStorage = ReadDB2<GlobalCurveRecord>("GlobalCurve.db2", HotfixStatements.SEL_GLOBAL_CURVE)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => GlyphBindableSpellStorage = ReadDB2<GlyphBindableSpellRecord>("GlyphBindableSpell.db2", HotfixStatements.SEL_GLYPH_BINDABLE_SPELL)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => GlyphPropertiesStorage = ReadDB2<GlyphPropertiesRecord>("GlyphProperties.db2", HotfixStatements.SEL_GLYPH_PROPERTIES)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => GlyphRequiredSpecStorage = ReadDB2<GlyphRequiredSpecRecord>("GlyphRequiredSpec.db2", HotfixStatements.SEL_GLYPH_REQUIRED_SPEC)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => GossipNPCOptionStorage = ReadDB2<GossipNPCOptionRecord>("GossipNPCOption.db2", HotfixStatements.SEL_GOSSIP_NPC_OPTION)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => GuildColorBackgroundStorage = ReadDB2<GuildColorBackgroundRecord>("GuildColorBackground.db2", HotfixStatements.SEL_GUILD_COLOR_BACKGROUND)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => GuildColorBorderStorage = ReadDB2<GuildColorBorderRecord>("GuildColorBorder.db2", HotfixStatements.SEL_GUILD_COLOR_BORDER)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => GuildColorEmblemStorage = ReadDB2<GuildColorEmblemRecord>("GuildColorEmblem.db2", HotfixStatements.SEL_GUILD_COLOR_EMBLEM)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => GuildPerkSpellsStorage = ReadDB2<GuildPerkSpellsRecord>("GuildPerkSpells.db2", HotfixStatements.SEL_GUILD_PERK_SPELLS)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => HeirloomStorage = ReadDB2<HeirloomRecord>("Heirloom.db2", HotfixStatements.SEL_HEIRLOOM, HotfixStatements.SEL_HEIRLOOM_LOCALE)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => HolidaysStorage = ReadDB2<HolidaysRecord>("Holidays.db2", HotfixStatements.SEL_HOLIDAYS)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => ImportPriceArmorStorage = ReadDB2<ImportPriceArmorRecord>("ImportPriceArmor.db2", HotfixStatements.SEL_IMPORT_PRICE_ARMOR)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => ImportPriceQualityStorage = ReadDB2<ImportPriceQualityRecord>("ImportPriceQuality.db2", HotfixStatements.SEL_IMPORT_PRICE_QUALITY)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => ImportPriceShieldStorage = ReadDB2<ImportPriceShieldRecord>("ImportPriceShield.db2", HotfixStatements.SEL_IMPORT_PRICE_SHIELD)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => ImportPriceWeaponStorage = ReadDB2<ImportPriceWeaponRecord>("ImportPriceWeapon.db2", HotfixStatements.SEL_IMPORT_PRICE_WEAPON)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => ItemAppearanceStorage = ReadDB2<ItemAppearanceRecord>("ItemAppearance.db2", HotfixStatements.SEL_ITEM_APPEARANCE)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => ItemArmorQualityStorage = ReadDB2<ItemArmorQualityRecord>("ItemArmorQuality.db2", HotfixStatements.SEL_ITEM_ARMOR_QUALITY)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => ItemArmorShieldStorage = ReadDB2<ItemArmorShieldRecord>("ItemArmorShield.db2", HotfixStatements.SEL_ITEM_ARMOR_SHIELD)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => ItemArmorTotalStorage = ReadDB2<ItemArmorTotalRecord>("ItemArmorTotal.db2", HotfixStatements.SEL_ITEM_ARMOR_TOTAL)).SingleInstance());
		//ItemBagFamilyStorage = ReadDB2<ItemBagFamilyRecord>("ItemBagFamily.db2", HotfixStatements.SEL_ITEM_BAG_FAMILY, HotfixStatements.SEL_ITEM_BAG_FAMILY_LOCALE)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => ItemBonusStorage = ReadDB2<ItemBonusRecord>("ItemBonus.db2", HotfixStatements.SEL_ITEM_BONUS)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => ItemBonusListLevelDeltaStorage = ReadDB2<ItemBonusListLevelDeltaRecord>("ItemBonusListLevelDelta.db2", HotfixStatements.SEL_ITEM_BONUS_LIST_LEVEL_DELTA)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => ItemBonusTreeNodeStorage = ReadDB2<ItemBonusTreeNodeRecord>("ItemBonusTreeNode.db2", HotfixStatements.SEL_ITEM_BONUS_TREE_NODE)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => ItemChildEquipmentStorage = ReadDB2<ItemChildEquipmentRecord>("ItemChildEquipment.db2", HotfixStatements.SEL_ITEM_CHILD_EQUIPMENT)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => ItemClassStorage = ReadDB2<ItemClassRecord>("ItemClass.db2", HotfixStatements.SEL_ITEM_CLASS, HotfixStatements.SEL_ITEM_CLASS_LOCALE)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => ItemCurrencyCostStorage = ReadDB2<ItemCurrencyCostRecord>("ItemCurrencyCost.db2", HotfixStatements.SEL_ITEM_CURRENCY_COST)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => ItemDamageAmmoStorage = ReadDB2<ItemDamageRecord>("ItemDamageAmmo.db2", HotfixStatements.SEL_ITEM_DAMAGE_AMMO)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => ItemDamageOneHandStorage = ReadDB2<ItemDamageRecord>("ItemDamageOneHand.db2", HotfixStatements.SEL_ITEM_DAMAGE_ONE_HAND)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => ItemDamageOneHandCasterStorage = ReadDB2<ItemDamageRecord>("ItemDamageOneHandCaster.db2", HotfixStatements.SEL_ITEM_DAMAGE_ONE_HAND_CASTER)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => ItemDamageTwoHandStorage = ReadDB2<ItemDamageRecord>("ItemDamageTwoHand.db2", HotfixStatements.SEL_ITEM_DAMAGE_TWO_HAND)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => ItemDamageTwoHandCasterStorage = ReadDB2<ItemDamageRecord>("ItemDamageTwoHandCaster.db2", HotfixStatements.SEL_ITEM_DAMAGE_TWO_HAND_CASTER)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => ItemDisenchantLootStorage = ReadDB2<ItemDisenchantLootRecord>("ItemDisenchantLoot.db2", HotfixStatements.SEL_ITEM_DISENCHANT_LOOT)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => ItemEffectStorage = ReadDB2<ItemEffectRecord>("ItemEffect.db2", HotfixStatements.SEL_ITEM_EFFECT)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => ItemStorage = ReadDB2<ItemRecord>("Item.db2", HotfixStatements.SEL_ITEM)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => ItemExtendedCostStorage = ReadDB2<ItemExtendedCostRecord>("ItemExtendedCost.db2", HotfixStatements.SEL_ITEM_EXTENDED_COST)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => ItemLevelSelectorStorage = ReadDB2<ItemLevelSelectorRecord>("ItemLevelSelector.db2", HotfixStatements.SEL_ITEM_LEVEL_SELECTOR)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => ItemLevelSelectorQualityStorage = ReadDB2<ItemLevelSelectorQualityRecord>("ItemLevelSelectorQuality.db2", HotfixStatements.SEL_ITEM_LEVEL_SELECTOR_QUALITY)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => ItemLevelSelectorQualitySetStorage = ReadDB2<ItemLevelSelectorQualitySetRecord>("ItemLevelSelectorQualitySet.db2", HotfixStatements.SEL_ITEM_LEVEL_SELECTOR_QUALITY_SET)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => ItemLimitCategoryStorage = ReadDB2<ItemLimitCategoryRecord>("ItemLimitCategory.db2", HotfixStatements.SEL_ITEM_LIMIT_CATEGORY, HotfixStatements.SEL_ITEM_LIMIT_CATEGORY_LOCALE)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => ItemLimitCategoryConditionStorage = ReadDB2<ItemLimitCategoryConditionRecord>("ItemLimitCategoryCondition.db2", HotfixStatements.SEL_ITEM_LIMIT_CATEGORY_CONDITION)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => ItemModifiedAppearanceStorage = ReadDB2<ItemModifiedAppearanceRecord>("ItemModifiedAppearance.db2", HotfixStatements.SEL_ITEM_MODIFIED_APPEARANCE)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => ItemModifiedAppearanceExtraStorage = ReadDB2<ItemModifiedAppearanceExtraRecord>("ItemModifiedAppearanceExtra.db2", HotfixStatements.SEL_ITEM_MODIFIED_APPEARANCE_EXTRA)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => ItemNameDescriptionStorage = ReadDB2<ItemNameDescriptionRecord>("ItemNameDescription.db2", HotfixStatements.SEL_ITEM_NAME_DESCRIPTION, HotfixStatements.SEL_ITEM_NAME_DESCRIPTION_LOCALE)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => ItemPriceBaseStorage = ReadDB2<ItemPriceBaseRecord>("ItemPriceBase.db2", HotfixStatements.SEL_ITEM_PRICE_BASE)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => ItemSearchNameStorage = ReadDB2<ItemSearchNameRecord>("ItemSearchName.db2", HotfixStatements.SEL_ITEM_SEARCH_NAME, HotfixStatements.SEL_ITEM_SEARCH_NAME_LOCALE)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => ItemSetStorage = ReadDB2<ItemSetRecord>("ItemSet.db2", HotfixStatements.SEL_ITEM_SET, HotfixStatements.SEL_ITEM_SET_LOCALE)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => ItemSetSpellStorage = ReadDB2<ItemSetSpellRecord>("ItemSetSpell.db2", HotfixStatements.SEL_ITEM_SET_SPELL)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => ItemSparseStorage = ReadDB2<ItemSparseRecord>("ItemSparse.db2", HotfixStatements.SEL_ITEM_SPARSE, HotfixStatements.SEL_ITEM_SPARSE_LOCALE)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => ItemSpecStorage = ReadDB2<ItemSpecRecord>("ItemSpec.db2", HotfixStatements.SEL_ITEM_SPEC)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => ItemSpecOverrideStorage = ReadDB2<ItemSpecOverrideRecord>("ItemSpecOverride.db2", HotfixStatements.SEL_ITEM_SPEC_OVERRIDE)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => ItemXBonusTreeStorage = ReadDB2<ItemXBonusTreeRecord>("ItemXBonusTree.db2", HotfixStatements.SEL_ITEM_X_BONUS_TREE)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => ItemXItemEffectStorage = ReadDB2<ItemXItemEffectRecord>("ItemXItemEffect.db2", HotfixStatements.SEL_ITEM_X_ITEM_EFFECT)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => JournalEncounterStorage = ReadDB2<JournalEncounterRecord>("JournalEncounter.db2", HotfixStatements.SEL_JOURNAL_ENCOUNTER, HotfixStatements.SEL_JOURNAL_ENCOUNTER_LOCALE)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => JournalEncounterSectionStorage = ReadDB2<JournalEncounterSectionRecord>("JournalEncounterSection.db2", HotfixStatements.SEL_JOURNAL_ENCOUNTER_SECTION, HotfixStatements.SEL_JOURNAL_ENCOUNTER_SECTION_LOCALE)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => JournalInstanceStorage = ReadDB2<JournalInstanceRecord>("JournalInstance.db2", HotfixStatements.SEL_JOURNAL_INSTANCE, HotfixStatements.SEL_JOURNAL_INSTANCE_LOCALE)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => JournalTierStorage = ReadDB2<JournalTierRecord>("JournalTier.db2", HotfixStatements.SEL_JOURNAL_TIER, HotfixStatements.SEL_JOURNAL_TIER_LOCALE)).SingleInstance());
		//KeyChainStorage = ReadDB2<KeyChainRecord>("KeyChain.db2", HotfixStatements.SEL_KEYCHAIN)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => KeystoneAffixStorage = ReadDB2<KeystoneAffixRecord>("KeystoneAffix.db2", HotfixStatements.SEL_KEYSTONE_AFFIX, HotfixStatements.SEL_KEYSTONE_AFFIX_LOCALE)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => LanguageWordsStorage = ReadDB2<LanguageWordsRecord>("LanguageWords.db2", HotfixStatements.SEL_LANGUAGE_WORDS)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => LanguagesStorage = ReadDB2<LanguagesRecord>("Languages.db2", HotfixStatements.SEL_LANGUAGES, HotfixStatements.SEL_LANGUAGES_LOCALE)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => LFGDungeonsStorage = ReadDB2<LFGDungeonsRecord>("LFGDungeons.db2", HotfixStatements.SEL_LFG_DUNGEONS, HotfixStatements.SEL_LFG_DUNGEONS_LOCALE)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => LightStorage = ReadDB2<LightRecord>("Light.db2", HotfixStatements.SEL_LIGHT)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => LiquidTypeStorage = ReadDB2<LiquidTypeRecord>("LiquidType.db2", HotfixStatements.SEL_LIQUID_TYPE)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => LockStorage = ReadDB2<LockRecord>("Lock.db2", HotfixStatements.SEL_LOCK)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => MailTemplateStorage = ReadDB2<MailTemplateRecord>("MailTemplate.db2", HotfixStatements.SEL_MAIL_TEMPLATE, HotfixStatements.SEL_MAIL_TEMPLATE_LOCALE)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => MapStorage = ReadDB2<MapRecord>("Map.db2", HotfixStatements.SEL_MAP, HotfixStatements.SEL_MAP_LOCALE)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => MapChallengeModeStorage = ReadDB2<MapChallengeModeRecord>("MapChallengeMode.db2", HotfixStatements.SEL_MAP_CHALLENGE_MODE, HotfixStatements.SEL_MAP_CHALLENGE_MODE_LOCALE)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => MapDifficultyStorage = ReadDB2<MapDifficultyRecord>("MapDifficulty.db2", HotfixStatements.SEL_MAP_DIFFICULTY, HotfixStatements.SEL_MAP_DIFFICULTY_LOCALE)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => MapDifficultyXConditionStorage = ReadDB2<MapDifficultyXConditionRecord>("MapDifficultyXCondition.db2", HotfixStatements.SEL_MAP_DIFFICULTY_X_CONDITION, HotfixStatements.SEL_MAP_DIFFICULTY_X_CONDITION_LOCALE)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => MawPowerStorage = ReadDB2<MawPowerRecord>("MawPower.db2", HotfixStatements.SEL_MAW_POWER)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => ModifierTreeStorage = ReadDB2<ModifierTreeRecord>("ModifierTree.db2", HotfixStatements.SEL_MODIFIER_TREE)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => MountCapabilityStorage = ReadDB2<MountCapabilityRecord>("MountCapability.db2", HotfixStatements.SEL_MOUNT_CAPABILITY)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => MountStorage = ReadDB2<MountRecord>("Mount.db2", HotfixStatements.SEL_MOUNT, HotfixStatements.SEL_MOUNT_LOCALE)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => MountTypeXCapabilityStorage = ReadDB2<MountTypeXCapabilityRecord>("MountTypeXCapability.db2", HotfixStatements.SEL_MOUNT_TYPE_X_CAPABILITY)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => MountXDisplayStorage = ReadDB2<MountXDisplayRecord>("MountXDisplay.db2", HotfixStatements.SEL_MOUNT_X_DISPLAY)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => MovieStorage = ReadDB2<MovieRecord>("Movie.db2", HotfixStatements.SEL_MOVIE)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => NameGenStorage = ReadDB2<NameGenRecord>("NameGen.db2", HotfixStatements.SEL_NAME_GEN)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => NamesProfanityStorage = ReadDB2<NamesProfanityRecord>("NamesProfanity.db2", HotfixStatements.SEL_NAMES_PROFANITY)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => NamesReservedStorage = ReadDB2<NamesReservedRecord>("NamesReserved.db2", HotfixStatements.SEL_NAMES_RESERVED, HotfixStatements.SEL_NAMES_RESERVED_LOCALE)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => NamesReservedLocaleStorage = ReadDB2<NamesReservedLocaleRecord>("NamesReservedLocale.db2", HotfixStatements.SEL_NAMES_RESERVED_LOCALE)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => NumTalentsAtLevelStorage = ReadDB2<NumTalentsAtLevelRecord>("NumTalentsAtLevel.db2", HotfixStatements.SEL_NUM_TALENTS_AT_LEVEL)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => OverrideSpellDataStorage = ReadDB2<OverrideSpellDataRecord>("OverrideSpellData.db2", HotfixStatements.SEL_OVERRIDE_SPELL_DATA)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => ParagonReputationStorage = ReadDB2<ParagonReputationRecord>("ParagonReputation.db2", HotfixStatements.SEL_PARAGON_REPUTATION)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => PhaseStorage = ReadDB2<PhaseRecord>("Phase.db2", HotfixStatements.SEL_PHASE)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => PhaseXPhaseGroupStorage = ReadDB2<PhaseXPhaseGroupRecord>("PhaseXPhaseGroup.db2", HotfixStatements.SEL_PHASE_X_PHASE_GROUP)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => PlayerConditionStorage = ReadDB2<PlayerConditionRecord>("PlayerCondition.db2", HotfixStatements.SEL_PLAYER_CONDITION, HotfixStatements.SEL_PLAYER_CONDITION_LOCALE)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => PowerDisplayStorage = ReadDB2<PowerDisplayRecord>("PowerDisplay.db2", HotfixStatements.SEL_POWER_DISPLAY)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => PowerTypeStorage = ReadDB2<PowerTypeRecord>("PowerType.db2", HotfixStatements.SEL_POWER_TYPE)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => PrestigeLevelInfoStorage = ReadDB2<PrestigeLevelInfoRecord>("PrestigeLevelInfo.db2", HotfixStatements.SEL_PRESTIGE_LEVEL_INFO, HotfixStatements.SEL_PRESTIGE_LEVEL_INFO_LOCALE)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => PvpDifficultyStorage = ReadDB2<PvpDifficultyRecord>("PVPDifficulty.db2", HotfixStatements.SEL_PVP_DIFFICULTY)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => PvpItemStorage = ReadDB2<PvpItemRecord>("PVPItem.db2", HotfixStatements.SEL_PVP_ITEM)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => PvpTalentStorage = ReadDB2<PvpTalentRecord>("PvpTalent.db2", HotfixStatements.SEL_PVP_TALENT, HotfixStatements.SEL_PVP_TALENT_LOCALE)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => PvpTalentCategoryStorage = ReadDB2<PvpTalentCategoryRecord>("PvpTalentCategory.db2", HotfixStatements.SEL_PVP_TALENT_CATEGORY)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => PvpTalentSlotUnlockStorage = ReadDB2<PvpTalentSlotUnlockRecord>("PvpTalentSlotUnlock.db2", HotfixStatements.SEL_PVP_TALENT_SLOT_UNLOCK)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => PvpTierStorage = ReadDB2<PvpTierRecord>("PvpTier.db2", HotfixStatements.SEL_PVP_TIER, HotfixStatements.SEL_PVP_TIER_LOCALE)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => QuestFactionRewardStorage = ReadDB2<QuestFactionRewardRecord>("QuestFactionReward.db2", HotfixStatements.SEL_QUEST_FACTION_REWARD)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => QuestInfoStorage = ReadDB2<QuestInfoRecord>("QuestInfo.db2", HotfixStatements.SEL_QUEST_INFO, HotfixStatements.SEL_QUEST_INFO_LOCALE)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => QuestPOIBlobStorage = ReadDB2<QuestPOIBlobEntry>("QuestPOIBlob.db2", HotfixStatements.SEL_QUEST_P_O_I_BLOB)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => QuestPOIPointStorage = ReadDB2<QuestPOIPointEntry>("QuestPOIPoint.db2", HotfixStatements.SEL_QUEST_P_O_I_POINT)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => QuestLineXQuestStorage = ReadDB2<QuestLineXQuestRecord>("QuestLineXQuest.db2", HotfixStatements.SEL_QUEST_LINE_X_QUEST)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => QuestMoneyRewardStorage = ReadDB2<QuestMoneyRewardRecord>("QuestMoneyReward.db2", HotfixStatements.SEL_QUEST_MONEY_REWARD)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => QuestPackageItemStorage = ReadDB2<QuestPackageItemRecord>("QuestPackageItem.db2", HotfixStatements.SEL_QUEST_PACKAGE_ITEM)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => QuestSortStorage = ReadDB2<QuestSortRecord>("QuestSort.db2", HotfixStatements.SEL_QUEST_SORT, HotfixStatements.SEL_QUEST_SORT_LOCALE)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => QuestV2Storage = ReadDB2<QuestV2Record>("QuestV2.db2", HotfixStatements.SEL_QUEST_V2)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => QuestXPStorage = ReadDB2<QuestXPRecord>("QuestXP.db2", HotfixStatements.SEL_QUEST_XP)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => RandPropPointsStorage = ReadDB2<RandPropPointsRecord>("RandPropPoints.db2", HotfixStatements.SEL_RAND_PROP_POINTS)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => RewardPackStorage = ReadDB2<RewardPackRecord>("RewardPack.db2", HotfixStatements.SEL_REWARD_PACK)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => RewardPackXCurrencyTypeStorage = ReadDB2<RewardPackXCurrencyTypeRecord>("RewardPackXCurrencyType.db2", HotfixStatements.SEL_REWARD_PACK_X_CURRENCY_TYPE)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => RewardPackXItemStorage = ReadDB2<RewardPackXItemRecord>("RewardPackXItem.db2", HotfixStatements.SEL_REWARD_PACK_X_ITEM)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => ScenarioStorage = ReadDB2<ScenarioRecord>("Scenario.db2", HotfixStatements.SEL_SCENARIO, HotfixStatements.SEL_SCENARIO_LOCALE)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => ScenarioStepStorage = ReadDB2<ScenarioStepRecord>("ScenarioStep.db2", HotfixStatements.SEL_SCENARIO_STEP, HotfixStatements.SEL_SCENARIO_STEP_LOCALE)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => SceneScriptStorage = ReadDB2<SceneScriptRecord>("SceneScript.db2", HotfixStatements.SEL_SCENE_SCRIPT)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => SceneScriptGlobalTextStorage = ReadDB2<SceneScriptGlobalTextRecord>("SceneScriptGlobalText.db2", HotfixStatements.SEL_SCENE_SCRIPT_GLOBAL_TEXT)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => SceneScriptPackageStorage = ReadDB2<SceneScriptPackageRecord>("SceneScriptPackage.db2", HotfixStatements.SEL_SCENE_SCRIPT_PACKAGE)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => SceneScriptTextStorage = ReadDB2<SceneScriptTextRecord>("SceneScriptText.db2", HotfixStatements.SEL_SCENE_SCRIPT_TEXT)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => SkillLineStorage = ReadDB2<SkillLineRecord>("SkillLine.db2", HotfixStatements.SEL_SKILL_LINE, HotfixStatements.SEL_SKILL_LINE_LOCALE)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => SkillLineAbilityStorage = ReadDB2<SkillLineAbilityRecord>("SkillLineAbility.db2", HotfixStatements.SEL_SKILL_LINE_ABILITY)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => SkillLineXTraitTreeStorage = ReadDB2<SkillLineXTraitTreeRecord>("SkillLineXTraitTree.db2", HotfixStatements.SEL_SKILL_LINE_X_TRAIT_TREE)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => SkillRaceClassInfoStorage = ReadDB2<SkillRaceClassInfoRecord>("SkillRaceClassInfo.db2", HotfixStatements.SEL_SKILL_RACE_CLASS_INFO)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => SoulbindConduitRankStorage = ReadDB2<SoulbindConduitRankRecord>("SoulbindConduitRank.db2", HotfixStatements.SEL_SOULBIND_CONDUIT_RANK)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => SoundKitStorage = ReadDB2<SoundKitRecord>("SoundKit.db2", HotfixStatements.SEL_SOUND_KIT)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => SpecializationSpellsStorage = ReadDB2<SpecializationSpellsRecord>("SpecializationSpells.db2", HotfixStatements.SEL_SPECIALIZATION_SPELLS, HotfixStatements.SEL_SPECIALIZATION_SPELLS_LOCALE)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => SpecSetMemberStorage = ReadDB2<SpecSetMemberRecord>("SpecSetMember.db2", HotfixStatements.SEL_SPEC_SET_MEMBER)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => SpellStorage = ReadDB2<SpellRecord>("Spell.db2", HotfixStatements.SEL_SPELL)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => SpellNameStorage = ReadDB2<SpellNameRecord>("SpellName.db2", HotfixStatements.SEL_SPELL_NAME, HotfixStatements.SEL_SPELL_NAME_LOCALE)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => SpellAuraOptionsStorage = ReadDB2<SpellAuraOptionsRecord>("SpellAuraOptions.db2", HotfixStatements.SEL_SPELL_AURA_OPTIONS)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => SpellAuraRestrictionsStorage = ReadDB2<SpellAuraRestrictionsRecord>("SpellAuraRestrictions.db2", HotfixStatements.SEL_SPELL_AURA_RESTRICTIONS)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => SpellCastTimesStorage = ReadDB2<SpellCastTimesRecord>("SpellCastTimes.db2", HotfixStatements.SEL_SPELL_CAST_TIMES)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => SpellCastingRequirementsStorage = ReadDB2<SpellCastingRequirementsRecord>("SpellCastingRequirements.db2", HotfixStatements.SEL_SPELL_CASTING_REQUIREMENTS)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => SpellCategoriesStorage = ReadDB2<SpellCategoriesRecord>("SpellCategories.db2", HotfixStatements.SEL_SPELL_CATEGORIES)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => SpellCategoryStorage = ReadDB2<SpellCategoryRecord>("SpellCategory.db2", HotfixStatements.SEL_SPELL_CATEGORY, HotfixStatements.SEL_SPELL_CATEGORY_LOCALE)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => SpellClassOptionsStorage = ReadDB2<SpellClassOptionsRecord>("SpellClassOptions.db2", HotfixStatements.SEL_SPELL_CLASS_OPTIONS)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => SpellCooldownsStorage = ReadDB2<SpellCooldownsRecord>("SpellCooldowns.db2", HotfixStatements.SEL_SPELL_COOLDOWNS)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => SpellDurationStorage = ReadDB2<SpellDurationRecord>("SpellDuration.db2", HotfixStatements.SEL_SPELL_DURATION)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => SpellEffectStorage = ReadDB2<SpellEffectRecord>("SpellEffect.db2", HotfixStatements.SEL_SPELL_EFFECT)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => SpellEmpowerStorage = ReadDB2<SpellEmpowerRecord>("SpellEmpower.db2", HotfixStatements.SEL_SPELL_EMPOWER)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => SpellEmpowerStageStorage = ReadDB2<SpellEmpowerStageRecord>("SpellEmpowerStage.db2", HotfixStatements.SEL_SPELL_EMPOWER_STAGE)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => SpellEquippedItemsStorage = ReadDB2<SpellEquippedItemsRecord>("SpellEquippedItems.db2", HotfixStatements.SEL_SPELL_EQUIPPED_ITEMS)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => SpellFocusObjectStorage = ReadDB2<SpellFocusObjectRecord>("SpellFocusObject.db2", HotfixStatements.SEL_SPELL_FOCUS_OBJECT, HotfixStatements.SEL_SPELL_FOCUS_OBJECT_LOCALE)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => SpellInterruptsStorage = ReadDB2<SpellInterruptsRecord>("SpellInterrupts.db2", HotfixStatements.SEL_SPELL_INTERRUPTS)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => SpellItemEnchantmentStorage = ReadDB2<SpellItemEnchantmentRecord>("SpellItemEnchantment.db2", HotfixStatements.SEL_SPELL_ITEM_ENCHANTMENT, HotfixStatements.SEL_SPELL_ITEM_ENCHANTMENT_LOCALE)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => SpellItemEnchantmentConditionStorage = ReadDB2<SpellItemEnchantmentConditionRecord>("SpellItemEnchantmentCondition.db2", HotfixStatements.SEL_SPELL_ITEM_ENCHANTMENT_CONDITION)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => SpellKeyboundOverrideStorage = ReadDB2<SpellKeyboundOverrideRecord>("SpellKeyboundOverride.db2", HotfixStatements.SEL_SPELL_KEYBOUND_OVERRIDE)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => SpellLabelStorage = ReadDB2<SpellLabelRecord>("SpellLabel.db2", HotfixStatements.SEL_SPELL_LABEL)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => SpellLearnSpellStorage = ReadDB2<SpellLearnSpellRecord>("SpellLearnSpell.db2", HotfixStatements.SEL_SPELL_LEARN_SPELL)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => SpellLevelsStorage = ReadDB2<SpellLevelsRecord>("SpellLevels.db2", HotfixStatements.SEL_SPELL_LEVELS)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => SpellMiscStorage = ReadDB2<SpellMiscRecord>("SpellMisc.db2", HotfixStatements.SEL_SPELL_MISC)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => SpellPowerStorage = ReadDB2<SpellPowerRecord>("SpellPower.db2", HotfixStatements.SEL_SPELL_POWER)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => SpellPowerDifficultyStorage = ReadDB2<SpellPowerDifficultyRecord>("SpellPowerDifficulty.db2", HotfixStatements.SEL_SPELL_POWER_DIFFICULTY)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => SpellProcsPerMinuteStorage = ReadDB2<SpellProcsPerMinuteRecord>("SpellProcsPerMinute.db2", HotfixStatements.SEL_SPELL_PROCS_PER_MINUTE)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => SpellProcsPerMinuteModStorage = ReadDB2<SpellProcsPerMinuteModRecord>("SpellProcsPerMinuteMod.db2", HotfixStatements.SEL_SPELL_PROCS_PER_MINUTE_MOD)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => SpellRadiusStorage = ReadDB2<SpellRadiusRecord>("SpellRadius.db2", HotfixStatements.SEL_SPELL_RADIUS)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => SpellRangeStorage = ReadDB2<SpellRangeRecord>("SpellRange.db2", HotfixStatements.SEL_SPELL_RANGE, HotfixStatements.SEL_SPELL_RANGE_LOCALE)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => SpellReagentsStorage = ReadDB2<SpellReagentsRecord>("SpellReagents.db2", HotfixStatements.SEL_SPELL_REAGENTS)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => SpellReagentsCurrencyStorage = ReadDB2<SpellReagentsCurrencyRecord>("SpellReagentsCurrency.db2", HotfixStatements.SEL_SPELL_REAGENTS_CURRENCY)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => SpellReplacementStorage = ReadDB2<SpellReplacementRecord>("SpellReplacement.db2", HotfixStatements.SEL_SPELL_REPLACEMENT)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => SpellScalingStorage = ReadDB2<SpellScalingRecord>("SpellScaling.db2", HotfixStatements.SEL_SPELL_SCALING)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => SpellShapeshiftStorage = ReadDB2<SpellShapeshiftRecord>("SpellShapeshift.db2", HotfixStatements.SEL_SPELL_SHAPESHIFT)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => SpellShapeshiftFormStorage = ReadDB2<SpellShapeshiftFormRecord>("SpellShapeshiftForm.db2", HotfixStatements.SEL_SPELL_SHAPESHIFT_FORM, HotfixStatements.SEL_SPELL_SHAPESHIFT_FORM_LOCALE)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => SpellTargetRestrictionsStorage = ReadDB2<SpellTargetRestrictionsRecord>("SpellTargetRestrictions.db2", HotfixStatements.SEL_SPELL_TARGET_RESTRICTIONS)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => SpellTotemsStorage = ReadDB2<SpellTotemsRecord>("SpellTotems.db2", HotfixStatements.SEL_SPELL_TOTEMS)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => SpellVisualStorage = ReadDB2<SpellVisualRecord>("SpellVisual.db2", HotfixStatements.SEL_SPELL_VISUAL)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => SpellVisualEffectNameStorage = ReadDB2<SpellVisualEffectNameRecord>("SpellVisualEffectName.db2", HotfixStatements.SEL_SPELL_VISUAL_EFFECT_NAME)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => SpellVisualMissileStorage = ReadDB2<SpellVisualMissileRecord>("SpellVisualMissile.db2", HotfixStatements.SEL_SPELL_VISUAL_MISSILE)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => SpellVisualKitStorage = ReadDB2<SpellVisualKitRecord>("SpellVisualKit.db2", HotfixStatements.SEL_SPELL_VISUAL_KIT)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => SpellXSpellVisualStorage = ReadDB2<SpellXSpellVisualRecord>("SpellXSpellVisual.db2", HotfixStatements.SEL_SPELL_X_SPELL_VISUAL)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => SummonPropertiesStorage = ReadDB2<SummonPropertiesRecord>("SummonProperties.db2", HotfixStatements.SEL_SUMMON_PROPERTIES)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => TactKeyStorage = ReadDB2<TactKeyRecord>("TactKey.db2", HotfixStatements.SEL_TACT_KEY)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => TalentStorage = ReadDB2<TalentRecord>("Talent.db2", HotfixStatements.SEL_TALENT, HotfixStatements.SEL_TALENT_LOCALE)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => TaxiNodesStorage = ReadDB2<TaxiNodesRecord>("TaxiNodes.db2", HotfixStatements.SEL_TAXI_NODES, HotfixStatements.SEL_TAXI_NODES_LOCALE)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => TaxiPathStorage = ReadDB2<TaxiPathRecord>("TaxiPath.db2", HotfixStatements.SEL_TAXI_PATH)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => TaxiPathNodeStorage = ReadDB2<TaxiPathNodeRecord>("TaxiPathNode.db2", HotfixStatements.SEL_TAXI_PATH_NODE)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => TotemCategoryStorage = ReadDB2<TotemCategoryRecord>("TotemCategory.db2", HotfixStatements.SEL_TOTEM_CATEGORY, HotfixStatements.SEL_TOTEM_CATEGORY_LOCALE)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => ToyStorage = ReadDB2<ToyRecord>("Toy.db2", HotfixStatements.SEL_TOY, HotfixStatements.SEL_TOY_LOCALE)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => TraitCondStorage = ReadDB2<TraitCondRecord>("TraitCond.db2", HotfixStatements.SEL_TRAIT_COND)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => TraitCostStorage = ReadDB2<TraitCostRecord>("TraitCost.db2", HotfixStatements.SEL_TRAIT_COST)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => TraitCurrencyStorage = ReadDB2<TraitCurrencyRecord>("TraitCurrency.db2", HotfixStatements.SEL_TRAIT_CURRENCY)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => TraitCurrencySourceStorage = ReadDB2<TraitCurrencySourceRecord>("TraitCurrencySource.db2", HotfixStatements.SEL_TRAIT_CURRENCY_SOURCE, HotfixStatements.SEL_TRAIT_CURRENCY_SOURCE_LOCALE)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => TraitDefinitionStorage = ReadDB2<TraitDefinitionRecord>("TraitDefinition.db2", HotfixStatements.SEL_TRAIT_DEFINITION, HotfixStatements.SEL_TRAIT_DEFINITION_LOCALE)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => TraitDefinitionEffectPointsStorage = ReadDB2<TraitDefinitionEffectPointsRecord>("TraitDefinitionEffectPoints.db2", HotfixStatements.SEL_TRAIT_DEFINITION_EFFECT_POINTS)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => TraitEdgeStorage = ReadDB2<TraitEdgeRecord>("TraitEdge.db2", HotfixStatements.SEL_TRAIT_EDGE)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => TraitNodeStorage = ReadDB2<TraitNodeRecord>("TraitNode.db2", HotfixStatements.SEL_TRAIT_NODE)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => TraitNodeEntryStorage = ReadDB2<TraitNodeEntryRecord>("TraitNodeEntry.db2", HotfixStatements.SEL_TRAIT_NODE_ENTRY)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => TraitNodeEntryXTraitCondStorage = ReadDB2<TraitNodeEntryXTraitCondRecord>("TraitNodeEntryXTraitCond.db2", HotfixStatements.SEL_TRAIT_NODE_ENTRY_X_TRAIT_COND)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => TraitNodeEntryXTraitCostStorage = ReadDB2<TraitNodeEntryXTraitCostRecord>("TraitNodeEntryXTraitCost.db2", HotfixStatements.SEL_TRAIT_NODE_ENTRY_X_TRAIT_COST)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => TraitNodeGroupStorage = ReadDB2<TraitNodeGroupRecord>("TraitNodeGroup.db2", HotfixStatements.SEL_TRAIT_NODE_GROUP)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => TraitNodeGroupXTraitCondStorage = ReadDB2<TraitNodeGroupXTraitCondRecord>("TraitNodeGroupXTraitCond.db2", HotfixStatements.SEL_TRAIT_NODE_GROUP_X_TRAIT_COND)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => TraitNodeGroupXTraitCostStorage = ReadDB2<TraitNodeGroupXTraitCostRecord>("TraitNodeGroupXTraitCost.db2", HotfixStatements.SEL_TRAIT_NODE_GROUP_X_TRAIT_COST)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => TraitNodeGroupXTraitNodeStorage = ReadDB2<TraitNodeGroupXTraitNodeRecord>("TraitNodeGroupXTraitNode.db2", HotfixStatements.SEL_TRAIT_NODE_GROUP_X_TRAIT_NODE)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => TraitNodeXTraitCondStorage = ReadDB2<TraitNodeXTraitCondRecord>("TraitNodeXTraitCond.db2", HotfixStatements.SEL_TRAIT_NODE_X_TRAIT_COND)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => TraitNodeXTraitCostStorage = ReadDB2<TraitNodeXTraitCostRecord>("TraitNodeXTraitCost.db2", HotfixStatements.SEL_TRAIT_NODE_X_TRAIT_COST)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => TraitNodeXTraitNodeEntryStorage = ReadDB2<TraitNodeXTraitNodeEntryRecord>("TraitNodeXTraitNodeEntry.db2", HotfixStatements.SEL_TRAIT_NODE_X_TRAIT_NODE_ENTRY)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => TraitTreeStorage = ReadDB2<TraitTreeRecord>("TraitTree.db2", HotfixStatements.SEL_TRAIT_TREE)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => TraitTreeLoadoutStorage = ReadDB2<TraitTreeLoadoutRecord>("TraitTreeLoadout.db2", HotfixStatements.SEL_TRAIT_TREE_LOADOUT)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => TraitTreeLoadoutEntryStorage = ReadDB2<TraitTreeLoadoutEntryRecord>("TraitTreeLoadoutEntry.db2", HotfixStatements.SEL_TRAIT_TREE_LOADOUT_ENTRY)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => TraitTreeXTraitCostStorage = ReadDB2<TraitTreeXTraitCostRecord>("TraitTreeXTraitCost.db2", HotfixStatements.SEL_TRAIT_TREE_X_TRAIT_COST)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => TraitTreeXTraitCurrencyStorage = ReadDB2<TraitTreeXTraitCurrencyRecord>("TraitTreeXTraitCurrency.db2", HotfixStatements.SEL_TRAIT_TREE_X_TRAIT_CURRENCY)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => TransmogHolidayStorage = ReadDB2<TransmogHolidayRecord>("TransmogHoliday.db2", HotfixStatements.SEL_TRANSMOG_HOLIDAY)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => TransmogIllusionStorage = ReadDB2<TransmogIllusionRecord>("TransmogIllusion.db2", HotfixStatements.SEL_TRANSMOG_ILLUSION)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => TransmogSetStorage = ReadDB2<TransmogSetRecord>("TransmogSet.db2", HotfixStatements.SEL_TRANSMOG_SET, HotfixStatements.SEL_TRANSMOG_SET_LOCALE)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => TransmogSetGroupStorage = ReadDB2<TransmogSetGroupRecord>("TransmogSetGroup.db2", HotfixStatements.SEL_TRANSMOG_SET_GROUP, HotfixStatements.SEL_TRANSMOG_SET_GROUP_LOCALE)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => TransmogSetItemStorage = ReadDB2<TransmogSetItemRecord>("TransmogSetItem.db2", HotfixStatements.SEL_TRANSMOG_SET_ITEM)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => TransportAnimationStorage = ReadDB2<TransportAnimationRecord>("TransportAnimation.db2", HotfixStatements.SEL_TRANSPORT_ANIMATION)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => TransportRotationStorage = ReadDB2<TransportRotationRecord>("TransportRotation.db2", HotfixStatements.SEL_TRANSPORT_ROTATION)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => UiMapStorage = ReadDB2<UiMapRecord>("UiMap.db2", HotfixStatements.SEL_UI_MAP, HotfixStatements.SEL_UI_MAP_LOCALE)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => UiMapAssignmentStorage = ReadDB2<UiMapAssignmentRecord>("UiMapAssignment.db2", HotfixStatements.SEL_UI_MAP_ASSIGNMENT)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => UiMapLinkStorage = ReadDB2<UiMapLinkRecord>("UiMapLink.db2", HotfixStatements.SEL_UI_MAP_LINK)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => UiMapXMapArtStorage = ReadDB2<UiMapXMapArtRecord>("UiMapXMapArt.db2", HotfixStatements.SEL_UI_MAP_X_MAP_ART)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => UISplashScreenStorage = ReadDB2<UISplashScreenRecord>("UISplashScreen.db2", HotfixStatements.SEL_UI_SPLASH_SCREEN, HotfixStatements.SEL_UI_SPLASH_SCREEN_LOCALE)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => UnitConditionStorage = ReadDB2<UnitConditionRecord>("UnitCondition.db2", HotfixStatements.SEL_UNIT_CONDITION)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => UnitPowerBarStorage = ReadDB2<UnitPowerBarRecord>("UnitPowerBar.db2", HotfixStatements.SEL_UNIT_POWER_BAR, HotfixStatements.SEL_UNIT_POWER_BAR_LOCALE)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => VehicleStorage = ReadDB2<VehicleRecord>("Vehicle.db2", HotfixStatements.SEL_VEHICLE)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => VehicleSeatStorage = ReadDB2<VehicleSeatRecord>("VehicleSeat.db2", HotfixStatements.SEL_VEHICLE_SEAT)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => WMOAreaTableStorage = ReadDB2<WMOAreaTableRecord>("WMOAreaTable.db2", HotfixStatements.SEL_WMO_AREA_TABLE, HotfixStatements.SEL_WMO_AREA_TABLE_LOCALE)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => WorldEffectStorage = ReadDB2<WorldEffectRecord>("WorldEffect.db2", HotfixStatements.SEL_WORLD_EFFECT)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => WorldMapOverlayStorage = ReadDB2<WorldMapOverlayRecord>("WorldMapOverlay.db2", HotfixStatements.SEL_WORLD_MAP_OVERLAY)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => WorldStateExpressionStorage = ReadDB2<WorldStateExpressionRecord>("WorldStateExpression.db2", HotfixStatements.SEL_WORLD_STATE_EXPRESSION)).SingleInstance());
		_taskManager.Post(() => builder.Register((c, p) => CharBaseInfoStorage = ReadDB2<CharBaseInfo>("CharBaseInfo.db2", HotfixStatements.SEL_CHAR_BASE_INFO)).SingleInstance());

		_taskManager.Complete();
		_taskManager.Completion.Wait();

		_db2Manager.LoadStores(this);
#if DEBUG
		Log.Logger.Information($"DB2  TableHash");

		foreach (var kvp in _db2Manager.Storage)
			if (kvp.Value != null)
				Log.Logger.Information($"{kvp.Value.GetName()}    {kvp.Key}");
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

			if (!_db2Manager.GetUiMapPosition(node.Pos.X, node.Pos.Y, node.Pos.Z, node.ContinentID, 0, 0, 0, UiMapSystem.Adventure, false, out int uiMapId))
				_db2Manager.GetUiMapPosition(node.Pos.X, node.Pos.Y, node.Pos.Z, node.ContinentID, 0, 0, 0, UiMapSystem.Taxi, false, out uiMapId);

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
			Log.Logger.Fatal("You have _outdated_ DB2 files. Please extract correct versions from current using client.");
			Environment.Exit(1);
		}

		Log.Logger.Information("Initialized {0} DB2 data storages in {1} ms", loadedFileCount, Time.GetMSTimeDiffToNow(oldMSTime));

		return availableDb2Locales;
	}

	public void LoadGameTables(string dataPath)
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

		Log.Logger.Information("Initialized {0} DBC GameTables data stores in {1} ms", loadedFileCount, Time.GetMSTimeDiffToNow(oldMSTime));
	}

	#region Main Collections

	public DB6Storage<AchievementRecord> AchievementStorage;
	public DB6Storage<AchievementCategoryRecord> AchievementCategoryStorage;
	public DB6Storage<AdventureJournalRecord> AdventureJournalStorage;
	public DB6Storage<AdventureMapPOIRecord> AdventureMapPOIStorage;
	public DB6Storage<AnimationDataRecord> AnimationDataStorage;
	public DB6Storage<AnimKitRecord> AnimKitStorage;
	public DB6Storage<AreaGroupMemberRecord> AreaGroupMemberStorage;
	public DB6Storage<AreaTableRecord> AreaTableStorage;
	public DB6Storage<AreaPOIRecord> AreaPOIStorage;
	public DB6Storage<AreaPOIStateRecord> AreaPOIStateStorage;
	public DB6Storage<AreaTriggerRecord> AreaTriggerStorage;
	public DB6Storage<ArmorLocationRecord> ArmorLocationStorage;
	public DB6Storage<ArtifactRecord> ArtifactStorage;
	public DB6Storage<ArtifactAppearanceRecord> ArtifactAppearanceStorage;
	public DB6Storage<ArtifactAppearanceSetRecord> ArtifactAppearanceSetStorage;
	public DB6Storage<ArtifactCategoryRecord> ArtifactCategoryStorage;
	public DB6Storage<ArtifactPowerRecord> ArtifactPowerStorage;
	public DB6Storage<ArtifactPowerLinkRecord> ArtifactPowerLinkStorage;
	public DB6Storage<ArtifactPowerPickerRecord> ArtifactPowerPickerStorage;
	public DB6Storage<ArtifactPowerRankRecord> ArtifactPowerRankStorage;
	public DB6Storage<ArtifactQuestXPRecord> ArtifactQuestXPStorage;
	public DB6Storage<ArtifactTierRecord> ArtifactTierStorage;
	public DB6Storage<ArtifactUnlockRecord> ArtifactUnlockStorage;
	public DB6Storage<AuctionHouseRecord> AuctionHouseStorage;
	public DB6Storage<AzeriteEmpoweredItemRecord> AzeriteEmpoweredItemStorage;
	public DB6Storage<AzeriteEssenceRecord> AzeriteEssenceStorage;
	public DB6Storage<AzeriteEssencePowerRecord> AzeriteEssencePowerStorage;
	public DB6Storage<AzeriteItemRecord> AzeriteItemStorage;
	public DB6Storage<AzeriteItemMilestonePowerRecord> AzeriteItemMilestonePowerStorage;
	public DB6Storage<AzeriteKnowledgeMultiplierRecord> AzeriteKnowledgeMultiplierStorage;
	public DB6Storage<AzeriteLevelInfoRecord> AzeriteLevelInfoStorage;
	public DB6Storage<AzeritePowerRecord> AzeritePowerStorage;
	public DB6Storage<AzeritePowerSetMemberRecord> AzeritePowerSetMemberStorage;
	public DB6Storage<AzeriteTierUnlockRecord> AzeriteTierUnlockStorage;
	public DB6Storage<AzeriteTierUnlockSetRecord> AzeriteTierUnlockSetStorage;
	public DB6Storage<AzeriteUnlockMappingRecord> AzeriteUnlockMappingStorage;
	public DB6Storage<BankBagSlotPricesRecord> BankBagSlotPricesStorage;
	public DB6Storage<BannedAddonsRecord> BannedAddOnsStorage;
	public DB6Storage<BarberShopStyleRecord> BarberShopStyleStorage;
	public DB6Storage<BattlePetBreedQualityRecord> BattlePetBreedQualityStorage;
	public DB6Storage<BattlePetBreedStateRecord> BattlePetBreedStateStorage;
	public DB6Storage<BattlePetSpeciesRecord> BattlePetSpeciesStorage;
	public DB6Storage<BattlePetSpeciesStateRecord> BattlePetSpeciesStateStorage;
	public DB6Storage<BattlemasterListRecord> BattlemasterListStorage;
	public DB6Storage<BroadcastTextRecord> BroadcastTextStorage;
	public DB6Storage<BroadcastTextDurationRecord> BroadcastTextDurationStorage;
	public DB6Storage<Cfg_RegionsRecord> CfgRegionsStorage;
	public DB6Storage<CharTitlesRecord> CharTitlesStorage;
	public DB6Storage<CharacterLoadoutRecord> CharacterLoadoutStorage;
	public DB6Storage<CharacterLoadoutItemRecord> CharacterLoadoutItemStorage;
	public DB6Storage<ChatChannelsRecord> ChatChannelsStorage;
	public DB6Storage<ChrClassUIDisplayRecord> ChrClassUIDisplayStorage;
	public DB6Storage<ChrClassesRecord> ChrClassesStorage;
	public DB6Storage<ChrClassesXPowerTypesRecord> ChrClassesXPowerTypesStorage;
	public DB6Storage<ChrCustomizationChoiceRecord> ChrCustomizationChoiceStorage;
	public DB6Storage<ChrCustomizationDisplayInfoRecord> ChrCustomizationDisplayInfoStorage;
	public DB6Storage<ChrCustomizationElementRecord> ChrCustomizationElementStorage;
	public DB6Storage<ChrCustomizationReqRecord> ChrCustomizationReqStorage;
	public DB6Storage<ChrCustomizationReqChoiceRecord> ChrCustomizationReqChoiceStorage;
	public DB6Storage<ChrModelRecord> ChrModelStorage;
	public DB6Storage<ChrRaceXChrModelRecord> ChrRaceXChrModelStorage;
	public DB6Storage<ChrCustomizationOptionRecord> ChrCustomizationOptionStorage;
	public DB6Storage<ChrRacesRecord> ChrRacesStorage;
	public DB6Storage<ChrSpecializationRecord> ChrSpecializationStorage;
	public DB6Storage<CinematicCameraRecord> CinematicCameraStorage;
	public DB6Storage<CinematicSequencesRecord> CinematicSequencesStorage;
	public DB6Storage<ContentTuningRecord> ContentTuningStorage;
	public DB6Storage<ContentTuningXExpectedRecord> ContentTuningXExpectedStorage;
	public DB6Storage<ConversationLineRecord> ConversationLineStorage;
	public DB6Storage<CorruptionEffectsRecord> CorruptionEffectsStorage;
	public DB6Storage<CreatureDisplayInfoRecord> CreatureDisplayInfoStorage;
	public DB6Storage<CreatureDisplayInfoExtraRecord> CreatureDisplayInfoExtraStorage;
	public DB6Storage<CreatureFamilyRecord> CreatureFamilyStorage;
	public DB6Storage<CreatureModelDataRecord> CreatureModelDataStorage;
	public DB6Storage<CreatureTypeRecord> CreatureTypeStorage;
	public DB6Storage<CriteriaRecord> CriteriaStorage;
	public DB6Storage<CriteriaTreeRecord> CriteriaTreeStorage;
	public DB6Storage<CurrencyContainerRecord> CurrencyContainerStorage;
	public DB6Storage<CurrencyTypesRecord> CurrencyTypesStorage;
	public DB6Storage<CurveRecord> CurveStorage;
	public DB6Storage<CurvePointRecord> CurvePointStorage;
	public DB6Storage<DestructibleModelDataRecord> DestructibleModelDataStorage;
	public DB6Storage<DifficultyRecord> DifficultyStorage;
	public DB6Storage<DungeonEncounterRecord> DungeonEncounterStorage;
	public DB6Storage<DurabilityCostsRecord> DurabilityCostsStorage;
	public DB6Storage<DurabilityQualityRecord> DurabilityQualityStorage;
	public DB6Storage<EmotesRecord> EmotesStorage;
	public DB6Storage<EmotesTextRecord> EmotesTextStorage;
	public DB6Storage<EmotesTextSoundRecord> EmotesTextSoundStorage;
	public DB6Storage<ExpectedStatRecord> ExpectedStatStorage;
	public DB6Storage<ExpectedStatModRecord> ExpectedStatModStorage;
	public DB6Storage<FactionRecord> FactionStorage;
	public DB6Storage<FactionTemplateRecord> FactionTemplateStorage;
	public DB6Storage<FriendshipRepReactionRecord> FriendshipRepReactionStorage;
	public DB6Storage<FriendshipReputationRecord> FriendshipReputationStorage;
	public DB6Storage<GameObjectArtKitRecord> GameObjectArtKitStorage;
	public DB6Storage<GameObjectDisplayInfoRecord> GameObjectDisplayInfoStorage;
	public DB6Storage<GameObjectsRecord> GameObjectsStorage;
	public DB6Storage<GarrAbilityRecord> GarrAbilityStorage;
	public DB6Storage<GarrBuildingRecord> GarrBuildingStorage;
	public DB6Storage<GarrBuildingPlotInstRecord> GarrBuildingPlotInstStorage;
	public DB6Storage<GarrClassSpecRecord> GarrClassSpecStorage;
	public DB6Storage<GarrFollowerRecord> GarrFollowerStorage;
	public DB6Storage<GarrFollowerXAbilityRecord> GarrFollowerXAbilityStorage;
	public DB6Storage<GarrMissionRecord> GarrMissionStorage;
	public DB6Storage<GarrPlotBuildingRecord> GarrPlotBuildingStorage;
	public DB6Storage<GarrPlotRecord> GarrPlotStorage;
	public DB6Storage<GarrPlotInstanceRecord> GarrPlotInstanceStorage;
	public DB6Storage<GarrSiteLevelRecord> GarrSiteLevelStorage;
	public DB6Storage<GarrSiteLevelPlotInstRecord> GarrSiteLevelPlotInstStorage;
	public DB6Storage<GarrTalentTreeRecord> GarrTalentTreeStorage;
	public DB6Storage<GemPropertiesRecord> GemPropertiesStorage;
	public DB6Storage<GlobalCurveRecord> GlobalCurveStorage;
	public DB6Storage<GlyphBindableSpellRecord> GlyphBindableSpellStorage;
	public DB6Storage<GlyphPropertiesRecord> GlyphPropertiesStorage;
	public DB6Storage<GlyphRequiredSpecRecord> GlyphRequiredSpecStorage;
	public DB6Storage<GossipNPCOptionRecord> GossipNPCOptionStorage;
	public DB6Storage<GuildColorBackgroundRecord> GuildColorBackgroundStorage;
	public DB6Storage<GuildColorBorderRecord> GuildColorBorderStorage;
	public DB6Storage<GuildColorEmblemRecord> GuildColorEmblemStorage;
	public DB6Storage<GuildPerkSpellsRecord> GuildPerkSpellsStorage;
	public DB6Storage<HeirloomRecord> HeirloomStorage;
	public DB6Storage<HolidaysRecord> HolidaysStorage;
	public DB6Storage<ImportPriceArmorRecord> ImportPriceArmorStorage;
	public DB6Storage<ImportPriceQualityRecord> ImportPriceQualityStorage;
	public DB6Storage<ImportPriceShieldRecord> ImportPriceShieldStorage;
	public DB6Storage<ImportPriceWeaponRecord> ImportPriceWeaponStorage;
	public DB6Storage<ItemAppearanceRecord> ItemAppearanceStorage;
	public DB6Storage<ItemArmorQualityRecord> ItemArmorQualityStorage;
	public DB6Storage<ItemArmorShieldRecord> ItemArmorShieldStorage;

	public DB6Storage<ItemArmorTotalRecord> ItemArmorTotalStorage;

	//public DB6Storage<ItemBagFamilyRecord> ItemBagFamilyStorage;
	public DB6Storage<ItemBonusRecord> ItemBonusStorage;
	public DB6Storage<ItemBonusListLevelDeltaRecord> ItemBonusListLevelDeltaStorage;
	public DB6Storage<ItemBonusTreeNodeRecord> ItemBonusTreeNodeStorage;
	public DB6Storage<ItemClassRecord> ItemClassStorage;
	public DB6Storage<ItemChildEquipmentRecord> ItemChildEquipmentStorage;
	public DB6Storage<ItemCurrencyCostRecord> ItemCurrencyCostStorage;
	public DB6Storage<ItemDamageRecord> ItemDamageAmmoStorage;
	public DB6Storage<ItemDamageRecord> ItemDamageOneHandStorage;
	public DB6Storage<ItemDamageRecord> ItemDamageOneHandCasterStorage;
	public DB6Storage<ItemDamageRecord> ItemDamageTwoHandStorage;
	public DB6Storage<ItemDamageRecord> ItemDamageTwoHandCasterStorage;
	public DB6Storage<ItemDisenchantLootRecord> ItemDisenchantLootStorage;
	public DB6Storage<ItemEffectRecord> ItemEffectStorage;
	public DB6Storage<ItemRecord> ItemStorage;
	public DB6Storage<ItemExtendedCostRecord> ItemExtendedCostStorage;
	public DB6Storage<ItemLevelSelectorRecord> ItemLevelSelectorStorage;
	public DB6Storage<ItemLevelSelectorQualityRecord> ItemLevelSelectorQualityStorage;
	public DB6Storage<ItemLevelSelectorQualitySetRecord> ItemLevelSelectorQualitySetStorage;
	public DB6Storage<ItemLimitCategoryRecord> ItemLimitCategoryStorage;
	public DB6Storage<ItemLimitCategoryConditionRecord> ItemLimitCategoryConditionStorage;
	public DB6Storage<ItemModifiedAppearanceRecord> ItemModifiedAppearanceStorage;
	public DB6Storage<ItemModifiedAppearanceExtraRecord> ItemModifiedAppearanceExtraStorage;
	public DB6Storage<ItemNameDescriptionRecord> ItemNameDescriptionStorage;
	public DB6Storage<ItemPriceBaseRecord> ItemPriceBaseStorage;
	public DB6Storage<ItemSearchNameRecord> ItemSearchNameStorage;
	public DB6Storage<ItemSetRecord> ItemSetStorage;
	public DB6Storage<ItemSetSpellRecord> ItemSetSpellStorage;
	public DB6Storage<ItemSparseRecord> ItemSparseStorage;
	public DB6Storage<ItemSpecRecord> ItemSpecStorage;
	public DB6Storage<ItemSpecOverrideRecord> ItemSpecOverrideStorage;
	public DB6Storage<ItemXBonusTreeRecord> ItemXBonusTreeStorage;
	public DB6Storage<ItemXItemEffectRecord> ItemXItemEffectStorage;
	public DB6Storage<JournalEncounterRecord> JournalEncounterStorage;
	public DB6Storage<JournalEncounterSectionRecord> JournalEncounterSectionStorage;
	public DB6Storage<JournalInstanceRecord> JournalInstanceStorage;

	public DB6Storage<JournalTierRecord> JournalTierStorage;

	//public DB6Storage<KeyChainRecord> KeyChainStorage;
	public DB6Storage<KeystoneAffixRecord> KeystoneAffixStorage;
	public DB6Storage<LanguageWordsRecord> LanguageWordsStorage;
	public DB6Storage<LanguagesRecord> LanguagesStorage;
	public DB6Storage<LFGDungeonsRecord> LFGDungeonsStorage;
	public DB6Storage<LightRecord> LightStorage;
	public DB6Storage<LiquidTypeRecord> LiquidTypeStorage;
	public DB6Storage<LockRecord> LockStorage;
	public DB6Storage<MailTemplateRecord> MailTemplateStorage;
	public DB6Storage<MapRecord> MapStorage;
	public DB6Storage<MapChallengeModeRecord> MapChallengeModeStorage;
	public DB6Storage<MapDifficultyRecord> MapDifficultyStorage;
	public DB6Storage<MapDifficultyXConditionRecord> MapDifficultyXConditionStorage;
	public DB6Storage<MawPowerRecord> MawPowerStorage;
	public DB6Storage<ModifierTreeRecord> ModifierTreeStorage;
	public DB6Storage<MountCapabilityRecord> MountCapabilityStorage;
	public DB6Storage<MountRecord> MountStorage;
	public DB6Storage<MountTypeXCapabilityRecord> MountTypeXCapabilityStorage;
	public DB6Storage<MountXDisplayRecord> MountXDisplayStorage;
	public DB6Storage<MovieRecord> MovieStorage;
	public DB6Storage<NameGenRecord> NameGenStorage;
	public DB6Storage<NamesProfanityRecord> NamesProfanityStorage;
	public DB6Storage<NamesReservedRecord> NamesReservedStorage;
	public DB6Storage<NamesReservedLocaleRecord> NamesReservedLocaleStorage;
	public DB6Storage<NumTalentsAtLevelRecord> NumTalentsAtLevelStorage;
	public DB6Storage<OverrideSpellDataRecord> OverrideSpellDataStorage;
	public DB6Storage<ParagonReputationRecord> ParagonReputationStorage;
	public DB6Storage<PhaseRecord> PhaseStorage;
	public DB6Storage<PhaseXPhaseGroupRecord> PhaseXPhaseGroupStorage;
	public DB6Storage<PlayerConditionRecord> PlayerConditionStorage;
	public DB6Storage<PowerDisplayRecord> PowerDisplayStorage;
	public DB6Storage<PowerTypeRecord> PowerTypeStorage;
	public DB6Storage<PrestigeLevelInfoRecord> PrestigeLevelInfoStorage;
	public DB6Storage<PvpDifficultyRecord> PvpDifficultyStorage;
	public DB6Storage<PvpItemRecord> PvpItemStorage;
	public DB6Storage<PvpTalentRecord> PvpTalentStorage;
	public DB6Storage<PvpTalentCategoryRecord> PvpTalentCategoryStorage;
	public DB6Storage<PvpTalentSlotUnlockRecord> PvpTalentSlotUnlockStorage;
	public DB6Storage<PvpTierRecord> PvpTierStorage;
	public DB6Storage<QuestFactionRewardRecord> QuestFactionRewardStorage;
	public DB6Storage<QuestInfoRecord> QuestInfoStorage;
	public DB6Storage<QuestPOIBlobEntry> QuestPOIBlobStorage;
	public DB6Storage<QuestPOIPointEntry> QuestPOIPointStorage;
	public DB6Storage<QuestLineXQuestRecord> QuestLineXQuestStorage;
	public DB6Storage<QuestMoneyRewardRecord> QuestMoneyRewardStorage;
	public DB6Storage<QuestPackageItemRecord> QuestPackageItemStorage;
	public DB6Storage<QuestSortRecord> QuestSortStorage;
	public DB6Storage<QuestV2Record> QuestV2Storage;
	public DB6Storage<QuestXPRecord> QuestXPStorage;
	public DB6Storage<RandPropPointsRecord> RandPropPointsStorage;
	public DB6Storage<RewardPackRecord> RewardPackStorage;
	public DB6Storage<RewardPackXCurrencyTypeRecord> RewardPackXCurrencyTypeStorage;
	public DB6Storage<RewardPackXItemRecord> RewardPackXItemStorage;
	public DB6Storage<ScenarioRecord> ScenarioStorage;
	public DB6Storage<ScenarioStepRecord> ScenarioStepStorage;
	public DB6Storage<SceneScriptRecord> SceneScriptStorage;
	public DB6Storage<SceneScriptGlobalTextRecord> SceneScriptGlobalTextStorage;
	public DB6Storage<SceneScriptPackageRecord> SceneScriptPackageStorage;
	public DB6Storage<SceneScriptTextRecord> SceneScriptTextStorage;
	public DB6Storage<SkillLineRecord> SkillLineStorage;
	public DB6Storage<SkillLineAbilityRecord> SkillLineAbilityStorage;
	public DB6Storage<SkillLineXTraitTreeRecord> SkillLineXTraitTreeStorage;
	public DB6Storage<SkillRaceClassInfoRecord> SkillRaceClassInfoStorage;
	public DB6Storage<SoulbindConduitRankRecord> SoulbindConduitRankStorage;
	public DB6Storage<SoundKitRecord> SoundKitStorage;
	public DB6Storage<SpecializationSpellsRecord> SpecializationSpellsStorage;
	public DB6Storage<SpecSetMemberRecord> SpecSetMemberStorage;
	public DB6Storage<SpellRecord> SpellStorage;
	public DB6Storage<SpellAuraOptionsRecord> SpellAuraOptionsStorage;
	public DB6Storage<SpellAuraRestrictionsRecord> SpellAuraRestrictionsStorage;
	public DB6Storage<SpellCastTimesRecord> SpellCastTimesStorage;
	public DB6Storage<SpellCastingRequirementsRecord> SpellCastingRequirementsStorage;
	public DB6Storage<SpellCategoriesRecord> SpellCategoriesStorage;
	public DB6Storage<SpellCategoryRecord> SpellCategoryStorage;
	public DB6Storage<SpellClassOptionsRecord> SpellClassOptionsStorage;
	public DB6Storage<SpellCooldownsRecord> SpellCooldownsStorage;
	public DB6Storage<SpellDurationRecord> SpellDurationStorage;
	public DB6Storage<SpellEffectRecord> SpellEffectStorage;
	public DB6Storage<SpellEmpowerRecord> SpellEmpowerStorage;
	public DB6Storage<SpellEmpowerStageRecord> SpellEmpowerStageStorage;
	public DB6Storage<SpellEquippedItemsRecord> SpellEquippedItemsStorage;
	public DB6Storage<SpellFocusObjectRecord> SpellFocusObjectStorage;
	public DB6Storage<SpellInterruptsRecord> SpellInterruptsStorage;
	public DB6Storage<SpellItemEnchantmentRecord> SpellItemEnchantmentStorage;
	public DB6Storage<SpellItemEnchantmentConditionRecord> SpellItemEnchantmentConditionStorage;
	public DB6Storage<SpellKeyboundOverrideRecord> SpellKeyboundOverrideStorage;
	public DB6Storage<SpellLabelRecord> SpellLabelStorage;
	public DB6Storage<SpellLearnSpellRecord> SpellLearnSpellStorage;
	public DB6Storage<SpellLevelsRecord> SpellLevelsStorage;
	public DB6Storage<SpellMiscRecord> SpellMiscStorage;
	public DB6Storage<SpellNameRecord> SpellNameStorage;
	public DB6Storage<SpellPowerRecord> SpellPowerStorage;
	public DB6Storage<SpellPowerDifficultyRecord> SpellPowerDifficultyStorage;
	public DB6Storage<SpellProcsPerMinuteRecord> SpellProcsPerMinuteStorage;
	public DB6Storage<SpellProcsPerMinuteModRecord> SpellProcsPerMinuteModStorage;
	public DB6Storage<SpellRadiusRecord> SpellRadiusStorage;
	public DB6Storage<SpellRangeRecord> SpellRangeStorage;
	public DB6Storage<SpellReagentsRecord> SpellReagentsStorage;
	public DB6Storage<SpellReagentsCurrencyRecord> SpellReagentsCurrencyStorage;
	public DB6Storage<SpellReplacementRecord> SpellReplacementStorage;
	public DB6Storage<SpellScalingRecord> SpellScalingStorage;
	public DB6Storage<SpellShapeshiftRecord> SpellShapeshiftStorage;
	public DB6Storage<SpellShapeshiftFormRecord> SpellShapeshiftFormStorage;
	public DB6Storage<SpellTargetRestrictionsRecord> SpellTargetRestrictionsStorage;
	public DB6Storage<SpellTotemsRecord> SpellTotemsStorage;
	public DB6Storage<SpellVisualRecord> SpellVisualStorage;
	public DB6Storage<SpellVisualEffectNameRecord> SpellVisualEffectNameStorage;
	public DB6Storage<SpellVisualMissileRecord> SpellVisualMissileStorage;
	public DB6Storage<SpellVisualKitRecord> SpellVisualKitStorage;
	public DB6Storage<SpellXSpellVisualRecord> SpellXSpellVisualStorage;
	public DB6Storage<SummonPropertiesRecord> SummonPropertiesStorage;
	public DB6Storage<TactKeyRecord> TactKeyStorage;
	public DB6Storage<TalentRecord> TalentStorage;
	public DB6Storage<TaxiNodesRecord> TaxiNodesStorage;
	public DB6Storage<TaxiPathRecord> TaxiPathStorage;
	public DB6Storage<TaxiPathNodeRecord> TaxiPathNodeStorage;
	public DB6Storage<TotemCategoryRecord> TotemCategoryStorage;
	public DB6Storage<ToyRecord> ToyStorage;
	public DB6Storage<TraitCondRecord> TraitCondStorage;
	public DB6Storage<TraitCostRecord> TraitCostStorage;
	public DB6Storage<TraitCurrencyRecord> TraitCurrencyStorage;
	public DB6Storage<TraitCurrencySourceRecord> TraitCurrencySourceStorage;
	public DB6Storage<TraitDefinitionRecord> TraitDefinitionStorage;
	public DB6Storage<TraitDefinitionEffectPointsRecord> TraitDefinitionEffectPointsStorage;
	public DB6Storage<TraitEdgeRecord> TraitEdgeStorage;
	public DB6Storage<TraitNodeRecord> TraitNodeStorage;
	public DB6Storage<TraitNodeEntryRecord> TraitNodeEntryStorage;
	public DB6Storage<TraitNodeEntryXTraitCondRecord> TraitNodeEntryXTraitCondStorage;
	public DB6Storage<TraitNodeEntryXTraitCostRecord> TraitNodeEntryXTraitCostStorage;
	public DB6Storage<TraitNodeGroupRecord> TraitNodeGroupStorage;
	public DB6Storage<TraitNodeGroupXTraitCondRecord> TraitNodeGroupXTraitCondStorage;
	public DB6Storage<TraitNodeGroupXTraitCostRecord> TraitNodeGroupXTraitCostStorage;
	public DB6Storage<TraitNodeGroupXTraitNodeRecord> TraitNodeGroupXTraitNodeStorage;
	public DB6Storage<TraitNodeXTraitCondRecord> TraitNodeXTraitCondStorage;
	public DB6Storage<TraitNodeXTraitCostRecord> TraitNodeXTraitCostStorage;
	public DB6Storage<TraitNodeXTraitNodeEntryRecord> TraitNodeXTraitNodeEntryStorage;
	public DB6Storage<TraitSystemRecord> TraitSystemStorage;
	public DB6Storage<TraitTreeRecord> TraitTreeStorage;
	public DB6Storage<TraitTreeLoadoutRecord> TraitTreeLoadoutStorage;
	public DB6Storage<TraitTreeLoadoutEntryRecord> TraitTreeLoadoutEntryStorage;
	public DB6Storage<TraitTreeXTraitCostRecord> TraitTreeXTraitCostStorage;
	public DB6Storage<TraitTreeXTraitCurrencyRecord> TraitTreeXTraitCurrencyStorage;
	public DB6Storage<TransmogHolidayRecord> TransmogHolidayStorage;
	public DB6Storage<TransmogIllusionRecord> TransmogIllusionStorage;
	public DB6Storage<TransmogSetRecord> TransmogSetStorage;
	public DB6Storage<TransmogSetGroupRecord> TransmogSetGroupStorage;
	public DB6Storage<TransmogSetItemRecord> TransmogSetItemStorage;
	public DB6Storage<TransportAnimationRecord> TransportAnimationStorage;
	public DB6Storage<TransportRotationRecord> TransportRotationStorage;
	public DB6Storage<UiMapRecord> UiMapStorage;
	public DB6Storage<UiMapAssignmentRecord> UiMapAssignmentStorage;
	public DB6Storage<UiMapLinkRecord> UiMapLinkStorage;
	public DB6Storage<UiMapXMapArtRecord> UiMapXMapArtStorage;
	public DB6Storage<UISplashScreenRecord> UISplashScreenStorage;
	public DB6Storage<UnitConditionRecord> UnitConditionStorage;
	public DB6Storage<UnitPowerBarRecord> UnitPowerBarStorage;
	public DB6Storage<VehicleRecord> VehicleStorage;
	public DB6Storage<VehicleSeatRecord> VehicleSeatStorage;
	public DB6Storage<WMOAreaTableRecord> WMOAreaTableStorage;
	public DB6Storage<WorldEffectRecord> WorldEffectStorage;
	public DB6Storage<WorldMapOverlayRecord> WorldMapOverlayStorage;
	public DB6Storage<WorldStateExpressionRecord> WorldStateExpressionStorage;
	public DB6Storage<CharBaseInfo> CharBaseInfoStorage;

	#endregion

	#region GameTables

	public GameTable<GtArtifactKnowledgeMultiplierRecord> ArtifactKnowledgeMultiplierGameTable;
	public GameTable<GtArtifactLevelXPRecord> ArtifactLevelXPGameTable;
	public GameTable<GtBarberShopCostBaseRecord> BarberShopCostBaseGameTable;
	public GameTable<GtBaseMPRecord> BaseMPGameTable;
	public GameTable<GtBattlePetXPRecord> BattlePetXPGameTable;
	public GameTable<GtCombatRatingsRecord> CombatRatingsGameTable;
	public GameTable<GtGenericMultByILvlRecord> CombatRatingsMultByILvlGameTable;
	public GameTable<GtHpPerStaRecord> HpPerStaGameTable;
	public GameTable<GtItemSocketCostPerLevelRecord> ItemSocketCostPerLevelGameTable;
	public GameTable<GtNpcManaCostScalerRecord> NpcManaCostScalerGameTable;
	public GameTable<GtSpellScalingRecord> SpellScalingGameTable;
	public GameTable<GtGenericMultByILvlRecord> StaminaMultByILvlGameTable;
	public GameTable<GtXpRecord> XpGameTable;

	#endregion

	#region Taxi Collections

	public byte[] TaxiNodesMask;
	public byte[] OldContinentsNodesMask;
	public byte[] HordeTaxiNodesMask;
	public byte[] AllianceTaxiNodesMask;
	public Dictionary<uint, Dictionary<uint, TaxiPathBySourceAndDestination>> TaxiPathSetBySource = new();
	public Dictionary<uint, TaxiPathNodeRecord[]> TaxiPathNodesByPath = new();

	#endregion

	#region Helper Methods

	public static float GetGameTableColumnForClass(dynamic row, PlayerClass class_)
	{
		switch (class_)
		{
			case PlayerClass.Warrior:
				return row.Warrior;
			case PlayerClass.Paladin:
				return row.Paladin;
			case PlayerClass.Hunter:
				return row.Hunter;
			case PlayerClass.Rogue:
				return row.Rogue;
			case PlayerClass.Priest:
				return row.Priest;
			case PlayerClass.Deathknight:
				return row.DeathKnight;
			case PlayerClass.Shaman:
				return row.Shaman;
			case PlayerClass.Mage:
				return row.Mage;
			case PlayerClass.Warlock:
				return row.Warlock;
			case PlayerClass.Monk:
				return row.Monk;
			case PlayerClass.Druid:
				return row.Druid;
			case PlayerClass.DemonHunter:
				return row.DemonHunter;
			case PlayerClass.Evoker:
				return row.Evoker;
			case PlayerClass.Adventurer:
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
			case (int)PlayerClass.Warrior:
				return row.Warrior;
			case (int)PlayerClass.Paladin:
				return row.Paladin;
			case (int)PlayerClass.Hunter:
				return row.Hunter;
			case (int)PlayerClass.Rogue:
				return row.Rogue;
			case (int)PlayerClass.Priest:
				return row.Priest;
			case (int)PlayerClass.Deathknight:
				return row.DeathKnight;
			case (int)PlayerClass.Shaman:
				return row.Shaman;
			case (int)PlayerClass.Mage:
				return row.Mage;
			case (int)PlayerClass.Warlock:
				return row.Warlock;
			case (int)PlayerClass.Monk:
				return row.Monk;
			case (int)PlayerClass.Druid:
				return row.Druid;
			case (int)PlayerClass.DemonHunter:
				return row.DemonHunter;
			case (int)PlayerClass.Evoker:
				return row.Evoker;
			case (int)PlayerClass.Adventurer:
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