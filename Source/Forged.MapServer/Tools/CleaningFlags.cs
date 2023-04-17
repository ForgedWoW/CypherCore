// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Forged.MapServer.Tools;

[Flags]
public enum CleaningFlags
{
    AchievementProgress = 0x1,
    Skills = 0x2,
    Spells = 0x4,
    Talents = 0x8,
    Queststatus = 0x10
}