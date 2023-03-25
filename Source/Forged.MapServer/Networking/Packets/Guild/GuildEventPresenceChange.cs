// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Game.Entities;

namespace Game.Networking.Packets;

public class GuildEventPresenceChange : ServerPacket
{
	public ObjectGuid Guid;
	public uint VirtualRealmAddress;
	public string Name;
	public bool Mobile;
	public bool LoggedOn;
	public GuildEventPresenceChange() : base(ServerOpcodes.GuildEventPresenceChange) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(Guid);
		_worldPacket.WriteUInt32(VirtualRealmAddress);

		_worldPacket.WriteBits(Name.GetByteCount(), 6);
		_worldPacket.WriteBit(LoggedOn);
		_worldPacket.WriteBit(Mobile);

		_worldPacket.WriteString(Name);
	}
}