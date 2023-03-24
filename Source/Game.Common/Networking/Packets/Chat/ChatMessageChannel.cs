// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Common.Entities.Objects;

namespace Game.Common.Networking.Packets.Chat;

public class ChatMessageChannel : ClientPacket
{
	public Language Language = Language.Universal;
	public ObjectGuid ChannelGUID;
	public string Text;
	public string Target;
	public ChatMessageChannel(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Language = (Language)_worldPacket.ReadInt32();
		ChannelGUID = _worldPacket.ReadPackedGuid();
		var targetLen = _worldPacket.ReadBits<uint>(9);
		var textLen = _worldPacket.ReadBits<uint>(11);
		Target = _worldPacket.ReadString(targetLen);
		Text = _worldPacket.ReadString(textLen);
	}
}
