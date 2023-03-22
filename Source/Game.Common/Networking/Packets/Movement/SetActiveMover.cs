// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;

namespace Game.Networking.Packets;

public class SetActiveMover : ClientPacket
{
	public ObjectGuid ActiveMover;

	public SetActiveMover(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		ActiveMover = _worldPacket.ReadPackedGuid();
	}
}