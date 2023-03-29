// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum NPCFlags2
{
    None = 0x00,
    ItemUpgradeMaster = 0x01,
    GarrisonArchitect = 0x02,
    Steering = 0x04,
    ShipmentCrafter = 0x10,
    GarrisonMissionNpc = 0x20,
    TradeskillNpc = 0x40,
    BlackMarketView = 0x80,
    GarrisonTalentNpc = 0x200,
    ContributionCollector = 0x400,
    AzeriteRespec = 0x4000,
    IslandsQueue = 0x8000,
    SuppressNpcSoundsExceptEndOfInteraction = 0x00010000,
}