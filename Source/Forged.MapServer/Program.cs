// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Autofac;
using Forged.MapServer.Accounts;
using Forged.MapServer.Achievements;
using Forged.MapServer.AI.SmartScripts;
using Forged.MapServer.Arenas;
using Forged.MapServer.Arenas.Zones;
using Forged.MapServer.AuctionHouse;
using Forged.MapServer.BattleFields;
using Forged.MapServer.BattleGrounds;
using Forged.MapServer.BattleGrounds.Zones;
using Forged.MapServer.Battlepay;
using Forged.MapServer.BattlePets;
using Forged.MapServer.BlackMarket;
using Forged.MapServer.Cache;
using Forged.MapServer.Calendar;
using Forged.MapServer.Chat;
using Forged.MapServer.Chat.Channels;
using Forged.MapServer.Collision;
using Forged.MapServer.Collision.Management;
using Forged.MapServer.Conditions;
using Forged.MapServer.DataStorage;
using Forged.MapServer.DungeonFinding;
using Forged.MapServer.Entities;
using Forged.MapServer.Entities.AreaTriggers;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.GameObjects;
using Forged.MapServer.Entities.Items;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Objects.Update;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Entities.Taxis;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Events;
using Forged.MapServer.Garrisons;
using Forged.MapServer.Globals;
using Forged.MapServer.Groups;
using Forged.MapServer.Guilds;
using Forged.MapServer.LootManagement;
using Forged.MapServer.Mails;
using Forged.MapServer.Maps;
using Forged.MapServer.Maps.Grids;
using Forged.MapServer.Maps.Instances;
using Forged.MapServer.Miscellaneous;
using Forged.MapServer.Movement;
using Forged.MapServer.Networking;
using Forged.MapServer.OpCodeHandlers;
using Forged.MapServer.OutdoorPVP;
using Forged.MapServer.Phasing;
using Forged.MapServer.Pools;
using Forged.MapServer.Quest;
using Forged.MapServer.Reputation;
using Forged.MapServer.Scenarios;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IServer;
using Forged.MapServer.Server;
using Forged.MapServer.Services;
using Forged.MapServer.Spells;
using Forged.MapServer.Spells.Skills;
using Forged.MapServer.SupportSystem;
using Forged.MapServer.Text;
using Forged.MapServer.Tools;
using Forged.MapServer.Warden;
using Forged.MapServer.World;
using Framework;
using Framework.Constants;
using Framework.Util;
using Game.Common;
using Microsoft.Extensions.Configuration;
using Forged.MapServer.MapWeather;

var configBuilder = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", false, true);

var configuration = configBuilder.Build();
var dataPath = configuration.GetDefaultValue("DataDir", "./");

IContainer container = null;
BitSet localeMask = null;
var builder = new ContainerBuilder();
builder.RegisterInstance(configuration).As<IConfiguration>().SingleInstance();

builder.AddFramework();
builder.AddCommon();
RegisterServerTypes();

container = builder.Build();

InitializeServer();

void InitializeServer()
{
    // we initialize the server by resolving these.
    container.Resolve<ClassFactory>();
    var cliDB = container.Resolve<CliDB>();
    var sm = container.Resolve<ScriptManager>();
    var gom = container.Resolve<GameObjectManager>();
    var worldManager = container.Resolve<WorldManager>();
    worldManager.SetDBCMask(localeMask);

    MultiMap<uint, uint> mapData = new();

    foreach (var mapEntry in cliDB.MapStorage.Values)
        if (mapEntry.ParentMapID != -1)
            mapData.Add((uint)mapEntry.ParentMapID, mapEntry.Id);
        else if (mapEntry.CosmeticParentMapID != -1)
            mapData.Add((uint)mapEntry.CosmeticParentMapID, mapEntry.Id);

    container.Resolve<TerrainManager>().InitializeParentMapData(mapData);
    container.Resolve<VMapManager>().Initialize(mapData);
    container.Resolve<MMapManager>().Initialize(mapData);
    container.Resolve<DisableManager>().CheckQuestDisables();
    container.Resolve<SpellManager>().LoadSpellAreas();
    container.Resolve<LFGManager>().LoadLFGDungeons();
    gom.LoadSpellScripts(); // must be after load Creature/Gameobject(Template/Data)
    gom.LoadEventScripts(); // must be after load Creature/Gameobject(Template/Data)
    gom.LoadWaypointScripts();
    gom.LoadSpellScriptNames();
    sm.Initialize();
    gom.ValidateSpellScripts();
    container.Resolve<MapManager>().Initialize();
    var eventManager = container.Resolve<GameEventManager>();
    worldManager.SetEventInterval(eventManager.StartSystem());
    container.Resolve<PlayerComputators>().DeleteOldCharacters();
    eventManager.StartArenaSeason();
    worldManager.Inject(container.Resolve<AccountManager>(), container.Resolve<CharacterCache>(), container.Resolve<ObjectAccessor>(),
                            container.Resolve<QuestPoolManager>(), container.Resolve<CalendarManager>(), container.Resolve<GuildManager>(),
                            container.Resolve<WorldStateManager>(), eventManager, gom);

    sm.ForEach<IServerLoadComplete>(s => s.LoadComplete());
}

