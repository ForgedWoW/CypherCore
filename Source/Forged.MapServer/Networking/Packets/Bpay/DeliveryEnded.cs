// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Networking.Packets.Item;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Bpay;

public class DeliveryEnded : ServerPacket
{
    public DeliveryEnded() : base(ServerOpcodes.BattlePayDeliveryEnded) { }

    public ulong DistributionID { get; set; } = 0;
    public List<ItemInstance> Item { get; set; } = new();
    public override void Write()
    {
        WorldPacket.Write(DistributionID);

        WorldPacket.Write(Item.Count);

        foreach (var itemData in Item)
            itemData.Write(WorldPacket);
    }
}