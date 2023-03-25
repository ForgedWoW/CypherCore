// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Forged.RealmServer.Entities;

namespace Forged.RealmServer.Networking.Packets;

public class ChannelNotify : ServerPacket
{
	public string Sender = "";
	public ObjectGuid SenderGuid;
	public ObjectGuid SenderAccountID;
	public ChatNotify Type;
	public ChannelMemberFlags OldFlags;
	public ChannelMemberFlags NewFlags;
	public string Channel;
	public uint SenderVirtualRealm;
	public ObjectGuid TargetGuid;
	public uint TargetVirtualRealm;
	public int ChatChannelID;
	public ChannelNotify() : base(ServerOpcodes.ChannelNotify) { }

	public override void Write()
	{
		_worldPacket.WriteBits(Type, 6);
		_worldPacket.WriteBits(Channel.GetByteCount(), 7);
		_worldPacket.WriteBits(Sender.GetByteCount(), 6);

		_worldPacket.WritePackedGuid(SenderGuid);
		_worldPacket.WritePackedGuid(SenderAccountID);
		_worldPacket.WriteUInt32(SenderVirtualRealm);
		_worldPacket.WritePackedGuid(TargetGuid);
		_worldPacket.WriteUInt32(TargetVirtualRealm);
		_worldPacket.WriteInt32(ChatChannelID);

		if (Type == ChatNotify.ModeChangeNotice)
		{
			_worldPacket.WriteUInt8((byte)OldFlags);
			_worldPacket.WriteUInt8((byte)NewFlags);
		}

		_worldPacket.WriteString(Channel);
		_worldPacket.WriteString(Sender);
	}
}