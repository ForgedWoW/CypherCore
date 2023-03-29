// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;
// type of Loot Item in Loot View

public enum LootRollIneligibilityReason
{
    None = 0,
    UnusableByClass = 1,       // Your class may not roll need on this item.
    MaxUniqueItemCount = 2,    // You already have the maximum amount of this item.
    CannotBeDisenchanted = 3,  // This item may not be disenchanted.
    EnchantingSkillTooLow = 4, // You do not have an Enchanter of skill %d in your group.
    NeedDisabled = 5,          // Need rolls are disabled for this item.
    OwnBetterItem = 6          // You already have a powerful version of this item.
}