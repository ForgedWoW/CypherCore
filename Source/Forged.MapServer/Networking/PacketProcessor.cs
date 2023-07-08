// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Reflection;
using Forged.MapServer.Server;
using Framework.Constants;
using Game.Common.Handlers;

namespace Forged.MapServer.Networking;

public class PacketProcessor
{
    private readonly IWorldSessionHandler _handler;
    private readonly WorldSession _session;
    private readonly MethodInfo _info;
    private readonly Type _packetType;

    public PacketProcessor(IWorldSessionHandler handler, WorldSession session, MethodInfo info, SessionStatus status, PacketProcessing processingplace, Type type)
    {
        SessionStatus = status;
        ProcessingPlace = processingplace;
        _handler = handler;
        _session = session;
        _info = info;
        _packetType = type;
    }

    public PacketProcessing ProcessingPlace { get; }
    public SessionStatus SessionStatus { get; }

    public void Invoke(WorldPacket packet)
    {
        if (_packetType == null)
            return;

        using var clientPacket = (ClientPacket)Activator.CreateInstance(_packetType, packet);

        if (clientPacket == null)
            return;

        clientPacket.Read();
        clientPacket.LogPacket(_session);
        _info.Invoke(_handler, new object[] { clientPacket });
    }
}