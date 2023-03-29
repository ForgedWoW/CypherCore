// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Framework.Constants;

[Flags]
public enum SpellCustomAttributes
{
    EnchantProc = 0x01,
    ConeBack = 0x02,
    ConeLine = 0x04,
    ShareDamage = 0x08,
    NoInitialThreat = 0x10,
    AuraCC = 0x20,
    DontBreakStealth = 0x40,
    CanCrit = 0x80,
    DirectDamage = 0x100,
    Charge = 0x200,
    PickPocket = 0x400,
    DeprecatedRollingPeriodic = 0x800, // DO NOT REUSE
    DeprecatedNegativeEff0 = 0x1000,   // DO NOT REUSE
    DeprecatedNegativeEff1 = 0x2000,   // DO NOT REUSE
    DeprecatedNegativeEff2 = 0x4000,   // DO NOT REUSE
    IgnoreArmor = 0x8000,
    ReqTargetFacingCaster = 0x10000,
    ReqCasterBehindTarget = 0x20000,
    AllowInflightTarget = 0x40000,
    NeedsAmmoData = 0x80000,
    BinarySpell = 0x100000,
    SchoolmaskNormalWithMagic = 0x200000,
    DeprecatedLiquidAura = 0x400000,
    IsTalent = 0x800000,
    AuraCannotBeSaved = 0x1000000
}