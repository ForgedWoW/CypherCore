// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum AtLoginFlags
{
    None = 0x00,
    Rename = 0x01,
    ResetSpells = 0x02,
    ResetTalents = 0x04,
    Customize = 0x08,
    ResetPetTalents = 0x10,
    FirstLogin = 0x20,
    ChangeFaction = 0x40,
    ChangeRace = 0x80,
    Resurrect = 0x100
}