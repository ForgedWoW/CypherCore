// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Framework.Database;
using Framework.IO;
using Forged.RealmServer.Entities;
using Forged.RealmServer.Loots;
using Forged.RealmServer.Spells;
using Forged.RealmServer.Entities.Items;

namespace Forged.RealmServer.Chat;

[CommandGroup("reload")]
class ReloadCommand
{
	[Command("access_requirement", RBACPermissions.CommandReloadAccessRequirement, true)]
	static bool HandleReloadAccessRequirementCommand(CommandHandler handler)
	{
		Log.Logger.Information("Re-Loading Access Requirement definitions...");
		_gameObjectManager.LoadAccessRequirements();
		handler.SendGlobalGMSysMessage("DB table `access_requirement` reloaded.");

		return true;
	}

	[Command("achievement_reward", RBACPermissions.CommandReloadAchievementReward, true)]
	static bool HandleReloadAchievementRewardCommand(CommandHandler handler)
	{
		Log.Logger.Information("Re-Loading Achievement Reward Data...");
		_achievementManager.LoadRewards();
		handler.SendGlobalGMSysMessage("DB table `achievement_reward` reloaded.");

		return true;
	}

	[Command("areatrigger_involvedrelation", RBACPermissions.CommandReloadAreatriggerInvolvedrelation, true)]
	static bool HandleReloadQuestAreaTriggersCommand(CommandHandler handler)
	{
		Log.Logger.Information("Re-Loading Quest Area Triggers...");
		_gameObjectManager.LoadQuestAreaTriggers();
		handler.SendGlobalGMSysMessage("DB table `areatrigger_involvedrelation` (quest area triggers) reloaded.");

		return true;
	}

	[Command("areatrigger_tavern", RBACPermissions.CommandReloadAreatriggerTavern, true)]
	static bool HandleReloadAreaTriggerTavernCommand(CommandHandler handler)
	{
		Log.Logger.Information("Re-Loading Tavern Area Triggers...");
		_gameObjectManager.LoadTavernAreaTriggers();
		handler.SendGlobalGMSysMessage("DB table `areatrigger_tavern` reloaded.");

		return true;
	}

	[Command("areatrigger_teleport", RBACPermissions.CommandReloadAreatriggerTeleport, true)]
	static bool HandleReloadAreaTriggerTeleportCommand(CommandHandler handler)
	{
		Log.Logger.Information("Re-Loading AreaTrigger teleport definitions...");
		_gameObjectManager.LoadAreaTriggerTeleports();
		handler.SendGlobalGMSysMessage("DB table `areatrigger_teleport` reloaded.");

		return true;
	}

	[Command("areatrigger_template", RBACPermissions.CommandReloadSceneTemplate, true)]
	static bool HandleReloadAreaTriggerTemplateCommand(CommandHandler handler)
	{
		Log.Logger.Information("Reloading areatrigger_template table...");
		Global.AreaTriggerDataStorage.LoadAreaTriggerTemplates();
		handler.SendGlobalGMSysMessage("AreaTrigger templates reloaded. Already spawned AT won't be affected. New scriptname need a reboot.");

		return true;
	}

	[Command("auctions", RBACPermissions.CommandReloadAuctions, true)]
	static bool HandleReloadAuctionsCommand(CommandHandler handler)
	{
		// Reload dynamic data tables from the database
		Log.Logger.Information("Re-Loading Auctions...");
		Global.AuctionHouseMgr.LoadAuctions();
		handler.SendGlobalGMSysMessage("Auctions reloaded.");

		return true;
	}

	[Command("autobroadcast", RBACPermissions.CommandReloadAutobroadcast, true)]
	static bool HandleReloadAutobroadcastCommand(CommandHandler handler)
	{
		Log.Logger.Information("Re-Loading Autobroadcasts...");
		_worldManager.LoadAutobroadcasts();
		handler.SendGlobalGMSysMessage("DB table `autobroadcast` reloaded.");

		return true;
	}

	[Command("battleground_template", RBACPermissions.CommandReloadBattlegroundTemplate, true)]
	static bool HandleReloadBattlegroundTemplate(CommandHandler handler)
	{
		Log.Logger.Information("Re-Loading Battleground Templates...");
		Global.BattlegroundMgr.LoadBattlegroundTemplates();
		handler.SendGlobalGMSysMessage("DB table `battleground_template` reloaded.");

		return true;
	}

	[Command("character_template", RBACPermissions.CommandReloadCharacterTemplate, true)]
	static bool HandleReloadCharacterTemplate(CommandHandler handler)
	{
		Log.Logger.Information("Re-Loading Character Templates...");
		Global.CharacterTemplateDataStorage.LoadCharacterTemplates();
		handler.SendGlobalGMSysMessage("DB table `character_template` and `character_template_class` reloaded.");

		return true;
	}

	[Command("conditions", RBACPermissions.CommandReloadConditions, true)]
	static bool HandleReloadConditions(CommandHandler handler)
	{
		Log.Logger.Information("Re-Loading Conditions...");
		_conditionManager.LoadConditions(true);
		handler.SendGlobalGMSysMessage("Conditions reloaded.");

		return true;
	}

	[Command("config", RBACPermissions.CommandReloadConfig, true)]
	static bool HandleReloadConfigCommand(CommandHandler handler)
	{
		Log.Logger.Information("Re-Loading config settings...");
		_worldManager.LoadConfigSettings(true);
		Global.MapMgr.InitializeVisibilityDistanceInfo();
		handler.SendGlobalGMSysMessage("World config settings reloaded.");

		return true;
	}

	[Command("conversation_template", RBACPermissions.CommandReloadConversationTemplate, true)]
	static bool HandleReloadConversationTemplateCommand(CommandHandler handler)
	{
		Log.Logger.Information("Reloading conversation_* tables...");
		Global.ConversationDataStorage.LoadConversationTemplates();
		handler.SendGlobalGMSysMessage("Conversation templates reloaded.");

		return true;
	}

	[Command("creature_linked_respawn", RBACPermissions.CommandReloadCreatureLinkedRespawn, true)]
	static bool HandleReloadLinkedRespawnCommand(CommandHandler handler)
	{
		Log.Logger.Information("Loading Linked Respawns... (`creature_linked_respawn`)");
		_gameObjectManager.LoadLinkedRespawn();
		handler.SendGlobalGMSysMessage("DB table `creature_linked_respawn` (creature linked respawns) reloaded.");

		return true;
	}

