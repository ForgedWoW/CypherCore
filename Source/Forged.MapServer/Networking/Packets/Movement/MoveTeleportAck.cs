// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Networking.Packets.Movement;

class MoveTeleportAck : ClientPacket
{
	public ObjectGuid MoverGUID;
	int AckIndex;
	int MoveTime;
	public MoveTeleportAck(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		MoverGUID = _worldPacket.ReadPackedGuid();
		AckIndex = _worldPacket.ReadInt32();
		MoveTime = _worldPacket.ReadInt32();
	}
}