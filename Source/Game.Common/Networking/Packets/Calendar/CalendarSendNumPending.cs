// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Networking;

namespace Game.Common.Networking.Packets.Calendar;

public class CalendarSendNumPending : ServerPacket
{
	public uint NumPending;

	public CalendarSendNumPending(uint numPending) : base(ServerOpcodes.CalendarSendNumPending)
	{
		NumPending = numPending;
	}

	public override void Write()
	{
		_worldPacket.WriteUInt32(NumPending);
	}
}
