// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Framework.Constants;

[Flags]
public enum GameObjectFlags
{
    InUse = 0x01,         // Disables Interaction While Animated
    Locked = 0x02,        // Require Key, Spell, Event, Etc To Be Opened. Makes "Locked" Appear In Tooltip
    InteractCond = 0x04,  // cannot interact (condition to interact - requires GO_DYNFLAG_LO_ACTIVATE to enable interaction clientside)
    Transport = 0x08,     // Any Kind Of Transport? Object Can Transport (Elevator, Boat, Car)
    NotSelectable = 0x10, // Not Selectable Even In Gm Mode
    NoDespawn = 0x20,     // Never Despawn, Typically For Doors, They Just Change State
    AiObstacle = 0x40,    // makes the client register the object in something called AIObstacleMgr, unknown what it does
    FreezeAnimation = 0x80,
    Damaged = 0x200,
    Destroyed = 0x400,

    IgnoreCurrentStateForUseSpell = 0x4000,                // Allows casting use spell without checking current state (opening open gameobjects, unlocking unlocked gameobjects and closing closed gameobjects)
    InteractDistanceIgnoresModel = 0x8000,                 // Client completely ignores model bounds for interaction distance check
    IgnoreCurrentStateForUseSpellExceptUnlocked = 0x40000, // Allows casting use spell without checking current state except unlocking unlocked gamobjets (opening open gameobjects and closing closed gameobjects)
    InteractDistanceUsesTemplateModel = 0x80000,           // client checks interaction distance from model sent in SMSG_QUERY_GAMEOBJECT_RESPONSE instead of GAMEOBJECT_DISPLAYID
    MapObject = 0x100000,                                  // pre-7.0 model loading used to be controlled by file extension (wmo vs m2)
    InMultiUse = 0x200000,                                 // GO_FLAG_IN_USE equivalent for objects usable by multiple players
    LowPrioritySelection = 0x4000000,                      // client will give lower cursor priority to this object when multiple objects overlap
}