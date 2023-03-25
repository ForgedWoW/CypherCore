﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;

namespace Game.Networking.Packets;

class RolePollInform : ServerPacket
{
	public sbyte PartyIndex;
	public ObjectGuid From;
	public RolePollInform() : base(ServerOpcodes.RolePollInform) { }

	public override void Write()
	{
		_worldPacket.WriteInt8(PartyIndex);
		_worldPacket.WritePackedGuid(From);
	}
}