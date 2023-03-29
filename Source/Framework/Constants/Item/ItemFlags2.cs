// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum ItemFlags2 : uint
{
    FactionHorde = 0x01,
    FactionAlliance = 0x02,
    DontIgnoreBuyPrice = 0x04, // When Item Uses Extended Cost, Gold Is Also Required
    ClassifyAsCaster = 0x08,
    ClassifyAsPhysical = 0x10,
    EveryoneCanRollNeed = 0x20,
    NoTradeBindOnAcquire = 0x40,
    CanTradeBindOnAcquire = 0x80,
    CanOnlyRollGreed = 0x100,
    CasterWeapon = 0x200,
    DeleteOnLogin = 0x400,
    InternalItem = 0x800,
    NoVendorValue = 0x1000,
    ShowBeforeDiscovered = 0x2000,
    OverrideGoldCost = 0x4000,
    IgnoreDefaultRatedBgRestrictions = 0x8000,
    NotUsableInRatedBg = 0x10000,
    BnetAccountTradeOk = 0x20000,
    ConfirmBeforeUse = 0x40000,
    ReevaluateBondingOnTransform = 0x80000,
    NoTransformOnChargeDepletion = 0x100000,
    NoAlterItemVisual = 0x200000,
    NoSourceForItemVisual = 0x400000,
    IgnoreQualityForItemVisualSource = 0x800000,
    NoDurability = 0x1000000,
    RoleTank = 0x2000000,
    RoleHealer = 0x4000000,
    RoleDamage = 0x8000000,
    CanDropInChallengeMode = 0x10000000,
    NeverStackInLootUi = 0x20000000,
    DisenchantToLootTable = 0x40000000,
    UsedInATradeskill = 0x80000000
}