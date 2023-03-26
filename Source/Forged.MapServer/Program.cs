// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections;
using System.IO;
using Autofac;
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
using Forged.MapServer.Maps;
using Forged.MapServer.Maps.Instances;
using Forged.MapServer.Warden;
using Forged.MapServer.Weather;
using Forged.MapServer.World;
using Framework;
using Framework.Constants;
using Framework.Database;
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


BitSet localeMask = null;

builder.Register((c, _) =>
		{
			var cli = new CliDB(c.Resolve<HotfixDatabase>(), c.Resolve<DB2Manager>());
			localeMask = cli.LoadStores(configuration.GetDefaultValue("DataDir", "./"), Locale.enUS, builder);

			return cli;
		})
		.SingleInstance();

var container = builder.Build();
container.Resolve<CliDB>();

void BuildServerTypes(ContainerBuilder containerBuilder)
{
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
}