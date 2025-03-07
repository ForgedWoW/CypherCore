﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;

namespace Game.Networking.Packets;

public class AttackStart : ServerPacket
{
	public ObjectGuid Attacker;
	public ObjectGuid Victim;
	public AttackStart() : base(ServerOpcodes.AttackStart, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(Attacker);
		_worldPacket.WritePackedGuid(Victim);
	}
}