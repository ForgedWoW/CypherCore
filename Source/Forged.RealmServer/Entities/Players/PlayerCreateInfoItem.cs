// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.RealmServer.Entities;

public class PlayerCreateInfoItem
{
	public uint ItemId { get; set; }
	public uint Amount { get; set; }

	public PlayerCreateInfoItem(uint id, uint amount)
	{
		ItemId = id;
		Amount = amount;
	}
}