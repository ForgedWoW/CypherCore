using Autofac;
using Game.Common.Scripting;

namespace Game.Common
{
    public static class CommonBuilder
    {
        public static ContainerBuilder AddCommon(this ContainerBuilder builder)
        {
            builder.RegisterType<ScriptManager>().SingleInstance(); 

            return builder;
        }

        public static ContainerBuilder AddSessionCommon(this ContainerBuilder builder)
        {

            return builder;
        }
    }
}
