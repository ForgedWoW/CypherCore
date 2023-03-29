// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum ConditionSourceType
{
    None = 0,
    CreatureLootTemplate = 1,
    DisenchantLootTemplate = 2,
    FishingLootTemplate = 3,
    GameobjectLootTemplate = 4,
    ItemLootTemplate = 5,
    MailLootTemplate = 6,
    MillingLootTemplate = 7,
    PickpocketingLootTemplate = 8,
    ProspectingLootTemplate = 9,
    ReferenceLootTemplate = 10,
    SkinningLootTemplate = 11,
    SpellLootTemplate = 12,
    SpellImplicitTarget = 13,
    GossipMenu = 14,
    GossipMenuOption = 15,
    CreatureTemplateVehicle = 16,
    Spell = 17,
    SpellClickEvent = 18,
    QuestAvailable = 19,

    // Condition source type 20 unused
    VehicleSpell = 21,
    SmartEvent = 22,
    NpcVendor = 23,
    SpellProc = 24,
    TerrainSwap = 25,
    Phase = 26,
    Graveyard = 27,
    AreaTrigger = 28,
    ConversationLine = 29,
    AreatriggerClientTriggered = 30,
    TrainerSpell = 31,
    ObjectIdVisibility = 32,
    SpawnGroup = 33,
    Max
}