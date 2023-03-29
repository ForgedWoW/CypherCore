// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum SpellAttr8 : uint
{
    CantMiss = 0x01,                        // 0
    Unk1 = 0x02,                            // 1
    Unk2 = 0x04,                            // 2
    Unk3 = 0x08,                            // 3
    Unk4 = 0x10,                            // 4
    Unk5 = 0x20,                            // 5
    Unk6 = 0x40,                            // 6
    Unk7 = 0x80,                            // 7
    AffectPartyAndRaid = 0x100,             // 8
    DontResetPeriodicTimer = 0x200,         // 9 Periodic Auras With This Flag Keep Old Periodic Timer When Refreshing At Close To One Tick Remaining (Kind Of Anti Dot Clipping)
    NameChangedDuringTransofrm = 0x400,     // 10
    Unk11 = 0x800,                          // 11
    AuraSendAmount = 0x1000,                // 12 Aura Must Have Flag AflagAnyEffectAmountSent To Send Amount
    Unk13 = 0x2000,                         // 13
    Unk14 = 0x4000,                         // 14
    WaterMount = 0x8000,                    // 15
    Unk16 = 0x10000,                        // 16
    HasteAffectsDuration = 0x20000,         // 17 Haste Affects Duration
    RememberSpells = 0x40000,               // 18
    UseComboPointsOnAnyTarget = 0x80000,    // 19
    ArmorSpecialization = 0x100000,         // 20
    Unk21 = 0x200000,                       // 21
    Unk22 = 0x400000,                       // 22
    BattleResurrection = 0x800000,          // 23
    HealingSpell = 0x1000000,               // 24
    Unk25 = 0x2000000,                      // 25
    RaidMarker = 0x4000000,                 // 26 Probably Spell No Need Learn To Cast
    Unk27 = 0x8000000,                      // 27
    NotInBgOrArena = 0x10000000,            // 28
    MasteryAffectPoints = 0x20000000,       // 29
    Unk30 = 0x40000000,                     // 30
    AttackIgnoreImmuneToPCFlag = 0x80000000 // 31
}