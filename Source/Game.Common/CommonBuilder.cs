using Game.Common.Handlers;
using Microsoft.Extensions.DependencyInjection;

namespace Game.Common
{
    public static class CommonBuilder
    {
        public static IServiceCollection AddCommon(this IServiceCollection services)
        {
            services.AddSingleton<AuthenticationHandler>();
            services.AddSingleton<BattlenetHandler>();
            services.AddSingleton<BattlepayHandler>();
            services.AddSingleton<CharacterHandler>();
            services.AddSingleton<ChatHandler>();
            services.AddSingleton<GarrisonHandler>();
            services.AddSingleton<GuildHandler>();
            services.AddSingleton<HotfixHandler>();
            services.AddSingleton<MythicPlusHandler>();
            services.AddSingleton<QueryHandler>();
            services.AddSingleton<SocialHandler>();
            services.AddSingleton<TimeHandler>();
            services.AddSingleton<TokenHandler>();
            return services;
        }
    }
}
