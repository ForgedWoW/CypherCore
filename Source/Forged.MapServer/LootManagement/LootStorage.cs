// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.


// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.LootManagement;

public class LootStorage
{
    public LootStore Creature;
    public LootStore Fishing;
    public LootStore Gameobject;
    public LootStore Items;
    public LootStore Mail;
    public LootStore Milling;
    public LootStore Pickpocketing;
    public LootStore Reference;
    public LootStore Skinning;
    public LootStore Disenchant;
    public LootStore Prospecting;
    public LootStore Spell;
}

public enum LootStorageType
{
    Creature,
    Fishing,
    Gameobject,
    Items,
    Mail,
    Milling,
    Pickpocketing,
    Reference,
    Skinning,
    Disenchant,
    Prospecting,
    Spell
}