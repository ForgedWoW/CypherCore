// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Chrono;
using Forged.MapServer.Networking;
using Forged.MapServer.Networking.Packets.Misc;
using Framework.Constants;
using Game.Common.Handlers;

namespace Forged.MapServer.Handlers;

public class TimeHandler : IWorldSessionHandler
{
    [WorldPacketHandler(ClientOpcodes.ServerTimeOffsetRequest, Status = SessionStatus.Authed, Processing = PacketProcessing.Inplace)]
    private void HandleServerTimeOffsetRequest(ServerTimeOffsetRequest packet)
    {
        ServerTimeOffset response = new()
        {
            Time = GameTime.CurrentTime
        };

        SendPacket(response);
    }
}