﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Game.Networking.Packets;

public class SpellStart : ServerPacket
{
	public SpellCastData Cast;

	public SpellStart() : base(ServerOpcodes.SpellStart, ConnectionType.Instance)
	{
		Cast = new SpellCastData();
	}

	public override void Write()
	{
		Cast.Write(_worldPacket);
	}
}