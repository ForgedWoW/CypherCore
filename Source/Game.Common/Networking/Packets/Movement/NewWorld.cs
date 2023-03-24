// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Common.Entities.Objects;

namespace Game.Common.Networking.Packets.Movement;

public class NewWorld : ServerPacket
{
	public uint MapID;
	public uint Reason;
	public TeleportLocation Loc = new();
	public Position MovementOffset; // Adjusts all pending movement events by this offset
	public NewWorld() : base(ServerOpcodes.NewWorld) { }

	public override void Write()
	{
		_worldPacket.WriteUInt32(MapID);
		Loc.Write(_worldPacket);
		_worldPacket.WriteUInt32(Reason);
		_worldPacket.WriteXYZ(MovementOffset);
	}
}
