// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections;
using System.IO;
using Autofac;
using Forged.RealmServer.Accounts;
using Forged.RealmServer.Conditions;
using Forged.RealmServer.Globals;
using Forged.RealmServer.Scripting;
using Forged.RealmServer.Services;
using Framework;
using Framework.Constants;
using Framework.Util;
using Game.Common;
using Microsoft.Extensions.Configuration;

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

        builder.AddFramework();
        builder.AddCommon();
        BuildServerTypes(ref builder);

        IContainer container = null;
        BitSet localeMask = null;

        builder.RegisterType<CliDB>().SingleInstance().OnActivated(c => localeMask = c.Instance.LoadStores(configuration.GetDefaultValue("DataDir", "./"), Locale.enUS, builder));
        builder.RegisterType<GameTime>().SingleInstance();
        container = builder.Build();

        // we initialize the server by resolving these.
        container.Resolve<CliDB>();
        container.Resolve<ScriptManager>();
        container.Resolve<WorldServiceManager>().LoadHandlers(container);


        // putting this here so I dont forget how to do it. not actual code that will be used
        WorldSession worldSession = new();
        var bnetHandler = container.Resolve<BattlenetHandler>(new TypedParameter(typeof(WorldSession), worldSession));
    }
    void BuildServerTypes(ref ContainerBuilder containerBuilder)
    {
        RegisterManagers(containerBuilder);


        // Handlers
    }

    void RegisterManagers(ContainerBuilder containerBuilder)
    {
        // Managers
        containerBuilder.RegisterType<AccountManager>().SingleInstance();
        containerBuilder.RegisterType<BNetAccountManager>().SingleInstance();
        containerBuilder.RegisterType<ConditionManager>().SingleInstance();
        containerBuilder.RegisterType<DisableManager>().SingleInstance();
        containerBuilder.RegisterType<GameEventManager>().SingleInstance();
        containerBuilder.RegisterType<GameObjectManager>().SingleInstance();
        containerBuilder.RegisterType<WorldManager>().SingleInstance();
        containerBuilder.RegisterType<WardenCheckManager>().SingleInstance();
        containerBuilder.RegisterType<WorldStateManager>().SingleInstance();
        containerBuilder.RegisterType<PoolManager>().SingleInstance();
        containerBuilder.RegisterType<QuestPoolManager>().SingleInstance();
        containerBuilder.RegisterType<GuildManager>().SingleInstance();
    }

    void RegisterInstanced(ContainerBuilder containerBuilder)
    {
        containerBuilder.RegisterType<WorldSession>();
    }
}