// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Framework.Constants;

[Flags]
public enum SkillLineAbilityFlags
{
	CanFallbackToLearnedOnSkillLearn = 0x80, // The skill is rewarded from a quest if player started on exile's reach
}