using Autofac;

namespace Game.Common
{
    public static class CommonBuilder
    {
        public static ContainerBuilder AddCommon(this ContainerBuilder builder)
        {
          
            return builder;
        }

        public static ContainerBuilder AddSessionCommon(this ContainerBuilder builder)
        {

            return builder;
        }
    }
}
