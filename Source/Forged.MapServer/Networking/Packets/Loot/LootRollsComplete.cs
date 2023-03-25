﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;

namespace Game.Networking.Packets;

class LootRollsComplete : ServerPacket
{
	public ObjectGuid LootObj;
	public byte LootListID;
	public LootRollsComplete() : base(ServerOpcodes.LootRollsComplete) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(LootObj);
		_worldPacket.WriteUInt8(LootListID);
	}
}