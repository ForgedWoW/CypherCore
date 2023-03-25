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
using Forged.MapServer.DataStorage;
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
builder.Register((c, s) => configuration).As<IConfiguration>().SingleInstance();

builder.AddFramework();
builder.AddCommon();
BuildServerTypes(builder);


BitSet localeMask = null;

builder.Register((c, p) =>
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
    containerBuilder.RegisterType<AuctionManager>().SingleInstance();
    containerBuilder.RegisterType<AuctionManager>().SingleInstance();
    containerBuilder.RegisterType<AuctionManager>().SingleInstance();
    containerBuilder.RegisterType<AuctionManager>().SingleInstance();
    containerBuilder.RegisterType<AuctionManager>().SingleInstance();
    containerBuilder.RegisterType<AuctionManager>().SingleInstance();
    containerBuilder.RegisterType<AuctionManager>().SingleInstance();
    containerBuilder.RegisterType<AuctionManager>().SingleInstance();
    containerBuilder.RegisterType<AuctionManager>().SingleInstance();
    containerBuilder.RegisterType<AuctionManager>().SingleInstance();
}