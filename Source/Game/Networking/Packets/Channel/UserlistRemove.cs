// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Game.Entities;

namespace Game.Networking.Packets;

class UserlistRemove : ServerPacket
{
	public ObjectGuid RemovedUserGUID;
	public ChannelFlags ChannelFlags;
	public uint ChannelID;
	public string ChannelName;
	public UserlistRemove() : base(ServerOpcodes.UserlistRemove) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(RemovedUserGUID);
		_worldPacket.WriteUInt32((uint)ChannelFlags);
		_worldPacket.WriteUInt32(ChannelID);

		_worldPacket.WriteBits(ChannelName.GetByteCount(), 7);
		_worldPacket.FlushBits();
		_worldPacket.WriteString(ChannelName);
	}
}