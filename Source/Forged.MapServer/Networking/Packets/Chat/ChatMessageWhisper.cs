// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Chat;

public class ChatMessageWhisper : ClientPacket
{
    public Language Language = Language.Universal;
    public string Text;
    public string Target;
    public ChatMessageWhisper(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        Language = (Language)_worldPacket.ReadInt32();
        var targetLen = _worldPacket.ReadBits<uint>(9);
        var textLen = _worldPacket.ReadBits<uint>(11);
        Target = _worldPacket.ReadString(targetLen);
        Text = _worldPacket.ReadString(textLen);
    }
}