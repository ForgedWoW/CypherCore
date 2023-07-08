// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using Autofac;
using Forged.MapServer.Server;
using Game.Common.Handlers;
using Serilog;

namespace Forged.MapServer.Services;

public class WorldServiceManager
{
    private readonly ConcurrentDictionary<(uint ServiceHash, uint MethodId), WorldServiceHandler> _serviceHandlers;

    public WorldServiceHandler GetHandler(uint serviceHash, uint methodId)
    {
        return _serviceHandlers.LookupByKey((serviceHash, methodId));
    }

    public void LoadHandlers(IContainer container, WorldSession session)
    {
        var impl = container.Resolve<IEnumerable<IWorldServiceHandler>>(new PositionalParameter(0, session));

        foreach (var handler in impl)
        {
            foreach (var methodInfo in handler.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                foreach (var serviceAttr in methodInfo.GetCustomAttributes<ServiceAttribute>())
                {
                    var key = (serviceAttr.ServiceHash, serviceAttr.MethodId);

                    if (_serviceHandlers.ContainsKey(key))
                    {
                        Log.Logger.Error($"Tried to override ServiceHandler: {_serviceHandlers[key]} with {methodInfo.Name} (ServiceHash: {serviceAttr.ServiceHash} MethodId: {serviceAttr.MethodId})");

                        continue;
                    }

                    var parameters = methodInfo.GetParameters();

                    if (parameters.Length == 0)
                    {
                        Log.Logger.Error($"Method: {methodInfo.Name} needs atleast one paramter");

                        continue;
                    }

                    _serviceHandlers[key] = new WorldServiceHandler(methodInfo, parameters);
                }
            }
        }
    }
}