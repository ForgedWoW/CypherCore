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
    builder.RegisterType<ClassFactory>().SingleInstance().OnActivated(c => c.Instance.Initialize(container));
    builder.RegisterType<AccountManager>().SingleInstance();
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
    builder.RegisterType<DisableManager>().SingleInstance();
    builder.RegisterType<PetitionManager>().SingleInstance();
    builder.RegisterType<SocialManager>().SingleInstance();
    builder.RegisterType<GameEventManager>().SingleInstance().OnActivated(p => p.Instance.Initialize());
    builder.RegisterType<GarrisonManager>().SingleInstance();
    builder.RegisterType<GameObjectManager>().SingleInstance().OnActivated(a =>
    {
        a.Instance.SetHighestGuids();

        if (!a.Instance.LoadCypherStrings())
            Environment.Exit(1);

        a.Instance.LoadInstanceTemplate();

    });
    builder.RegisterType<WeatherManager>().SingleInstance().OnActivated(m => m.Instance.LoadWeatherData());
    builder.RegisterType<WorldManager>().SingleInstance();
    builder.RegisterType<WardenCheckManager>().SingleInstance();
    builder.RegisterType<WorldStateManager>().SingleInstance();
    builder.RegisterType<CharacterCache>().SingleInstance();
    builder.RegisterType<InstanceLockManager>().SingleInstance();
    builder.RegisterType<MapManager>().SingleInstance();
    builder.RegisterType<MMapManager>().SingleInstance();
    builder.RegisterType<TransportManager>().SingleInstance();
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
    });
    builder.RegisterType<SupportManager>().SingleInstance();
    builder.RegisterType<PoolManager>().SingleInstance().OnActivated(p => p.Instance.Initialize());
    builder.RegisterType<QuestPoolManager>().SingleInstance();
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
}

void RegisterFactories()
{
    // Factories
    builder.RegisterType<LootFactory>().SingleInstance();
    builder.RegisterType<SpellFactory>().SingleInstance();
}

void RegisterInstanced()
{
    builder.RegisterType<Loot>();
    builder.RegisterType<Spell>();
}