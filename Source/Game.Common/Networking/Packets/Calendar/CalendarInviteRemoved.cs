// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;
using Game.Common.Entities.Objects;
using Game.Common.Networking;

namespace Game.Common.Networking.Packets.Calendar;

public class CalendarInviteRemoved : ServerPacket
{
	public ObjectGuid InviteGuid;
	public ulong EventID;
	public uint Flags;
	public bool ClearPending;
	public CalendarInviteRemoved() : base(ServerOpcodes.CalendarInviteRemoved) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(InviteGuid);
		_worldPacket.WriteUInt64(EventID);
		_worldPacket.WriteUInt32(Flags);

		_worldPacket.WriteBit(ClearPending);
		_worldPacket.FlushBits();
	}
}
