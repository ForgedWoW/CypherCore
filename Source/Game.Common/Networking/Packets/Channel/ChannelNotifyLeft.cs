// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;

namespace Game.Common.Networking.Packets.Channel;

public class ChannelNotifyLeft : ServerPacket
{
	public string Channel;
	public uint ChatChannelID;
	public bool Suspended;
	public ChannelNotifyLeft() : base(ServerOpcodes.ChannelNotifyLeft) { }

	public override void Write()
	{
		_worldPacket.WriteBits(Channel.GetByteCount(), 7);
		_worldPacket.WriteBit(Suspended);
		_worldPacket.WriteUInt32(ChatChannelID);
		_worldPacket.WriteString(Channel);
	}
}
