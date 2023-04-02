// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.Server;
using Framework.Constants;
using Serilog;

namespace Forged.MapServer.Networking;

public abstract class ClientPacket : IDisposable
{
    protected WorldPacket WorldPacket;

    protected ClientPacket(WorldPacket worldPacket)
    {
        WorldPacket = worldPacket;
    }

    public void Dispose()
    {
        WorldPacket.Dispose();
    }

    public ClientOpcodes GetOpcode()
    {
        return (ClientOpcodes)WorldPacket.Opcode;
    }

    public void LogPacket(WorldSession session)
    {
        Log.Logger.Debug("Received ClientOpcode: {0} From: {1}", GetOpcode(), session != null ? session.GetPlayerInfo() : "Unknown IP");
    }

    public abstract void Read();
}