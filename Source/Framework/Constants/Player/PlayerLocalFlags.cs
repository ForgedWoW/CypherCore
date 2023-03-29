// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum PlayerLocalFlags
{
    ControllingPet = 0x01, // Displays "You have an active summon already" when trying to tame new pet
    TrackStealthed = 0x02,
    ReleaseTimer = 0x08,    // Display time till auto release spirit
    NoReleaseWindow = 0x10, // Display no "release spirit" window at all
    NoPetBar = 0x20,        // CGPetInfo::IsPetBarUsed
    OverrideCameraMinHeight = 0x40,
    NewlyBosstedCharacter = 0x80,
    UsingPartGarrison = 0x100,
    CanUseObjectsMounted = 0x200,
    CanVisitPartyGarrison = 0x400,
    WarMode = 0x800,
    AccountSecured = 0x1000, // Script_IsAccountSecured
    OverrideTransportServerTime = 0x8000,
    MentorRestricted = 0x20000,
    WeeklyRewardAvailable = 0x40000
}