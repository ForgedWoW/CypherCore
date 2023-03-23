// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Game.Entities;
using Game.Common.Entities.Objects;
using Game.Common.Networking;

namespace Game.Common.Networking.Packets.Channel;

public class UserlistUpdate : ServerPacket
{
	public ObjectGuid UpdatedUserGUID;
	public ChannelFlags ChannelFlags;
	public ChannelMemberFlags UserFlags;
	public uint ChannelID;
	public string ChannelName;
	public UserlistUpdate() : base(ServerOpcodes.UserlistUpdate) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(UpdatedUserGUID);
		_worldPacket.WriteUInt8((byte)UserFlags);
		_worldPacket.WriteUInt32((uint)ChannelFlags);
		_worldPacket.WriteUInt32(ChannelID);

		_worldPacket.WriteBits(ChannelName.GetByteCount(), 7);
		_worldPacket.FlushBits();
		_worldPacket.WriteString(ChannelName);
	}
}