void RegisterServerTypes()
{
    RegisterManagers();
    RegisterFactories();
    RegisterInstanced();
    RegisterOpCodeHandlers();
}

void RegisterManagers()
{
    // Managers
    builder.RegisterType<Realm>().SingleInstance();
    builder.RegisterType<CliDB>().SingleInstance().OnActivated(c =>
    {
        localeMask = c.Instance.LoadStores(configuration.GetDefaultValue("DataDir", "./"), Locale.enUS, builder);
        c.Instance.LoadGameTables(dataPath, builder);
    });
    builder.RegisterType<M2Storage>().SingleInstance().OnActivated(a => a.Instance.LoadM2Cameras(dataPath));
    builder.RegisterType<TaxiPathGraph>().SingleInstance().OnActivated(a => a.Instance.Initialize());
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
    builder.RegisterType<SmartAIManager>().SingleInstance().OnActivated(s =>
    {
        s.Instance.LoadWaypointFromDB();
        s.Instance.LoadFromDB();
    });
    builder.RegisterType<ArenaTeamManager>().SingleInstance().OnActivated(a => a.Instance.LoadArenaTeams());
    builder.RegisterType<BattleFieldManager>().SingleInstance().OnActivated(b => b.Instance.InitBattlefield());
    builder.RegisterType<BattlegroundManager>().SingleInstance().OnActivated(b =>
    {
        b.Instance.LoadBattleMastersEntry();
        b.Instance.LoadBattlegroundTemplates();
    });
    builder.RegisterType<AuctionManager>().SingleInstance().OnActivated(a => a.Instance.LoadAuctions());
    builder.RegisterType<VMapManager>().SingleInstance();
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
        o.Instance.Initialize(o.Context.Resolve<ClassFactory>());
        o.Instance.Inject(o.Context.Resolve<DB2Manager>());
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
    builder.RegisterType<WeatherManager>().SingleInstance().OnActivated(m => m.Instance.LoadWeatherData());
    builder.RegisterType<WorldManager>().SingleInstance();
    builder.RegisterType<WardenCheckManager>().SingleInstance().OnActivated(w =>
    {
        w.Instance.LoadWardenChecks();
        w.Instance.LoadWardenOverrides();
    });
    builder.RegisterType<WorldStateManager>().SingleInstance().OnActivated(w =>
    {
        w.Instance.LoadFromDB();
        w.Instance.SetValue(WorldStates.CurrentPvpSeasonId, configuration.GetDefaultValue("Arena:ArenaSeason:InProgress", false) ? configuration.GetDefaultValue("Arena:ArenaSeason:ID", 32) : 0, false, null);
        w.Instance.SetValue(WorldStates.PreviousPvpSeasonId, configuration.GetDefaultValue("Arena:ArenaSeason:ID", 32) - (configuration.GetDefaultValue("Arena:ArenaSeason:InProgress", false) ? 1 : 0), false, null);
    });
    builder.RegisterType<CharacterCache>().SingleInstance().OnActivated(c => c.Instance.LoadCharacterCacheStorage());
    builder.RegisterType<InstanceLockManager>().SingleInstance().OnActivated(i => i.Instance.Load());
    builder.RegisterType<MapManager>().SingleInstance().OnActivated(m => m.Instance.InitInstanceIds());
    builder.RegisterType<MMapManager>().SingleInstance();
    builder.RegisterType<TransportManager>().SingleInstance().OnActivated(t =>
    {
        t.Instance.LoadTransportTemplates();
        t.Instance.LoadTransportAnimationAndRotation();
        t.Instance.LoadTransportSpawns();
    });
    builder.RegisterType<WaypointManager>().SingleInstance().OnActivated(w => w.Instance.Load());
    builder.RegisterType<OutdoorPvPManager>().SingleInstance().OnActivated(o => o.Instance.InitOutdoorPvP());
    builder.RegisterType<SpellManager>().SingleInstance().OnActivated(s =>
    {
        s.Instance.LoadSpellInfoStore();
        s.Instance.LoadSpellInfoServerside();
        s.Instance.LoadSpellInfoCorrections();
        s.Instance.LoadSkillLineAbilityMap();
        s.Instance.LoadSpellInfoCustomAttributes();
        s.Instance.LoadSpellInfoDiminishing();
        s.Instance.LoadSpellInfoImmunities();
        s.Instance.LoadPetFamilySpellsStore();
        s.Instance.LoadSpellTotemModel();
        s.Instance.LoadSpellInfosLateFix();
        s.Instance.LoadSpellRanks();
        s.Instance.LoadSpellRequired();
        s.Instance.LoadSpellGroups();
        s.Instance.LoadSpellLearnSkills();
        s.Instance.LoadSpellInfoSpellSpecificAndAuraState();
        s.Instance.LoadSpellLearnSpells();
        s.Instance.LoadSpellProcs();
        s.Instance.LoadSpellThreats();
        s.Instance.LoadSpellGroupStackRules();
        s.Instance.LoadSpellEnchantProcData();
        s.Instance.LoadPetLevelupSpellMap();
        s.Instance.LoadPetDefaultSpells();
        s.Instance.LoadSpellPetAuras();
        s.Instance.LoadSpellTargetPositions();
        s.Instance.LoadSpellLinked();
    });
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

    builder.RegisterType<LootItemStorage>().SingleInstance().OnActivated(l => l.Instance.LoadStorageFromDB());
    builder.RegisterType<LootStoreBox>().SingleInstance();
    builder.RegisterType<LootManager>().SingleInstance().OnActivated(l => l.Instance.LoadLootTables());
    builder.RegisterType<TraitMgr>().SingleInstance().OnActivated(t => t.Instance.Load());
    builder.RegisterType<LanguageManager>().SingleInstance().OnActivated(l =>
    {
        l.Instance.LoadLanguages();
        l.Instance.LoadLanguagesWords();
    });
    builder.RegisterType<ItemEnchantmentManager>().SingleInstance().OnActivated(i => i.Instance.LoadItemRandomBonusListTemplates());
    builder.RegisterType<LFGManager>().SingleInstance().OnActivated(l => l.Instance.LoadRewards());
    builder.RegisterType<AreaTriggerDataStorage>().SingleInstance().OnActivated(a =>
    {
        a.Instance.LoadAreaTriggerTemplates();
        a.Instance.LoadAreaTriggerSpawns();
    });
    builder.RegisterType<ConversationDataStorage>().SingleInstance().OnActivated(a => a.Instance.LoadConversationTemplates());
    builder.RegisterType<CharacterTemplateDataStorage>().SingleInstance().OnActivated(a => a.Instance.LoadCharacterTemplates());
    builder.RegisterType<WhoListStorageManager>().SingleInstance();
    builder.RegisterType<CharacterDatabaseCleaner>().SingleInstance().OnActivated(c => c.Instance.CleanDatabase());
    builder.RegisterType<SkillDiscovery>().SingleInstance().OnActivated(a => a.Instance.LoadSkillDiscoveryTable());
    builder.RegisterType<SkillExtraItems>().SingleInstance().OnActivated(a => a.Instance.LoadSkillExtraItemTable());
    builder.RegisterType<SkillPerfectItems>().SingleInstance().OnActivated(a => a.Instance.LoadSkillPerfectItemTable());
    builder.RegisterType<BlackMarketManager>().SingleInstance().OnActivated(b =>
    {
        if (!configuration.GetDefaultValue("BlackMarket:Enabled", true))
            return;

        b.Instance.LoadTemplates();
        b.Instance.LoadAuctions();
    });
    builder.RegisterType<FormationMgr>().SingleInstance().OnActivated(f => f.Instance.LoadCreatureFormations());
    builder.RegisterType<MountCache>().SingleInstance().OnActivated(m => m.Instance.LoadMountDefinitions());
    builder.RegisterType<MountCache>().SingleInstance().OnActivated(m => m.Instance.LoadMountDefinitions());
    builder.RegisterType<CreatureTextManager>().SingleInstance().OnActivated(c =>
    {
        c.Instance.LoadCreatureTexts();
        c.Instance.LoadCreatureTextLocales();
    });

    builder.RegisterType<CalendarManager>().SingleInstance().OnActivated(c => c.Instance.LoadFromDB());
    builder.RegisterType<BattlePetMgr>().SingleInstance();
    builder.RegisterType<BattlePetData>().SingleInstance();
    builder.RegisterType<UnitCombatHelpers>().SingleInstance();
    builder.RegisterType<PlayerComputators>().SingleInstance();
    builder.RegisterType<CommandManager>().SingleInstance();
    builder.RegisterType<ObjectAccessor>().SingleInstance();
    builder.RegisterType<PlayerNameMapHolder>().SingleInstance();
    builder.RegisterType<GridDefines>().SingleInstance();
    builder.RegisterType<CellCalculator>().SingleInstance();
    builder.RegisterType<Formulas>().SingleInstance();
    builder.RegisterType<PhasingHandler>().SingleInstance();
    builder.RegisterType<CriteriaDataValidator>().SingleInstance();
}

