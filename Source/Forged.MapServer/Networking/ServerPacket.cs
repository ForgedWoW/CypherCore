// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Game.Networking;

public abstract class ServerPacket
{
	protected WorldPacket _worldPacket;
	readonly ConnectionType connectionType;

	byte[] buffer;

	protected ServerPacket(ServerOpcodes opcode)
	{
		connectionType = ConnectionType.Realm;
		_worldPacket = new WorldPacket(opcode);
	}

	protected ServerPacket(ServerOpcodes opcode, ConnectionType type = ConnectionType.Realm)
	{
		connectionType = type;
		_worldPacket = new WorldPacket(opcode);
	}

	public void Clear()
	{
		_worldPacket.Clear();
		buffer = null;
	}

	public ServerOpcodes GetOpcode()
	{
		return (ServerOpcodes)_worldPacket.GetOpcode();
	}

	public byte[] GetData()
	{
		return buffer;
	}

	public void LogPacket(WorldSession session)
	{
		Log.Logger.Debug("Sent ServerOpcode: {0} To: {1}", GetOpcode(), session != null ? session.GetPlayerInfo() : "");
	}

	public abstract void Write();

	public void WritePacketData()
	{
		if (buffer != null)
			return;

		Write();

		buffer = _worldPacket.GetData();
		_worldPacket.Dispose();
	}

	public ConnectionType GetConnection()
	{
		return connectionType;
	}
}