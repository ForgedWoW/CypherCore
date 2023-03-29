// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum SpellShapeshiftFormFlags
{
    Stance = 0x01,
    NotToggleable = 0x02, // player cannot cancel the aura giving this shapeshift
    PersistOnDeath = 0x04,
    CanInteractNPC = 0x08, // if the form does not have SHAPESHIFT_FORM_IS_NOT_A_SHAPESHIFT then this flag must be present to allow NPC interaction
    DontUseWeapon = 0x10,

    CanUseEquippedItems = 0x40, // if the form does not have SHAPESHIFT_FORM_IS_NOT_A_SHAPESHIFT then this flag allows equipping items without ITEM_FLAG_USABLE_WHEN_SHAPESHIFTED
    CanUseItems = 0x80,         // if the form does not have SHAPESHIFT_FORM_IS_NOT_A_SHAPESHIFT then this flag allows using items without ITEM_FLAG_USABLE_WHEN_SHAPESHIFTED
    DontAutoUnshift = 0x100,    // clientside
    ConsideredDead = 0x200,
    CanOnlyCastShapeshiftSpells = 0x400, // prevents using spells that don't have any shapeshift requirement
    StanceCancelsAtFlightmaster = 0x800,
    NoEmoteSounds = 0x1000,
    NoTriggerTeleport = 0x2000,
    CannotChangeEquippedItems = 0x4000,

    CannotUseGameObjects = 0x10000
}