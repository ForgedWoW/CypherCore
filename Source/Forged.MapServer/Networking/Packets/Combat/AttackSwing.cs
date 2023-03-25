// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;

namespace Game.Networking.Packets;

public class AttackSwing : ClientPacket
{
	public ObjectGuid Victim;
	public AttackSwing(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Victim = _worldPacket.ReadPackedGuid();
	}
}

//Structs