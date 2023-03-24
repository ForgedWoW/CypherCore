// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Common.Networking;
using Game.Common.Networking.Packets.Misc;
using Game.Common.Server;

namespace Game.Common.Handlers;

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
