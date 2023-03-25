﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;

namespace Game.Networking.Packets;

public class SpellDelayed : ServerPacket
{
	public ObjectGuid Caster;
	public int ActualDelay;
	public SpellDelayed() : base(ServerOpcodes.SpellDelayed, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(Caster);
		_worldPacket.WriteInt32(ActualDelay);
	}
}