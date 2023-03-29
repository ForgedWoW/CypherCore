// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum SmartActionSummonCreatureFlags
{
    None = 0,
    PersonalSpawn = 1,
    PreferUnit = 2,

    All = PersonalSpawn | PreferUnit,
}