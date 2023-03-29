// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Reflection;
using Forged.MapServer.Server;
using Framework.Constants;

namespace Forged.MapServer.Networking;

public class PacketHandler
{
    private readonly Action<WorldSession, ClientPacket> _methodCaller;
    private readonly Type _packetType;
	public PacketProcessing ProcessingPlace { get; private set; }
	public SessionStatus SessionStatus { get; private set; }

	public PacketHandler(MethodInfo info, SessionStatus status, PacketProcessing processingplace, Type type)
	{
		_methodCaller = (Action<WorldSession, ClientPacket>)GetType()
															.GetMethod("CreateDelegate", BindingFlags.Static | BindingFlags.NonPublic)
                                                            ?.MakeGenericMethod(type)
															.Invoke(null,
																	new object[]
																	{
																		info
																	});

		SessionStatus = status;
		ProcessingPlace = processingplace;
		_packetType = type;
	}

	public void Invoke(WorldSession session, WorldPacket packet)
	{
		if (_packetType == null)
			return;

		using var clientPacket = (ClientPacket)Activator.CreateInstance(_packetType, packet);

        if (clientPacket == null)
            return;

        clientPacket.Read();
        clientPacket.LogPacket(session);
        _methodCaller(session, clientPacket);
    }

    // ReSharper disable once UnusedMember.Local
    private static Action<WorldSession, ClientPacket> CreateDelegate<TP1>(MethodInfo method) where TP1 : ClientPacket
	{
		// create first delegate. It is not fine because its 
		// signature contains unknown types T and P1
		var d = (Action<WorldSession, TP1>)method.CreateDelegate(typeof(Action<WorldSession, TP1>));

		// create another delegate having necessary signature. 
		// It encapsulates first delegate with a closure
		return delegate(WorldSession target, ClientPacket p) { d(target, (TP1)p); };
	}
}