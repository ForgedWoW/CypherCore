// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Entities.Objects;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.


namespace Game.Common.Networking.Packets.Calendar;

struct CalendarAddEventInviteInfo
{
	public void Read(WorldPacket data)
	{
		Guid = data.ReadPackedGuid();
		Status = data.ReadUInt8();
		Moderator = data.ReadUInt8();

		var hasUnused801_1 = data.HasBit();
		var hasUnused801_2 = data.HasBit();
		var hasUnused801_3 = data.HasBit();

		if (hasUnused801_1)
			Unused801_1 = data.ReadPackedGuid();

		if (hasUnused801_2)
			Unused801_2 = data.ReadUInt64();

		if (hasUnused801_3)
			Unused801_3 = data.ReadUInt64();
	}

	public ObjectGuid Guid;
	public byte Status;
	public byte Moderator;
	public ObjectGuid? Unused801_1;
	public ulong? Unused801_2;
	public ulong? Unused801_3;
}
