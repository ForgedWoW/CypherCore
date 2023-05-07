// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Autofac;
using Autofac.Core;

namespace Game.Common
{
    public class ClassFactory
    {
        public IContainer Container { get; private set; }

        public void Initialize(IContainer container)
        {
            Container = container;
        }

        public T Resolve<T>()
        {
            return Container.Resolve<T>();
        }

        public T Resolve<T>(params Parameter[] parameters)
        {
            return Container.Resolve<T>(parameters);
        }

        public T ResolveWithPositionalParameters<T>(params object[] parameters)
        {
            var positionalParameters = new Parameter[parameters.Length];
            for (var i = 0; i < parameters.Length; i++)
                positionalParameters[i] = new PositionalParameter(i, parameters[i]);

            return Container.Resolve<T>(positionalParameters);
        }
    }
}
