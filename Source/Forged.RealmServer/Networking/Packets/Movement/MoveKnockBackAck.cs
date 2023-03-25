// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.RealmServer.Networking.Packets;

class MoveKnockBackAck : ClientPacket
{
	public MovementAck Ack;
	public MoveKnockBackSpeeds? Speeds;
	public MoveKnockBackAck(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Ack.Read(_worldPacket);

		if (_worldPacket.HasBit())
		{
			Speeds = new MoveKnockBackSpeeds();
			Speeds.Value.Read(_worldPacket);
		}
	}
}