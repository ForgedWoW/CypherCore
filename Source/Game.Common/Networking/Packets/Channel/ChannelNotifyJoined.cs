// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Game.Common.Entities.Objects;

namespace Game.Common.Networking.Packets.Channel;

public class ChannelNotifyJoined : ServerPacket
{
	public string ChannelWelcomeMsg = "";
	public int ChatChannelID;
	public ulong InstanceID;
	public ChannelFlags ChannelFlags;
	public string Channel = "";
	public ObjectGuid ChannelGUID;
	public ChannelNotifyJoined() : base(ServerOpcodes.ChannelNotifyJoined) { }

	public override void Write()
	{
		_worldPacket.WriteBits(Channel.GetByteCount(), 7);
		_worldPacket.WriteBits(ChannelWelcomeMsg.GetByteCount(), 11);
		_worldPacket.WriteUInt32((uint)ChannelFlags);
		_worldPacket.WriteInt32(ChatChannelID);
		_worldPacket.WriteUInt64(InstanceID);
		_worldPacket.WritePackedGuid(ChannelGUID);
		_worldPacket.WriteString(Channel);
		_worldPacket.WriteString(ChannelWelcomeMsg);
	}
}
