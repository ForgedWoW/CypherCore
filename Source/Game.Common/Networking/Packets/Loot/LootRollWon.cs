﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;
using Game.Common.Entities.Objects;
using Game.Common.Networking;
using Game.Common.Networking.Packets.Loot;

namespace Game.Common.Networking.Packets.Loot;

public class LootRollWon : ServerPacket
{
	public ObjectGuid LootObj;
	public ObjectGuid Winner;
	public int Roll;
	public RollVote RollType;
	public LootItemData Item = new();
	public bool MainSpec;
	public LootRollWon() : base(ServerOpcodes.LootRollWon) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(LootObj);
		_worldPacket.WritePackedGuid(Winner);
		_worldPacket.WriteInt32(Roll);
		_worldPacket.WriteUInt8((byte)RollType);
		Item.Write(_worldPacket);
		_worldPacket.WriteBit(MainSpec);
		_worldPacket.FlushBits();
	}
}
