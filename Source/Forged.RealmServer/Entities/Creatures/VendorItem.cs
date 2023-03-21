// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;

namespace Forged.RealmServer.Entities;

public class VendorItem
{
	public uint Item { get; set; }
	public uint Maxcount { get; set; } // 0 for infinity item amount
	public uint Incrtime { get; set; } // time for restore items amount if maxcount != 0
	public uint ExtendedCost { get; set; }
	public ItemVendorType Type { get; set; }
	public List<uint> BonusListIDs { get; set; } = new();
	public uint PlayerConditionId { get; set; }
	public bool IgnoreFiltering { get; set; }

	public VendorItem() { }

	public VendorItem(uint item, int maxcount, uint incrtime, uint extendedCost, ItemVendorType type)
	{
		Item = item;
		Maxcount = (uint)maxcount;
		Incrtime = incrtime;
		ExtendedCost = extendedCost;
		Type = type;
	}
}