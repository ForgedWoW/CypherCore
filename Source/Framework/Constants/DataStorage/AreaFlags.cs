// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Framework.Constants;

[Flags]
public enum AreaFlags
{
    Snow = 0x01,                       // Snow (Only Dun Morogh, Naxxramas, Razorfen Downs And Winterspring)
    Unk1 = 0x02,                       // Razorfen Downs, Naxxramas And Acherus: The Ebon Hold (3.3.5a)
    Unk2 = 0x04,                       // Only Used For Areas On Map 571 (Development Before)
    SlaveCapital = 0x08,               // City And City Subzones
    Unk3 = 0x10,                       // Can'T Find Common Meaning
    SlaveCapital2 = 0x20,              // Slave Capital City Flag?
    AllowDuels = 0x40,                 // Allow To Duel Here
    Arena = 0x80,                      // Arena, Both Instanced And World Arenas
    Capital = 0x100,                   // Main Capital City Flag
    City = 0x200,                      // Only For One Zone Named "City" (Where It Located?)
    Outland = 0x400,                   // Expansion Zones? (Only Eye Of The Storm Not Have This Flag, But Have 0x4000 Flag)
    Sanctuary = 0x800,                 // Sanctuary Area (Pvp Disabled)
    NeedFly = 0x1000,                  // Unknown
    Unused1 = 0x2000,                  // Unused In 3.3.5a
    Outland2 = 0x4000,                 // Expansion Zones? (Only Circle Of Blood Arena Not Have This Flag, But Have 0x400 Flag)
    OutdoorPvp = 0x8000,               // Pvp Objective Area? (Death'S Door Also Has This Flag Although It'S No Pvp Object Area)
    ArenaInstance = 0x10000,           // Used By Instanced Arenas Only
    Unused2 = 0x20000,                 // Unused In 3.3.5a
    ContestedArea = 0x40000,           // On Pvp Servers These Areas Are Considered Contested, Even Though The Zone It Is Contained In Is A Horde/Alliance Territory.
    Unk6 = 0x80000,                    // Valgarde And Acherus: The Ebon Hold
    Lowlevel = 0x100000,               // Used For Some Starting Areas With AreaLevel <= 15
    Town = 0x200000,                   // Small Towns With Inn
    RestZoneHorde = 0x400000,          // Warsong Hold, Acherus: The Ebon Hold, New Agamand Inn, Vengeance Landing Inn, Sunreaver Pavilion (Something To Do With Team?)
    RestZoneAlliance = 0x800000,       // Valgarde, Acherus: The Ebon Hold, Westguard Inn, Silver Covenant Pavilion (Something To Do With Team?)
    Combat = 0x1000000,                // "combat" area (Script_GetZonePVPInfo), used
    Inside = 0x2000000,                // Used For Determinating Spell Related Inside/Outside Questions In Map.Isoutdoors
    Outside = 0x4000000,               // Used For Determinating Spell Related Inside/Outside Questions In Map.Isoutdoors
    CanHearthAndResurrect = 0x8000000, // Can Hearth And Resurrect From Area
    NoFlyZone = 0x20000000,            // Marks Zones Where You Cannot Fly
    Unk9 = 0x40000000,
}