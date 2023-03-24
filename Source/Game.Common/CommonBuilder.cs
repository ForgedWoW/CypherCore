using Autofac;
using Game.Common.Accounts;
using Game.Common.Battlepay;
using Game.Common.Cache;
using Game.Common.Handlers;
using Game.Common.Services;
using Game.Common.World;

namespace Game.Common
{
    public static class CommonBuilder
    {
        public static ContainerBuilder AddCommon(this ContainerBuilder builder)
        {
            builder.RegisterType<AccountManager>().SingleInstance();
            builder.RegisterType<BNetAccountManager>().SingleInstance();
            builder.RegisterType<BattlePayDataStoreMgr>().SingleInstance();
            builder.RegisterType<CharacterCache>().SingleInstance();
            builder.RegisterType<WorldManager>().SingleInstance();
            
            return builder;
        }

        public static ContainerBuilder AddSessionCommon(this ContainerBuilder builder)
        {
            builder.RegisterType<RealmRequestService>().SingleInstance();
            builder.RegisterType<AuthenticationHandler>().As<IWorldSessionHandler>().SingleInstance();
            builder.RegisterType<BattlenetHandler>().As<IWorldSessionHandler>().SingleInstance();
            builder.RegisterType<BattlepayHandler>().As<IWorldSessionHandler>().SingleInstance();
            builder.RegisterType<CharacterHandler>().As<IWorldSessionHandler>().SingleInstance();
            builder.RegisterType<ChatHandler>().As<IWorldSessionHandler>().SingleInstance();
            builder.RegisterType<GarrisonHandler>().As<IWorldSessionHandler>().SingleInstance();
            builder.RegisterType<GuildHandler>().As<IWorldSessionHandler>().SingleInstance();
            builder.RegisterType<HotfixHandler>().As<IWorldSessionHandler>().SingleInstance();
            builder.RegisterType<MythicPlusHandler>().As<IWorldSessionHandler>().SingleInstance();
            builder.RegisterType<QueryHandler>().As<IWorldSessionHandler>().SingleInstance();
            builder.RegisterType<SocialHandler>().As<IWorldSessionHandler>().SingleInstance();
            builder.RegisterType<TimeHandler>().As<IWorldSessionHandler>().SingleInstance();
            builder.RegisterType<TokenHandler>().As<IWorldSessionHandler>().SingleInstance();
            return builder;
        }
    }
}
