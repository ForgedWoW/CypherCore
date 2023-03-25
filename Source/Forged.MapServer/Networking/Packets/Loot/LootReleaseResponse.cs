﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;

namespace Game.Networking.Packets;

class LootReleaseResponse : ServerPacket
{
	public ObjectGuid LootObj;
	public ObjectGuid Owner;
	public LootReleaseResponse() : base(ServerOpcodes.LootRelease) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(LootObj);
		_worldPacket.WritePackedGuid(Owner);
	}
}