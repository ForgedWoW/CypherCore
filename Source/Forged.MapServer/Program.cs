// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

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
using Forged.MapServer.Globals.Caching;
using Forged.MapServer.Groups;
using Forged.MapServer.Guilds;
using Forged.MapServer.LootManagement;
using Forged.MapServer.Mails;
using Forged.MapServer.Maps;
using Forged.MapServer.Maps.Grids;
using Forged.MapServer.Maps.Instances;
using Forged.MapServer.MapWeather;
using Forged.MapServer.Miscellaneous;
using Forged.MapServer.Movement;
using Forged.MapServer.Networking;
using Forged.MapServer.OpCodeHandlers;
using Forged.MapServer.OutdoorPVP;
using Forged.MapServer.Phasing;
using Forged.MapServer.Pools;
using Forged.MapServer.Questing;
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
using Framework.Database;
using Framework.Util;
using Game.Common;
using Microsoft.Extensions.Configuration;
using static Forged.MapServer.LootManagement.LootTemplate;

var configBuilder = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", false, true);

var configuration = configBuilder.Build() as IConfiguration;
var dataPath = configuration.GetDefaultValue("DataDir", "./");

IContainer container = null;
var builder = new ContainerBuilder();
builder.RegisterInstance(configuration).As<IConfiguration>().SingleInstance();

var hotfixDatabase = new HotfixDatabase(configuration);
var cliDB = new CliDB(hotfixDatabase); 
var localeMask = cliDB.LoadStores(configuration.GetDefaultValue("DataDir", "./"), Locale.enUS, builder);
cliDB.LoadGameTables(dataPath, builder);

builder.RegisterInstance(cliDB).As<CliDB>().SingleInstance();
builder.RegisterInstance(hotfixDatabase).As<HotfixDatabase>().SingleInstance();

builder.RegisterType<WorldDatabase>();
builder.RegisterType<CharacterDatabase>();
builder.RegisterType<HotfixDatabase>();
builder.AddFramework();
builder.AddCommon();
RegisterServerTypes();

container = builder.Build();

InitializeServer();

void InitializeServer()
{
    // we initialize the server by resolving these.
    container.Resolve<ClassFactory>();
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
    RegisterCaches();
    RegisterManagers();
    RegisterFactories();
    RegisterInstanced();
    RegisterOpCodeHandlers();
}

void RegisterManagers()
{
    // Managers
    builder.RegisterType<Realm>().SingleInstance();
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
    builder.RegisterType<LFGGroupScript>().SingleInstance();
}

