// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections;
using System.IO;
using Autofac;
using Forged.RealmServer.Accounts;
using Forged.RealmServer.Achievements;
using Forged.RealmServer.Arenas;
using Forged.RealmServer.BattleGrounds;
using Forged.RealmServer.Cache;
using Forged.RealmServer.Chat;
using Forged.RealmServer.Conditions;
using Forged.RealmServer.DataStorage;
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
        builder.RegisterType<GameTime>().SingleInstance();
        BuildServerTypes(builder);

        IContainer container = null;
        BitSet localeMask = null;

        builder.RegisterType<CliDB>().SingleInstance();
        container = builder.Build();

        // we initialize the server by resolving these.
        container.Resolve<CliDB>().LoadStores(configuration.GetDefaultValue("DataDir", "./"), Locale.enUS, builder);
        container.Resolve<ScriptManager>().Initialize();
        container.Resolve<WorldServiceManager>().LoadHandlers(container);


        // putting this here so I dont forget how to do it. not actual code that will be used
        WorldSession worldSession = new();
        var bnetHandler = container.Resolve<BattlenetHandler>(new TypedParameter(typeof(WorldSession), worldSession));
    }
    static void BuildServerTypes(ContainerBuilder containerBuilder)
    {
        RegisterManagers(containerBuilder);
        RegisterCache(containerBuilder);

        // Handlers
    }

    private static void RegisterCache(ContainerBuilder containerBuilder)
    {
        containerBuilder.RegisterType<CharacterCache>().SingleInstance();
        containerBuilder.RegisterType<CharacterTemplateDataStorage>().SingleInstance();
        containerBuilder.RegisterType<ConversationDataStorage>().SingleInstance();
    }

    static void RegisterManagers(ContainerBuilder containerBuilder)
    {
        // Managers
        containerBuilder.RegisterType<AccountManager>().SingleInstance();
        containerBuilder.RegisterType<AchievementGlobalMgr>().SingleInstance();
        containerBuilder.RegisterType<AreaTriggerDataStorage>().SingleInstance();
        containerBuilder.RegisterType<ArenaTeamManager>().SingleInstance();
        containerBuilder.RegisterType<BattlegroundManager>().SingleInstance();
        containerBuilder.RegisterType<BNetAccountManager>().SingleInstance();
        containerBuilder.RegisterType<CalendarManager>().SingleInstance();
        containerBuilder.RegisterType<CharacterTemplateDataStorage>().SingleInstance();
        containerBuilder.RegisterType<ConditionManager>().SingleInstance();
        containerBuilder.RegisterType<CriteriaManager>().SingleInstance();
        containerBuilder.RegisterType<DisableManager>().SingleInstance();
        containerBuilder.RegisterType<GameEventManager>().SingleInstance();
        containerBuilder.RegisterType<GameObjectManager>().SingleInstance();
        containerBuilder.RegisterType<GuildManager>().SingleInstance();
        containerBuilder.RegisterType<LanguageManager>().SingleInstance();
        containerBuilder.RegisterType<PoolManager>().SingleInstance();
        containerBuilder.RegisterType<QuestPoolManager>().SingleInstance();
        containerBuilder.RegisterType<WorldManager>().SingleInstance();
        containerBuilder.RegisterType<WardenCheckManager>().SingleInstance();
        containerBuilder.RegisterType<WorldStateManager>().SingleInstance();
    }

    static void RegisterInstanced(ContainerBuilder containerBuilder)
    {
        containerBuilder.RegisterType<WorldSession>();
    }
}