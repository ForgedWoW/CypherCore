// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Channel;

class UserlistAdd : ServerPacket
{
	public ObjectGuid AddedUserGUID;
	public ChannelFlags ChannelFlags;
	public ChannelMemberFlags UserFlags;
	public uint ChannelID;
	public string ChannelName;
	public UserlistAdd() : base(ServerOpcodes.UserlistAdd) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(AddedUserGUID);
		_worldPacket.WriteUInt8((byte)UserFlags);
		_worldPacket.WriteUInt32((uint)ChannelFlags);
		_worldPacket.WriteUInt32(ChannelID);

		_worldPacket.WriteBits(ChannelName.GetByteCount(), 7);
		_worldPacket.FlushBits();
		_worldPacket.WriteString(ChannelName);
	}
}