// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Entities.Objects;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Networking;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Networking.Packets.Addon;

namespace Game.Common.Networking.Packets.AuctionHouse;

public class AuctionRemoveItem : ClientPacket
{
	public ObjectGuid Auctioneer;
	public uint AuctionID;
	public int ItemID;
	public AddOnInfo? TaintedBy;

	public AuctionRemoveItem(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Auctioneer = _worldPacket.ReadPackedGuid();
		AuctionID = _worldPacket.ReadUInt32();
		ItemID = _worldPacket.ReadInt32();

		if (_worldPacket.HasBit())
		{
			TaintedBy = new AddOnInfo();
			TaintedBy.Value.Read(_worldPacket);
		}
	}
}
