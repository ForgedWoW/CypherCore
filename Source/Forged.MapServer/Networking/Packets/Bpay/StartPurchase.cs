// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Networking.Packets.Bpay;

public class StartPurchase : ClientPacket
{
    public StartPurchase(WorldPacket packet) : base(packet) { }

    public uint ClientToken { get; set; } = 0;
    public uint ProductID { get; set; } = 0;
    public ObjectGuid TargetCharacter { get; set; } = new();
    public override void Read()
    {
        ClientToken = WorldPacket.ReadUInt32();
        ProductID = WorldPacket.ReadUInt32();
        TargetCharacter = WorldPacket.ReadPackedGuid();
    }
}