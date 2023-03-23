// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game;
using Game.Common.Networking;
using Game.Common.Networking.Packets.MythicPlus;

namespace Game.Common.Handlers;

public class MythicPlusHandler
{
	[WorldPacketHandler(ClientOpcodes.RequestMythicPlusSeasonData)]
	void RequestMythicPlusSeasonData(ClientPacket packet)
	{
		SendPacket(new MythicPlusSeasonData());
	}
}
