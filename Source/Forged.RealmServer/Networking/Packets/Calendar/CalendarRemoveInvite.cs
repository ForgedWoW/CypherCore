// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.RealmServer.Entities;

namespace Forged.RealmServer.Networking.Packets;

class CalendarRemoveInvite : ClientPacket
{
	public ObjectGuid Guid;
	public ulong EventID;
	public ulong ModeratorID;
	public ulong InviteID;
	public CalendarRemoveInvite(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Guid = _worldPacket.ReadPackedGuid();
		InviteID = _worldPacket.ReadUInt64();
		ModeratorID = _worldPacket.ReadUInt64();
		EventID = _worldPacket.ReadUInt64();
	}
}