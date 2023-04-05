// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using Framework.Constants;
using Serilog;

namespace Forged.MapServer.Networking;

public class PacketManager
{
    private readonly ConcurrentDictionary<ClientOpcodes, PacketHandler> _clientPacketTable = new();

    public bool ContainsHandler(ClientOpcodes opcode)
    {
        return _clientPacketTable.ContainsKey(opcode);
    }

    public PacketHandler GetHandler(ClientOpcodes opcode)
    {
        return _clientPacketTable.LookupByKey(opcode);
    }

    public void Initialize()
    {
        var currentAsm = Assembly.GetExecutingAssembly();

        foreach (var type in currentAsm.GetTypes())
        {
            foreach (var methodInfo in type.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
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

                    _clientPacketTable[msgAttr.Opcode] = new PacketHandler(methodInfo, msgAttr.Status, msgAttr.Processing, parameters[0].ParameterType);
                }
            }
        }
    }

    public bool IsInstanceOnlyOpcode(ServerOpcodes opcode)
    {
        return opcode switch
        {
            ServerOpcodes.QuestGiverStatus => // ClientQuest
                true,
            ServerOpcodes.DuelRequested => // Client
                true,
            ServerOpcodes.DuelInBounds => // Client
                true,
            ServerOpcodes.QueryTimeResponse => // Client
                true,
            ServerOpcodes.DuelWinner => // Client
                true,
            ServerOpcodes.DuelComplete => // Client
                true,
            ServerOpcodes.DuelOutOfBounds => // Client
                true,
            ServerOpcodes.AttackStop => // Client
                true,
            ServerOpcodes.AttackStart => // Client
                true,
            ServerOpcodes.MountResult => // Client
                true,
            _ => false
        };
    }
}