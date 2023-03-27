// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections;
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
using Forged.MapServer.Collision.Management;
using Forged.MapServer.Conditions;
using Forged.MapServer.DataStorage;
using Forged.MapServer.Entities.Players;
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

var builder = new ContainerBuilder();
builder.RegisterInstance(configuration).As<IConfiguration>().SingleInstance();

builder.AddFramework();
builder.AddCommon();
BuildServerTypes(builder);

IContainer container = null;
BitSet localeMask = null;

builder.RegisterType<CliDB>().SingleInstance().OnActivated(c => localeMask = c.Instance.LoadStores(configuration.GetDefaultValue("DataDir", "./"), Locale.enUS, builder));
builder.RegisterType<ClassFactory>().SingleInstance().OnActivated(c => c.Instance.Initialize(container));
container = builder.Build();

// we initialize the server by resolving these.
container.Resolve<CliDB>(); 
container.Resolve<ScriptManager>();
container.Resolve<WorldServiceManager>().LoadHandlers(container);


void BuildServerTypes(ContainerBuilder containerBuilder)
{
    RegisterManagers(containerBuilder);
    RegisterFactories(containerBuilder);


    // Handlers
}

void RegisterManagers(ContainerBuilder containerBuilder)
{
    // Managers
    containerBuilder.RegisterType<AccountManager>().SingleInstance();
    containerBuilder.RegisterType<BNetAccountManager>().SingleInstance();
    containerBuilder.RegisterType<AchievementGlobalMgr>().SingleInstance();
    containerBuilder.RegisterType<DB2Manager>().SingleInstance();
    containerBuilder.RegisterType<CriteriaManager>().SingleInstance();
    containerBuilder.RegisterType<SmartAIManager>().SingleInstance();
    containerBuilder.RegisterType<ArenaTeamManager>().SingleInstance();
    containerBuilder.RegisterType<BattleFieldManager>().SingleInstance();
    containerBuilder.RegisterType<BattlegroundManager>().SingleInstance();
    containerBuilder.RegisterType<AuctionManager>().SingleInstance();
    containerBuilder.RegisterType<VMapManager>().SingleInstance();
    containerBuilder.RegisterType<ConditionManager>().SingleInstance();
    containerBuilder.RegisterType<DisableManager>().SingleInstance();
    containerBuilder.RegisterType<PetitionManager>().SingleInstance();
    containerBuilder.RegisterType<SocialManager>().SingleInstance();
    containerBuilder.RegisterType<GameEventManager>().SingleInstance();
    containerBuilder.RegisterType<GarrisonManager>().SingleInstance();
    containerBuilder.RegisterType<GameObjectManager>().SingleInstance();
    containerBuilder.RegisterType<WeatherManager>().SingleInstance().OnActivated(m => m.Instance.LoadWeatherData());
    containerBuilder.RegisterType<WorldManager>().SingleInstance();
    containerBuilder.RegisterType<WardenCheckManager>().SingleInstance();
    containerBuilder.RegisterType<WorldStateManager>().SingleInstance();
    containerBuilder.RegisterType<CharacterCache>().SingleInstance();
    containerBuilder.RegisterType<InstanceLockManager>().SingleInstance();
    containerBuilder.RegisterType<MapManager>().SingleInstance();
    containerBuilder.RegisterType<MMapManager>().SingleInstance();
    containerBuilder.RegisterType<TransportManager>().SingleInstance();
    containerBuilder.RegisterType<WaypointManager>().SingleInstance();
    containerBuilder.RegisterType<OutdoorPvPManager>().SingleInstance();
    containerBuilder.RegisterType<WorldServiceManager>().SingleInstance();
    containerBuilder.RegisterType<SpellManager>().SingleInstance();
    containerBuilder.RegisterType<SupportManager>().SingleInstance();
    containerBuilder.RegisterType<PoolManager>().SingleInstance();
    containerBuilder.RegisterType<QuestPoolManager>().SingleInstance();
    containerBuilder.RegisterType<ScenarioManager>().SingleInstance();
    containerBuilder.RegisterType<ScriptManager>().SingleInstance();
    containerBuilder.RegisterType<GroupManager>().SingleInstance();
    containerBuilder.RegisterType<GuildManager>().SingleInstance();
    containerBuilder.RegisterType<LootItemStorage>().SingleInstance();
    containerBuilder.RegisterType<LootStorage>().SingleInstance();
}

void RegisterFactories(ContainerBuilder containerBuilder)
{
    // Factories
    containerBuilder.RegisterType<LootFactory>().SingleInstance();
    containerBuilder.RegisterType<SpellFactory>().SingleInstance();
}

void RegisterInstanced(ContainerBuilder containerBuilder)
{
    containerBuilder.RegisterType<Loot>();
    containerBuilder.RegisterType<Spell>();
}