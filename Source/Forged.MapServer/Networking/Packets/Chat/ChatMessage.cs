// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Chat;

public class ChatMessage : ClientPacket
{
	public string Text;
	public Language Language = Language.Universal;
	public ChatMessage(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Language = (Language)_worldPacket.ReadInt32();
		var len = _worldPacket.ReadBits<uint>(11);
		Text = _worldPacket.ReadString(len);
	}
}