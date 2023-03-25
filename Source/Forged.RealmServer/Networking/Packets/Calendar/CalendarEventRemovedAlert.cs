// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.RealmServer.Networking.Packets;

class CalendarEventRemovedAlert : ServerPacket
{
	public ulong EventID;
	public long Date;
	public bool ClearPending;
	public CalendarEventRemovedAlert() : base(ServerOpcodes.CalendarEventRemovedAlert) { }

	public override void Write()
	{
		_worldPacket.WriteUInt64(EventID);
		_worldPacket.WritePackedTime(Date);

		_worldPacket.WriteBit(ClearPending);
		_worldPacket.FlushBits();
	}
}