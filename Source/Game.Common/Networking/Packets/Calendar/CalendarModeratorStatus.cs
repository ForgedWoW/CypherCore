// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;

namespace Game.Networking.Packets;

public class CalendarModeratorStatus : ServerPacket
{
	public ObjectGuid InviteGuid;
	public ulong EventID;
	public CalendarInviteStatus Status;
	public bool ClearPending;
	public CalendarModeratorStatus() : base(ServerOpcodes.CalendarModeratorStatus) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(InviteGuid);
		_worldPacket.WriteUInt64(EventID);
		_worldPacket.WriteUInt8((byte)Status);

		_worldPacket.WriteBit(ClearPending);
		_worldPacket.FlushBits();
	}
}