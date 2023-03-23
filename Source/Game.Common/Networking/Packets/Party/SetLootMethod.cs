// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;
using Game.Common.Entities.Objects;
using Game.Common.Networking;

namespace Game.Common.Networking.Packets.Party;

public class SetLootMethod : ClientPacket
{
	public sbyte PartyIndex;
	public ObjectGuid LootMasterGUID;
	public LootMethod LootMethod;
	public ItemQuality LootThreshold;
	public SetLootMethod(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		PartyIndex = _worldPacket.ReadInt8();
		LootMethod = (LootMethod)_worldPacket.ReadUInt8();
		LootMasterGUID = _worldPacket.ReadPackedGuid();
		LootThreshold = (ItemQuality)_worldPacket.ReadUInt32();
	}
}
