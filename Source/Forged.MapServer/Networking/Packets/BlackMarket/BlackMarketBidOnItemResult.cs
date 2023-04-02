// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Networking.Packets.Item;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.BlackMarket;

internal class BlackMarketBidOnItemResult : ServerPacket
{
    public ItemInstance Item;
    public uint MarketID;
    public BlackMarketError Result;
    public BlackMarketBidOnItemResult() : base(ServerOpcodes.BlackMarketBidOnItemResult) { }

    public override void Write()
    {
        WorldPacket.WriteUInt32(MarketID);
        WorldPacket.WriteUInt32((uint)Result);
        Item.Write(WorldPacket);
    }
}