// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Autofac;
using Forged.RealmServer.Accounts;
using Forged.RealmServer.Achievements;
using Forged.RealmServer.Arenas;
using Forged.RealmServer.BattleGrounds;
using Forged.RealmServer.BattlePets;
using Forged.RealmServer.Cache;
using Forged.RealmServer.Chat;
using Forged.RealmServer.Conditions;
using Forged.RealmServer.DataStorage;
using Forged.RealmServer.DungeonFinding;
using Forged.RealmServer.Entities;
using Forged.RealmServer.Garrisons;
using Forged.RealmServer.Globals;
using Forged.RealmServer.Groups;
using Forged.RealmServer.Networking;
using Forged.RealmServer.Scenarios;
using Forged.RealmServer.Scripting;
using Forged.RealmServer.Services;
using Forged.RealmServer.SupportSystem;
using Forged.RealmServer.World;
using Framework;
using Framework.Constants;
using Framework.Util;
using Game.Common;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections;
using System.IO;

namespace Forged.RealmServer;

internal class Program
{
    private static void Main(string[] args)
    {
        var configBuilder = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", false, true);

        var configuration = configBuilder.Build();

        var builder = new ContainerBuilder();
        builder.RegisterInstance(configuration).As<IConfiguration>().SingleInstance();
        builder.RegisterType<WorldConfig>().SingleInstance().OnActivated(a => a.Instance.Load());

        builder.AddFramework();
        builder.AddCommon();
        builder.RegisterType<GameTime>().SingleInstance();

        var dataPath = configuration.GetDefaultValue("DataDir", "./");
        IContainer container = null;
        BitSet localeMask = null;

        builder.RegisterType<CliDB>().SingleInstance();
        container = builder.Build();

        container.Resolve<WorldServiceManager>().LoadHandlers(container);
        builder.RegisterType<ClassFactory>().SingleInstance().OnActivated(c => c.Instance.Initialize(container));
        RegisterManagers(builder);
        RegisterInstanced(builder);

        // we initialize the server by resolving these.
        container.Resolve<CliDB>().LoadStores(configuration.GetDefaultValue("DataDir", "./"), Locale.enUS, builder);
        container.Resolve<ScriptManager>().Initialize();

        // putting this here so I dont forget how to do it. not actual code that will be used
        //WorldSession worldSession = new();
        //var bnetHandler = container.Resolve<BattlenetHandler>(new TypedParameter(typeof(WorldSession), worldSession));

        var worldManager = container.Resolve<WorldManager>();
        worldManager.SetDBCMask(localeMask);

        var eventManager = container.Resolve<GameEventManager>();
        worldManager.SetEventInterval(eventManager.StartSystem());
        Player.DeleteOldCharacters();
        eventManager.StartArenaSeason();
        worldManager.Initialize(container.Resolve<ClassFactory>());

        void RegisterManagers(ContainerBuilder builder)
        {

            // Managers
            builder.RegisterType<CharacterTemplateDataStorage>().SingleInstance();
            builder.RegisterType<Realm>().SingleInstance();
            builder.RegisterType<CliDB>().SingleInstance().OnActivated(c =>
            {
                localeMask = c.Instance.LoadStores(configuration.GetDefaultValue("DataDir", "./"), Locale.enUS, builder);
                c.Instance.LoadGameTables(dataPath);
            });
            builder.RegisterType<M2Storage>().SingleInstance().OnActivated(a => a.Instance.LoadM2Cameras(dataPath));
            builder.RegisterType<AccountManager>().SingleInstance().OnActivated(d => d.Instance.LoadRBAC());
            builder.RegisterType<BNetAccountManager>().SingleInstance();
            builder.RegisterType<AchievementGlobalMgr>().SingleInstance().OnActivated(a =>
            {
                a.Instance.LoadAchievementReferenceList();
                a.Instance.LoadAchievementScripts();
                a.Instance.LoadRewards();
                a.Instance.LoadRewardLocales();
                a.Instance.LoadCompletedAchievements();
            });
            builder.RegisterType<DB2Manager>().SingleInstance().OnActivated(p =>
            {
                p.Instance.LoadHotfixBlob(localeMask);
                p.Instance.LoadHotfixData();
                p.Instance.LoadHotfixOptionalData(localeMask);
            });
            builder.RegisterType<CriteriaManager>().SingleInstance().OnActivated(c =>
            {
                c.Instance.LoadCriteriaModifiersTree();
                c.Instance.LoadCriteriaList();
                c.Instance.LoadCriteriaData();
            });
            builder.RegisterType<ArenaTeamManager>().SingleInstance().OnActivated(a => a.Instance.LoadArenaTeams());
            builder.RegisterType<BattlegroundManager>().SingleInstance().OnActivated(b =>
            {
                b.Instance.LoadBattleMastersEntry();
                b.Instance.LoadBattlegroundTemplates();
            });
            builder.RegisterType<ConditionManager>().SingleInstance().OnActivated(c => c.Instance.LoadConditions());
            builder.RegisterType<DisableManager>().SingleInstance().OnActivated(d => d.Instance.LoadDisables());
            builder.RegisterType<PetitionManager>().SingleInstance().OnActivated(p =>
            {
                p.Instance.LoadPetitions();
                p.Instance.LoadSignatures();
            });
            builder.RegisterType<SocialManager>().SingleInstance();
            builder.RegisterType<GameEventManager>().SingleInstance().OnActivated(p =>
            {
                p.Instance.Initialize();
                p.Instance.LoadFromDB();
            });
            builder.RegisterType<GarrisonManager>().SingleInstance().OnActivated(g => g.Instance.Initialize());
            builder.RegisterType<GameObjectManager>().SingleInstance().OnActivated(o =>
            {
                o.Instance.SetHighestGuids();

                if (!o.Instance.LoadCypherStrings())
                    Environment.Exit(1);

                o.Instance.LoadInstanceTemplate();
                o.Instance.LoadCreatureLocales();
                o.Instance.LoadGameObjectLocales();
                o.Instance.LoadQuestTemplateLocale();
                o.Instance.LoadQuestOfferRewardLocale();
                o.Instance.LoadQuestRequestItemsLocale();
                o.Instance.LoadQuestObjectivesLocale();
                o.Instance.LoadPageTextLocales();
                o.Instance.LoadGossipMenuItemsLocales();
                o.Instance.LoadPointOfInterestLocales();
                o.Instance.LoadPageTexts();
                o.Instance.LoadGameObjectTemplate();
                o.Instance.LoadGameObjectTemplateAddons();
                o.Instance.LoadNPCText();
                o.Instance.LoadItemTemplates(); // must be after LoadRandomEnchantmentsTable and LoadPageTexts
                o.Instance.LoadItemTemplateAddon(); // must be after LoadItemPrototypes
                o.Instance.LoadItemScriptNames(); // must be after LoadItemPrototypes
                o.Instance.LoadCreatureModelInfo();
                o.Instance.LoadCreatureTemplates();
                o.Instance.LoadEquipmentTemplates();
                o.Instance.LoadCreatureTemplateAddons();
                o.Instance.LoadCreatureScalingData();
                o.Instance.LoadReputationRewardRate();
                o.Instance.LoadReputationOnKill();
                o.Instance.LoadReputationSpilloverTemplate();
                o.Instance.LoadPointsOfInterest();
                o.Instance.LoadCreatureClassLevelStats();
                o.Instance.LoadSpawnGroupTemplates();
                o.Instance.LoadCreatures();
                o.Instance.LoadTempSummons(); // must be after LoadCreatureTemplates() and LoadGameObjectTemplates()
                o.Instance.LoadCreatureAddons();
                o.Instance.LoadCreatureMovementOverrides(); // must be after LoadCreatures()
                o.Instance.LoadGameObjects();
                o.Instance.LoadSpawnGroups();
                o.Instance.LoadInstanceSpawnGroups();
                o.Instance.LoadGameObjectAddons(); // must be after LoadGameObjects()
                o.Instance.LoadGameObjectOverrides(); // must be after LoadGameObjects()
                o.Instance.LoadGameObjectQuestItems();
                o.Instance.LoadCreatureQuestItems();
                o.Instance.LoadLinkedRespawn(); // must be after LoadCreatures(), LoadGameObjects()
                o.Instance.LoadQuests();
                o.Instance.LoadQuestPOI();
                o.Instance.LoadQuestStartersAndEnders(); // must be after quest load
                o.Instance.LoadQuestGreetings();
                o.Instance.LoadQuestGreetingLocales();
                o.Instance.LoadNPCSpellClickSpells();
                o.Instance.LoadVehicleTemplate(); // must be after LoadCreatureTemplates()
                o.Instance.LoadVehicleTemplateAccessories(); // must be after LoadCreatureTemplates() and LoadNPCSpellClickSpells()
                o.Instance.LoadVehicleAccessories(); // must be after LoadCreatureTemplates() and LoadNPCSpellClickSpells()
                o.Instance.LoadVehicleSeatAddon(); // must be after loading DBC
                o.Instance.LoadWorldSafeLocs(); // must be before LoadAreaTriggerTeleports and LoadGraveyardZones
                o.Instance.LoadAreaTriggerTeleports();
                o.Instance.LoadAccessRequirements(); // must be after item template load
                o.Instance.LoadQuestAreaTriggers(); // must be after LoadQuests
                o.Instance.LoadTavernAreaTriggers();
                o.Instance.LoadAreaTriggerScripts();
                o.Instance.LoadInstanceEncounters();
                o.Instance.LoadGraveyardZones();
                o.Instance.LoadSceneTemplates(); // must be before LoadPlayerInfo
                o.Instance.LoadPlayerInfo();
                o.Instance.LoadExplorationBaseXP();
                o.Instance.LoadPetNames();
                o.Instance.LoadPlayerChoices();
                o.Instance.LoadPlayerChoicesLocale();
                o.Instance.LoadJumpChargeParams();
                o.Instance.LoadPetNumber();
                o.Instance.LoadPetLevelInfo();
                o.Instance.LoadMailLevelRewards();
                o.Instance.LoadFishingBaseSkillLevel();
                o.Instance.LoadSkillTiers();
                o.Instance.LoadReservedPlayersNames();
                o.Instance.LoadGameObjectForQuests();
                o.Instance.LoadGameTele();
                o.Instance.LoadTrainers(); // must be after load CreatureTemplate
                o.Instance.LoadGossipMenu();
                o.Instance.LoadGossipMenuItems();
                o.Instance.LoadGossipMenuAddon();
                o.Instance.LoadCreatureTrainers(); // must be after LoadGossipMenuItems
                o.Instance.LoadVendors();          // must be after load CreatureTemplate and ItemTemplate
                o.Instance.LoadPhases();
                o.Instance.LoadFactionChangeAchievements();
                o.Instance.LoadFactionChangeSpells();
                o.Instance.LoadFactionChangeItems();
                o.Instance.LoadFactionChangeQuests();
                o.Instance.LoadFactionChangeReputations();
                o.Instance.LoadFactionChangeTitles();
                o.Instance.ReturnOrDeleteOldMails(false);
                o.Instance.InitializeQueriesData(QueryDataGroup.All);
                o.Instance.LoadRaceAndClassExpansionRequirements();
                o.Instance.LoadRealmNames();
                o.Instance.LoadPhaseNames();
            });
            builder.RegisterType<AreaTriggerDataStorage>().SingleInstance();
            builder.RegisterType<ConversationDataStorage>().SingleInstance();
            builder.RegisterType<WorldManager>().SingleInstance();
            builder.RegisterType<ChannelManager>().SingleInstance().OnActivated(w =>
            {
                w.Instance.LoadFromDB();
            });
            builder.RegisterType<CharacterCache>().SingleInstance();
            builder.RegisterType<WardenCheckManager>().SingleInstance().OnActivated(w =>
            {
                w.Instance.LoadWardenChecks();
                w.Instance.LoadWardenOverrides();
            });
            builder.RegisterType<CharacterCache>().SingleInstance().OnActivated(c => c.Instance.LoadCharacterCacheStorage());
            builder.RegisterType<WorldServiceManager>().SingleInstance();
            builder.RegisterType<SupportManager>().SingleInstance().OnActivated(s =>
            {
                s.Instance.LoadBugTickets();
                s.Instance.LoadComplaintTickets();
                s.Instance.LoadSuggestionTickets();
            });
            builder.RegisterType<PoolManager>().SingleInstance().OnActivated(p =>
            {
                p.Instance.Initialize();
                p.Instance.LoadFromDB();
            });
            builder.RegisterType<QuestPoolManager>().SingleInstance().OnActivated(q => q.Instance.LoadFromDB());
            builder.RegisterType<ScenarioManager>().SingleInstance().OnActivated(s =>
            {
                s.Instance.LoadDB2Data();
                s.Instance.LoadDBData();
                s.Instance.LoadScenarioPOI();
            });
            builder.RegisterType<ScriptManager>().SingleInstance();
            builder.RegisterType<GroupManager>().SingleInstance().OnActivated(g => g.Instance.LoadGroups());
            builder.RegisterType<GuildManager>().SingleInstance().OnActivated(g =>
            {
                g.Instance.LoadGuildRewards();
                g.Instance.LoadGuilds();
            });
            builder.RegisterType<CalendarManager>().SingleInstance();

            builder.RegisterType<LanguageManager>().SingleInstance().OnActivated(l =>
            {
                l.Instance.LoadLanguages();
                l.Instance.LoadLanguagesWords();
            });
            builder.RegisterType<LFGManager>().SingleInstance().OnActivated(l => l.Instance.LoadRewards());
            builder.RegisterType<AreaTriggerDataStorage>().SingleInstance().OnActivated(a =>
            {
                a.Instance.LoadAreaTriggerTemplates();

            });
            builder.RegisterType<ConversationDataStorage>().SingleInstance().OnActivated(a => a.Instance.LoadConversationTemplates());
            builder.RegisterType<CharacterTemplateDataStorage>().SingleInstance().OnActivated(a => a.Instance.LoadCharacterTemplates());
            builder.RegisterType<WhoListStorageManager>().SingleInstance();
            builder.RegisterType<CharacterDatabaseCleaner>().SingleInstance().OnActivated(c => c.Instance.CleanDatabase());
            builder.RegisterType<FormationMgr>().SingleInstance().OnActivated(f => f.Instance.LoadCreatureFormations());
            builder.RegisterType<CreatureTextManager>().SingleInstance().OnActivated(c =>
            {
                c.Instance.LoadCreatureTexts();
                c.Instance.LoadCreatureTextLocales();
            });

            builder.RegisterType<CalendarManager>().SingleInstance().OnActivated(c => c.Instance.LoadFromDB());
            builder.RegisterType<PacketManager>().SingleInstance().OnActivated(c => c.Instance.Initialize());
            builder.RegisterType<BattlePetMgr>().SingleInstance();
        }

        void RegisterInstanced(ContainerBuilder containerBuilder)
        {
            containerBuilder.RegisterType<WorldSession>();
        }
    }
}