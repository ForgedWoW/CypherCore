// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;

namespace Game.Networking.Packets;

class BlackMarketRequestItems : ClientPacket
{
	public ObjectGuid Guid;
	public long LastUpdateID;
	public BlackMarketRequestItems(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Guid = _worldPacket.ReadPackedGuid();
		LastUpdateID = _worldPacket.ReadInt64();
	}
}