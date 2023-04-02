// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Networking.Packets.NPC;

public class GossipSelectOption : ClientPacket
{
    public uint GossipID;
    public int GossipOptionID;
    public ObjectGuid GossipUnit;
    public string PromotionCode;
    public GossipSelectOption(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        GossipUnit = WorldPacket.ReadPackedGuid();
        GossipID = WorldPacket.ReadUInt32();
        GossipOptionID = WorldPacket.ReadInt32();

        var length = WorldPacket.ReadBits<uint>(8);
        PromotionCode = WorldPacket.ReadString(length);
    }
}