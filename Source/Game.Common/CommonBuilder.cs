using Autofac;
using Game.Common.Accounts;
using Game.Common.Battlepay;
using Game.Common.Cache;
using Game.Common.Handlers;
using Game.Common.World;

namespace Game.Common
{
    public static class CommonBuilder
    {
        public static ContainerBuilder AddCommon(this ContainerBuilder services)
        {
            services.RegisterType<AccountManager>().SingleInstance();
            services.RegisterType<BNetAccountManager>().SingleInstance();
            services.RegisterType<BattlePayDataStoreMgr>().SingleInstance();
            services.RegisterType<CharacterCache>().SingleInstance();
            services.RegisterType<WorldManager>().SingleInstance();
            services.RegisterType<AuthenticationHandler>().As<IWorldSessionHandler>().SingleInstance();
            services.RegisterType<BattlenetHandler>().As<IWorldSessionHandler>().SingleInstance();
            services.RegisterType<BattlepayHandler>().As<IWorldSessionHandler>().SingleInstance();
            services.RegisterType<CharacterHandler>().As<IWorldSessionHandler>().SingleInstance();
            services.RegisterType<ChatHandler>().As<IWorldSessionHandler>().SingleInstance();
            services.RegisterType<GarrisonHandler>().As<IWorldSessionHandler>().SingleInstance();
            services.RegisterType<GuildHandler>().As<IWorldSessionHandler>().SingleInstance();
            services.RegisterType<HotfixHandler>().As<IWorldSessionHandler>().SingleInstance();
            services.RegisterType<MythicPlusHandler>().As<IWorldSessionHandler>().SingleInstance();
            services.RegisterType<QueryHandler>().As<IWorldSessionHandler>().SingleInstance();
            services.RegisterType<SocialHandler>().As<IWorldSessionHandler>().SingleInstance();
            services.RegisterType<TimeHandler>().As<IWorldSessionHandler>().SingleInstance();
            services.RegisterType<TokenHandler>().As<IWorldSessionHandler>().SingleInstance();
            return services;
        }
    }
}
