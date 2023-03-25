// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Collections;
using Framework.Database;
using Forged.RealmServer.Networking.Packets.Item;

namespace Forged.RealmServer.BlackMarket;

public class BlackMarketTemplate
{
	public uint MarketID;
	public uint SellerNPC;
	public uint Quantity;
	public ulong MinBid;
	public long Duration;
	public float Chance;
	public ItemInstance Item;

	public bool LoadFromDB(SQLFields fields)
	{
		MarketID = fields.Read<uint>(0);
		SellerNPC = fields.Read<uint>(1);
		Item = new ItemInstance();
		Item.ItemID = fields.Read<uint>(2);
		Quantity = fields.Read<uint>(3);
		MinBid = fields.Read<ulong>(4);
		Duration = fields.Read<uint>(5);
		Chance = fields.Read<float>(6);

		var bonusListIDsTok = new StringArray(fields.Read<string>(7), ' ');
		List<uint> bonusListIDs = new();

		if (!bonusListIDsTok.IsEmpty())
			foreach (string token in bonusListIDsTok)
				if (uint.TryParse(token, out var id))
					bonusListIDs.Add(id);

		if (!bonusListIDs.Empty())
		{
			Item.ItemBonus = new ItemBonuses();
			Item.ItemBonus.BonusListIDs = bonusListIDs;
		}

		if (Global.ObjectMgr.GetCreatureTemplate(SellerNPC) == null)
		{
			Log.Logger.Error("Black market template {0} does not have a valid seller. (Entry: {1})", MarketID, SellerNPC);

			return false;
		}

		if (Global.ObjectMgr.GetItemTemplate(Item.ItemID) == null)
		{
			Log.Logger.Error("Black market template {0} does not have a valid item. (Entry: {1})", MarketID, Item.ItemID);

			return false;
		}

		return true;
	}
}