void RegisterFactories()
{
    // Factories
    builder.RegisterType<LootFactory>().SingleInstance();
    builder.RegisterType<SpellFactory>();
    builder.RegisterType<ChannelManagerFactory>().SingleInstance();
    // We are doing this to inject the container into the class factory. The container is not yet built at this point, so we need to do this after the container is built.
    // ReSharper disable once AccessToModifiedClosure
    builder.RegisterType<ClassFactory>().SingleInstance().OnActivated(c => c.Instance.Initialize(container));
    builder.RegisterType<CreatureFactory>().SingleInstance();
    builder.RegisterType<BattlePayDataStoreMgr>().SingleInstance();
    builder.RegisterType<ItemFactory>().SingleInstance();
    builder.RegisterType<AzeriteEmpoweredItemFactory>().SingleInstance();
    builder.RegisterType<AzeriteItemFactory>().SingleInstance();
    builder.RegisterType<ConversationFactory>().SingleInstance();
    builder.RegisterType<SceneFactory>().SingleInstance();
}

void RegisterInstanced()
{
    builder.RegisterType<Loot>();
    builder.RegisterType<Spell>();
    builder.RegisterType<CollectionMgr>();
    builder.RegisterType<UnitData>();
    builder.RegisterType<GossipMenu>();
    builder.RegisterType<PlayerMenu>();
    builder.RegisterType<AuctionHouseObject>();
    builder.RegisterType<SmartScript>();
    builder.RegisterType<CriteriaData>();
    builder.RegisterType<BattlegroundQueue>();
    builder.RegisterType<PlayerAchievementMgr>();
    builder.RegisterType<QuestObjectiveCriteriaManager>();
    builder.RegisterType<CinematicManager>();
    builder.RegisterType<ReputationMgr>();
    builder.RegisterType<SceneMgr>();
    builder.RegisterType<AreaTrigger>();
    builder.RegisterType<QuestMenu>();
    builder.RegisterType<GameObject>();
    builder.RegisterType<BattlePet>();
    builder.RegisterType<Creature>();
    builder.RegisterType<BlackMarketEntry>();
    builder.RegisterType<ObjectGuidGenerator>();
    builder.RegisterType<Channel>();
    builder.RegisterType<Condition>();
    builder.RegisterType<Garrison>();
    builder.RegisterType<Garrison.Building>();
    builder.RegisterType<Garrison.Plot>();
    builder.RegisterType<PlayerGroup>();
    builder.RegisterType<Guild>();
    builder.RegisterType<GuildAchievementMgr>();
    builder.RegisterType<MailDraft>();
    builder.RegisterType<Cell>();
    builder.RegisterType<Map>();
    builder.RegisterType<DynamicMapTree>();
    builder.RegisterType<TerrainInfo>();
    builder.RegisterType<RASocket>();
    builder.RegisterType<PacketLog>();
    builder.RegisterType<WorldSocketManager>();
    builder.RegisterType<Quest>();
    builder.RegisterType<InstanceScenario>();
    builder.RegisterType<AccountInfoQueryHolder>();
    builder.RegisterType<AccountInfoQueryHolderPerRealm>();
    builder.RegisterType<DosProtection>();
    builder.RegisterType<Warden>();
    builder.RegisterType<WardenWin>();
    builder.RegisterType<SpellInfo>();
    builder.RegisterType<SpellEffectInfo>();
    builder.RegisterType<Petition>();
    builder.RegisterType<PlayerTaxi>();
    builder.RegisterType<Item>();
    builder.RegisterType<AzeriteEmpoweredItem>();
    builder.RegisterType<AzeriteItem>();
    builder.RegisterType<Bag>();
    builder.RegisterType<Conversation>();
    builder.RegisterType<Corpse>();
    builder.RegisterType<DynamicObject>();
    builder.RegisterType<TempSummon>();
    builder.RegisterType<Transport>();
    builder.RegisterType<Minion>();
    builder.RegisterType<Guardian>();
    builder.RegisterType<Puppet>();
    builder.RegisterType<Totem>();
    builder.RegisterType<Pet>();
    builder.RegisterType<SceneObject>();
    builder.RegisterType<Vehicle>();
    builder.RegisterType<Battleground>();
    builder.RegisterType<Arena>();
    builder.RegisterType<BgWarsongGluch>();
    builder.RegisterType<BgArathiBasin>();
    builder.RegisterType<NagrandArena>();
    builder.RegisterType<BladesEdgeArena>();
    builder.RegisterType<BgEyeofStorm>();
    builder.RegisterType<RuinsofLordaeronArena>();
    builder.RegisterType<BgStrandOfAncients>();
    builder.RegisterType<DalaranSewersArena>();
    builder.RegisterType<RingofValorArena>();
    builder.RegisterType<ArenaTeam>();
    builder.RegisterType<CriteriaDataSet>();
    builder.RegisterType<Weather>();
    builder.RegisterType<PacketRouter>();
}

