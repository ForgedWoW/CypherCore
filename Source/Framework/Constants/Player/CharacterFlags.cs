// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Framework.Constants;

[Flags]
public enum CharacterFlags : uint
{
    None = 0x00000000,
    Unk1 = 0x00000001,
    Unk2 = 0x00000002,
    CharacterLockedForTransfer = 0x00000004,
    Unk4 = 0x00000008,
    Unk5 = 0x00000010,
    Unk6 = 0x00000020,
    Unk7 = 0x00000040,
    Unk8 = 0x00000080,
    Unk9 = 0x00000100,
    Unk10 = 0x00000200,
    HideHelm = 0x00000400,
    HideCloak = 0x00000800,
    Unk13 = 0x00001000,
    Ghost = 0x00002000,
    Rename = 0x00004000,
    Unk16 = 0x00008000,
    Unk17 = 0x00010000,
    Unk18 = 0x00020000,
    Unk19 = 0x00040000,
    Unk20 = 0x00080000,
    Unk21 = 0x00100000,
    Unk22 = 0x00200000,
    Unk23 = 0x00400000,
    Unk24 = 0x00800000,
    LockedByBilling = 0x01000000,
    Declined = 0x02000000,
    Unk27 = 0x04000000,
    Unk28 = 0x08000000,
    Unk29 = 0x10000000,
    Unk30 = 0x20000000,
    Unk31 = 0x40000000,
    Unk32 = 0x80000000
}