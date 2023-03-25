// See https://aka.ms/new-console-template for more information

using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Autofac;
using Forged.MapServer.Accounts;
using Framework;
using Framework.Database;
using Game;
using Game.Common;
using System.Collections;
using Forged.MapServer.Achievements;
using Forged.MapServer.DataStorage;
using Framework.Constants;
using Framework.Util;

var configBuilder = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

var configuration = configBuilder.Build();

var builder = new ContainerBuilder();
builder.Register((c, s) => configuration).As<IConfiguration>().SingleInstance();

builder.AddFramework();
builder.AddCommon();
builder.RegisterType<AccountManager>().SingleInstance();
builder.RegisterType<BNetAccountManager>().SingleInstance();
builder.RegisterType<AchievementGlobalMgr>().SingleInstance();
builder.RegisterType<DB2Manager>().SingleInstance();
builder.RegisterType<CriteriaManager>().SingleInstance();

BitSet localeMask = null;
builder.Register((c, p) =>
{
    var cli = new CliDB(c.Resolve<HotfixDatabase>(), c.Resolve<DB2Manager>());
    localeMask = cli.LoadStores(configuration.GetDefaultValue("DataDir", "./"), Locale.enUS, builder);
    return cli;
}).SingleInstance();

var container = builder.Build();
container.Resolve<CliDB>();


