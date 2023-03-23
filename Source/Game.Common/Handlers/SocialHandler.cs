// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Framework.Database;
using Game.DataStorage;
using Game.Entities;
using Game;
using Game.Common.Networking;
using Game.Common.Networking.Packets.Social;

namespace Game.Common.Handlers;

public class SocialHandler
{
	[WorldPacketHandler(ClientOpcodes.SocialContractRequest)]
	void HandleSocialContractRequest(SocialContractRequest socialContractRequest)
	{
		SocialContractRequestResponse response = new();
		response.ShowSocialContract = false;
		SendPacket(response);
	}
}
