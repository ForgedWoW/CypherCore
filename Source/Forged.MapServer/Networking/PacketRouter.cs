// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Reflection;
using Autofac;
using Forged.MapServer.Server;
using Framework.Constants;
using Game.Common;
using Game.Common.Handlers;
using Serilog;

namespace Forged.MapServer.Networking;

public class PacketRouter
{
    private readonly Dictionary<ClientOpcodes, PacketProcessor> _clientPacketTable = new();
    private readonly ClassFactory _container;
    private readonly Dictionary<Type, IWorldSessionHandler> _opCodeHandler = new();

    public PacketRouter(ClassFactory container)
    {
        _container = container;
    }

    public bool ContainsProcessor(ClientOpcodes opcode)
    {
        return _clientPacketTable.ContainsKey(opcode);
    }

    public void Initialize(WorldSession session)
    {
        var impl = _container.Resolve<IEnumerable<IWorldSessionHandler>>(new PositionalParameter(0, session));

        foreach (var worldSocketHandler in impl)
        {
            _opCodeHandler[worldSocketHandler.GetType()] = worldSocketHandler;

            foreach (var methodInfo in worldSocketHandler.GetType().GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
            {
                foreach (var msgAttr in methodInfo.GetCustomAttributes<WorldPacketHandlerAttribute>())
                {
                    if (msgAttr.Opcode == ClientOpcodes.Unknown)
                    {
                        Log.Logger.Error("Opcode {0} does not have a value", msgAttr.Opcode);

                        continue;
                    }

                    if (_clientPacketTable.ContainsKey(msgAttr.Opcode))
                    {
                        Log.Logger.Error("Tried to override OpcodeHandler of {0} with {1} (Opcode {2})", _clientPacketTable[msgAttr.Opcode].ToString(), methodInfo.Name, msgAttr.Opcode);

                        continue;
                    }

                    var parameters = methodInfo.GetParameters();

                    if (parameters.Length == 0)
                    {
                        Log.Logger.Error("Method: {0} Has no paramters", methodInfo.Name);

                        continue;
                    }

                    if (parameters[0].ParameterType.BaseType != typeof(ClientPacket))
                    {
                        Log.Logger.Error("Method: {0} has wrong BaseType", methodInfo.Name);

                        continue;
                    }

                    _clientPacketTable[msgAttr.Opcode] = new PacketProcessor(worldSocketHandler, session, methodInfo, msgAttr.Status, msgAttr.Processing, parameters[0].ParameterType, _container);
                }
            }
        }
    }

    public bool TryGetOpCodeHandler<T>(out T opCodeHandler) where T : IWorldSessionHandler
    {
        var found = _opCodeHandler.TryGetValue(typeof(T), out var handler);

        if (!found)
        {
            opCodeHandler = default;
            return false;
        }

        opCodeHandler = (T)handler;
        return true;
    }

    public T OpCodeHandler<T>() where T : IWorldSessionHandler
    {
        return (T)_opCodeHandler[typeof(T)];
    }

    public bool TryGetProcessor(ClientOpcodes opcode, out PacketProcessor packetProcessor)
    {
        return _clientPacketTable.TryGetValue(opcode, out packetProcessor);
    }
}