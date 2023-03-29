// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum SelectTargetType
{
    DontCare = 0, //All target types allowed
    Self,         //Only Self casting
    SingleEnemy,  //Only Single Enemy
    AoeEnemy,     //Only AoE Enemy
    AnyEnemy,     //AoE or Single Enemy
    SingleFriend, //Only Single Friend
    AoeFriend,    //Only AoE Friend
    AnyFriend     //AoE or Single Friend
}