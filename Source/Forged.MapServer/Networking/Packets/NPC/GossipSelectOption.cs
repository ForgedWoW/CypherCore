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
        GossipUnit = _worldPacket.ReadPackedGuid();
        GossipID = _worldPacket.ReadUInt32();
        GossipOptionID = _worldPacket.ReadInt32();

        var length = _worldPacket.ReadBits<uint>(8);
        PromotionCode = _worldPacket.ReadString(length);
    }
}