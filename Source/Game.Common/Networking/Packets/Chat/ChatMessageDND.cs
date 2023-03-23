// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Networking;

// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Common.Networking.Packets.Chat;

public class ChatMessageDND : ClientPacket
{
	public string Text;
	public ChatMessageDND(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		var len = _worldPacket.ReadBits<uint>(11);
		Text = _worldPacket.ReadString(len);
	}
}
