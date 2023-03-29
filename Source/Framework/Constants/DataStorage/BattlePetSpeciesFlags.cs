// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum BattlePetSpeciesFlags : int
{
    NoRename = 0x01,
    WellKnown = 0x02,
    NotAccountWide = 0x04,
    Capturable = 0x08,
    NotTradable = 0x10,
    HideFromJournal = 0x20,
    LegacyAccountUnique = 0x40,
    CantBattle = 0x80,
    HordeOnly = 0x100,
    AllianceOnly = 0x200,
    Boss = 0x400,
    RandomDisplay = 0x800,
    NoLicenseRequired = 0x1000,
    AddsAllowedWithBoss = 0x2000,
    HideUntilLearned = 0x4000,
    MatchPlayerHighPetLevel = 0x8000,
    NoWildPetAddsAllowed = 0x10000,
}