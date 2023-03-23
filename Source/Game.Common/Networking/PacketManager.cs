// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using Framework.Constants;
using Game.Common.Networking;

namespace Game.Common.Networking;

public static class PacketManager
{
	static readonly ConcurrentDictionary<ClientOpcodes, PacketHandler> _clientPacketTable = new();

	public static void Initialize()
	{
		var currentAsm = Assembly.GetExecutingAssembly();

		foreach (var type in currentAsm.GetTypes())
		{
			foreach (var methodInfo in type.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
			{
				foreach (var msgAttr in methodInfo.GetCustomAttributes<WorldPacketHandlerAttribute>())
				{
					if (msgAttr == null)
						continue;

					if (msgAttr.Opcode == ClientOpcodes.Unknown)
					{
						Log.outError(LogFilter.Network, "Opcode {0} does not have a value", msgAttr.Opcode);

						continue;
					}

					if (_clientPacketTable.ContainsKey(msgAttr.Opcode))
					{
						Log.outError(LogFilter.Network, "Tried to override OpcodeHandler of {0} with {1} (Opcode {2})", _clientPacketTable[msgAttr.Opcode].ToString(), methodInfo.Name, msgAttr.Opcode);

						continue;
					}

					var parameters = methodInfo.GetParameters();

					if (parameters.Length == 0)
					{
						Log.outError(LogFilter.Network, "Method: {0} Has no paramters", methodInfo.Name);

						continue;
					}

					if (parameters[0].ParameterType.BaseType != typeof(ClientPacket))
					{
						Log.outError(LogFilter.Network, "Method: {0} has wrong BaseType", methodInfo.Name);

						continue;
					}

					_clientPacketTable[msgAttr.Opcode] = new PacketHandler(methodInfo, msgAttr.Status, msgAttr.Processing, parameters[0].ParameterType);
				}
			}
		}
	}

	public static PacketHandler GetHandler(ClientOpcodes opcode)
	{
		return _clientPacketTable.LookupByKey(opcode);
	}

	public static bool ContainsHandler(ClientOpcodes opcode)
	{
		return _clientPacketTable.ContainsKey(opcode);
	}

	public static bool IsInstanceOnlyOpcode(ServerOpcodes opcode)
	{
		switch (opcode)
		{
			case ServerOpcodes.QuestGiverStatus:  // ClientQuest
			case ServerOpcodes.DuelRequested:     // Client
			case ServerOpcodes.DuelInBounds:      // Client
			case ServerOpcodes.QueryTimeResponse: // Client
			case ServerOpcodes.DuelWinner:        // Client
			case ServerOpcodes.DuelComplete:      // Client
			case ServerOpcodes.DuelOutOfBounds:   // Client
			case ServerOpcodes.AttackStop:        // Client
			case ServerOpcodes.AttackStart:       // Client
			case ServerOpcodes.MountResult:       // Client
				return true;
			default:
				return false;
		}
	}
}
