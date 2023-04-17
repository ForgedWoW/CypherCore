// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Networking.Packets.Bpay;

public class BpayDistributionObject
{
    public ulong DistributionID { get; set; } = 0;
    public BpayProduct Product { get; set; }
    public uint ProductID { get; set; } = 0;
    public ulong PurchaseID { get; set; } = 0;
    public bool Revoked { get; set; } = false;
    public uint Status { get; set; } = 0;
    public uint TargetNativeRealm { get; set; } = 0;
    public ObjectGuid TargetPlayer { get; set; } = new();
    public uint TargetVirtualRealm { get; set; } = 0;

    public void Write(WorldPacket _worldPacket)
    {
        _worldPacket.Write(DistributionID);

        _worldPacket.Write(Status);
        _worldPacket.Write(ProductID);

        _worldPacket.Write(TargetPlayer);
        _worldPacket.Write(TargetVirtualRealm);
        _worldPacket.Write(TargetNativeRealm);

        _worldPacket.Write(PurchaseID);
        _worldPacket.WriteBit(Product.has_value());
        _worldPacket.WriteBit(Revoked);
        _worldPacket.FlushBits();

        if (Product.has_value())
            Product.Write(_worldPacket);
    }
}