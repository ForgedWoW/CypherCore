// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Serilog;

namespace Forged.MapServer.Networking.Packets.Bpay;

public class SyncWowEntitlements : ServerPacket
{
    public SyncWowEntitlements() : base(ServerOpcodes.SyncWowEntitlements) { }

    public List<BpayProduct> Product { get; set; } = new();
    public List<uint> ProductCount { get; set; } = new();
    public List<uint> PurchaseCount { get; set; } = new();
    /*void WorldPackets::BattlePay::PurchaseProduct::Read()
    {
        _worldPacket >> ClientToken;
        _worldPacket >> ProductID;
        _worldPacket >> TargetCharacter;
   
        uint32 strlen1 = _worldPacket.ReadBits(6);
        uint32 strlen2 = _worldPacket.ReadBits(12);
        WowSytem = _worldPacket.ReadString(strlen1);
        PublicKey = _worldPacket.ReadString(strlen2);
    }*/

    public override void Write()
    {
        Log.Logger.Information("SyncWowEntitlements");
        WorldPacket.WriteUInt32((uint)PurchaseCount.Count);
        WorldPacket.WriteUInt32((uint)Product.Count);

        foreach (var purchases in PurchaseCount)
        {
            WorldPacket.WriteUInt32(0);  // productID ?
            WorldPacket.WriteUInt32(0);  // flags?
            WorldPacket.WriteUInt32(0);  // idem to flags?
            WorldPacket.WriteUInt32(0);  // always 0
            WorldPacket.WriteBits(0, 7); // always 0
            WorldPacket.WriteBit(false); // always false
        }

        foreach (var product in Product)
        {
            WorldPacket.Write(product.ProductId);
            WorldPacket.Write(product.Type);
            WorldPacket.Write(product.Flags);
            WorldPacket.Write(product.Unk1);
            WorldPacket.Write(product.DisplayId);
            WorldPacket.Write(product.ItemId);
            WorldPacket.WriteUInt32(0);
            WorldPacket.WriteUInt32(2);
            WorldPacket.WriteUInt32(0);
            WorldPacket.WriteUInt32(0);
            WorldPacket.WriteUInt32(0);
            WorldPacket.WriteUInt32(0);

            WorldPacket.WriteBits((uint)product.UnkString.Length, 8);
            WorldPacket.WriteBit(product.UnkBits != 0);
            WorldPacket.WriteBit(product.UnkBit);
            WorldPacket.WriteBits((uint)product.Items.Count, 7);
            WorldPacket.WriteBit(product.Display != null);
            WorldPacket.WriteBit(false); // unk

            if (product.UnkBits != 0)
                WorldPacket.WriteBits(product.UnkBits, 4);

            WorldPacket.FlushBits();

            foreach (var productItem in product.Items)
            {
                WorldPacket.WriteUInt32(productItem.ID);
                WorldPacket.WriteUInt8(productItem.UnkByte);
                WorldPacket.WriteUInt32(productItem.ItemID);
                WorldPacket.WriteUInt32(productItem.Quantity);
                WorldPacket.WriteUInt32(productItem.UnkInt1);
                WorldPacket.WriteUInt32(productItem.UnkInt2);

                WorldPacket.WriteBit(productItem.IsPet);
                WorldPacket.WriteBit(productItem.PetResult != 0);
                WorldPacket.WriteBit(productItem.Display != null);

                if (productItem.PetResult != 0)
                    WorldPacket.WriteBits(productItem.PetResult, 4);

                WorldPacket.FlushBits();

                productItem.Display?.Write(WorldPacket);
            }

            WorldPacket.WriteString(product.UnkString);

            product.Display?.Write(WorldPacket);
        }
    }
}