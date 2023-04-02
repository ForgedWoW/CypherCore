// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.BlackMarket;

public class BlackMarketRequestItemsResult : ServerPacket
{
    public List<BlackMarketItem> Items = new();
    public long LastUpdateID;
    public BlackMarketRequestItemsResult() : base(ServerOpcodes.BlackMarketRequestItemsResult) { }

    public override void Write()
    {
        WorldPacket.WriteInt64(LastUpdateID);
        WorldPacket.WriteInt32(Items.Count);

        foreach (var item in Items)
            item.Write(WorldPacket);
    }
}