	[Command("creature_loot_template", RBACPermissions.CommandReloadCreatureLootTemplate, true)]
	static bool HandleReloadLootTemplatesCreatureCommand(CommandHandler handler)
	{
		Log.Logger.Information("Re-Loading Loot Tables... (`creature_loot_template`)");
		LootManager.LoadLootTemplates_Creature();
		LootStorage.Creature.CheckLootRefs();
		handler.SendGlobalGMSysMessage("DB table `creature_loot_template` reloaded.");
		_conditionManager.LoadConditions(true);

		return true;
	}

	[Command("creature_movement_override", RBACPermissions.CommandReloadCreatureMovementOverride, true)]
	static bool HandleReloadCreatureMovementOverrideCommand(CommandHandler handler)
	{
		Log.Logger.Information("Re-Loading Creature movement overrides...");
		_gameObjectManager.LoadCreatureMovementOverrides();
		handler.SendGlobalGMSysMessage("DB table `creature_movement_override` reloaded.");

		return true;
	}

	[Command("creature_onkill_reputation", RBACPermissions.CommandReloadCreatureOnkillReputation, true)]
	static bool HandleReloadOnKillReputationCommand(CommandHandler handler)
	{
		Log.Logger.Information("Re-Loading creature award reputation definitions...");
		_gameObjectManager.LoadReputationOnKill();
		handler.SendGlobalGMSysMessage("DB table `creature_onkill_reputation` reloaded.");

		return true;
	}

	[Command("creature_questender", RBACPermissions.CommandReloadCreatureQuestender, true)]
	static bool HandleReloadCreatureQuestEnderCommand(CommandHandler handler)
	{
		Log.Logger.Information("Loading Quests Relations... (`creature_questender`)");
		_gameObjectManager.LoadCreatureQuestEnders();
		handler.SendGlobalGMSysMessage("DB table `creature_questender` reloaded.");

		return true;
	}

	[Command("creature_queststarter", RBACPermissions.CommandReloadCreatureQueststarter, true)]
	static bool HandleReloadCreatureQuestStarterCommand(CommandHandler handler)
	{
		Log.Logger.Information("Loading Quests Relations... (`creature_queststarter`)");
		_gameObjectManager.LoadCreatureQuestStarters();
		handler.SendGlobalGMSysMessage("DB table `creature_queststarter` reloaded.");

		return true;
	}

	[Command("creature_summon_groups", RBACPermissions.CommandReloadCreatureSummonGroups, true)]
	static bool HandleReloadCreatureSummonGroupsCommand(CommandHandler handler)
	{
		Log.Logger.Information("Reloading creature summon groups...");
		_gameObjectManager.LoadTempSummons();
		handler.SendGlobalGMSysMessage("DB table `creature_summon_groups` reloaded.");

		return true;
	}

	[Command("creature_template", RBACPermissions.CommandReloadCreatureTemplate, true)]
	static bool HandleReloadCreatureTemplateCommand(CommandHandler handler, StringArguments args)
	{
		if (args.Empty())
			return false;

		uint entry;

		while ((entry = args.NextUInt32()) != 0)
		{
			var stmt = _worldDatabase.GetPreparedStatement(WorldStatements.SEL_CREATURE_TEMPLATE);
			stmt.AddValue(0, entry);
			stmt.AddValue(1, 0);
			var result = _worldDatabase.Query(stmt);

			if (result.IsEmpty())
			{
				handler.SendSysMessage(CypherStrings.CommandCreaturetemplateNotfound, entry);

				continue;
			}

			var cInfo = _gameObjectManager.GetCreatureTemplate(entry);

			if (cInfo == null)
			{
				handler.SendSysMessage(CypherStrings.CommandCreaturestorageNotfound, entry);

				continue;
			}

			Log.Logger.Information("Reloading creature template entry {0}", entry);

			_gameObjectManager.LoadCreatureTemplate(result.GetFields());
			_gameObjectManager.CheckCreatureTemplate(cInfo);
		}

		_gameObjectManager.InitializeQueriesData(QueryDataGroup.Creatures);
		handler.SendGlobalGMSysMessage("Creature template reloaded.");

		return true;
	}

	[Command("creature_text", RBACPermissions.CommandReloadCreatureText, true)]
	static bool HandleReloadCreatureText(CommandHandler handler)
	{
		Log.Logger.Information("Re-Loading Creature Texts...");
		_creatureTextManager.LoadCreatureTexts();
		handler.SendGlobalGMSysMessage("Creature Texts reloaded.");

		return true;
	}

	[Command("trinity_string", RBACPermissions.CommandReloadCypherString, true)]
	static bool HandleReloadCypherStringCommand(CommandHandler handler)
	{
		Log.Logger.Information("Re-Loading trinity_string Table!");
		_gameObjectManager.LoadCypherStrings();
		handler.SendGlobalGMSysMessage("DB table `trinity_string` reloaded.");

		return true;
	}

	[Command("criteria_data", RBACPermissions.CommandReloadCriteriaData, true)]
	static bool HandleReloadCriteriaDataCommand(CommandHandler handler)
	{
		Log.Logger.Information("Re-Loading Additional Criteria Data...");
		_criteriaManager.LoadCriteriaData();
		handler.SendGlobalGMSysMessage("DB table `criteria_data` reloaded.");

		return true;
	}

	[Command("disables", RBACPermissions.CommandReloadDisables, true)]
	static bool HandleReloadDisablesCommand(CommandHandler handler)
	{
		Log.Logger.Information("Re-Loading disables table...");
		Global.DisableMgr.LoadDisables();
		Log.Logger.Information("Checking quest disables...");
		Global.DisableMgr.CheckQuestDisables();
		handler.SendGlobalGMSysMessage("DB table `disables` reloaded.");

		return true;
	}

	[Command("disenchant_loot_template", RBACPermissions.CommandReloadDisenchantLootTemplate, true)]
	static bool HandleReloadLootTemplatesDisenchantCommand(CommandHandler handler)
	{
		Log.Logger.Information("Re-Loading Loot Tables... (`disenchant_loot_template`)");
		LootManager.LoadLootTemplates_Disenchant();
		LootStorage.Disenchant.CheckLootRefs();
		handler.SendGlobalGMSysMessage("DB table `disenchant_loot_template` reloaded.");
		_conditionManager.LoadConditions(true);

		return true;
	}

