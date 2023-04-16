// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Server;
using Framework.Constants;
using Serilog;

namespace Forged.MapServer.Networking;

public abstract class ServerPacket
{
    protected WorldPacket WorldPacket;

    protected ServerPacket(ServerOpcodes opcode)
    {
        Connection = ConnectionType.Realm;
        WorldPacket = new WorldPacket(opcode);
    }

    protected ServerPacket(ServerOpcodes opcode, ConnectionType type = ConnectionType.Realm)
    {
        Connection = type;
        WorldPacket = new WorldPacket(opcode);
    }

    public byte[] BufferData { get; private set; }

    public ConnectionType Connection { get; }

    public ServerOpcodes Opcode => (ServerOpcodes)WorldPacket.Opcode;
    public void Clear()
    {
        WorldPacket.Clear();
        BufferData = null;
    }

    public void LogPacket(WorldSession session)
    {
        Log.Logger.Debug("Sent ServerOpcode: {0} To: {1}", Opcode, session != null ? session.GetPlayerInfo() : "");
    }

    public abstract void Write();

    public void WritePacketData()
    {
        if (BufferData != null)
            return;

        Write();

        BufferData = WorldPacket.GetData();
        WorldPacket.Dispose();
    }
}