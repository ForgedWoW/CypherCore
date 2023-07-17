// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Networking;
using Forged.MapServer.Networking.Packets.Token;
using Forged.MapServer.Server;
using Framework.Constants;
using Game.Common.Handlers;

// ReSharper disable UnusedMember.Local

namespace Forged.MapServer.OpCodeHandlers;

public class TokenHandler : IWorldSessionHandler
{
    private readonly WorldSession _session;

    public TokenHandler(WorldSession session)
    {
        _session = session;
    }

    [WorldPacketHandler(ClientOpcodes.CommerceTokenGetLog)]
    private void HandleCommerceTokenGetLog(CommerceTokenGetLog commerceTokenGetLog)
    {
        _session.SendPacket(new CommerceTokenGetLogResponse()
        {
            // @todo: fix 6.x implementation
            UnkInt = commerceTokenGetLog.UnkInt,
            Result = TokenResult.Success
        });
    }

    [WorldPacketHandler(ClientOpcodes.CommerceTokenGetMarketPrice)]
    private void HandleCommerceTokenGetMarketPrice(CommerceTokenGetMarketPrice commerceTokenGetMarketPrice)
    {
        _session.SendPacket(new CommerceTokenGetMarketPriceResponse()
        {
            // @todo: 6.x fix implementation
            CurrentMarketPrice = 300000000,
            UnkInt = commerceTokenGetMarketPrice.UnkInt,
            Result = TokenResult.Success
        });
    }
}