// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Chat;

class ChatServerMessage : ServerPacket
{
	public int MessageID;
	public string StringParam = "";
	public ChatServerMessage() : base(ServerOpcodes.ChatServerMessage) { }

	public override void Write()
	{
		_worldPacket.WriteInt32(MessageID);

		_worldPacket.WriteBits(StringParam.GetByteCount(), 11);
		_worldPacket.WriteString(StringParam);
	}
}