	[Command("event_scripts", RBACPermissions.CommandReloadEventScripts, true)]
	static bool HandleReloadEventScriptsCommand(CommandHandler handler, StringArguments args)
	{
		if (Global.MapMgr.IsScriptScheduled())
		{
			handler.SendSysMessage("DB scripts used currently, please attempt reload later.");

			return false;
		}

		if (args != null)
			Log.Logger.Information("Re-Loading Scripts from `event_scripts`...");

		_gameObjectManager.LoadEventScripts();

		if (args != null)
			handler.SendGlobalGMSysMessage("DB table `event_scripts` reloaded.");

		return true;
	}

	[Command("fishing_loot_template", RBACPermissions.CommandReloadFishingLootTemplate, true)]
	static bool HandleReloadLootTemplatesFishingCommand(CommandHandler handler)
	{
		Log.Logger.Information("Re-Loading Loot Tables... (`fishing_loot_template`)");
		LootManager.LoadLootTemplates_Fishing();
		LootStorage.Fishing.CheckLootRefs();
		handler.SendGlobalGMSysMessage("DB table `fishing_loot_template` reloaded.");
		_conditionManager.LoadConditions(true);

		return true;
	}

	[Command("graveyard_zone", RBACPermissions.CommandReloadGraveyardZone, true)]
	static bool HandleReloadGameGraveyardZoneCommand(CommandHandler handler)
	{
		Log.Logger.Information("Re-Loading Graveyard-zone links...");

		_gameObjectManager.LoadGraveyardZones();

		handler.SendGlobalGMSysMessage("DB table `game_graveyard_zone` reloaded.");

		return true;
	}

	[Command("game_tele", RBACPermissions.CommandReloadGameTele, true)]
	static bool HandleReloadGameTeleCommand(CommandHandler handler)
	{
		Log.Logger.Information("Re-Loading Game Tele coordinates...");

		_gameObjectManager.LoadGameTele();

		handler.SendGlobalGMSysMessage("DB table `game_tele` reloaded.");

		return true;
	}

	[Command("gameobject_loot_template", RBACPermissions.CommandReloadGameobjectQuestLootTemplate, true)]
	static bool HandleReloadLootTemplatesGameobjectCommand(CommandHandler handler)
	{
		Log.Logger.Information("Re-Loading Loot Tables... (`gameobject_loot_template`)");
		LootManager.LoadLootTemplates_Gameobject();
		LootStorage.Gameobject.CheckLootRefs();
		handler.SendGlobalGMSysMessage("DB table `gameobject_loot_template` reloaded.");
		_conditionManager.LoadConditions(true);

		return true;
	}

	[Command("gameobject_questender", RBACPermissions.CommandReloadGameobjectQuestender, true)]
	static bool HandleReloadGOQuestEnderCommand(CommandHandler handler)
	{
		Log.Logger.Information("Loading Quests Relations... (`gameobject_questender`)");
		_gameObjectManager.LoadGameobjectQuestEnders();
		handler.SendGlobalGMSysMessage("DB table `gameobject_questender` reloaded.");

		return true;
	}

	[Command("gameobject_queststarter", RBACPermissions.CommandReloadGameobjectQueststarter, true)]
	static bool HandleReloadGOQuestStarterCommand(CommandHandler handler)
	{
		Log.Logger.Information("Loading Quests Relations... (`gameobject_queststarter`)");
		_gameObjectManager.LoadGameobjectQuestStarters();
		handler.SendGlobalGMSysMessage("DB table `gameobject_queststarter` reloaded.");

		return true;
	}

	[Command("gossip_menu", RBACPermissions.CommandReloadGossipMenu, true)]
	static bool HandleReloadGossipMenuCommand(CommandHandler handler)
	{
		Log.Logger.Information("Re-Loading `gossip_menu` Table!");
		_gameObjectManager.LoadGossipMenu();
		handler.SendGlobalGMSysMessage("DB table `gossip_menu` reloaded.");
		_conditionManager.LoadConditions(true);

		return true;
	}

	[Command("gossip_menu_option", RBACPermissions.CommandReloadGossipMenuOption, true)]
	static bool HandleReloadGossipMenuOptionCommand(CommandHandler handler)
	{
		Log.Logger.Information("Re-Loading `gossip_menu_option` Table!");
		_gameObjectManager.LoadGossipMenuItems();
		handler.SendGlobalGMSysMessage("DB table `gossip_menu_option` reloaded.");
		_conditionManager.LoadConditions(true);

		return true;
	}

	[Command("item_random_bonus_list_template", RBACPermissions.CommandReloadItemRandomBonusListTemplate, true)]
	static bool HandleReloadItemRandomBonusListTemplatesCommand(CommandHandler handler)
	{
		Log.Logger.Information("Re-Loading Random item bonus list definitions...");
		ItemEnchantmentManager.LoadItemRandomBonusListTemplates();
		handler.SendGlobalGMSysMessage("DB table `item_random_bonus_list_template` reloaded.");

		return true;
	}

	[Command("item_loot_template", RBACPermissions.CommandReloadItemLootTemplate, true)]
	static bool HandleReloadLootTemplatesItemCommand(CommandHandler handler)
	{
		Log.Logger.Information("Re-Loading Loot Tables... (`item_loot_template`)");
		LootManager.LoadLootTemplates_Item();
		LootStorage.Items.CheckLootRefs();
		handler.SendGlobalGMSysMessage("DB table `item_loot_template` reloaded.");
		_conditionManager.LoadConditions(true);

		return true;
	}

	[Command("lfg_dungeon_rewards", RBACPermissions.CommandReloadLfgDungeonRewards, true)]
	static bool HandleReloadLfgRewardsCommand(CommandHandler handler)
	{
		Log.Logger.Information("Re-Loading lfg dungeon rewards...");
		_lFGManager.LoadRewards();
		handler.SendGlobalGMSysMessage("DB table `lfg_dungeon_rewards` reloaded.");

		return true;
	}

	[Command("achievement_reward_locale", RBACPermissions.CommandReloadAchievementRewardLocale, true)]
	static bool HandleReloadAchievementRewardLocaleCommand(CommandHandler handler)
	{
		Log.Logger.Information("Re-Loading Achievement Reward Data Locale...");
		_achievementManager.LoadRewardLocales();
		handler.SendGlobalGMSysMessage("DB table `achievement_reward_locale` reloaded.");

		return true;
	}

