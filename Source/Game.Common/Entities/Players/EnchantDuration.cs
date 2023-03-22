// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Game.Entities;

public class EnchantDuration
{
	public Item Item { get; set; }
	public EnchantmentSlot Slot { get; set; }
	public uint Leftduration { get; set; }

	public EnchantDuration(Item item = null, EnchantmentSlot slot = EnchantmentSlot.Max, uint leftduration = 0)
	{
		Item = item;
		Slot = slot;
		Leftduration = leftduration;
	}
}