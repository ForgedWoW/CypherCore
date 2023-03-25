// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.Item;

class ItemPurchaseContents
{
	public ulong Money;
	public ItemPurchaseRefundItem[] Items = new ItemPurchaseRefundItem[5];
	public ItemPurchaseRefundCurrency[] Currencies = new ItemPurchaseRefundCurrency[5];

	public void Write(WorldPacket data)
	{
		data.WriteUInt64(Money);

		for (var i = 0; i < 5; ++i)
			Items[i].Write(data);

		for (var i = 0; i < 5; ++i)
			Currencies[i].Write(data);
	}
}