void RegisterOpCodeHandlers()
{
    builder.RegisterType<WorldServiceManager>();
    builder.RegisterType<BattlepayHandler>();
    builder.RegisterType<ArtifactHandler>();
    builder.RegisterType<BattleGroundHandler>();
    builder.RegisterType<BattlePetHandler>();
    builder.RegisterType<BlackMarketHandlers>();
    builder.RegisterType<CombatHandler>();
    builder.RegisterType<GarrisonHandler>();
    builder.RegisterType<GuildHandler>();
    builder.RegisterType<HotfixHandler>();
    builder.RegisterType<InspectHandler>();
    builder.RegisterType<ItemHandler>();
    builder.RegisterType<LogoutHandler>();
    builder.RegisterType<LootHandler>();
    builder.RegisterType<MailHandler>();
    builder.RegisterType<MiscHandler>();
    builder.RegisterType<MovementHandler>();
    builder.RegisterType<MythicPlusHandler>();
    builder.RegisterType<NPCHandler>();
    builder.RegisterType<PetHandler>();
    builder.RegisterType<QuestHandler>();
    builder.RegisterType<SceneHandler>();
    builder.RegisterType<SkillHandler>();
    builder.RegisterType<SpellHandler>();
    builder.RegisterType<TaxiHandler>();
    builder.RegisterType<TimeHandler>();
    builder.RegisterType<WoWTokenHandler>();
    builder.RegisterType<ToyHandler>();
    builder.RegisterType<TransmogrificationHandler>();
    builder.RegisterType<VehicleHandler>();
    builder.RegisterType<VoidStorageHandler>();
    builder.RegisterType<TradeHandler>();
    builder.RegisterType<TraitHandler>();
    builder.RegisterType<AuctionHandler>();
}