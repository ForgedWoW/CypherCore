// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum SpellCategoryFlags : int
{
    CooldownScalesWithWeaponSpeed = 0x01, // unused
    CooldownStartsOnEvent = 0x04,
    CooldownExpiresAtDailyReset = 0x08
}