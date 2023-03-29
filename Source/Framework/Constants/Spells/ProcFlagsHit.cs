// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Framework.Constants;

[Flags]
public enum ProcFlagsHit
{
    None = 0x0,   // No Value - Proc_Hit_Normal | Proc_Hit_Critical For Taken Proc Type, Proc_Hit_Normal | Proc_Hit_Critical | Proc_Hit_Absorb For Done
    Normal = 0x1, // Non-Critical Hits
    Critical = 0x2,
    Miss = 0x4,
    FullResist = 0x8,
    Dodge = 0x10,
    Parry = 0x20,
    Block = 0x40, // Partial Or Full Block
    Evade = 0x80,
    Immune = 0x100,
    Deflect = 0x200,
    Absorb = 0x400, // Partial Or Full Absorb
    Reflect = 0x800,
    Interrupt = 0x1000,
    FullBlock = 0x2000,
    MaskAll = 0x0003FFF
}