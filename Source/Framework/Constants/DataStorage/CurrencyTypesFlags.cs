// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Framework.Constants;

[Flags]
public enum CurrencyTypesFlags : uint
{
    Tradable = 0x00000001,              // NYI
    AppearsInLootWindow = 0x00000002,   // NYI
    ComputedWeeklyMaximum = 0x00000004, // NYI
    _100_Scaler = 0x00000008,
    NoLowLevelDrop = 0x00000010, // NYI
    IgnoreMaxQtyOnLoad = 0x00000020,
    LogOnWorldChange = 0x00000040, // NYI
    TrackQuantity = 0x00000080,
    ResetTrackedQuantity = 0x00000100, // NYI
    UpdateVersionIgnoreMax = 0x00000200,
    SuppressChatMessageOnVersionChange = 0x00000400,
    SingleDropInLoot = 0x00000800,        // NYI
    HasWeeklyCatchup = 0x00001000,        // NYI
    DoNotCompressChat = 0x00002000,       // NYI
    DoNotLogAcquisitionToBi = 0x00004000, // NYI
    NoRaidDrop = 0x00008000,              // NYI
    NotPersistent = 0x00010000,           // NYI
    Deprecated = 0x00020000,              // NYI
    DynamicMaximum = 0x00040000,
    SuppressChatMessages = 0x00080000,
    DoNotToast = 0x00100000,               // NYI
    DestroyExtraOnLoot = 0x00200000,       // NYI
    DontShowTotalInTooltip = 0x00400000,   // NYI
    DontCoalesceInLootWindow = 0x00800000, // NYI
    AccountWide = 0x01000000,              // NYI
    AllowOverflowMailer = 0x02000000,      // NYI
    HideAsReward = 0x04000000,             // NYI
    HasWarmodeBonus = 0x08000000,          // NYI
    IsAllianceOnly = 0x10000000,
    IsHordeOnly = 0x20000000,
    LimitWarmodeBonusOncePerTooltip = 0x40000000, // NYI
    DeprecatedCurrencyFlag = 0x80000000           // this flag itself is deprecated, not currency that has it
}