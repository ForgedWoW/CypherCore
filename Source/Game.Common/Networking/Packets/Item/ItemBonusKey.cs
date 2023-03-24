// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;

namespace Game.Common.Networking.Packets.Item;

public class ItemBonusKey : IEquatable<ItemBonusKey>
{
	public uint ItemID;
	public List<uint> BonusListIDs = new();
	public List<ItemMod> Modifications = new();

	public bool Equals(ItemBonusKey right)
	{
		if (ItemID != right.ItemID)
			return false;

		if (BonusListIDs != right.BonusListIDs)
			return false;

		if (Modifications != right.Modifications)
			return false;

		return true;
	}

	public void Write(WorldPacket data)
	{
		data.WriteUInt32(ItemID);
		data.WriteInt32(BonusListIDs.Count);
		data.WriteInt32(Modifications.Count);

		if (!BonusListIDs.Empty())
			foreach (var id in BonusListIDs)
				data.WriteUInt32(id);

		foreach (var modification in Modifications)
			modification.Write(data);
	}
}
