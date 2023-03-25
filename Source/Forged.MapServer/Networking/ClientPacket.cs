// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.Services;
using Framework.Constants;

namespace Forged.MapServer.Networking;

public abstract class ClientPacket : IDisposable
{
	protected WorldPacket _worldPacket;

	protected ClientPacket(WorldPacket worldPacket)
	{
		_worldPacket = worldPacket;
	}

	public void Dispose()
	{
		_worldPacket.Dispose();
	}

	public abstract void Read();

	public ClientOpcodes GetOpcode()
	{
		return (ClientOpcodes)_worldPacket.GetOpcode();
	}

	public void LogPacket(WorldSession session)
	{
		Log.Logger.Debug("Received ClientOpcode: {0} From: {1}", GetOpcode(), session != null ? session.GetPlayerInfo() : "Unknown IP");
	}
}