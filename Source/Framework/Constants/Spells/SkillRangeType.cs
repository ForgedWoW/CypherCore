// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum SkillRangeType
{
    Language, // 300..300
    Level,    // 1..max skill for level
    Mono,     // 1..1, grey monolite bar
    Rank,     // 1..skill for known rank
    None      // 0..0 always
}