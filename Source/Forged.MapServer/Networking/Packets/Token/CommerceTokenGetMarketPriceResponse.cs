// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Token;

internal class CommerceTokenGetMarketPriceResponse : ServerPacket
{
    public uint AuctionDuration;
    public ulong CurrentMarketPrice;
    public TokenResult Result;
    public uint UnkInt; // send CMSG_REQUEST_WOW_TOKEN_MARKET_PRICE
                        // preset auction duration enum
    public CommerceTokenGetMarketPriceResponse() : base(ServerOpcodes.CommerceTokenGetMarketPriceResponse) { }

    public override void Write()
    {
        _worldPacket.WriteUInt64(CurrentMarketPrice);
        _worldPacket.WriteUInt32(UnkInt);
        _worldPacket.WriteUInt32((uint)Result);
        _worldPacket.WriteUInt32(AuctionDuration);
    }
}