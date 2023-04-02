// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.Bpay;

public class ConfirmPurchaseResponse : ClientPacket
{
    public ConfirmPurchaseResponse(WorldPacket packet) : base(packet) { }

    public ulong ClientCurrentPriceFixedPoint { get; set; } = 0;
    public bool ConfirmPurchase { get; set; } = false;
    public uint ServerToken { get; set; } = 0;
    public override void Read()
    {
        ConfirmPurchase = WorldPacket.ReadBool();
        ServerToken = WorldPacket.ReadUInt32();
        ClientCurrentPriceFixedPoint = WorldPacket.ReadUInt64();
    }
}