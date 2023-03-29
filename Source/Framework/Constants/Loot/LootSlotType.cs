// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum LootSlotType
{
    AllowLoot = 0,   // Player Can Loot The Item.
    RollOngoing = 1, // Roll Is Ongoing. Player Cannot Loot.
    Locked = 2,      // Item Is Shown In Red. Player Cannot Loot.
    Master = 3,      // Item Can Only Be Distributed By Group Loot Master.
    Owner = 4        // Ignore Binding Confirmation And Etc, For Single Player Looting
}