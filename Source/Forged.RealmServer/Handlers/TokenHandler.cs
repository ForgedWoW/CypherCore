// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Forged.RealmServer.Networking;
using Forged.RealmServer.Networking.Packets.Token;
using Forged.RealmServer.Server;

namespace Forged.RealmServer.Handlers;

public class TokenHandler : IWorldSessionHandler
{
    private readonly WorldSession _session;

    public TokenHandler(WorldSession session)
    {
        _session = session;
    }

    [WorldPacketHandler(ClientOpcodes.CommerceTokenGetLog)]
	void HandleCommerceTokenGetLog(CommerceTokenGetLog commerceTokenGetLog)
	{
		CommerceTokenGetLogResponse response = new();

		// @todo: fix 6.x implementation
		response.UnkInt = commerceTokenGetLog.UnkInt;
		response.Result = TokenResult.Success;

		_session.SendPacket(response);
	}

	[WorldPacketHandler(ClientOpcodes.CommerceTokenGetMarketPrice)]
	void HandleCommerceTokenGetMarketPrice(CommerceTokenGetMarketPrice commerceTokenGetMarketPrice)
	{
		CommerceTokenGetMarketPriceResponse response = new();

		// @todo: 6.x fix implementation
		response.CurrentMarketPrice = 300000000;
		response.UnkInt = commerceTokenGetMarketPrice.UnkInt;
		response.Result = TokenResult.Success;
		//packet.ReadUInt32("UnkInt32");

		_session.SendPacket(response);
	}
}
