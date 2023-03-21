// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Forged.RealmServer.Networking;
using Forged.RealmServer.Networking.Packets;

namespace Forged.RealmServer;

public partial class WorldSession
{
	[WorldPacketHandler(ClientOpcodes.ServerTimeOffsetRequest, Status = SessionStatus.Authed, Processing = PacketProcessing.Inplace)]
	void HandleServerTimeOffsetRequest(ServerTimeOffsetRequest packet)
	{
		ServerTimeOffset response = new();
		response.Time = GameTime.GetGameTime();
		SendPacket(response);
	}
}