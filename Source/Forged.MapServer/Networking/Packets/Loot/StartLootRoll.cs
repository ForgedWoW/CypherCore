// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;

namespace Game.Networking.Packets;

class StartLootRoll : ServerPacket
{
	public ObjectGuid LootObj;
	public int MapID;
	public uint RollTime;
	public LootMethod Method;
	public RollMask ValidRolls;
	public Array<LootRollIneligibilityReason> LootRollIneligibleReason = new(4);
	public LootItemData Item = new();
	public StartLootRoll() : base(ServerOpcodes.StartLootRoll) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(LootObj);
		_worldPacket.WriteInt32(MapID);
		_worldPacket.WriteUInt32(RollTime);
		_worldPacket.WriteUInt8((byte)ValidRolls);

		foreach (var reason in LootRollIneligibleReason)
			_worldPacket.WriteUInt32((uint)reason);

		_worldPacket.WriteUInt8((byte)Method);
		Item.Write(_worldPacket);
	}
}