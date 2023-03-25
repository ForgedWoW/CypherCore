// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Loot;

class LootList : ServerPacket
{
	public ObjectGuid Owner;
	public ObjectGuid LootObj;
	public ObjectGuid? Master;
	public ObjectGuid? RoundRobinWinner;
	public LootList() : base(ServerOpcodes.LootList, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(Owner);
		_worldPacket.WritePackedGuid(LootObj);

		_worldPacket.WriteBit(Master.HasValue);
		_worldPacket.WriteBit(RoundRobinWinner.HasValue);
		_worldPacket.FlushBits();

		if (Master.HasValue)
			_worldPacket.WritePackedGuid(Master.Value);

		if (RoundRobinWinner.HasValue)
			_worldPacket.WritePackedGuid(RoundRobinWinner.Value);
	}
}