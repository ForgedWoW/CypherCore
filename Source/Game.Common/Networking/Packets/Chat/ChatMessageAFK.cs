// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.


// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Common.Networking.Packets.Chat;

public class ChatMessageAFK : ClientPacket
{
	public string Text;
	public ChatMessageAFK(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		var len = _worldPacket.ReadBits<uint>(11);
		Text = _worldPacket.ReadString(len);
	}
}
