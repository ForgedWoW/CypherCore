// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Framework.Constants;

[Flags]
public enum ItemFieldFlags : uint
{
    Soulbound = 0x01,     // Item Is Soulbound And Cannot Be Traded <<--
    Translated = 0x02,    // Item text will not read as garbage when player does not know the language
    Unlocked = 0x04,      // Item Had Lock But Can Be Opened Now
    Wrapped = 0x08,       // Item Is Wrapped And Contains Another Item
    Unk2 = 0x10,          // ?
    Unk3 = 0x20,          // ?
    Unk4 = 0x40,          // ?
    Unk5 = 0x80,          // ?
    BopTradeable = 0x100, // Allows Trading Soulbound Items
    Readable = 0x200,     // Opens Text Page When Right Clicked
    Unk6 = 0x400,         // ?
    Unk7 = 0x800,         // ?
    Refundable = 0x1000,  // Item Can Be Returned To Vendor For Its Original Cost (Extended Cost)
    Unk8 = 0x2000,        // ?
    Unk9 = 0x4000,        // ?
    Unk10 = 0x8000,       // ?
    Unk11 = 0x00010000,   // ?
    Unk12 = 0x00020000,   // ?
    Unk13 = 0x00040000,   // ?
    Child = 0x00080000,
    Unk15 = 0x00100000,                      // ?
    NewItem = 0x00200000,                    // Item glows in inventory
    AzeriteEmpoweredItemViewed = 0x00400000, // Won't play azerite powers animation when viewing it
    Unk18 = 0x00800000,                      // ?
    Unk19 = 0x01000000,                      // ?
    Unk20 = 0x02000000,                      // ?
    Unk21 = 0x04000000,                      // ?
    Unk22 = 0x08000000,                      // ?
    Unk23 = 0x10000000,                      // ?
    Unk24 = 0x20000000,                      // ?
    Unk25 = 0x40000000,                      // ?
    Unk26 = 0x80000000                       // ?
}