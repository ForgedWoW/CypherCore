// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Chat;

public class ChatMessageWhisper : ClientPacket
{
    public Language Language = Language.Universal;
    public string Target;
    public string Text;
    public ChatMessageWhisper(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        Language = (Language)WorldPacket.ReadInt32();
        var targetLen = WorldPacket.ReadBits<uint>(9);
        var textLen = WorldPacket.ReadBits<uint>(11);
        Target = WorldPacket.ReadString(targetLen);
        Text = WorldPacket.ReadString(textLen);
    }
}