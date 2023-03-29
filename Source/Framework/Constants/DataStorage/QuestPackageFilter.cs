// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum QuestPackageFilter : byte
{
    LootSpecialization = 0, // Players can select this quest reward if it matches their selected loot specialization
    Class = 1,              // Players can select this quest reward if it matches their class
    Unmatched = 2,          // Players can select this quest reward if no class/loot_spec rewards are available
    Everyone = 3            // Players can always select this quest reward
}