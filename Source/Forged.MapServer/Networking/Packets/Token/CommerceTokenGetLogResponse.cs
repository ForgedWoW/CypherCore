// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Token;

internal class CommerceTokenGetLogResponse : ServerPacket
{
    public TokenResult Result;
    public uint UnkInt; // send CMSG_UPDATE_WOW_TOKEN_AUCTIONABLE_LIST
    private readonly List<AuctionableTokenInfo> AuctionableTokenAuctionableList = new();
    public CommerceTokenGetLogResponse() : base(ServerOpcodes.CommerceTokenGetLogResponse, ConnectionType.Instance) { }

    public override void Write()
    {
        WorldPacket.WriteUInt32(UnkInt);
        WorldPacket.WriteUInt32((uint)Result);
        WorldPacket.WriteInt32(AuctionableTokenAuctionableList.Count);

        foreach (var auctionableTokenAuctionable in AuctionableTokenAuctionableList)
        {
            WorldPacket.WriteUInt64(auctionableTokenAuctionable.UnkInt1);
            WorldPacket.WriteInt64(auctionableTokenAuctionable.UnkInt2);
            WorldPacket.WriteUInt64(auctionableTokenAuctionable.BuyoutPrice);
            WorldPacket.WriteUInt32(auctionableTokenAuctionable.Owner);
            WorldPacket.WriteUInt32(auctionableTokenAuctionable.DurationLeft);
        }
    }

    private struct AuctionableTokenInfo
    {
        public ulong BuyoutPrice;
        public uint DurationLeft;
        public uint Owner;
        public ulong UnkInt1;
        public long UnkInt2;
    }
}