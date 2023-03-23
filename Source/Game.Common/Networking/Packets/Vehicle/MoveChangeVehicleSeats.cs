// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Entities.Objects;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Networking;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Networking.Packets.Movement;

namespace Game.Common.Networking.Packets.Vehicle;

public class MoveChangeVehicleSeats : ClientPacket
{
	public ObjectGuid DstVehicle;
	public MovementInfo Status;
	public byte DstSeatIndex = 255;
	public MoveChangeVehicleSeats(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Status = MovementExtensions.ReadMovementInfo(_worldPacket);
		DstVehicle = _worldPacket.ReadPackedGuid();
		DstSeatIndex = _worldPacket.ReadUInt8();
	}
}
