// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Reflection;
using Forged.MapServer.Server;
using Forged.MapServer.Services;
using Framework.Constants;

namespace Forged.MapServer.Networking;

public class PacketHandler
{
    private readonly Action<WorldSession, ClientPacket> methodCaller;
    private readonly Type packetType;
	public PacketProcessing ProcessingPlace { get; private set; }
	public SessionStatus sessionStatus { get; private set; }

	public PacketHandler(MethodInfo info, SessionStatus status, PacketProcessing processingplace, Type type)
	{
		methodCaller = (Action<WorldSession, ClientPacket>)GetType()
															.GetMethod("CreateDelegate", BindingFlags.Static | BindingFlags.NonPublic)
															.MakeGenericMethod(type)
															.Invoke(null,
																	new object[]
																	{
																		info
																	});

		sessionStatus = status;
		ProcessingPlace = processingplace;
		packetType = type;
	}

	public void Invoke(WorldSession session, WorldPacket packet)
	{
		if (packetType == null)
			return;

		using var clientPacket = (ClientPacket)Activator.CreateInstance(packetType, packet);
		clientPacket.Read();
		clientPacket.LogPacket(session);
		methodCaller(session, clientPacket);
	}

    private static Action<WorldSession, ClientPacket> CreateDelegate<P1>(MethodInfo method) where P1 : ClientPacket
	{
		// create first delegate. It is not fine because its 
		// signature contains unknown types T and P1
		var d = (Action<WorldSession, P1>)method.CreateDelegate(typeof(Action<WorldSession, P1>));

		// create another delegate having necessary signature. 
		// It encapsulates first delegate with a closure
		return delegate(WorldSession target, ClientPacket p) { d(target, (P1)p); };
	}
}