	[Command("creature_template_locale", RBACPermissions.CommandReloadCreatureTemplateLocale, true)]
	static bool HandleReloadCreatureTemplateLocaleCommand(CommandHandler handler)
	{
		Log.Logger.Information("Re-Loading Creature Template Locale...");
		_gameObjectManager.LoadCreatureLocales();
		handler.SendGlobalGMSysMessage("DB table `Creature Template Locale` reloaded.");

		return true;
	}

	[Command("creature_text_locale", RBACPermissions.CommandReloadCreatureTextLocale, true)]
	static bool HandleReloadCreatureTextLocaleCommand(CommandHandler handler)
	{
		Log.Logger.Information("Re-Loading Creature Texts Locale...");
		_creatureTextManager.LoadCreatureTextLocales();
		handler.SendGlobalGMSysMessage("DB table `creature_text_locale` reloaded.");

		return true;
	}

	[Command("gameobject_template_locale", RBACPermissions.CommandReloadGameobjectTemplateLocale, true)]
	static bool HandleReloadGameobjectTemplateLocaleCommand(CommandHandler handler)
	{
		Log.Logger.Information("Re-Loading Gameobject Template Locale... ");
		_gameObjectManager.LoadGameObjectLocales();
		handler.SendGlobalGMSysMessage("DB table `gameobject_template_locale` reloaded.");

		return true;
	}

	[Command("gossip_menu_option_locale", RBACPermissions.CommandReloadGossipMenuOptionLocale, true)]
	static bool HandleReloadGossipMenuOptionLocaleCommand(CommandHandler handler)
	{
		Log.Logger.Information("Re-Loading Gossip Menu Option Locale... ");
		_gameObjectManager.LoadGossipMenuItemsLocales();
		handler.SendGlobalGMSysMessage("DB table `gossip_menu_option_locale` reloaded.");

		return true;
	}

	[Command("page_text_locale", RBACPermissions.CommandReloadPageTextLocale, true)]
	static bool HandleReloadPageTextLocaleCommand(CommandHandler handler)
	{
		Log.Logger.Information("Re-Loading Page Text Locale... ");
		_gameObjectManager.LoadPageTextLocales();
		handler.SendGlobalGMSysMessage("DB table `page_text_locale` reloaded.");

		return true;
	}

	[Command("points_of_interest_locale", RBACPermissions.CommandReloadPointsOfInterestLocale, true)]
	static bool HandleReloadPointsOfInterestLocaleCommand(CommandHandler handler)
	{
		Log.Logger.Information("Re-Loading Points Of Interest Locale... ");
		_gameObjectManager.LoadPointOfInterestLocales();
		handler.SendGlobalGMSysMessage("DB table `points_of_interest_locale` reloaded.");

		return true;
	}

	[Command("mail_level_reward", RBACPermissions.CommandReloadMailLevelReward, true)]
	static bool HandleReloadMailLevelRewardCommand(CommandHandler handler)
	{
		Log.Logger.Information("Re-Loading Player level dependent mail rewards...");
		_gameObjectManager.LoadMailLevelRewards();
		handler.SendGlobalGMSysMessage("DB table `mail_level_reward` reloaded.");

		return true;
	}

	[Command("mail_loot_template", RBACPermissions.CommandReloadMailLootTemplate, true)]
	static bool HandleReloadLootTemplatesMailCommand(CommandHandler handler)
	{
		Log.Logger.Information("Re-Loading Loot Tables... (`mail_loot_template`)");
		LootManager.LoadLootTemplates_Mail();
		LootStorage.Mail.CheckLootRefs();
		handler.SendGlobalGMSysMessage("DB table `mail_loot_template` reloaded.");
		_conditionManager.LoadConditions(true);

		return true;
	}

	[Command("milling_loot_template", RBACPermissions.CommandReloadMillingLootTemplate, true)]
	static bool HandleReloadLootTemplatesMillingCommand(CommandHandler handler)
	{
		Log.Logger.Information("Re-Loading Loot Tables... (`milling_loot_template`)");
		LootManager.LoadLootTemplates_Milling();
		LootStorage.Milling.CheckLootRefs();
		handler.SendGlobalGMSysMessage("DB table `milling_loot_template` reloaded.");
		_conditionManager.LoadConditions(true);

		return true;
	}

	[Command("npc_spellclick_spells", RBACPermissions.CommandReloadNpcSpellclickSpells, true)]
	static bool HandleReloadSpellClickSpellsCommand(CommandHandler handler)
	{
		Log.Logger.Information("Re-Loading `npc_spellclick_spells` Table!");
		_gameObjectManager.LoadNPCSpellClickSpells();
		handler.SendGlobalGMSysMessage("DB table `npc_spellclick_spells` reloaded.");

		return true;
	}

	[Command("npc_vendor", RBACPermissions.CommandReloadNpcVendor, true)]
	static bool HandleReloadNpcVendorCommand(CommandHandler handler)
	{
		Log.Logger.Information("Re-Loading `npc_vendor` Table!");
		_gameObjectManager.LoadVendors();
		handler.SendGlobalGMSysMessage("DB table `npc_vendor` reloaded.");

		return true;
	}

	[Command("page_text", RBACPermissions.CommandReloadPageText, true)]
	static bool HandleReloadPageTextsCommand(CommandHandler handler)
	{
		Log.Logger.Information("Re-Loading Page Text...");
		_gameObjectManager.LoadPageTexts();
		handler.SendGlobalGMSysMessage("DB table `page_text` reloaded.");

		return true;
	}

	[Command("pickpocketing_loot_template", RBACPermissions.CommandReloadPickpocketingLootTemplate, true)]
	static bool HandleReloadLootTemplatesPickpocketingCommand(CommandHandler handler)
	{
		Log.Logger.Information("Re-Loading Loot Tables... (`pickpocketing_loot_template`)");
		LootManager.LoadLootTemplates_Pickpocketing();
		LootStorage.Pickpocketing.CheckLootRefs();
		handler.SendGlobalGMSysMessage("DB table `pickpocketing_loot_template` reloaded.");
		_conditionManager.LoadConditions(true);

		return true;
	}

	[Command("points_of_interest", RBACPermissions.CommandReloadPointsOfInterest, true)]
	static bool HandleReloadPointsOfInterestCommand(CommandHandler handler)
	{
		Log.Logger.Information("Re-Loading `points_of_interest` Table!");
		_gameObjectManager.LoadPointsOfInterest();
		handler.SendGlobalGMSysMessage("DB table `points_of_interest` reloaded.");

		return true;
	}

