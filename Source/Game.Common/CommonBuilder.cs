using Autofac;
using Game.Common.Handlers;

namespace Game.Common
{
    public static class CommonBuilder
    {
        public static ContainerBuilder AddCommon(this ContainerBuilder services)
        {
            services.RegisterType<AuthenticationHandler>().SingleInstance();
            services.RegisterType<BattlenetHandler>().SingleInstance();
            services.RegisterType<BattlepayHandler>().SingleInstance();
            services.RegisterType<CharacterHandler>().SingleInstance();
            services.RegisterType<ChatHandler>().SingleInstance();
            services.RegisterType<GarrisonHandler>().SingleInstance();
            services.RegisterType<GuildHandler>().SingleInstance();
            services.RegisterType<HotfixHandler>().SingleInstance();
            services.RegisterType<MythicPlusHandler>().SingleInstance();
            services.RegisterType<QueryHandler>().SingleInstance();
            services.RegisterType<SocialHandler>().SingleInstance();
            services.RegisterType<TimeHandler>().SingleInstance();
            services.RegisterType<TokenHandler>().SingleInstance();
            return services;
        }
    }
}
