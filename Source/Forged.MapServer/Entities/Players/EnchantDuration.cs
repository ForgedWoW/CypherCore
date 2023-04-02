// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Items;
using Framework.Constants;

namespace Forged.MapServer.Entities.Players;

public class EnchantDuration
{
    public EnchantDuration(Item item = null, EnchantmentSlot slot = EnchantmentSlot.Max, uint leftduration = 0)
    {
        Item = item;
        Slot = slot;
        Leftduration = leftduration;
    }

    public Item Item { get; set; }
    public uint Leftduration { get; set; }
    public EnchantmentSlot Slot { get; set; }
}