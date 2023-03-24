﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Entities.Objects;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.


namespace Game.Common.Networking.Packets.Vehicle;

public class RequestVehicleSwitchSeat : ClientPacket
{
	public ObjectGuid Vehicle;
	public byte SeatIndex = 255;
	public RequestVehicleSwitchSeat(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Vehicle = _worldPacket.ReadPackedGuid();
		SeatIndex = _worldPacket.ReadUInt8();
	}
}
