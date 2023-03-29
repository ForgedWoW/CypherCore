// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum AbilityLearnType : byte
{
    OnSkillValue = 1,     // Spell state will update depending on skill value
    OnSkillLearn = 2,     // Spell will be learned/removed together with entire skill
    RewardedFromQuest = 4 // Learned as quest reward, also re-learned if missing
}