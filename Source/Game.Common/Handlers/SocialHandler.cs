// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Common.Networking;
using Game.Common.Networking.Packets.Social;
using Game.Common.Server;

namespace Game.Common.Handlers;

public class SocialHandler
{
    private readonly WorldSession _session;

    public SocialHandler(WorldSession session)
    {
        _session = session;
    }

    [WorldPacketHandler(ClientOpcodes.SocialContractRequest)]
	void HandleSocialContractRequest(SocialContractRequest socialContractRequest)
	{
		SocialContractRequestResponse response = new();
		response.ShowSocialContract = false;
        _session.SendPacket(response);
	}
}
