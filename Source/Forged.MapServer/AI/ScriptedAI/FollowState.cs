// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Forged.MapServer.AI.ScriptedAI;

[Flags]
internal enum FollowState
{
    None = 0x00,
    Inprogress = 0x01, //must always have this state for any follow
    Paused = 0x02,     //disables following
    Complete = 0x04,   //follow is completed and may end
    PreEvent = 0x08,   //not implemented (allow pre event to run, before follow is initiated)
    PostEvent = 0x10   //can be set at complete and allow post event to run
}