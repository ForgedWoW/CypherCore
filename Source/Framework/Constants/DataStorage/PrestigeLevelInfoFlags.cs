// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum PrestigeLevelInfoFlags : byte
{
    Disabled = 0x01 // Prestige levels with this flag won't be included to calculate max prestigelevel.
}