	[Command("prospecting_loot_template", RBACPermissions.CommandReloadProspectingLootTemplate, true)]
	static bool HandleReloadLootTemplatesProspectingCommand(CommandHandler handler)
	{
		Log.Logger.Information("Re-Loading Loot Tables... (`prospecting_loot_template`)");
		LootManager.LoadLootTemplates_Prospecting();
		LootStorage.Prospecting.CheckLootRefs();
		handler.SendGlobalGMSysMessage("DB table `prospecting_loot_template` reloaded.");
		_conditionManager.LoadConditions(true);

		return true;
	}

	[Command("quest_greeting", RBACPermissions.CommandReloadQuestGreeting, true)]
	static bool HandleReloadQuestGreetingCommand(CommandHandler handler)
	{
		Log.Logger.Information("Re-Loading Quest Greeting ... ");
		_gameObjectManager.LoadQuestGreetings();
		handler.SendGlobalGMSysMessage("DB table `quest_greeting` reloaded.");

		return true;
	}

	[Command("quest_locale", RBACPermissions.CommandReloadQuestTemplateLocale, true)]
	static bool HandleReloadQuestTemplateLocaleCommand(CommandHandler handler)
	{
		Log.Logger.Information("Re-Loading Quest Locale... ");
		_gameObjectManager.LoadQuestTemplateLocale();
		_gameObjectManager.LoadQuestObjectivesLocale();
		_gameObjectManager.LoadQuestGreetingLocales();
		_gameObjectManager.LoadQuestOfferRewardLocale();
		_gameObjectManager.LoadQuestRequestItemsLocale();
		handler.SendGlobalGMSysMessage("DB table `quest_template_locale` reloaded.");
		handler.SendGlobalGMSysMessage("DB table `quest_objectives_locale` reloaded.");
		handler.SendGlobalGMSysMessage("DB table `quest_greeting_locale` reloaded.");
		handler.SendGlobalGMSysMessage("DB table `quest_offer_reward_locale` reloaded.");
		handler.SendGlobalGMSysMessage("DB table `quest_request_items_locale` reloaded.");

		return true;
	}

	[Command("quest_poi", RBACPermissions.CommandReloadQuestPoi, true)]
	static bool HandleReloadQuestPOICommand(CommandHandler handler)
	{
		Log.Logger.Information("Re-Loading Quest POI ...");
		_gameObjectManager.LoadQuestPOI();
		_gameObjectManager.InitializeQueriesData(QueryDataGroup.POIs);
		handler.SendGlobalGMSysMessage("DB Table `quest_poi` and `quest_poi_points` reloaded.");

		return true;
	}

	[Command("quest_template", RBACPermissions.CommandReloadQuestTemplate, true)]
	static bool HandleReloadQuestTemplateCommand(CommandHandler handler)
	{
		Log.Logger.Information("Re-Loading Quest Templates...");
		_gameObjectManager.LoadQuests();
		_gameObjectManager.InitializeQueriesData(QueryDataGroup.Quests);
		handler.SendGlobalGMSysMessage("DB table `quest_template` (quest definitions) reloaded.");

		// dependent also from `gameobject` but this table not reloaded anyway
		Log.Logger.Information("Re-Loading GameObjects for quests...");
		_gameObjectManager.LoadGameObjectForQuests();
		handler.SendGlobalGMSysMessage("Data GameObjects for quests reloaded.");

		return true;
	}

	[Command("rbac", RBACPermissions.CommandReloadRbac, true)]
	static bool HandleReloadRBACCommand(CommandHandler handler)
	{
		Log.Logger.Information("Reloading RBAC tables...");
		Global.AccountMgr.LoadRBAC();
		_worldManager.ReloadRBAC();
		handler.SendGlobalGMSysMessage("RBAC data reloaded.");

		return true;
	}

	[Command("reference_loot_template", RBACPermissions.CommandReloadReferenceLootTemplate, true)]
	static bool HandleReloadLootTemplatesReferenceCommand(CommandHandler handler)
	{
		Log.Logger.Information("Re-Loading Loot Tables... (`reference_loot_template`)");
		LootManager.LoadLootTemplates_Reference();
		handler.SendGlobalGMSysMessage("DB table `reference_loot_template` reloaded.");
		_conditionManager.LoadConditions(true);

		return true;
	}

	[Command("reputation_reward_rate", RBACPermissions.CommandReloadReputationRewardRate, true)]
	static bool HandleReloadReputationRewardRateCommand(CommandHandler handler)
	{
		Log.Logger.Information("Re-Loading `reputation_reward_rate` Table!");
		_gameObjectManager.LoadReputationRewardRate();
		handler.SendGlobalSysMessage("DB table `reputation_reward_rate` reloaded.");

		return true;
	}

	[Command("reputation_spillover_template", RBACPermissions.CommandReloadSpilloverTemplate, true)]
	static bool HandleReloadReputationSpilloverTemplateCommand(CommandHandler handler)
	{
		Log.Logger.Information("Re-Loading `reputation_spillover_template` Table!");
		_gameObjectManager.LoadReputationSpilloverTemplate();
		handler.SendGlobalSysMessage("DB table `reputation_spillover_template` reloaded.");

		return true;
	}

	[Command("reserved_name", RBACPermissions.CommandReloadReservedName, true)]
	static bool HandleReloadReservedNameCommand(CommandHandler handler)
	{
		Log.Logger.Information("Loading ReservedNames... (`reserved_name`)");
		_gameObjectManager.LoadReservedPlayersNames();
		handler.SendGlobalGMSysMessage("DB table `reserved_name` (player reserved names) reloaded.");

		return true;
	}

	[Command("scene_template", RBACPermissions.CommandReloadSceneTemplate, true)]
	static bool HandleReloadSceneTemplateCommand(CommandHandler handler)
	{
		Log.Logger.Information("Reloading scene_template table...");
		_gameObjectManager.LoadSceneTemplates();
		handler.SendGlobalGMSysMessage("Scenes templates reloaded. New scriptname need a reboot.");

		return true;
	}

