// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Autofac;
using Framework.Database;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Framework;

public static class FrameworkBuilder
{
    public static ContainerBuilder AddFramework(this ContainerBuilder builder)
    {
        builder.Register((c, p) =>
               {
                   var configuration = c.Resolve<IConfiguration>();

                   var logger = new LoggerConfiguration()
                                .ReadFrom.Configuration(configuration)
                                .CreateLogger();

                   Log.Logger = logger;

                   return logger;
               })
               .As<ILogger>()
               .SingleInstance();

        builder.RegisterType<LoginDatabase>().SingleInstance();
        builder.RegisterType<CharacterDatabase>().SingleInstance();
        builder.RegisterType<WorldDatabase>().SingleInstance();
        builder.RegisterType<HotfixDatabase>().SingleInstance();
        builder.RegisterType<RealmManager>().SingleInstance();
        ;

        return builder;
    }
}