// These are all extrations from the old ObjectMgr. Extracted into seperate classes for better maintainability and to support Dependency Injection
void RegisterCaches()
{
    builder.RegisterType<AccessRequirementsCache>().SingleInstance().OnActivated(c => c.Instance.Load());
    builder.RegisterType<AreaTriggerCache>().SingleInstance().OnActivated(c => c.Instance.Load());
    builder.RegisterType<ClassAndRaceExpansionRequirementsCache>().SingleInstance().OnActivated(c => c.Instance.Load());
    builder.RegisterType<CreatureAddonCache>().SingleInstance().OnActivated(c => c.Instance.Load());
    builder.RegisterType<CreatureBaseStatsCache>().SingleInstance().OnActivated(c => c.Instance.Load());
    builder.RegisterType<CreatureDataCache>().SingleInstance().OnActivated(c => c.Instance.Load());
    builder.RegisterType<CreatureDefaultTrainersCache>().SingleInstance().OnActivated(c => c.Instance.Load());
    builder.RegisterType<CreatureLocaleCache>().SingleInstance().OnActivated(c => c.Instance.Load());
    builder.RegisterType<CreatureModelCache>().SingleInstance().OnActivated(c => c.Instance.Load());
    builder.RegisterType<CreatureMovementOverrideCache>().SingleInstance().OnActivated(c => c.Instance.Load());
    builder.RegisterType<CreatureTemplateCache>().SingleInstance().OnActivated(c => c.Instance.Load());
    builder.RegisterType<EquipmentInfoCache>().SingleInstance().OnActivated(c => c.Instance.Load());
    builder.RegisterType<ExplorationExpCache>().SingleInstance().OnActivated(c => c.Instance.Load());
    builder.RegisterType<FactionChangeTitleCache>().SingleInstance().OnActivated(c => c.Instance.Load());
    builder.RegisterType<FishingBaseForAreaCache>().SingleInstance().OnActivated(c => c.Instance.Load());
    builder.RegisterType<GameObjectCache>().SingleInstance().OnActivated(c => c.Instance.Load());
    builder.RegisterType<GameObjectTemplateCache>().SingleInstance().OnActivated(c => c.Instance.Load());
    builder.RegisterType<GameTeleObjectCache>().SingleInstance().OnActivated(c => c.Instance.Load());
    builder.RegisterType<GossipMenuItemsCache>().SingleInstance().OnActivated(c => c.Instance.Load());
    builder.RegisterType<IdGeneratorCache>().SingleInstance().OnActivated(c => c.Instance.Load());
    builder.RegisterType<InstanceTemplateCache>().SingleInstance().OnActivated(c => c.Instance.Load());
    builder.RegisterType<ItemTemplateCache>().SingleInstance().OnActivated(c => c.Instance.Load());
    builder.RegisterType<MapObjectCache>().SingleInstance();
    builder.RegisterType<MapSpawnGroupCache>().SingleInstance().OnActivated(c => c.Instance.Load());
    builder.RegisterType<PageTextCache>().SingleInstance().OnActivated(c => c.Instance.Load());
    builder.RegisterType<PointOfInterestCache>().SingleInstance().OnActivated(c => c.Instance.Load());
    builder.RegisterType<QuestTemplateCache>().SingleInstance().OnActivated(c => c.Instance.Load());
    builder.RegisterType<ScriptLoader>().SingleInstance().OnActivated(c => c.Instance.Load());
    builder.RegisterType<SpawnDataCacheRouter>().SingleInstance();
    builder.RegisterType<SpawnGroupDataCache>().SingleInstance().OnActivated(c => c.Instance.Load());
    builder.RegisterType<SpellClickInfoCache>().SingleInstance().OnActivated(c => c.Instance.Load());
    builder.RegisterType<TrainerCache>().SingleInstance().OnActivated(c => c.Instance.Load());
    builder.RegisterType<VehicleObjectCache>().SingleInstance().OnActivated(c => c.Instance.Load());
    builder.RegisterType<VendorItemCache>().SingleInstance().OnActivated(c => c.Instance.Load());
    builder.RegisterType<WorldSafeLocationsCache>().SingleInstance().OnActivated(c => c.Instance.Load());
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
    builder.RegisterType<ObjectGuidGeneratorFactory>().SingleInstance();
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
    builder.RegisterType<LFGQueue>();
    builder.RegisterType<ItemTemplate>();
    builder.RegisterType<ItemSpecStats>();
    builder.RegisterType<SpellHistory>();
    builder.RegisterType<LootTemplate>();
    builder.RegisterType<LootStore>();
    builder.RegisterType<Loot>();
    builder.RegisterType<LootGroup>();
    builder.RegisterType<BlackMarketTemplate>();
    builder.RegisterType<LootRoll>();
    builder.RegisterType<GuildBankTab>();
    builder.RegisterType<Trainer>();
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
    builder.RegisterType<AuthenticationHandler>();
    builder.RegisterType<AzeriteHandler>();
    builder.RegisterType<BankHandler>();
    builder.RegisterType<BattlenetHandler>();
    builder.RegisterType<CalendarHandler>();
    builder.RegisterType<ChannelHandler>();
    builder.RegisterType<CharacterHandler>();
    builder.RegisterType<TokenHandler>();
    builder.RegisterType<TicketHandler>();
    builder.RegisterType<SocialHandler>();
    builder.RegisterType<ScenarioHandler>();
    builder.RegisterType<QueryHandler>();
    builder.RegisterType<PetitionsHandler>();
    builder.RegisterType<LFGHandler>();
    builder.RegisterType<GroupHandler>();
    builder.RegisterType<CollectionsHandler>();
    builder.RegisterType<ChatHandler>();
}