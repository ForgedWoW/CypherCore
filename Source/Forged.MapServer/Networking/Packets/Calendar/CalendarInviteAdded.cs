// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Calendar;

internal class CalendarInviteAdded : ServerPacket
{
	public ulong InviteID;
	public long ResponseTime;
	public byte Level = 100;
	public ObjectGuid InviteGuid;
	public ulong EventID;
	public byte Type;
	public bool ClearPending;
	public CalendarInviteStatus Status;
	public CalendarInviteAdded() : base(ServerOpcodes.CalendarInviteAdded) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(InviteGuid);
		_worldPacket.WriteUInt64(EventID);
		_worldPacket.WriteUInt64(InviteID);
		_worldPacket.WriteUInt8(Level);
		_worldPacket.WriteUInt8((byte)Status);
		_worldPacket.WriteUInt8(Type);
		_worldPacket.WritePackedTime(ResponseTime);

		_worldPacket.WriteBit(ClearPending);
		_worldPacket.FlushBits();
	}
}