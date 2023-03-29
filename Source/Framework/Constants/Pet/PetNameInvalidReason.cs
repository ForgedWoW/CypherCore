// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum PetNameInvalidReason
{
    // custom, not send
    Success = 0,

    Invalid = 1,
    NoName = 2,
    TooShort = 3,
    TooLong = 4,
    MixedLanguages = 6,
    Profane = 7,
    Reserved = 8,
    ThreeConsecutive = 11,
    InvalidSpace = 12,
    ConsecutiveSpaces = 13,
    RussianConsecutiveSilentCharacters = 14,
    RussianSilentCharacterAtBeginningOrEnd = 15,
    DeclensionDoesntMatchBaseName = 16
}