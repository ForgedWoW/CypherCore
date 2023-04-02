// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Networking.Packets.Bpay;

public class DistributionAssignToTarget : ClientPacket
{
    public DistributionAssignToTarget(WorldPacket packet) : base(packet) { }

    public ushort ChoiceID { get; set; } = 0;
    public ulong DistributionID { get; set; } = 0;
    public uint ProductID { get; set; } = 0;
    public ushort SpecializationID { get; set; } = 0;
    public ObjectGuid TargetCharacter { get; set; } = new();
    public override void Read()
    {
        ProductID = WorldPacket.ReadUInt32();
        DistributionID = WorldPacket.ReadUInt64();
        TargetCharacter = WorldPacket.ReadPackedGuid();
        SpecializationID = WorldPacket.ReadUInt16();
        ChoiceID = WorldPacket.ReadUInt16();
    }
}