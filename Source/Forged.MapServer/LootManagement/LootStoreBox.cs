// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.LootManagement;

public class LootStoreBox
{
    public LootStore Creature { get; set; }
    public LootStore Disenchant { get; set; }
    public LootStore Fishing { get; set; }
    public LootStore Gameobject { get; set; }
    public LootStore Items { get; set; }
    public LootStore Mail { get; set; }
    public LootStore Milling { get; set; }
    public LootStore Pickpocketing { get; set; }
    public LootStore Prospecting { get; set; }
    public LootStore Reference { get; set; }
    public LootStore Skinning { get; set; }
    public LootStore Spell { get; set; }
}