// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Framework.Constants;

[Flags]
public enum GroupUpdateFlags
{
    None = 0x00,          // nothing
    Unk704 = 0x01,        // Uint8[2] (Unk)
    Status = 0x02,        // public ushort (Groupmemberstatusflag)
    PowerType = 0x04,     // Uint8 (Powertype)
    Unk322 = 0x08,        // public ushort (Unk)
    CurHp = 0x10,         // Uint32 (Hp)
    MaxHp = 0x20,         // Uint32 (Max Hp)
    CurPower = 0x40,      // Int16 (Power Value)
    MaxPower = 0x80,      // Int16 (Max Power Value)
    Level = 0x100,        // public ushort (Level Value)
    Unk200000 = 0x200,    // Int16 (Unk)
    Zone = 0x400,         // public ushort (Zone Id)
    Unk2000000 = 0x800,   // Int16 (Unk)
    Unk4000000 = 0x1000,  // Int32 (Unk)
    Position = 0x2000,    // public ushort (X), public ushort (Y), public ushort (Z)
    VehicleSeat = 0x4000, // Int32 (Vehicle Seat Id)
    Auras = 0x8000,       // Uint8 (Unk), Uint64 (Mask), Uint32 (Count), For Each Bit Set: Uint32 (Spell Id) + public ushort (Auraflags)  (If Has Flags Scalable -> 3x Int32 (Bps))
    Pet = 0x10000,        // Complex (Pet)
    Phase = 0x20000,      // Int32 (Unk), Uint32 (Phase Count), For (Count) Uint16(Phaseid)

    Full = Unk704 |
           Status |
           PowerType |
           Unk322 |
           CurHp |
           MaxHp |
           CurPower |
           MaxPower |
           Level |
           Unk200000 |
           Zone |
           Unk2000000 |
           Unk4000000 |
           Position |
           VehicleSeat |
           Auras |
           Pet |
           Phase // All Known Flags
}