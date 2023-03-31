// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.RealmServer.Networking;
using Forged.RealmServer.Networking.Packets.MythicPlus;
using Framework.Constants;
using Game.Common.Handlers;

namespace Forged.RealmServer.Handlers;

public class MythicPlusHandler : IWorldSessionHandler
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
