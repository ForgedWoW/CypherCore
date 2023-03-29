// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum ItemFlags3 : uint
{
    DontDestroyOnQuestAccept = 0x01,
    ItemCanBeUpgraded = 0x02,
    UpgradeFromItemOverridesDropUpgrade = 0x04,
    AlwaysFfaInLoot = 0x08,
    HideUpgradeLevelsIfNotUpgraded = 0x10,
    UpdateInteractions = 0x20,
    UpdateDoesntLeaveProgressiveWinHistory = 0x40,
    IgnoreItemHistoryTracker = 0x80,
    IgnoreItemLevelCapInPvp = 0x100,
    DisplayAsHeirloom = 0x200, // Item Appears As Having Heirloom Quality Ingame Regardless Of Its Real Quality (Does Not Affect Stat Calculation)
    SkipUseCheckOnPickup = 0x400,
    Obsolete = 0x800,
    DontDisplayInGuildNews = 0x1000, // Item Is Not Included In The Guild News Panel
    PvpTournamentGear = 0x2000,
    RequiresStackChangeLog = 0x4000,
    UnusedFlag = 0x8000,
    HideNameSuffix = 0x10000,
    PushLoot = 0x20000,
    DontReportLootLogToParty = 0x40000,
    AlwaysAllowDualWield = 0x80000,
    Obliteratable = 0x100000,
    ActsAsTransmogHiddenVisualOption = 0x200000,
    ExpireOnWeeklyReset = 0x400000,
    DoesntShowUpInTransmogUntilCollected = 0x800000,
    CanStoreEnchants = 0x1000000,
    HideQuestItemFromObjectTooltip = 0x2000000,
    DoNotToast = 0x4000000,
    IgnoreCreationContextForProgressiveWinHistory = 0x8000000,
    ForceAllSpecsForItemHistory = 0x10000000,
    SaveOnConsume = 0x20000000,
    ContainerSavesPlayerData = 0x40000000,
    NoVoidStorage = 0x80000000
}