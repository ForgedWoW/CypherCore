// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Linq;
using System.Reflection;
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

        public T ActiveNonRegistered<T>(params object[] args) where T : class
        {
            args ??= Array.Empty<object>();
            var constructors = typeof(T).GetConstructors();
            var highest = constructors.OrderByDescending(x => x.GetParameters().Length).First();

            return highest.GetParameters().Length switch
            {
                0 or 99 => Activator.CreateInstance(typeof(T)) as T,
                _       => Activator.CreateInstance(typeof(T), args.Combine(highest.GetParameters().Where(p => p.ParameterType.IsClass).Select(param => Container.Resolve(param.ParameterType)).ToArray())) as T
            };
        }

        public T ActiveNonRegistered<T>(ConstructorInfo constructor, params object[] args) where T : class
        {
            args ??= Array.Empty<object>();
            return constructor.GetParameters().Length switch
            {
                0 or 99 => Activator.CreateInstance(typeof(T)) as T,
                _       => Activator.CreateInstance(typeof(T), args.Combine(constructor.GetParameters().Where(p => p.ParameterType.IsClass).Select(param => Container.Resolve(param.ParameterType)).ToArray())) as T
            };
        }
    }
}
