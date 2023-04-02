// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.Ticket;

internal class BugReport : ClientPacket
{
    public string DiagInfo;
    public string Text;
    public uint Type;
    public BugReport(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        Type = WorldPacket.ReadBit();
        var diagLen = WorldPacket.ReadBits<uint>(12);
        var textLen = WorldPacket.ReadBits<uint>(10);
        DiagInfo = WorldPacket.ReadString(diagLen);
        Text = WorldPacket.ReadString(textLen);
    }
}