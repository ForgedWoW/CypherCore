// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Bpay;

public class PurchaseListResponse : ServerPacket
{
    public PurchaseListResponse() : base(ServerOpcodes.BattlePayGetPurchaseListResponse) { }

    public List<BpayPurchase> Purchase { get; set; } = new();
    public uint Result { get; set; } = 0;

    public override void Write()
    {
        WorldPacket.Write(Result);
        WorldPacket.WriteUInt32((uint)Purchase.Count);

        foreach (var purchaseData in Purchase)
            purchaseData.Write(WorldPacket);
    }
}