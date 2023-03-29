// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum QuestStatus
{
    None = 0,
    Complete = 1,

    //Unavailable    = 2,
    Incomplete = 3,

    //Available      = 4,
    Failed = 5,
    Rewarded = 6, // Not Used In Db
    Max
}