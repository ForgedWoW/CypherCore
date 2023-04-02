// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Chat;

public class ChatMessage : ClientPacket
{
    public Language Language = Language.Universal;
    public string Text;
    public ChatMessage(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        Language = (Language)WorldPacket.ReadInt32();
        var len = WorldPacket.ReadBits<uint>(11);
        Text = WorldPacket.ReadString(len);
    }
}