using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using Framework.Database;

namespace Framework
{
    public static class FrameworkBuilder
    {
        public static ContainerBuilder AddFramework(this ContainerBuilder builder)
        {
            builder.RegisterType<LoginDatabase>().SingleInstance();
            builder.RegisterType<CharacterDatabase>().SingleInstance();
            builder.RegisterType<WorldDatabase>().SingleInstance();
            builder.RegisterType<HotfixDatabase>().SingleInstance();
            builder.RegisterType<RealmManager>().SingleInstance();

            return builder;
        }
    }
}
