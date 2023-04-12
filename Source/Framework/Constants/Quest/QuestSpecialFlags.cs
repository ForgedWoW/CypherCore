// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Framework.Constants;

[Flags]
public enum QuestSpecialFlags
{
    None = 0x00,

    // Flags For Set Specialflags In Db If Required But Used Only At Server
    Repeatable = 0x001,
    ExplorationOrEvent = 0x002, // If Required Area Explore, Spell Spell_Effect_Quest_Complete Casting, Table `*_Script` Command Script_Command_Quest_Explored Use, Set From Script)
    AutoAccept = 0x004,         // Quest Is To Be Auto-Accepted.
    DfQuest = 0x008,            // Quest Is Used By Dungeon Finder.
    Monthly = 0x010,            // Quest Is Reset At The Begining Of The Month
    // Room For More Custom Flags

    DbAllowed = Repeatable | ExplorationOrEvent | AutoAccept | DfQuest | Monthly,

    SequencedObjectives = 0x20 // Internal flag computed only
}