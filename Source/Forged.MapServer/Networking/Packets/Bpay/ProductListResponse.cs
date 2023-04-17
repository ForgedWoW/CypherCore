// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Bpay;

public class ProductListResponse : ServerPacket
{
    public ProductListResponse() : base(ServerOpcodes.BattlePayGetProductListResponse) { }

    public uint CurrencyID { get; set; } = 0;
    public List<BpayGroup> ProductGroups { get; set; } = new();
    public List<BpayProductInfo> ProductInfos { get; set; } = new();
    public List<BpayProduct> Products { get; set; } = new();
    public uint Result { get; set; } = 0;
    public List<BpayShop> Shops { get; set; } = new();

    public override void Write()
    {
        WorldPacket.Write(Result);
        WorldPacket.Write(CurrencyID);
        WorldPacket.WriteUInt32((uint)ProductInfos.Count);
        WorldPacket.WriteUInt32((uint)Products.Count);
        WorldPacket.WriteUInt32((uint)ProductGroups.Count);
        WorldPacket.WriteUInt32((uint)Shops.Count);

        foreach (var p in ProductInfos)
            p.Write(WorldPacket);

        foreach (var p in Products)
            p.Write(WorldPacket);

        foreach (var p in ProductGroups)
            p.Write(WorldPacket);

        foreach (var p in Shops)
            p.Write(WorldPacket);
    }
}