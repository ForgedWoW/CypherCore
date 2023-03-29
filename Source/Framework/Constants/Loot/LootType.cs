// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum LootType
{
    None = 0,
    Corpse = 1,
    Pickpocketing = 2,
    Fishing = 3,
    Disenchanting = 4,
    Item = 5,
    Skinning = 6,
    GatheringNode = 8,
    Chest = 9,
    CorpsePersonal = 14,

    Fishinghole = 20, // Unsupported By Client, Sending Fishing Instead
    Insignia = 21,    // Unsupported By Client, Sending Corpse Instead
    FishingJunk = 22, // unsupported by client, sending LOOT_FISHING instead
    Prospecting = 23,
    Milling = 24
}