// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.LootManagement;

public class NotNormalLootItem
{
    public NotNormalLootItem()
    {
        LootListId = 0;
        IsLooted = false;
    }

    public NotNormalLootItem(byte index, bool islooted = false)
    {
        LootListId = index;
        IsLooted = islooted;
    }

    public bool IsLooted { get; set; }
    public byte LootListId { get; set; } // position in quest_items or items;
}