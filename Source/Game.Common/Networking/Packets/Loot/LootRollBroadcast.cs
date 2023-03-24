// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Common.Entities.Objects;

namespace Game.Common.Networking.Packets.Loot;

public class LootRollBroadcast : ServerPacket
{
	public ObjectGuid LootObj;
	public ObjectGuid Player;
	public int Roll; // Roll value can be negative, it means that it is an "offspec" roll but only during roll selection broadcast (not when sending the result)
	public RollVote RollType;
	public LootItemData Item = new();
	public bool Autopassed; // Triggers message |HlootHistory:%d|h[Loot]|h: You automatically passed on: %s because you cannot loot that item.
	public LootRollBroadcast() : base(ServerOpcodes.LootRoll) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(LootObj);
		_worldPacket.WritePackedGuid(Player);
		_worldPacket.WriteInt32(Roll);
		_worldPacket.WriteUInt8((byte)RollType);
		Item.Write(_worldPacket);
		_worldPacket.WriteBit(Autopassed);
		_worldPacket.FlushBits();
	}
}
