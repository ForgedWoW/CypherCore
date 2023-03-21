// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Forged.RealmServer.Entities;

namespace Forged.RealmServer.Networking.Packets;

public class GuildEventNewLeader : ServerPacket
{
	public ObjectGuid NewLeaderGUID;
	public string NewLeaderName;
	public uint NewLeaderVirtualRealmAddress;
	public ObjectGuid OldLeaderGUID;
	public string OldLeaderName = "";
	public uint OldLeaderVirtualRealmAddress;
	public bool SelfPromoted;
	public GuildEventNewLeader() : base(ServerOpcodes.GuildEventNewLeader) { }

	public override void Write()
	{
		_worldPacket.WriteBit(SelfPromoted);
		_worldPacket.WriteBits(OldLeaderName.GetByteCount(), 6);
		_worldPacket.WriteBits(NewLeaderName.GetByteCount(), 6);

		_worldPacket.WritePackedGuid(OldLeaderGUID);
		_worldPacket.WriteUInt32(OldLeaderVirtualRealmAddress);
		_worldPacket.WritePackedGuid(NewLeaderGUID);
		_worldPacket.WriteUInt32(NewLeaderVirtualRealmAddress);

		_worldPacket.WriteString(OldLeaderName);
		_worldPacket.WriteString(NewLeaderName);
	}
}