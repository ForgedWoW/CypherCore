// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum ItemFlags4
{
    HandleOnUseEffectImmediately = 0x01,
    AlwaysShowItemLevelInTooltip = 0x02,
    ShowsGenerationWithRandomStats = 0x04,
    ActivateOnEquipEffectsWhenTransmogrified = 0x08,
    EnforceTransmogWithChildItem = 0x10,
    Scrapable = 0x20,
    BypassRepRequirementsForTransmog = 0x40,
    DisplayOnlyOnDefinedRaces = 0x80,
    RegulatedCommodity = 0x100,
    CreateLootImmediately = 0x200,
    GenerateLootSpecItem = 0x400,
    HiddenInRewardsSummaries = 0x800,
    DisallowWhileLevelLinked = 0x1000,
    DisallowEnchant = 0x2000,
    SquishUsingItemLevelAsPlayerLevel = 0x4000,
    AlwaysShowPriceInTooltip = 0x8000,
    CosmeticItem = 0x10000,
    NoSpellEffectTooltipPrefixes = 0x20000
}