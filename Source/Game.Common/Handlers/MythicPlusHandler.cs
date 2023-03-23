// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Common.Networking;
using Game.Common.Networking.Packets.MythicPlus;
using Game.Common.Server;

namespace Game.Common.Handlers;

public class MythicPlusHandler
{
    private readonly WorldSession _session;

    public MythicPlusHandler(WorldSession session)
    {
        _session = session;
    }

    [WorldPacketHandler(ClientOpcodes.RequestMythicPlusSeasonData)]
	void RequestMythicPlusSeasonData(ClientPacket packet)
	{
        _session.SendPacket(new MythicPlusSeasonData());
	}
}
