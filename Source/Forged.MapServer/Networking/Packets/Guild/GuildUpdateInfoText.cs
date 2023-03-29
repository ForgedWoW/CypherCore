// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.Guild;

public class GuildUpdateInfoText : ClientPacket
{
    public string InfoText;
    public GuildUpdateInfoText(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        var textLen = _worldPacket.ReadBits<uint>(11);
        InfoText = _worldPacket.ReadString(textLen);
    }
}