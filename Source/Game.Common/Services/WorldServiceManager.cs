// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;

namespace Game.Common.Services;

public class WorldServiceManager : Singleton<WorldServiceManager>
{
	readonly ConcurrentDictionary<(uint ServiceHash, uint MethodId), WorldServiceHandler> _serviceHandlers;

	WorldServiceManager()
	{
		_serviceHandlers = new ConcurrentDictionary<(uint ServiceHash, uint MethodId), WorldServiceHandler>();

		var currentAsm = Assembly.GetExecutingAssembly();

		foreach (var type in currentAsm.GetTypes())
		{
			foreach (var methodInfo in type.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic))
			{
				foreach (var serviceAttr in methodInfo.GetCustomAttributes<ServiceAttribute>())
				{
					if (serviceAttr == null)
						continue;

					var key = (serviceAttr.ServiceHash, serviceAttr.MethodId);

					if (_serviceHandlers.ContainsKey(key))
					{
						Log.outError(LogFilter.Network, $"Tried to override ServiceHandler: {_serviceHandlers[key]} with {methodInfo.Name} (ServiceHash: {serviceAttr.ServiceHash} MethodId: {serviceAttr.MethodId})");

						continue;
					}

					var parameters = methodInfo.GetParameters();

					if (parameters.Length == 0)
					{
						Log.outError(LogFilter.Network, $"Method: {methodInfo.Name} needs atleast one paramter");

						continue;
					}

					_serviceHandlers[key] = new WorldServiceHandler(methodInfo, parameters);
				}
			}
		}
	}

	public WorldServiceHandler GetHandler(uint serviceHash, uint methodId)
	{
		return _serviceHandlers.LookupByKey((serviceHash, methodId));
	}
}
