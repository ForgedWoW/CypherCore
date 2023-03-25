﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Forged.RealmServer.Networking;
using Forged.RealmServer.Networking.Packets.Misc;
using Forged.RealmServer.Server;

namespace Forged.RealmServer.Handlers;

public class TimeHandler : IWorldSessionHandler
{
    private readonly WorldSession _session;

    public TimeHandler(WorldSession session)
    {
        _session = session;
    }

    [WorldPacketHandler(ClientOpcodes.ServerTimeOffsetRequest, Status = SessionStatus.Authed, Processing = PacketProcessing.Inplace)]
	void HandleServerTimeOffsetRequest(ServerTimeOffsetRequest packet)
	{
		ServerTimeOffset response = new();
		response.Time = GameTime.GetGameTime();
		_session.SendPacket(response);
	}
}
