// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Networking;
using Forged.MapServer.Networking.Packets.MythicPlus;
using Framework.Constants;
using Game.Common.Handlers;

namespace Forged.MapServer.Handlers;

public class MythicPlusHandler : IWorldSessionHandler
{
	[WorldPacketHandler(ClientOpcodes.RequestMythicPlusSeasonData)]
    private void RequestMythicPlusSeasonData(ClientPacket packet)
	{
		SendPacket(new MythicPlusSeasonData());
	}
}