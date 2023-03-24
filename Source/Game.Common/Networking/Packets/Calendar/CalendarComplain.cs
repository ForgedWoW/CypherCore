// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Entities.Objects;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.


namespace Game.Common.Networking.Packets.Calendar;

public class CalendarComplain : ClientPacket
{
	ObjectGuid InvitedByGUID;
	ulong InviteID;
	ulong EventID;
	public CalendarComplain(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		InvitedByGUID = _worldPacket.ReadPackedGuid();
		EventID = _worldPacket.ReadUInt64();
		InviteID = _worldPacket.ReadUInt64();
	}
}
