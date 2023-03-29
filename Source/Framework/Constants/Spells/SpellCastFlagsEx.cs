// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

[System.Flags]
public enum SpellCastFlagsEx
{
    None = 0x0,
    Unknown1 = 0x01,
    Unknown2 = 0x02,
    Unknown3 = 0x04,
    Unknown4 = 0x08,
    Unknown5 = 0x10,
    Unknown6 = 0x20,
    Unknown7 = 0x40,
    Unknown8 = 0x80,
    Unknown9 = 0x100,
    IgnoreCooldown = 0x200, // makes client not automatically start cooldown after SPELL_GO
    Unknown11 = 0x400,
    Unknown12 = 0x800,
    Unknown13 = 0x1000,
    Unknown14 = 0x2000,
    Unknown15 = 0x4000,
    UseToySpell = 0x8000, // Starts Cooldown On Toy
    Unknown17 = 0x10000,
    Unknown18 = 0x20000,
    Unknown19 = 0x40000,
    Unknown20 = 0x80000
}