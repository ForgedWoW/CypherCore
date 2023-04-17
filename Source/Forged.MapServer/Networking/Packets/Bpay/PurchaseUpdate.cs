// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Bpay;

public class PurchaseUpdate : ServerPacket
{
    public PurchaseUpdate() : base(ServerOpcodes.BattlePayPurchaseUpdate) { }

    public List<BpayPurchase> Purchase { get; set; } = new();

    public override void Write()
    {
        WorldPacket.WriteUInt32((uint)Purchase.Count);

        foreach (var purchaseData in Purchase)
            purchaseData.Write(WorldPacket);
    }
}