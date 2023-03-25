// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Game.DataStorage;

namespace Game.Entities;

public class ItemSetEffect
{
	public uint ItemSetId { get; set; }
	public List<Item> EquippedItems { get; set; } = new();
	public List<ItemSetSpellRecord> SetBonuses { get; set; } = new();
}