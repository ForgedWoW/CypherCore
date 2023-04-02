// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Networking.Packets.Guild;

public class SaveGuildEmblem : ClientPacket
{
    public uint BColor;
    public uint Bg;
    public uint BStyle;
    public uint EColor;
    public uint EStyle;
    public ObjectGuid Vendor;
    public SaveGuildEmblem(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        Vendor = WorldPacket.ReadPackedGuid();
        EStyle = WorldPacket.ReadUInt32();
        EColor = WorldPacket.ReadUInt32();
        BStyle = WorldPacket.ReadUInt32();
        BColor = WorldPacket.ReadUInt32();
        Bg = WorldPacket.ReadUInt32();
    }
}