	[Command("skill_discovery_template", RBACPermissions.CommandReloadSkillDiscoveryTemplate, true)]
	static bool HandleReloadSkillDiscoveryTemplateCommand(CommandHandler handler)
	{
		Log.Logger.Information("Re-Loading Skill Discovery Table...");
		SkillDiscovery.LoadSkillDiscoveryTable();
		handler.SendGlobalGMSysMessage("DB table `skill_discovery_template` (recipes discovered at crafting) reloaded.");

		return true;
	}

	static bool HandleReloadSkillPerfectItemTemplateCommand(CommandHandler handler)
	{
		// latched onto HandleReloadSkillExtraItemTemplateCommand as it's part of that table group (and i don't want to chance all the command IDs)
		Log.Logger.Information("Re-Loading Skill Perfection Data Table...");
		SkillPerfectItems.LoadSkillPerfectItemTable();
		handler.SendGlobalGMSysMessage("DB table `skill_perfect_item_template` (perfect item procs when crafting) reloaded.");

		return true;
	}

	[Command("skill_extra_item_template", RBACPermissions.CommandReloadSkillExtraItemTemplate, true)]
	static bool HandleReloadSkillExtraItemTemplateCommand(CommandHandler handler)
	{
		Log.Logger.Information("Re-Loading Skill Extra Item Table...");
		SkillExtraItems.LoadSkillExtraItemTable();
		handler.SendGlobalGMSysMessage("DB table `skill_extra_item_template` (extra item creation when crafting) reloaded.");

		return HandleReloadSkillPerfectItemTemplateCommand(handler);
	}

	[Command("skill_fishing_base_level", RBACPermissions.CommandReloadSkillFishingBaseLevel, true)]
	static bool HandleReloadSkillFishingBaseLevelCommand(CommandHandler handler)
	{
		Log.Logger.Information("Re-Loading Skill Fishing base level requirements...");
		_gameObjectManager.LoadFishingBaseSkillLevel();
		handler.SendGlobalGMSysMessage("DB table `skill_fishing_base_level` (fishing base level for zone/subzone) reloaded.");

		return true;
	}

	[Command("skinning_loot_template", RBACPermissions.CommandReloadSkinningLootTemplate, true)]
	static bool HandleReloadLootTemplatesSkinningCommand(CommandHandler handler)
	{
		Log.Logger.Information("Re-Loading Loot Tables... (`skinning_loot_template`)");
		LootManager.LoadLootTemplates_Skinning();
		LootStorage.Skinning.CheckLootRefs();
		handler.SendGlobalGMSysMessage("DB table `skinning_loot_template` reloaded.");
		_conditionManager.LoadConditions(true);

		return true;
	}

	[Command("smart_scripts", RBACPermissions.CommandReloadSmartScripts, true)]
	static bool HandleReloadSmartScripts(CommandHandler handler)
	{
		Log.Logger.Information("Re-Loading Smart Scripts...");
		Global.SmartAIMgr.LoadFromDB();
		handler.SendGlobalGMSysMessage("Smart Scripts reloaded.");

		return true;
	}

	[Command("spell_area", RBACPermissions.CommandReloadSpellArea, true)]
	static bool HandleReloadSpellAreaCommand(CommandHandler handler)
	{
		Log.Logger.Information("Re-Loading SpellArea Data...");
		Global.SpellMgr.LoadSpellAreas();
		handler.SendGlobalGMSysMessage("DB table `spell_area` (spell dependences from area/quest/auras state) reloaded.");

		return true;
	}

	[Command("spell_group", RBACPermissions.CommandReloadSpellGroup, true)]
	static bool HandleReloadSpellGroupsCommand(CommandHandler handler)
	{
		Log.Logger.Information("Re-Loading Spell Groups...");
		Global.SpellMgr.LoadSpellGroups();
		handler.SendGlobalGMSysMessage("DB table `spell_group` (spell groups) reloaded.");

		return true;
	}

	[Command("spell_group_stack_rules", RBACPermissions.CommandReloadSpellGroupStackRules, true)]
	static bool HandleReloadSpellGroupStackRulesCommand(CommandHandler handler)
	{
		Log.Logger.Information("Re-Loading Spell Group Stack Rules...");
		Global.SpellMgr.LoadSpellGroupStackRules();
		handler.SendGlobalGMSysMessage("DB table `spell_group_stack_rules` (spell stacking definitions) reloaded.");

		return true;
	}

	[Command("spell_learn_spell", RBACPermissions.CommandReloadSpellLearnSpell, true)]
	static bool HandleReloadSpellLearnSpellCommand(CommandHandler handler)
	{
		Log.Logger.Information("Re-Loading Spell Learn Spells...");
		Global.SpellMgr.LoadSpellLearnSpells();
		handler.SendGlobalGMSysMessage("DB table `spell_learn_spell` reloaded.");

		return true;
	}

	[Command("spell_linked_spell", RBACPermissions.CommandReloadSpellLinkedSpell, true)]
	static bool HandleReloadSpellLinkedSpellCommand(CommandHandler handler)
	{
		Log.Logger.Information("Re-Loading Spell Linked Spells...");
		Global.SpellMgr.LoadSpellLinked();
		handler.SendGlobalGMSysMessage("DB table `spell_linked_spell` reloaded.");

		return true;
	}

	[Command("spell_loot_template", RBACPermissions.CommandReloadSpellLootTemplate, true)]
	static bool HandleReloadLootTemplatesSpellCommand(CommandHandler handler)
	{
		Log.Logger.Information("Re-Loading Loot Tables... (`spell_loot_template`)");
		LootManager.LoadLootTemplates_Spell();
		LootStorage.Spell.CheckLootRefs();
		handler.SendGlobalGMSysMessage("DB table `spell_loot_template` reloaded.");
		_conditionManager.LoadConditions(true);

		return true;
	}

	[Command("spell_pet_auras", RBACPermissions.CommandReloadSpellPetAuras, true)]
	static bool HandleReloadSpellPetAurasCommand(CommandHandler handler)
	{
		Log.Logger.Information("Re-Loading Spell pet auras...");
		Global.SpellMgr.LoadSpellPetAuras();
		handler.SendGlobalGMSysMessage("DB table `spell_pet_auras` reloaded.");

		return true;
	}

	[Command("spell_proc", RBACPermissions.CommandReloadSpellProc, true)]
	static bool HandleReloadSpellProcsCommand(CommandHandler handler)
	{
		Log.Logger.Information("Re-Loading Spell Proc conditions and data...");
		Global.SpellMgr.LoadSpellProcs();
		handler.SendGlobalGMSysMessage("DB table `spell_proc` (spell proc conditions and data) reloaded.");

		return true;
	}

