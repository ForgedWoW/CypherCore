// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Framework.Constants;

[Flags]
public enum HitInfo
{
    NormalSwing = 0x0,
    Unk1 = 0x01, // req correct packet structure
    AffectsVictim = 0x02,
    OffHand = 0x04,
    Unk2 = 0x08,
    Miss = 0x10,
    FullAbsorb = 0x20,
    PartialAbsorb = 0x40,
    FullResist = 0x80,
    PartialResist = 0x100,
    CriticalHit = 0x200, // critical hit
    Unk10 = 0x400,
    Unk11 = 0x800,
    Unk12 = 0x1000,
    Block = 0x2000, // blocked damage
    Unk14 = 0x4000, // set only if meleespellid is present//  no world text when victim is hit for 0 dmg(HideWorldTextForNoDamage?)
    Unk15 = 0x8000, // player victim?// something related to blod sprut visual (BloodSpurtInBack?)
    Glancing = 0x10000,
    Crushing = 0x20000,
    NoAnimation = 0x40000,
    Unk19 = 0x80000,
    Unk20 = 0x100000,
    SwingNoHitSound = 0x200000, // unused?
    Unk22 = 0x00400000,
    RageGain = 0x800000,
    FakeDamage = 0x1000000 // enables damage animation even if no damage done, set only if no damage
}