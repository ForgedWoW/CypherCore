﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;

namespace Game.Networking.Packets;

class LootRollPacket : ClientPacket
{
	public ObjectGuid LootObj;
	public byte LootListID;
	public RollVote RollType;
	public LootRollPacket(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		LootObj = _worldPacket.ReadPackedGuid();
		LootListID = _worldPacket.ReadUInt8();
		RollType = (RollVote)_worldPacket.ReadUInt8();
	}
}