// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Forged.RealmServer.Networking;
using Game.Common.Handlers;
using Forged.RealmServer.Networking.Packets;

namespace Forged.RealmServer.Handlers;

public class TimeHandler : IWorldSessionHandler
{
    private readonly WorldSession _session;
    private readonly GameTime _gameTime;

    public TimeHandler(WorldSession session, GameTime gameTime)
    {
        _session = session;
        _gameTime = gameTime;
    }

    [WorldPacketHandler(ClientOpcodes.ServerTimeOffsetRequest, Status = SessionStatus.Authed, Processing = PacketProcessing.Inplace)]
	void HandleServerTimeOffsetRequest(ServerTimeOffsetRequest packet)
	{
		ServerTimeOffset response = new();
		response.Time = _gameTime.CurrentGameTime;
		_session.SendPacket(response);
	}
}
