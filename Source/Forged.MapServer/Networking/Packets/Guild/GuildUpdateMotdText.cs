// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.Guild;

public class GuildUpdateMotdText : ClientPacket
{
    public string MotdText;
    public GuildUpdateMotdText(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        var textLen = WorldPacket.ReadBits<uint>(11);
        MotdText = WorldPacket.ReadString(textLen);
    }
}