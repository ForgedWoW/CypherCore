// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Networking.Packets.Item;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Bpay;

public class DeliveryEnded : ServerPacket
{
	public List<ItemInstance> Item { get; set; } = new();
	public ulong DistributionID { get; set; } = 0;

	public DeliveryEnded() : base(ServerOpcodes.BattlePayDeliveryEnded) { }

	public override void Write()
	{
		_worldPacket.Write(DistributionID);

		_worldPacket.Write(Item.Count);

		foreach (var itemData in Item)
			itemData.Write(_worldPacket);
	}
}