	[Command("spell_required", RBACPermissions.CommandReloadSpellRequired, true)]
	static bool HandleReloadSpellRequiredCommand(CommandHandler handler)
	{
		Log.Logger.Information("Re-Loading Spell Required Data... ");
		Global.SpellMgr.LoadSpellRequired();
		handler.SendGlobalGMSysMessage("DB table `spell_required` reloaded.");

		return true;
	}

	[Command("spell_scripts", RBACPermissions.CommandReloadSpellScripts, true)]
	static bool HandleReloadSpellScriptsCommand(CommandHandler handler, StringArguments args)
	{
		if (Global.MapMgr.IsScriptScheduled())
		{
			handler.SendSysMessage("DB scripts used currently, please attempt reload later.");

			return false;
		}

		if (args != null)
			Log.Logger.Information("Re-Loading Scripts from `spell_scripts`...");

		_gameObjectManager.LoadSpellScripts();

		if (args != null)
			handler.SendGlobalGMSysMessage("DB table `spell_scripts` reloaded.");

		return true;
	}

	[Command("spell_script_names", RBACPermissions.CommandReloadSpellScriptNames, true)]
	static bool HandleReloadSpellScriptNamesCommand(CommandHandler handler)
	{
		Log.Logger.Information("Reloading spell_script_names table...");
		_gameObjectManager.LoadSpellScriptNames();
		//_scriptManager.NotifyScriptIDUpdate();
		_gameObjectManager.ValidateSpellScripts();
		handler.SendGlobalGMSysMessage("Spell scripts reloaded.");

		return true;
	}

	[Command("spell_target_position", RBACPermissions.CommandReloadSpellTargetPosition, true)]
	static bool HandleReloadSpellTargetPositionCommand(CommandHandler handler)
	{
		Log.Logger.Information("Re-Loading Spell target coordinates...");
		Global.SpellMgr.LoadSpellTargetPositions();
		handler.SendGlobalGMSysMessage("DB table `spell_target_position` (destination coordinates for spell targets) reloaded.");

		return true;
	}

	[Command("spell_threats", RBACPermissions.CommandReloadSpellThreats, true)]
	static bool HandleReloadSpellThreatsCommand(CommandHandler handler)
	{
		Log.Logger.Information("Re-Loading Aggro Spells Definitions...");
		Global.SpellMgr.LoadSpellThreats();
		handler.SendGlobalGMSysMessage("DB table `spell_threat` (spell aggro definitions) reloaded.");

		return true;
	}

	[Command("support", RBACPermissions.CommandReloadSupportSystem, true)]
	static bool HandleReloadSupportSystemCommand(CommandHandler handler)
	{
		Log.Logger.Information("Re-Loading Support System Tables...");
		Global.SupportMgr.LoadBugTickets();
		Global.SupportMgr.LoadComplaintTickets();
		Global.SupportMgr.LoadSuggestionTickets();
		handler.SendGlobalGMSysMessage("DB tables `gm_*` reloaded.");

		return true;
	}

	[Command("trainer", RBACPermissions.CommandReloadTrainer, true)]
	static bool HandleReloadTrainerCommand(CommandHandler handler)
	{
		Log.Logger.Information("Re-Loading `trainer` Table!");
		_gameObjectManager.LoadTrainers();
		_gameObjectManager.LoadCreatureTrainers();
		handler.SendGlobalGMSysMessage("DB table `trainer` reloaded.");
		handler.SendGlobalGMSysMessage("DB table `trainer_locale` reloaded.");
		handler.SendGlobalGMSysMessage("DB table `trainer_spell` reloaded.");
		handler.SendGlobalGMSysMessage("DB table `creature_trainer` reloaded.");

		return true;
	}

	[Command("vehicle_accessory", RBACPermissions.CommandReloadVehicleAccesory, true)]
	static bool HandleReloadVehicleAccessoryCommand(CommandHandler handler)
	{
		Log.Logger.Information("Reloading vehicle_accessory table...");
		_gameObjectManager.LoadVehicleAccessories();
		handler.SendGlobalGMSysMessage("Vehicle accessories reloaded.");

		return true;
	}

	[Command("vehicle_template", RBACPermissions.CommandReloadVehicleTemplate, true)]
	static bool HandleReloadVehicleTemplateCommand(CommandHandler handler)
	{
		Log.Logger.Information("Reloading vehicle_template table...");
		_gameObjectManager.LoadVehicleTemplate();
		handler.SendGlobalGMSysMessage("Vehicle templates reloaded.");

		return true;
	}

	[Command("vehicle_template_accessory", RBACPermissions.CommandReloadVehicleTemplateAccessory, true)]
	static bool HandleReloadVehicleTemplateAccessoryCommand(CommandHandler handler)
	{
		Log.Logger.Information("Reloading vehicle_template_accessory table...");
		_gameObjectManager.LoadVehicleTemplateAccessories();
		handler.SendGlobalGMSysMessage("Vehicle template accessories reloaded.");

		return true;
	}

	[Command("waypoint_data", RBACPermissions.CommandReloadWaypointData, true)]
	static bool HandleReloadWpCommand(CommandHandler handler, StringArguments args)
	{
		if (args != null)
			Log.Logger.Information("Re-Loading Waypoints data from 'waypoints_data'");

		Global.WaypointMgr.Load();

		if (args != null)
			handler.SendGlobalGMSysMessage("DB Table 'waypoint_data' reloaded.");

		return true;
	}

	[Command("waypoint_scripts", RBACPermissions.CommandReloadWaypointScripts, true)]
	static bool HandleReloadWpScriptsCommand(CommandHandler handler, StringArguments args)
	{
		if (Global.MapMgr.IsScriptScheduled())
		{
			handler.SendSysMessage("DB scripts used currently, please attempt reload later.");

			return false;
		}

		if (args != null)
			Log.Logger.Information("Re-Loading Scripts from `waypoint_scripts`...");

		_gameObjectManager.LoadWaypointScripts();

		if (args != null)
			handler.SendGlobalGMSysMessage("DB table `waypoint_scripts` reloaded.");

		return true;
	}

