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
        Vendor = _worldPacket.ReadPackedGuid();
        EStyle = _worldPacket.ReadUInt32();
        EColor = _worldPacket.ReadUInt32();
        BStyle = _worldPacket.ReadUInt32();
        BColor = _worldPacket.ReadUInt32();
        Bg = _worldPacket.ReadUInt32();
    }
}