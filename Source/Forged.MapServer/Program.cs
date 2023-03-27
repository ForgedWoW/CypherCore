// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Autofac;
using Forged.MapServer;
using Forged.MapServer.Accounts;
using Forged.MapServer.Achievements;
using Forged.MapServer.AI.SmartScripts;
using Forged.MapServer.Arenas;
using Forged.MapServer.AuctionHouse;
using Forged.MapServer.BattleFields;
using Forged.MapServer.BattleGrounds;
using Forged.MapServer.Cache;
using Forged.MapServer.Chat;
using Forged.MapServer.Collision.Management;
using Forged.MapServer.Conditions;
using Forged.MapServer.DataStorage;
using Forged.MapServer.DungeonFinding;
using Forged.MapServer.Entities.Items;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Entities.Taxis;
using Forged.MapServer.Events;
using Forged.MapServer.Garrisons;
using Forged.MapServer.Globals;
using Forged.MapServer.Groups;
using Forged.MapServer.Guilds;
using Forged.MapServer.LootManagement;
using Forged.MapServer.Maps;
using Forged.MapServer.Maps.Instances;
using Forged.MapServer.Movement;
using Forged.MapServer.OutdoorPVP;
using Forged.MapServer.Pools;
using Forged.MapServer.Scenarios;
using Forged.MapServer.Scripting;
using Forged.MapServer.Services;
using Forged.MapServer.Spells;
using Forged.MapServer.SupportSystem;
using Forged.MapServer.Warden;
using Forged.MapServer.Weather;
using Forged.MapServer.World;
using Framework;
using Framework.Constants;
using Framework.Util;
using Game.Common;
using Microsoft.Extensions.Configuration;


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
BuildServerTypes();

container = builder.Build();

InitializeServer();


void InitializeServer()
{
    // we initialize the server by resolving these.
    var cliDB = container.Resolve<CliDB>();
    container.Resolve<ScriptManager>();
    container.Resolve<WorldServiceManager>().LoadHandlers(container);
    container.Resolve<GameObjectManager>();
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
}

void BuildServerTypes()
{
    RegisterManagers();
    RegisterFactories();
    RegisterInstanced();
}

void RegisterManagers()
{
    // Managers
    builder.RegisterType<CliDB>().SingleInstance().OnActivated(c =>
    {
        localeMask = c.Instance.LoadStores(configuration.GetDefaultValue("DataDir", "./"), Locale.enUS, builder);
        c.Instance.LoadGameTables(dataPath);
    });
    builder.RegisterType<M2Storage>().SingleInstance().OnActivated(a => a.Instance.LoadM2Cameras(dataPath));
    builder.RegisterType<TaxiPathGraph>().SingleInstance().OnActivated(a => a.Instance.Initialize());
    // We are doing this to inject the container into the class factory. The container is not yet built at this point, so we need to do this after the container is built.
    // ReSharper disable once AccessToModifiedClosure
    builder.RegisterType<AccountManager>().SingleInstance().OnActivated(d => d.Instance.LoadRBAC());
    builder.RegisterType<BNetAccountManager>().SingleInstance();
    builder.RegisterType<AchievementGlobalMgr>().SingleInstance();
    builder.RegisterType<DB2Manager>().SingleInstance().OnActivated(p =>
    {
        p.Instance.LoadHotfixBlob(localeMask);
        p.Instance.LoadHotfixData();
        p.Instance.LoadHotfixOptionalData(localeMask);
    });
    builder.RegisterType<CriteriaManager>().SingleInstance();
    builder.RegisterType<SmartAIManager>().SingleInstance();
    builder.RegisterType<ArenaTeamManager>().SingleInstance();
    builder.RegisterType<BattleFieldManager>().SingleInstance();
    builder.RegisterType<BattlegroundManager>().SingleInstance();
    builder.RegisterType<AuctionManager>().SingleInstance();
    builder.RegisterType<VMapManager>().SingleInstance();
    builder.RegisterType<ConditionManager>().SingleInstance();
    builder.RegisterType<DisableManager>().SingleInstance().OnActivated(d => d.Instance.LoadDisables());
    builder.RegisterType<PetitionManager>().SingleInstance();
    builder.RegisterType<SocialManager>().SingleInstance();
    builder.RegisterType<GameEventManager>().SingleInstance().OnActivated(p =>
    {
        p.Instance.Initialize();
        p.Instance.LoadFromDB();
    });
    builder.RegisterType<GarrisonManager>().SingleInstance();
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
    });
    builder.RegisterType<WeatherManager>().SingleInstance().OnActivated(m => m.Instance.LoadWeatherData());
    builder.RegisterType<WorldManager>().SingleInstance();
    builder.RegisterType<WardenCheckManager>().SingleInstance();
    builder.RegisterType<WorldStateManager>().SingleInstance();
    builder.RegisterType<CharacterCache>().SingleInstance();
    builder.RegisterType<InstanceLockManager>().SingleInstance().OnActivated(i => i.Instance.Load());
    builder.RegisterType<MapManager>().SingleInstance().OnActivated(m => m.Instance.InitInstanceIds());
    builder.RegisterType<MMapManager>().SingleInstance();
    builder.RegisterType<TransportManager>().SingleInstance().OnActivated(t =>
    {
        t.Instance.LoadTransportTemplates();
        t.Instance.LoadTransportAnimationAndRotation();
        t.Instance.LoadTransportSpawns();
    });
    builder.RegisterType<WaypointManager>().SingleInstance();
    builder.RegisterType<OutdoorPvPManager>().SingleInstance();
    builder.RegisterType<WorldServiceManager>().SingleInstance();
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
    builder.RegisterType<SupportManager>().SingleInstance();
    builder.RegisterType<PoolManager>().SingleInstance().OnActivated(p =>
    {
        p.Instance.Initialize();
        p.Instance.LoadFromDB();
    });
    builder.RegisterType<QuestPoolManager>().SingleInstance().OnActivated(q => q.Instance.LoadFromDB());
    builder.RegisterType<ScenarioManager>().SingleInstance();
    builder.RegisterType<ScriptManager>().SingleInstance();
    builder.RegisterType<GroupManager>().SingleInstance();
    builder.RegisterType<GuildManager>().SingleInstance();
    builder.RegisterType<LootItemStorage>().SingleInstance();
    builder.RegisterType<LootStorage>().SingleInstance();
    builder.RegisterType<TraitMgr>().SingleInstance().OnActivated(t => t.Instance.Load());
    builder.RegisterType<LanguageManager>().SingleInstance().OnActivated(l =>
    {
        l.Instance.LoadLanguages();
        l.Instance.LoadLanguagesWords();
    });
    builder.RegisterType<ItemEnchantmentManager>().SingleInstance().OnActivated(i => i.Instance.LoadItemRandomBonusListTemplates());
    builder.RegisterType<LFGManager>().SingleInstance().OnActivated(l => l.Instance.LoadRewards());
}

void RegisterFactories()
{
    // Factories
    builder.RegisterType<LootFactory>().SingleInstance();
    builder.RegisterType<SpellFactory>().SingleInstance();
    builder.RegisterType<ClassFactory>().SingleInstance().OnActivated(c => c.Instance.Initialize(container));
}

void RegisterInstanced()
{
    builder.RegisterType<Loot>();
    builder.RegisterType<Spell>();
}