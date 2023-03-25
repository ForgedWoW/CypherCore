﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;

namespace Game.Networking.Packets;

class ReadItemResultOK : ServerPacket
{
	public ObjectGuid Item;
	public ReadItemResultOK() : base(ServerOpcodes.ReadItemResultOk) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(Item);
	}
}