	[CommandGroup("all")]
	class AllCommand
	{
		[Command("", RBACPermissions.CommandReloadAll, true)]
		static bool HandleReloadAllCommand(CommandHandler handler)
		{
			HandleReloadSkillFishingBaseLevelCommand(handler);

			HandleReloadAllAchievementCommand(handler);
			HandleReloadAllAreaCommand(handler);
			HandleReloadAllLootCommand(handler);
			HandleReloadAllNpcCommand(handler);
			HandleReloadAllQuestCommand(handler);
			HandleReloadAllSpellCommand(handler);
			HandleReloadAllItemCommand(handler);
			HandleReloadAllGossipsCommand(handler);
			HandleReloadAllLocalesCommand(handler);

			HandleReloadAccessRequirementCommand(handler);
			HandleReloadMailLevelRewardCommand(handler);
			HandleReloadReservedNameCommand(handler);
			HandleReloadCypherStringCommand(handler);
			HandleReloadGameTeleCommand(handler);

			HandleReloadCreatureMovementOverrideCommand(handler);
			HandleReloadCreatureSummonGroupsCommand(handler);

			HandleReloadVehicleAccessoryCommand(handler);
			HandleReloadVehicleTemplateAccessoryCommand(handler);

			HandleReloadAutobroadcastCommand(handler);
			HandleReloadBattlegroundTemplate(handler);
			HandleReloadCharacterTemplate(handler);

			return true;
		}

		[Command("achievement", RBACPermissions.CommandReloadAllAchievement, true)]
		static bool HandleReloadAllAchievementCommand(CommandHandler handler)
		{
			HandleReloadCriteriaDataCommand(handler);
			HandleReloadAchievementRewardCommand(handler);

			return true;
		}

		[Command("area", RBACPermissions.CommandReloadAllArea, true)]
		static bool HandleReloadAllAreaCommand(CommandHandler handler)
		{
			HandleReloadAreaTriggerTeleportCommand(handler);
			HandleReloadAreaTriggerTavernCommand(handler);
			HandleReloadGameGraveyardZoneCommand(handler);

			return true;
		}

		[Command("gossips", RBACPermissions.CommandReloadAllGossip, true)]
		static bool HandleReloadAllGossipsCommand(CommandHandler handler)
		{
			HandleReloadGossipMenuCommand(handler);
			HandleReloadGossipMenuOptionCommand(handler);
			HandleReloadPointsOfInterestCommand(handler);

			return true;
		}

		[Command("item", RBACPermissions.CommandReloadAllItem, true)]
		static bool HandleReloadAllItemCommand(CommandHandler handler)
		{
			HandleReloadPageTextsCommand(handler);
			HandleReloadItemRandomBonusListTemplatesCommand(handler);

			return true;
		}

		[Command("locales", RBACPermissions.CommandReloadAllLocales, true)]
		static bool HandleReloadAllLocalesCommand(CommandHandler handler)
		{
			HandleReloadAchievementRewardLocaleCommand(handler);
			HandleReloadCreatureTemplateLocaleCommand(handler);
			HandleReloadCreatureTextLocaleCommand(handler);
			HandleReloadGameobjectTemplateLocaleCommand(handler);
			HandleReloadGossipMenuOptionLocaleCommand(handler);
			HandleReloadPageTextLocaleCommand(handler);
			HandleReloadPointsOfInterestCommand(handler);
			HandleReloadQuestTemplateLocaleCommand(handler);

			return true;
		}

		[Command("loot", RBACPermissions.CommandReloadAllLoot, true)]
		static bool HandleReloadAllLootCommand(CommandHandler handler)
		{
			Log.Logger.Information("Re-Loading Loot Tables...");
			LootManager.LoadLootTables();
			handler.SendGlobalGMSysMessage("DB tables `*_loot_template` reloaded.");
			_conditionManager.LoadConditions(true);

			return true;
		}

		[Command("npc", RBACPermissions.CommandReloadAllNpc, true)]
		static bool HandleReloadAllNpcCommand(CommandHandler handler)
		{
			HandleReloadTrainerCommand(handler);
			HandleReloadNpcVendorCommand(handler);
			HandleReloadPointsOfInterestCommand(handler);
			HandleReloadSpellClickSpellsCommand(handler);

			return true;
		}

		[Command("quest", RBACPermissions.CommandReloadAllQuest, true)]
		static bool HandleReloadAllQuestCommand(CommandHandler handler)
		{
			HandleReloadQuestAreaTriggersCommand(handler);
			HandleReloadQuestGreetingCommand(handler);
			HandleReloadQuestPOICommand(handler);
			HandleReloadQuestTemplateCommand(handler);

			Log.Logger.Information("Re-Loading Quests Relations...");
			_gameObjectManager.LoadQuestStartersAndEnders();
			handler.SendGlobalGMSysMessage("DB tables `*_queststarter` and `*_questender` reloaded.");

			return true;
		}

		[Command("scripts", RBACPermissions.CommandReloadAllScripts, true)]
		static bool HandleReloadAllScriptsCommand(CommandHandler handler)
		{
			if (Global.MapMgr.IsScriptScheduled())
			{
				handler.SendSysMessage("DB scripts used currently, please attempt reload later.");

				return false;
			}

			Log.Logger.Information("Re-Loading Scripts...");
			HandleReloadEventScriptsCommand(handler, null);
			HandleReloadSpellScriptsCommand(handler, null);
			handler.SendGlobalGMSysMessage("DB tables `*_scripts` reloaded.");
			HandleReloadWpScriptsCommand(handler, null);
			HandleReloadWpCommand(handler, null);

			return true;
		}

		[Command("spell", RBACPermissions.CommandReloadAllSpell, true)]
		static bool HandleReloadAllSpellCommand(CommandHandler handler)
		{
			HandleReloadSkillDiscoveryTemplateCommand(handler);
			HandleReloadSkillExtraItemTemplateCommand(handler);
			HandleReloadSpellRequiredCommand(handler);
			HandleReloadSpellAreaCommand(handler);
			HandleReloadSpellGroupsCommand(handler);
			HandleReloadSpellLearnSpellCommand(handler);
			HandleReloadSpellLinkedSpellCommand(handler);
			HandleReloadSpellProcsCommand(handler);
			HandleReloadSpellTargetPositionCommand(handler);
			HandleReloadSpellThreatsCommand(handler);
			HandleReloadSpellGroupStackRulesCommand(handler);
			HandleReloadSpellPetAurasCommand(handler);

			return true;
		}
	}
}